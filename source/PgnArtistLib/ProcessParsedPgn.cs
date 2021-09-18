namespace PgnArtistLib;

internal class ProcessParsedPgn
{
    private const string BOARD_FEN = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";

    internal static List<Game<MoveStorage>> GetFilteredGameList(MoveImageData moveImageData)
    {
        List<Game<MoveStorage>> filteredGames = new();

        //Filter the game list
        foreach (Game<MoveStorage> game in moveImageData.ParsedGames )
        {
            Console.WriteLine($"::{game.TagSection["Opening"]}");


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

            if (!string.IsNullOrEmpty(moveImageData.Filter.FilterECO) &&
                !string.Equals(game.TagSection.Get("ECO"), moveImageData.Filter.FilterECO, StringComparison.InvariantCultureIgnoreCase))
            {
                isIncluded = false;
            }


            if (isIncluded)
            {
                filteredGames.Add(game);
            }
        }

        return filteredGames;
    }


    internal static async Task ProcessGame(bool isFromWhitesPerspective,
                                          int maxWidth,
                                          Game<MoveStorage> game,
                                          GameFilter gameFilter, 
                                          Dictionary<string, string> lastMoveDict,
                                          List<SortedList<string, RenderableGameMove>> moveLines)
    {
        string moveKey = BOARD_FEN;
        string lastGameKey = BOARD_FEN;
        int moveCount = 0;
        int plyCount = 0;

        while (game.HasNextMove && (plyCount++) < gameFilter.MaxPly)
        {
            lastGameKey = $"{game.CurrentFEN.Split(" ")[0]}";
            game.TraverseForward();

            string gameKey = $"{game.CurrentFEN.Split(" ")[0]}";
            moveKey += gameKey;

            if (moveLines.Count <= ++moveCount) { moveLines.Add(new SortedList<string, RenderableGameMove>()); }

            if (!moveLines[moveCount].ContainsKey($"{moveKey}"))
            {
                moveLines[moveCount].Add($"{moveKey}", new RenderableGameMove() { San = game.CurrentMoveNode.Value.SAN, LastBoardFen = lastGameKey, BoardFen = $"{gameKey}", BoardImage = null, Comment = game.CurrentMoveNode.Value.Comment });
            }

            if (moveCount + 1 < moveLines.Count)
            {
                int addedCount = moveLines[moveCount + 1].Where(x => x.Key.StartsWith(moveKey, StringComparison.OrdinalIgnoreCase)).Count();
                if (addedCount >= 1)
                {
                    try
                    {
                        moveLines[moveCount].Add($"{moveKey}{addedCount}", new RenderableGameMove { San = "", BoardImage = null, BoardFen = "", Comment = "" });
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"ERROR {addedCount}");
                    }
                }
            }


            if (!game.HasNextMove)
            {
                if (game.TagSection.ContainsKey("Opening"))
                {
                    Console.WriteLine($"FOUND: {game.TagSection["Opening"]}");

                    if (lastMoveDict.ContainsKey(moveKey))
                    {
                        lastMoveDict[moveKey] += $"** OR ** {game.TagSection["Opening"]}";
                    }
                    else
                    {
                        lastMoveDict.Add(moveKey, game.TagSection["Opening"]);
                    }
                }
                else
                {
                    Console.WriteLine($"KEY MISSING");
                }
            }
        }

        game.GoToInitialState();
    }


    [SupportedOSPlatform("windows")]
    internal static async Task<RenderableGame> BuildMoveImageData(MoveImageData moveImageData)
    {
        IBoardRenderer boardRenderer = new ShadowBoardRenderer(logger: null);

        //Render initial position
        //IBoardRenderer boardRenderer = new ShadowBoardRenderer(logger: null);
        //using MemoryStream memStream = new(await boardRenderer.GetPngImageFromFenAsync(BOARD_FEN, DiagramRenderer.SQUARE_SIZE, moveImageData.IsFromWhitesPerspective).ConfigureAwait(false));
        //Bitmap startBoardresizedBmp = (Bitmap)Bitmap.FromStream(memStream);



        int maxWidth = 0;
        SortedList<string, string> lastMoveNameList = new();

        GameLine moveLines = new();

        moveLines.MoveLines.Add(new());
        moveLines.MoveLines[0].Add(BOARD_FEN, new RenderableGameMove() { BoardFen = BOARD_FEN, BoardImage = null, San = "", Comment = "" });

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
            await ProcessGame(moveImageData.IsFromWhitesPerspective,
                              maxWidth,
                              game,
                              moveImageData.Filter,
                              moveLines.TextForKey,
                              moveLines.MoveLines);
        }


        moveLines.MoveLines.ForEach(async games =>
        {
            foreach (var game in games)
            {
                if (string.IsNullOrEmpty(game.Value.BoardFen)) continue;

                byte[] boardImgBytes = await boardRenderer.GetPngImageDiffFromFenAsync(game.Value.BoardFen,
                                                                                       game.Value.LastBoardFen,
                                                                                       DiagramRenderer.SQUARE_SIZE,
                                                                                       moveImageData.IsFromWhitesPerspective);
                using MemoryStream memStreamBoard = new(boardImgBytes);
                game.Value.BoardImage = (Bitmap)Bitmap.FromStream(memStreamBoard);
            }
        });


        for (int loopY = 1; loopY < (moveLines.MoveLines.Count - 1); loopY++)
        {
            for (int loopX = 0; loopX < moveLines.MoveLines[loopY].Count; loopX++)
            {
                if (moveLines.MoveLines[loopY].Values[loopX].BoardImage != null)
                {
                    int addedCount = moveLines.MoveLines[loopY + 1].Where(x => x.Key.StartsWith(moveLines.MoveLines[loopY].Keys[loopX], StringComparison.OrdinalIgnoreCase)).Count();

                    if (addedCount == 0)
                    {
                        for (int loopInnerY = loopY + 1; loopInnerY < moveLines.MoveLines.Count; loopInnerY++)
                        {
                            moveLines.MoveLines[loopInnerY].Add($"{moveLines.MoveLines[loopY].Keys[loopX]}{addedCount}", new RenderableGameMove { San = "", BoardImage = null, BoardFen = "", Comment = "" });
                        }
                    }
                }
            }

            maxWidth = Math.Max(maxWidth, moveLines.MoveLines[loopY].Count);
        }

        return new RenderableGame() { MoveLines = moveLines, MaxWidth = maxWidth };
    }
}
