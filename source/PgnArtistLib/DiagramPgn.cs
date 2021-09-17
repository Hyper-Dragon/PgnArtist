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
    public float ConnectSize { get; set; } = 3.0f;
    public int BlockSizeX => BoardSize + SpacerSizeX;
    public int BlockSizeY => BoardSize + SpacerSizeY;
    public string SubmittedPgn => _submittedPgn ?? "";
    public int BoardShadowOffset { get; set; } = 8;
    

    private string? _submittedPgn;
    private IEnumerable<Game<ChessLib.Data.MoveRepresentation.MoveStorage>>? _parsedGames;
    private (SortedList<string, string> LastMoveNameList, List<SortedList<string, (string, string, Image, string)>> MoveLines, int MaxWidth) _moveData;


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

    public async Task BuildMoveData(bool isFromWhitesPerspective, GameFilter filter)
    {
        if (_parsedGames is null)
        {
            throw new NullReferenceException("Call 'LoadPgn' BEFORE trying to generate any diagrams.");
        }

        _moveData = await BuildMoveImageData(_parsedGames, filter, isFromWhitesPerspective);
    }

    public async Task<Bitmap> GenerateDiagram(string diagramTitle, float titleSize)
    {
        if (_parsedGames is null) throw new NullReferenceException("Call 'LoadPgn' and then 'BuildMoveData' BEFORE trying to generate any diagrams.");
        
        return await Task.Run<Bitmap>(() => RenderMoveImageData(_moveData.LastMoveNameList, _moveData.MoveLines, diagramTitle, titleSize));
    }

    private static async Task<IEnumerable<Game<ChessLib.Data.MoveRepresentation.MoveStorage>>> ParseAndValidatePgn(string preParsedPgn) => await (new PGNParser()).GetGamesFromPGNAsync(preParsedPgn.ToString(CultureInfo.InvariantCulture));

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

    private Bitmap RenderMoveImageData(SortedList<string, string> lastMoveNameList, List<SortedList<string, (string, string, Image, string)>> moveLines, string diagramTitle, float titleSize)
    {
        // Create font/brush/pen.
        using Font headFont = new(FontFamily.GenericSansSerif, HeadFontSize);
        using Font stripeFont = new(FontFamily.GenericMonospace, SpacerSizeX / 2f);
        using Brush drawBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
        using Brush moveBkgBrush = new SolidBrush(Color.FromArgb(235, 200, 0, 0));
        using Brush transBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
        using Brush bkgStripeBrush = new SolidBrush(Color.FromArgb(5, 255, 255, 255));
        using Pen connectorShadowPen = new(new SolidBrush(Color.FromArgb(200, 0, 0, 0))) { Width = ConnectSize };
        using Pen connectorPen = new(Brushes.Orange) { Width = ConnectSize };

        CreateDrawingSurface(moveLines, out Bitmap image, out Graphics graphics);

        RenderBackgroundFromStream(graphics,
                                   image.Size,
                                   (new EmbeddedFileProvider(typeof(DiagramPgn).Assembly)).GetFileInfo("DefaultBkg01.png").CreateReadStream());

        RenderTitle(graphics, transBrush, diagramTitle, titleSize, shadowOffset: BoardShadowOffset);
        RenderTitle(graphics, Brushes.White, diagramTitle, titleSize);
        RenderBackgroundStripes(graphics, bkgStripeBrush, image.Size);
        RenderPinstripes(graphics, moveLines, lastMoveNameList, drawBrush, stripeFont, image.Size);
        RenderBoards(graphics, transBrush, moveLines, shadowOffset: BoardShadowOffset);
        RenderMoveText(graphics, moveLines, headFont, transBrush, shadowOffset: BoardShadowOffset);
        RenderConnectors(graphics, moveLines, connectorShadowPen, shadowOffset: BoardShadowOffset);
        RenderConnectors(graphics, moveLines, connectorPen);
        RenderMoveText(graphics, moveLines, headFont, moveBkgBrush, textBrush: drawBrush);
        RenderBoards(graphics, transBrush, moveLines);

        return image;
    }

    private void CreateDrawingSurface(List<SortedList<string, (string, string, Image, string)>> moveLines, out Bitmap image, out Graphics graphics)
    {
        image = new(((moveLines.Max(item => item.Count)) * BlockSizeX) + SpacerSizeX + (SpacerSizeX / 2),
                    (moveLines.Count * BlockSizeY) + (SpacerSizeY / 4),
                    PixelFormat.Format32bppArgb);

        graphics = Graphics.FromImage(image);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        graphics.Clear(Color.Black);
    }

    private void RenderMoveText(Graphics graphics, List<SortedList<string, (string, string, Image, string)>> moveLines, Font headFont, Brush moveBkgBrush, int shadowOffset = 0, Brush? textBrush = null)
    {
        if (textBrush is null && shadowOffset == 0)
        {
            throw new ArgumentException("You must provide a 'textBrush' if is 'ShadowLayer' is false");
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
                                if (shadowOffset > 0)
                                {
                                    graphics.FillRectangle(moveBkgBrush,
                                                           shadowOffset + (SpacerSizeX) + ((loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2)) - (BoxWidth / 2),
                                                           shadowOffset + ((loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2)) - (BoxHeight / 2),
                                                           BoxWidth,
                                                           BoxHeight);
                                }
                                else
                                {
                                    graphics.FillRectangle(moveBkgBrush,
                                                           (SpacerSizeX) + ((loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2)) - (BoxWidth / 2),
                                                           ((loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2)) - (BoxHeight / 2),
                                                           BoxWidth,
                                                           BoxHeight);

                                    graphics.DrawString($"{(int)Math.Round((loopY + 1) / 2d, MidpointRounding.AwayFromZero)}.{((loopY + 1) % 2d != 0 ? "" : "..")} {moveLines[loopY + 1].Values[loopNextRowX].Item1}",
                                                            headFont,
                                                            textBrush ?? Brushes.White,
                                                            (SpacerSizeX) + ((loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2)) - (BoxWidth / 2) + 1,
                                                            ((loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2)) - (BoxHeight / 2) + 1);
                                }
                            }
                        }
                    }
                }

            }
        }
    }

    private void RenderBoards(Graphics graphics, Brush transBrush, List<SortedList<string, (string, string, Image, string)>> moveLines, int shadowOffset = 0)
    {
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
                        if (shadowOffset>0)
                        {
                            graphics.FillRectangle(transBrush,
                                               (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + shadowOffset,
                                               (loopY * BlockSizeY) + (SpacerSizeY / 2) + shadowOffset,
                                               moveLine[loopX].Value.Item3.Width,
                                               moveLine[loopX].Value.Item3.Height);
                        }
                        else
                        {
                            graphics.DrawImage(moveLine[loopX].Value.Item3,
                                               (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2),
                                               (loopY * BlockSizeY) + (SpacerSizeY / 2));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }
    }

    private void RenderConnectors(Graphics graphics, List<SortedList<string, (string, string, Image, string)>> moveLines, Pen connectorPen, int shadowOffset = 0)
    {
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
                                graphics.DrawLine(connectorPen,
                                                  shadowOffset + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  (loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize),
                                                  shadowOffset + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  (loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2));

                                graphics.DrawLine(connectorPen,
                                                  shadowOffset + (SpacerSizeX) + (loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  shadowOffset + (loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2),
                                                  shadowOffset + (SpacerSizeX) + (loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  shadowOffset + ((loopY + 1) * BlockSizeY) + (SpacerSizeY / 2));

                                graphics.DrawLine(connectorPen,
                                                  shadowOffset + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  shadowOffset + (loopY * BlockSizeY) + (SpacerSizeY / 2) + (BoardSize) + (SpacerSizeY / 2),
                                                  shadowOffset + (SpacerSizeX) + (loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (BoardSize / 2),
                                                  shadowOffset + ((loopY + 1) * BlockSizeY));
                            }
                        }
                    }
                }
            }
        }
    }

    private void RenderPinstripes(Graphics graphics, List<SortedList<string, (string, string, Image, string)>> moveLines, SortedList<string, string> lastMoveNameList, Brush drawBrush, Font stripeFont, Size imageSize)
    {
        //Draw Pinstripes
        int loopY = moveLines.Count - 1;

        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        KeyValuePair<string, (string, string, Image, string)>[] moveLine = moveLines[loopY].OrderBy(x => x.Key).ToArray();


        for (int loopX = 0; loopX < moveLine.Length; loopX++)
        {
            //Draw Srtipes   
            if (lastMoveNameList.ContainsKey(moveLines[loopY].Keys[loopX]))
            {
                Console.WriteLine($">>>> {loopX,4} {loopY,4} {lastMoveNameList[moveLines[loopY].Keys[loopX]]}");

                using StringFormat drawFormat = new() { FormatFlags = StringFormatFlags.DirectionVertical };
                SizeF stringSize = graphics.MeasureString(lastMoveNameList[moveLines[loopY].Keys[loopX]], stripeFont);

                for (float txtLoop = (SpacerSizeY / 2) + BlockSizeY; txtLoop < imageSize.Height; txtLoop += (stringSize.Width + 20))
                {
                    graphics.DrawString(lastMoveNameList[moveLines[loopY].Keys[loopX]],
                                        stripeFont,
                                        Brushes.Black,
                                        ((SpacerSizeX) + ((loopX * BlockSizeX) + (SpacerSizeX / 2)) - StripeTxtOffset) + 1,
                                        txtLoop + 1,
                                        drawFormat);

                    graphics.DrawString(lastMoveNameList[moveLines[loopY].Keys[loopX]],
                                        stripeFont,
                                        drawBrush,
                                        (SpacerSizeX) + ((loopX * BlockSizeX) + (SpacerSizeX / 2)) - StripeTxtOffset,
                                        txtLoop,
                                        drawFormat);
                }
            }
        }
    }

    private void RenderBackgroundStripes(Graphics graphics, Brush hStripeBrush, Size imageSize)
    {
        for (int loopY = BlockSizeY; loopY < imageSize.Height; loopY += (BlockSizeY * 2))
        {
            graphics.FillRectangle(hStripeBrush, new Rectangle(0, loopY, imageSize.Width, BlockSizeY));
        }
    }

    private void RenderTitle(Graphics graphics, Brush titleBrush, string diagramTitle, float titleSize, int shadowOffset = 0)
    {
        graphics.DrawString(diagramTitle,
                            new Font(FontFamily.GenericSansSerif, titleSize, FontStyle.Regular),
                            titleBrush,
                            BlockSizeX + SpacerSizeX + shadowOffset,
                            SpacerSizeY + shadowOffset);
    }

    private static void RenderBackgroundFromStream(Graphics graphics, Size imageSize, Stream reader)
    {
        using Image bkgImage = Image.FromStream(reader);
        ImageAttributes imageAttr = new();
        imageAttr.SetWrapMode(WrapMode.Tile);

        // Create a TextureBrush.
        Rectangle brushRect = new(0, 0, bkgImage.Width, bkgImage.Height);
        using TextureBrush myTBrush = new(bkgImage, brushRect, imageAttr);

        // Draw to the screen a rectangle filled with the texture
        graphics.FillRectangle(myTBrush, 0, 0, imageSize.Width, imageSize.Height);
    }
}

