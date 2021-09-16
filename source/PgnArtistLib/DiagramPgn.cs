using ChessLib.Data;
using ChessLib.Parse.PGN;
using DynamicBoard;
using Microsoft.Extensions.FileProviders;
using PgnArtistLib.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace PgnArtistLib;

[SupportedOSPlatform("windows")]
public class DiagramPgn
{
    private const string BOARD_FEN = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
    private const int BoardSize = 160;

    public int BoxWidth { get; set; } = 62;
    public int BoxHeight { get; set; } = 14;
    public float HeadFontSize { get; set; } = 9f;
    public int SpacerSizeX { get; set; } = 20;
    public int SpacerSizeY { get; set; } = 30;
    public int StripeTxtOffset { get; set; } = 19;
    public float ConnectSize { get; set; } = 5.0f;
    public int BlockSizeX => BoardSize + SpacerSizeX;
    public int BlockSizeY => BoardSize + SpacerSizeY;
    public string SubmittedPgn => _submittedPgn ?? "";

    private string? _submittedPgn;
    private IEnumerable<Game<ChessLib.Data.MoveRepresentation.MoveStorage>>? _parsedGames;

    private (SortedList<string, string> LastMoveNameList, List<SortedList<string, (string, string, Image, string)>> MoveLines, int MaxWidth) _moveData;
    //public DiagramPgn() { }

    public async Task<bool> AssignPgn(string pgnText)
    {
        _parsedGames = await ParseAndValidatePgn(pgnText).ConfigureAwait(false);
        _submittedPgn = pgnText;
        return true;
    }

    public async Task<bool> AssignPgn(FileInfo pgnFileInfo)
    {
        string preParsedPgn = File.ReadAllText(pgnFileInfo.FullName);
        _parsedGames = await ParseAndValidatePgn(preParsedPgn).ConfigureAwait(false);
        _submittedPgn = preParsedPgn;
        return true;
    }

    public async Task BuildMoveData(bool isFromWhitesPerspective, string diagramTitle, float titleSize, GameFilter filter)
    {
        if (_parsedGames is null)
        {
            throw new NullReferenceException("Call 'LoadPgn' BEFORE trying to generate any diagrams.");
        }

        _moveData = await BuildMoveImageData(_parsedGames, filter, isFromWhitesPerspective);
    }

    public async Task<Bitmap> GenerateDiagram(bool isFromWhitesPerspective, string diagramTitle, float titleSize, GameFilter filter)
    {
        if (_parsedGames is null)// || _moveData is null)
        {
            throw new NullReferenceException("Call 'LoadPgn' and then 'BuildMoveData' BEFORE trying to generate any diagrams.");
        }

        return await Task.Run<Bitmap>(() =>
        {
            return (Bitmap) RenderMoveImageData(_moveData.LastMoveNameList, _moveData.MoveLines, diagramTitle, titleSize, _moveData.MaxWidth);
        });

    }

    private static async Task<IEnumerable<Game<ChessLib.Data.MoveRepresentation.MoveStorage>>> ParseAndValidatePgn(string preParsedPgn)
    {
        PGNParser pgnParser = new();
        System.Collections.Generic.IEnumerable<Game<ChessLib.Data.MoveRepresentation.MoveStorage>> parsedPgn = await pgnParser.GetGamesFromPGNAsync(preParsedPgn.ToString(CultureInfo.InvariantCulture));

        return parsedPgn;
    }

