namespace PgnArtistLib;

internal class ProcessParsedPgn
{
    private const string BOARD_FEN = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
    internal static List<Game<MoveStorage>> GetFilteredGameList(MoveImageData moveImageData)
    {
        List<Game<MoveStorage>> filteredGames = new();

        //Filter the game list
        foreach (Game<MoveStorage> game in moveImageData.ParsedGames)
        {
            bool isIncluded = true;

            if (!string.IsNullOrEmpty(moveImageData.Filter.FilterWhite) &&
                !string.Equals(game.TagSection.Get("White"), moveImageData.Filter.FilterWhite, StringComparison.InvariantCultureIgnoreCase))
            {
                isIncluded = false;
            }

            if (!string.IsNullOrEmpty(moveImageData.Filter.FilterBlack) &&
                !string.Equals(game.TagSection.Get("Black"), moveImageData.Filter.FilterBlack, StringComparison.InvariantCultureIgnoreCase))
            {
                isIncluded = false;
            }

            //if (!string.IsNullOrEmpty(filter.FilterEither) &&
            //
            //
            //    string.Equals(game.TagSection.Get("White"), filter.FilterWhite, StringComparison.InvariantCultureIgnoreCase) ||
            //         string.Equals(game.TagSection.Get("Black"), filter.FilterBlack, StringComparison.InvariantCultureIgnoreCase) )
            //{
            //    isIncluded = false;
            //}

            if (!string.IsNullOrEmpty(moveImageData.Filter.FilterECO) &&
                !string.Equals(game.TagSection.Get("ECO"), moveImageData.Filter.FilterECO, StringComparison.InvariantCultureIgnoreCase))
            {
                isIncluded = false;
            }


            if (isIncluded)
            {
                filteredGames.Add(game);
            }


            //game.TagSection.Get("ECO")


        }
        return filteredGames;
    }

    [SupportedOSPlatform("windows")]
    internal static async Task ProcessGame(bool isFromWhitesPerspective,
                                          IBoardRenderer boardRenderer,
                                          int maxWidth,
                                          Game<MoveStorage> game,
                                          GameFilter gameFilter,
                                          SortedList<string, string> lastMoveNameList,
                                          List<SortedList<string, MoveLine>> moveLines)
    {
        string moveKey = BOARD_FEN;
        int moveCount = 0;
        int plyCount = 0;
        string lastGameKey = BOARD_FEN;

        while (game.HasNextMove && (plyCount++) < gameFilter.MaxPly)
        {
            game.TraverseForward();

            string[] fenSplit = game.CurrentFEN.Split(" ");
            string gameKey = $"{fenSplit[0]}";
            moveKey += gameKey;

            if (moveLines.Count <= ++moveCount) { moveLines.Add(new SortedList<string, MoveLine>()); }

            if (!moveLines[moveCount].ContainsKey($"{moveKey}"))
            {
                byte[] boardImgBytes = await boardRenderer.GetPngImageDiffFromFenAsync(gameKey, lastGameKey, DiagramRenderer.SQUARE_SIZE, isFromWhitesPerspective);
                lastGameKey = gameKey;
                using MemoryStream memStreamBoard = new(boardImgBytes);
                Bitmap resizedBmp = (Bitmap)Bitmap.FromStream(memStreamBoard);
                moveLines[moveCount].Add($"{moveKey}", new MoveLine() { San = game.CurrentMoveNode.Value.SAN, BoardFen = $"{gameKey}", BoardImage = resizedBmp, Comment = game.CurrentMoveNode.Value.Comment });
            }

            if (moveCount + 1 < moveLines.Count)
            {
                int addedCount = moveLines[moveCount + 1].Where(x => x.Key.StartsWith(moveKey, StringComparison.OrdinalIgnoreCase)).Count();
                if (addedCount >= 1)
                {
                    try
                    {
                        moveLines[moveCount].Add($"{moveKey}{addedCount}", new MoveLine { San = "", BoardImage = null, BoardFen = "", Comment = "" });
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"ERROR {addedCount}");
                    }
                }
            }

            maxWidth = Math.Max(maxWidth, moveLines[moveCount].Count);

            if (!game.HasNextMove && game.TagSection.ContainsKey("Opening"))
            {
                lastMoveNameList.Add(moveKey, game.TagSection["Opening"]);
            }
            else if (!game.HasNextMove)
            {
                try
                {
                    if (!lastMoveNameList.ContainsKey(moveKey))
                    {
                        lastMoveNameList.Add(moveKey, $"[{game.TagSection["White"]} vs {game.TagSection["Black"]} >> { game.TagSection["Termination"] }]");
                    }
                }
                catch (Exception) { Console.WriteLine("I know - fix me"); }
            }
        }

