namespace PgnArtistLib;

internal class ProcessParsedPgn
{
    internal static List<Game<MoveStorage>> GetFilteredGameList(MoveImageData moveImageData)
    {
        List<Game<MoveStorage>> filteredGames = new();

        //Filter the game list
        foreach (Game<MoveStorage> game in moveImageData.ParsedGames)
        {
            //Console.WriteLine($"::{game.TagSection["Opening"]}");


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


    internal static async Task<(List<RenderableGameMove> renderableMoves, string lastMoveKey)> ProcessGame(string initialFen,
                                                                                                           Game<MoveStorage> game)
    {
        return await Task.Run(() =>
        {
            (List<RenderableGameMove> renderableMoves, string lastMoveKey) renderableGame = new();
            string gameKey = initialFen, moveKey = initialFen, lastGameKey = initialFen;

            renderableGame.renderableMoves = new();
            renderableGame.renderableMoves.Add(new RenderableGameMove()
            {
                IsHidden = false,
                San = game.CurrentMoveNode.Value.SAN,
                LastBoardFen = lastGameKey,
                BoardFen = moveKey,
                BoardImage = null,
                Comment = game.CurrentMoveNode.Value.Comment
            });

            while (game.HasNextMove)
            {
                lastGameKey = gameKey;
                game.TraverseForward();

                gameKey = $"{game.CurrentFEN.Split(" ")[0]}";
                moveKey += gameKey;


                renderableGame.renderableMoves.Add(new RenderableGameMove()
                {
                    IsHidden = false,
                    San = game.CurrentMoveNode.Value.SAN,
                    LastBoardFen = lastGameKey,
                    BoardFen = $"{gameKey}",
                    BoardImage = null,
                    Comment = game.CurrentMoveNode.Value.Comment
                });
            }

            renderableGame.lastMoveKey = moveKey;
            game.GoToInitialState();

            return renderableGame;
        });
    }


    [SupportedOSPlatform("windows")]
    internal static async Task<RenderableGameCollection> BuildMoveImageData(MoveImageData moveImageData, string initialFen)
    {
        RenderableGameMove emptyMove = new() { IsHidden = true, BoardFen = "", BoardImage = null, Comment = "", LastBoardFen = "", San = "" };

        IBoardRenderer boardRenderer = new ShadowBoardRenderer(logger: null);
        SortedList<string, List<RenderableGameMove>> renderableGameList = new();

        int maxMoves = 0;

        foreach (Game<MoveStorage>? game in (moveImageData.Filter.TakeGamesFromEnd) ?
                 GetFilteredGameList(moveImageData).TakeLast(moveImageData.Filter.MaxGames) :
                 GetFilteredGameList(moveImageData).Take(moveImageData.Filter.MaxGames))
        {
            (List<RenderableGameMove> renderableMoves, string lastMoveKey) = await ProcessGame(initialFen, game);

            if (!renderableGameList.ContainsKey(lastMoveKey))
            {
                renderableGameList.Add(lastMoveKey, renderableMoves);
                maxMoves = Math.Max(maxMoves, renderableMoves.Count);
            }
        }

        //Copy all moves into a grid
        RenderableGameMove[,] displayGrid = new RenderableGameMove[renderableGameList.Count, maxMoves];
        KeyValuePair<string, List<RenderableGameMove>>[]? fixedGameList = renderableGameList.ToArray();

        Parallel.For(0, fixedGameList.Length, loopX =>
        {
            for (int loopY = 0; loopY < maxMoves; loopY++)
            {
                displayGrid[loopX, loopY] = loopY < fixedGameList[loopX].Value.Count ?
                                                    fixedGameList[loopX].Value[loopY] :
                                                    emptyMove;
            }
        });

        // Hide duplicates along the X Axis
        Parallel.For(0, displayGrid.GetLength(1), loopY =>
        {
            string skipFen = "";
            for (int loopX = 0; loopX < displayGrid.GetLength(0); loopX++)
            {
                if (string.Equals(skipFen, displayGrid[loopX, loopY].BoardFen))
                {
                    displayGrid[loopX, loopY].IsHidden = true;
                }
                else
                {
                    skipFen = displayGrid[loopX, loopY].BoardFen;
                }
            }
        });


        // Get the board graphics
        for (int loopX = 0; loopX < fixedGameList.Length; loopX++)
        {
            for (int loopY = 0; loopY < maxMoves; loopY++)
            {
                if (displayGrid[loopX, loopY].IsHidden)
                {
                    continue;
                }

                byte[] boardImgBytes = await boardRenderer.GetPngImageDiffFromFenAsync(displayGrid[loopX, loopY].BoardFen,
                                                                                       displayGrid[loopX, loopY].LastBoardFen,
                                                                                       DiagramRenderer.SQUARE_SIZE,
                                                                                       moveImageData.IsFromWhitesPerspective);

                using MemoryStream memStreamBoard = new(boardImgBytes);
                displayGrid[loopX, loopY].BoardImage = (Bitmap)Bitmap.FromStream(memStreamBoard);
            }
        }

        return new RenderableGameCollection() { DisplayGrid = displayGrid, MoveLines = new(), MaxWidth = 0 };
    }
}