    private static async Task<(SortedList<string, string> LastMoveNameList, List<SortedList<string, (string, string, Image, string)>> MoveLines, int MaxWidth)> BuildMoveImageData(IEnumerable<Game<ChessLib.Data.MoveRepresentation.MoveStorage>> parsedGames, GameFilter filter, bool isFromWhitesPerspective)
    {
        //Render initial position
        IBoardRenderer boardRenderer = new ShadowBoardRenderer(logger: null);
        byte[] boardImgBytes = await boardRenderer.GetPngImageFromFenAsync(BOARD_FEN, BoardSize, isFromWhitesPerspective).ConfigureAwait(false);

        using MemoryStream memStream = new(boardImgBytes);
        Bitmap startBoardresizedBmp = (Bitmap)Bitmap.FromStream(memStream);


        SortedList<string, string> lastMoveNameList = new();

        int moveCount = 0;
        int maxWidth = 0;

        List<SortedList<string, (string, string, Image, string)>> moveLines = new()
        {
            new SortedList<string, (string, string, Image, string)>()
        };
        moveLines[0].Add(BOARD_FEN, ("", BOARD_FEN, startBoardresizedBmp, ""));



        List<Game<ChessLib.Data.MoveRepresentation.MoveStorage>> filteredGames = new();

        //Filter the game list
        foreach (Game<ChessLib.Data.MoveRepresentation.MoveStorage> game in parsedGames)
        {
            bool isIncluded = true;

            if (!string.IsNullOrEmpty(filter.FilterWhite) &&
                !string.Equals(game.TagSection.Get("White"), filter.FilterWhite, StringComparison.InvariantCultureIgnoreCase))
            {
                isIncluded = false;
            }

            if (!string.IsNullOrEmpty(filter.FilterBlack) &&
                !string.Equals(game.TagSection.Get("Black"), filter.FilterBlack, StringComparison.InvariantCultureIgnoreCase))
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

            if (!string.IsNullOrEmpty(filter.FilterECO) &&
                !string.Equals(game.TagSection.Get("ECO"), filter.FilterECO, StringComparison.InvariantCultureIgnoreCase))
            {
                isIncluded = false;
            }


            if (isIncluded)
            {
                filteredGames.Add(game);
            }


            //game.TagSection.Get("ECO")


        }



        foreach (Game<ChessLib.Data.MoveRepresentation.MoveStorage> game in ((filter.TakeGamesFromEnd) ? filteredGames.TakeLast(filter.MaxGames) : filteredGames.Take(filter.MaxGames)))
        {
            string moveKey = BOARD_FEN;
            moveCount = 0;
            int plyCount = 0;
            string lastGameKey = BOARD_FEN;

            while (game.HasNextMove && (plyCount++) < filter.MaxPly)
            {
                game.TraverseForward();

                string[] fenSplit = game.CurrentFEN.Split(" ");
                string gameKey = $"{fenSplit[0]}";
                moveKey += gameKey;

                if (moveLines.Count <= ++moveCount) { moveLines.Add(new SortedList<string, (string, string, Image, string)>()); }

                if (!moveLines[moveCount].ContainsKey($"{moveKey}"))
                {
                    boardImgBytes = await boardRenderer.GetPngImageDiffFromFenAsync(gameKey, lastGameKey, BoardSize, isFromWhitesPerspective).ConfigureAwait(false);
                    lastGameKey = gameKey;
                    using MemoryStream memStreamBoard = new(boardImgBytes);
                    Bitmap resizedBmp = (Bitmap)Bitmap.FromStream(memStreamBoard);
                    moveLines[moveCount].Add($"{moveKey}", (game.CurrentMoveNode.Value.SAN, $"{gameKey}", resizedBmp, game.CurrentMoveNode.Value.Comment));
                }

                if (moveCount + 1 < moveLines.Count)
                {
                    int addedCount = moveLines[moveCount + 1].Where(x => x.Key.StartsWith(moveKey, StringComparison.OrdinalIgnoreCase)).Count();
                    if (addedCount >= 1)
                    {
                        try
                        {
                            moveLines[moveCount].Add($"{moveKey}{addedCount}", ("", "", null, ""));
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
                else
                {
                    if (!lastMoveNameList.ContainsKey(moveKey))
                    {
                        lastMoveNameList.Add(moveKey, $"[{game.TagSection["White"]} vs {game.TagSection["Black"]} >> { game.TagSection["Termination"] }]");
                    }
                }
            }

            game.GoToInitialState();
        }

        for (int loopY = 1; loopY < (moveLines.Count - 1); loopY++)
        {
            for (int loopX = 0; loopX < moveLines[loopY].Count; loopX++)
            {
                if (moveLines[loopY].Values[loopX].Item3 != null)
                {
                    int addedCount = moveLines[loopY + 1].Where(x => x.Key.StartsWith(moveLines[loopY].Keys[loopX], StringComparison.OrdinalIgnoreCase)).Count();

                    if (addedCount == 0)
                    {
                        for (int loopInnerY = loopY + 1; loopInnerY < moveLines.Count; loopInnerY++)
                        {
                            moveLines[loopInnerY].Add($"{moveLines[loopY].Keys[loopX]}{addedCount}", ("", "", null, ""));
                        }
                    }
                }
            }

            maxWidth = Math.Max(maxWidth, moveLines[loopY].Count);
        }

        return (lastMoveNameList, moveLines, maxWidth);
    }

    private Bitmap RenderMoveImageData(SortedList<string, string> lastMoveNameList, List<SortedList<string, (string, string, Image, string)>> moveLines, string diagramTitle, float titleSize, int maxWidth)
    {
        // Create font/brush/pen.
        using Font headFont = new(FontFamily.GenericSansSerif, HeadFontSize);
        using Font stripeFont = new(FontFamily.GenericMonospace, ((float)SpacerSizeX) / 2f);
        using Brush drawBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
        using Brush moveBkgBrush = new SolidBrush(Color.FromArgb(235, 200, 0, 0));
        using Brush transBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        using Pen connectorPen = new(Brushes.Orange) { Width = ConnectSize };
        using Pen connectorShadowPen = new(new SolidBrush(Color.FromArgb(100, 0, 0, 0))) { Width = ConnectSize };

        var maxMoveLine = moveLines.Max(item => item.Count);

        Bitmap image = new((maxMoveLine * BlockSizeX) + SpacerSizeX + (SpacerSizeX / 2),
                            (moveLines.Count * BlockSizeY) + (SpacerSizeY / 4),
                            PixelFormat.Format32bppArgb);

        using Graphics graphics = Graphics.FromImage(image);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

        graphics.Clear(Color.Black);

        EmbeddedFileProvider embeddedProvider = new(typeof(DiagramPgn).Assembly);

        using (Stream reader = embeddedProvider.GetFileInfo("DefaultBkg01.png").CreateReadStream())
        {
            Image bkgImage = Image.FromStream(reader);

            // Set the wrap mode.
            using ImageAttributes imageAttr = new();
            imageAttr.SetWrapMode(WrapMode.Tile);

            // Create a TextureBrush.
            Rectangle brushRect = new(0, 0, bkgImage.Width, bkgImage.Height);
            using TextureBrush myTBrush = new(bkgImage, brushRect, imageAttr);

            // Draw to the screen a rectangle filled with the texture
            graphics.FillRectangle(myTBrush, 0, 0, image.Width, image.Height);

            graphics.DrawString(diagramTitle,
                new Font(FontFamily.GenericSansSerif, titleSize, FontStyle.Regular),
                Brushes.Black,
                205,
                30);


            graphics.DrawString(diagramTitle,
                new Font(FontFamily.GenericSansSerif, titleSize, FontStyle.Regular),
                Brushes.White,
                200,
                25);

        }

        // Draw the horezontal stripes
        using Brush hStripeBrush = new SolidBrush(Color.FromArgb(5, 255, 255, 255));
        for (int loopY = 1; loopY < moveLines.Count; loopY += 2)
        {
            Console.WriteLine($">>>> {"Horizontal Stripe (Y)",30} {loopY,4}");
            graphics.FillRectangle(hStripeBrush, new Rectangle(0, loopY * BlockSizeY, image.Width, BlockSizeY));
        }



        while (true)
        {
            //Draw Pinstripes
            int loopYYYY = moveLines.Count - 1;

            Console.WriteLine($">>>> {"Pin Stripe (Y)",30} {loopYYYY,4}");

            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            KeyValuePair<string, (string, string, Image, string)>[] moveLine = moveLines[loopYYYY].OrderBy(x => x.Key).ToArray();

            for (int loopX = 0; loopX < moveLine.Length; loopX++)
            {
                //Draw Srtipes   
                if (lastMoveNameList.ContainsKey(moveLines[loopYYYY].Keys[loopX]))
                {
                    Console.WriteLine($">>>> {loopX,4} {loopYYYY,4} {lastMoveNameList[moveLines[loopYYYY].Keys[loopX]]}");

                    using StringFormat drawFormat = new() { FormatFlags = StringFormatFlags.DirectionVertical };
                    SizeF stringSize = graphics.MeasureString(lastMoveNameList[moveLines[loopYYYY].Keys[loopX]], stripeFont);

                    for (float txtLoop = (SpacerSizeY / 2) + BlockSizeY; txtLoop < image.Height; txtLoop += (stringSize.Width + 20))
                    {
                        graphics.DrawString(lastMoveNameList[moveLines[loopYYYY].Keys[loopX]],
                                            stripeFont,
                                            Brushes.Black,
                                            ((SpacerSizeX) + ((loopX * BlockSizeX) + (SpacerSizeX / 2)) - StripeTxtOffset) + 1,
                                            txtLoop + 1,
                                            drawFormat);

                        graphics.DrawString(lastMoveNameList[moveLines[loopYYYY].Keys[loopX]],
                                            stripeFont,
                                            drawBrush,
                                            (SpacerSizeX) + ((loopX * BlockSizeX) + (SpacerSizeX / 2)) - StripeTxtOffset,
                                            txtLoop,
                                            drawFormat);
                    }
                }
            }

            break;
        }

        // Draw the connectors
        for (int loopY = 0; loopY < moveLines.Count; loopY++)
        {
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            KeyValuePair<string, (string, string, Image, string)>[] moveLine = moveLines[loopY].OrderBy(x => x.Key).ToArray();

            for (int loopX = 0; loopX < moveLine.Length; loopX++)
            {
                //Draw Connector
                if (loopY + 1 < moveLines.Count)
                {
                    for (int loopNextRowX = 0; loopNextRowX < moveLines[loopY + 1].Count; loopNextRowX++)
                    {
                        if (moveLines[loopY + 1].Keys[loopNextRowX].Contains(moveLines[loopY].Keys[loopX], StringComparison.OrdinalIgnoreCase))
                        {
                            if (moveLines[loopY + 1].Values[loopNextRowX].Item3 != null)
                            {
                                graphics.DrawLine(connectorShadowPen,
                                                  3 + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                    (loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize),
                                                  3 + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                    (loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2));

                                graphics.DrawLine(connectorShadowPen,
                                                  3 + (SpacerSizeX) + (loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  3 + (loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2),
                                                  3 + (SpacerSizeX) + (loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  3 + ((loopY + 1) * BlockSizeY) + (SpacerSizeY / 2));

                                graphics.DrawLine(connectorShadowPen,
                                                  3 + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  3 + (loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2),
                                                  3 + (SpacerSizeX) + (loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  3 + ((loopY + 1) * BlockSizeY));
                            }
                        }
                    }
                }

                if (loopY + 1 < moveLines.Count)
                {
                    for (int loopNextRowX = 0; loopNextRowX < moveLines[loopY + 1].Count; loopNextRowX++)
                    {
                        if (moveLines[loopY + 1].Keys[loopNextRowX].Contains(moveLines[loopY].Keys[loopX], StringComparison.OrdinalIgnoreCase))
                        {
                            if (moveLines[loopY + 1].Values[loopNextRowX].Item3 != null)
                            {
                                graphics.DrawLine(connectorPen,
                                                  (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  (loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize),
                                                  (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  (loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2));

                                graphics.DrawLine(connectorPen,
                                                  (SpacerSizeX) + (loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  (loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2),
                                                  (SpacerSizeX) + (loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  ((loopY + 1) * BlockSizeY) + (SpacerSizeY / 2));

                                graphics.DrawLine(connectorPen,
                                                  (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  (loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2),
                                                  (SpacerSizeX) + (loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  ((loopY + 1) * BlockSizeY));
                            }
                        }
                    }
                }
            }
        }


        for (int loopY = 0; loopY < moveLines.Count; loopY++)
        {
            KeyValuePair<string, (string, string, Image, string)>[] moveLine = moveLines[loopY].OrderBy(x => x.Key).ToArray();

            for (int loopX = 0; loopX < moveLine.Length; loopX++)
            {
                //Draw Moves
                if (loopY + 1 < moveLines.Count)
                {
                    bool isWhite = true;
                    for (int loopNextRowX = 0, moveNum = 1; loopNextRowX < moveLines[loopY + 1].Count; loopNextRowX++, moveNum = !isWhite ? moveNum + 1 : moveNum, isWhite = !isWhite)
                    {
                        if (moveLines[loopY + 1].Keys[loopNextRowX].Contains(moveLines[loopY].Keys[loopX], StringComparison.OrdinalIgnoreCase))
                        {
                            if (moveLines[loopY + 1].Values[loopNextRowX].Item3 != null)
                            {
                                graphics.FillRectangle(moveBkgBrush,
                                                       (SpacerSizeX) + ((loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2)) - (BoxWidth / 2),
                                                       ((loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2)) - (BoxHeight / 2),
                                                       BoxWidth,
                                                       BoxHeight);

                                graphics.DrawString($"{(int)Math.Round((loopY + 1) / 2d, MidpointRounding.AwayFromZero)}.{((loopY + 1) % 2d != 0 ? "" : "..")} {moveLines[loopY + 1].Values[loopNextRowX].Item1}",
                                                        headFont,
                                                        drawBrush,
                                                        (SpacerSizeX) + ((loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2)) - (BoxWidth / 2) + 1,
                                                        ((loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2)) - (BoxHeight / 2) + 1);
                            }
                        }
                    }
                }

            }
        }


        // Draw the boards
        for (int loopY = 0; loopY < moveLines.Count; loopY++)
        {
            KeyValuePair<string, (string, string, Image, string)>[] moveLine = moveLines[loopY].OrderBy(x => x.Key).ToArray();

            for (int loopX = 0; loopX < moveLine.Length; loopX++)
            {
                //Draw Board Shadow
                if (moveLine[loopX].Value.Item3 != null)
                {
                    try
                    {
                        graphics.FillRectangle(transBrush,
                                           (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + 5,
                                           (loopY * BlockSizeY) + (SpacerSizeY / 2) + 5,
                                           moveLine[loopX].Value.Item3.Width,
                                           moveLine[loopX].Value.Item3.Height);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                //Draw Board
                if (moveLine[loopX].Value.Item3 != null)
                {
                    try
                    {
                        graphics.DrawImage(moveLine[loopX].Value.Item3,
                                           (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2),
                                           (loopY * BlockSizeY) + (SpacerSizeY / 2));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        return image;
    }
}