        game.GoToInitialState();
    }



    [SupportedOSPlatform("windows")]
    internal static async Task<RenderableGame> BuildMoveImageData(MoveImageData moveImageData)
    {
        //Render initial position
        IBoardRenderer boardRenderer = new ShadowBoardRenderer(logger: null);
        byte[] boardImgBytes = await boardRenderer.GetPngImageFromFenAsync(BOARD_FEN, DiagramRenderer.SQUARE_SIZE, moveImageData.IsFromWhitesPerspective).ConfigureAwait(false);

        using MemoryStream memStream = new(boardImgBytes);
        Bitmap startBoardresizedBmp = (Bitmap)Bitmap.FromStream(memStream);

        SortedList<string, string> lastMoveNameList = new();

        int maxWidth = 0;

        List<SortedList<string, MoveLine>> moveLines = new()
        {
            new SortedList<string, MoveLine>()
        };
        moveLines[0].Add(BOARD_FEN, new MoveLine() { BoardFen = BOARD_FEN, BoardImage = startBoardresizedBmp, San = "", Comment = "" });

        List<Game<MoveStorage>> filteredGames = GetFilteredGameList(moveImageData);


        //Parallel.ForEach(
        //    (moveImageData.Filter.TakeGamesFromEnd) ? filteredGames.TakeLast(moveImageData.Filter.MaxGames) : filteredGames.Take(moveImageData.Filter.MaxGames),
        //        async game =>
        //        {
        //            await ProcessGame(moveImageData.IsFromWhitesPerspective, boardRenderer, maxWidth, game, moveImageData.Filter, lastMoveNameList, moveLines);
        //        });


        foreach (var game in (moveImageData.Filter.TakeGamesFromEnd) ? 
                              filteredGames.TakeLast(moveImageData.Filter.MaxGames) : 
                              filteredGames.Take(moveImageData.Filter.MaxGames)) 
        {
            await ProcessGame(moveImageData.IsFromWhitesPerspective, boardRenderer, maxWidth, game, moveImageData.Filter, lastMoveNameList, moveLines);
        }
 

        for (int loopY = 1; loopY < (moveLines.Count - 1); loopY++)
        {
            for (int loopX = 0; loopX < moveLines[loopY].Count; loopX++)
            {
                if (moveLines[loopY].Values[loopX].BoardImage != null)
                {
                    int addedCount = moveLines[loopY + 1].Where(x => x.Key.StartsWith(moveLines[loopY].Keys[loopX], StringComparison.OrdinalIgnoreCase)).Count();

                    if (addedCount == 0)
                    {
                        for (int loopInnerY = loopY + 1; loopInnerY < moveLines.Count; loopInnerY++)
                        {
                            moveLines[loopInnerY].Add($"{moveLines[loopY].Keys[loopX]}{addedCount}", new MoveLine { San = "", BoardImage = null, BoardFen = "", Comment = "" });
                        }
                    }
                }
            }

            maxWidth = Math.Max(maxWidth, moveLines[loopY].Count);
        }

        return new RenderableGame() { LastMoveNameList = lastMoveNameList, MoveLines = moveLines, MaxWidth = maxWidth };
    }

}
