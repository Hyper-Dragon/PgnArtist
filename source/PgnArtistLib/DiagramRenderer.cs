namespace PgnArtistLib;

[SupportedOSPlatform("windows")]
internal class DiagramRenderer
{

    public const int SQUARE_SIZE = 160;
    private const int PINSTRIPE_SPACE = 20;
    private const string DEFAULT_BACKGROUND = "DefaultBkg01.png";

    public int BoxWidth { get; set; } = 62;
    public int BoxHeight { get; set; } = 14;
    public float HeadFontSize { get; set; } = 9f;
    public int SpacerSizeX { get; set; } = 20;
    public int SpacerSizeY { get; set; } = 30;
    public int StripeTxtOffset { get; set; } = 19;
    public float ConnectSize { get; set; } = 3.0f;
    public int BlockSizeX => SQUARE_SIZE + SpacerSizeX;
    public int BlockSizeY => SQUARE_SIZE + SpacerSizeY;
    public int BoardShadowOffset { get; set; } = 8;


    public Bitmap RenderMoveImageData(RenderableGame moveLines, string diagramTitle, float titleSize)
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

        CreateDrawingSurface(moveLines.MoveLines.MoveLines, out Bitmap image, out Graphics graphics);

        RenderBackgroundFromStream(graphics,
                                   image.Size,
                                   (new EmbeddedFileProvider(typeof(DiagramPgn).Assembly)).GetFileInfo(DEFAULT_BACKGROUND).CreateReadStream());

        RenderTitle(graphics, transBrush, diagramTitle, titleSize, shadowOffset: BoardShadowOffset);
        RenderTitle(graphics, Brushes.White, diagramTitle, titleSize);
        RenderBackgroundStripes(graphics, bkgStripeBrush, image.Size);
        RenderPinstripes(graphics, moveLines, drawBrush, stripeFont, image.Size);
        RenderBoards(graphics, moveLines.MoveLines.MoveLines, transBrush, shadowOffset: BoardShadowOffset);
        RenderMoveText(graphics, moveLines.MoveLines.MoveLines, headFont, transBrush, shadowOffset: BoardShadowOffset);
        RenderConnectors(graphics, moveLines.MoveLines.MoveLines, connectorShadowPen, shadowOffset: BoardShadowOffset);
        RenderConnectors(graphics, moveLines.MoveLines.MoveLines, connectorPen);
        RenderMoveText(graphics, moveLines.MoveLines.MoveLines, headFont, moveBkgBrush, textBrush: drawBrush);
        RenderBoards(graphics, moveLines.MoveLines.MoveLines, transBrush);

        return image;
    }

    private void CreateDrawingSurface(List<SortedList<string, RenderableGameMove>> moveLines, out Bitmap image, out Graphics graphics)
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

    private void RenderMoveText(Graphics graphics, List<SortedList<string, RenderableGameMove>> moveLines, Font headFont, Brush moveBkgBrush, int shadowOffset = 0, Brush? textBrush = null)
    {
        if (textBrush is null && shadowOffset == 0)
        {
            throw new ArgumentException("You must provide a 'textBrush' if is 'ShadowLayer' is false");
        }

        for (int loopY = 0; loopY < moveLines.Count; loopY++)
        {
            KeyValuePair<string, RenderableGameMove>[] moveLine = moveLines[loopY].OrderBy(x => x.Key).ToArray();

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
                            if (moveLines[loopY + 1].Values[loopNextRowX].BoardImage != null)
                            {
                                if (shadowOffset > 0)
                                {
                                    graphics.FillRectangle(moveBkgBrush,
                                                           shadowOffset + (SpacerSizeX) + ((loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2)) - (BoxWidth / 2),
                                                           shadowOffset + ((loopY * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2)) - (BoxHeight / 2),
                                                           BoxWidth,
                                                           BoxHeight);
                                }
                                else
                                {
                                    graphics.FillRectangle(moveBkgBrush,
                                                           (SpacerSizeX) + ((loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2)) - (BoxWidth / 2),
                                                           ((loopY * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2)) - (BoxHeight / 2),
                                                           BoxWidth,
                                                           BoxHeight);

                                    graphics.DrawString($"{(int)Math.Round((loopY + 1) / 2d, MidpointRounding.AwayFromZero)}.{((loopY + 1) % 2d != 0 ? "" : "..")} {moveLines[loopY + 1].Values[loopNextRowX].San}",
                                                        headFont,
                                                        textBrush ?? Brushes.White,
                                                        (SpacerSizeX) + ((loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2)) - (BoxWidth / 2) + 1,
                                                        ((loopY * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2)) - (BoxHeight / 2) + 1);
                                }
                            }
                        }
                    }
                }

            }
        }
    }

    private void RenderBoards(Graphics graphics, List<SortedList<string, RenderableGameMove>> moveLines, Brush transBrush, int shadowOffset = 0)
    {
        for (int loopY = 0; loopY < moveLines.Count; loopY++)
        {
            KeyValuePair<string, RenderableGameMove>[] moveLine = moveLines[loopY].OrderBy(x => x.Key).ToArray();

            for (int loopX = 0; loopX < moveLine.Length; loopX++)
            {
                //Draw Board Shadow
                if (moveLine[loopX].Value.BoardImage != null)
                {
                    try
                    {
                        if (shadowOffset > 0)
                        {
                            graphics.FillRectangle(transBrush,
                                               (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + shadowOffset,
                                               (loopY * BlockSizeY) + (SpacerSizeY / 2) + shadowOffset,
                                               moveLine[loopX].Value.BoardImage.Width,
                                               moveLine[loopX].Value.BoardImage.Height);
                        }
                        else
                        {
                            graphics.DrawImage(moveLine[loopX].Value.BoardImage,
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

    private void RenderConnectors(Graphics graphics, List<SortedList<string, RenderableGameMove>> moveLines, Pen connectorPen, int shadowOffset = 0)
    {
        graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

        for (int loopY = 0; loopY < moveLines.Count; loopY++)
        {
            KeyValuePair<string, RenderableGameMove>[] moveLine = moveLines[loopY].OrderBy(x => x.Key).ToArray();

            for (int loopX = 0; loopX < moveLine.Length; loopX++)
            {
                //Draw Connector
                if (loopY + 1 < moveLines.Count)
                {
                    for (int loopNextRowX = 0; loopNextRowX < moveLines[loopY + 1].Count; loopNextRowX++)
                    {
                        if (moveLines[loopY + 1].Keys[loopNextRowX].Contains(moveLines[loopY].Keys[loopX], StringComparison.OrdinalIgnoreCase))
                        {
                            if (moveLines[loopY + 1].Values[loopNextRowX].BoardImage != null)
                            {
                                graphics.DrawLine(connectorPen,
                                                  shadowOffset + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2),
                                                  (loopY * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE),
                                                  shadowOffset + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2),
                                                  (loopY * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2));

                                graphics.DrawLine(connectorPen,
                                                  shadowOffset + (SpacerSizeX) + (loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2),
                                                  shadowOffset + (loopY * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2),
                                                  shadowOffset + (SpacerSizeX) + (loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2),
                                                  shadowOffset + ((loopY + 1) * BlockSizeY) + (SpacerSizeY / 2));

                                graphics.DrawLine(connectorPen,
                                                  shadowOffset + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2),
                                                  shadowOffset + (loopY * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2),
                                                  shadowOffset + (SpacerSizeX) + (loopNextRowX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2),
                                                  shadowOffset + ((loopY + 1) * BlockSizeY));
                            }
                        }
                    }
                }
            }
        }
    }

    private void RenderPinstripes(Graphics graphics, RenderableGame renderableGame, Brush drawBrush, Font stripeFont, Size imageSize)
    {
        var moveLines = renderableGame.MoveLines.MoveLines;
        var lastMoveNameList = renderableGame.MoveLines.TextForKey;
        //Draw Pinstripes
        //int loopY = moveLines.Count - 1;

        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;


        for (int loopY = 0; loopY < moveLines.Count; loopY++)
        {
            KeyValuePair<string, RenderableGameMove>[] moveLine = moveLines[loopY].OrderBy(x => x.Key).ToArray();

            for (int loopX = 0; loopX < moveLine.Length; loopX++)
            {
                //Draw Srtipes   
                if (lastMoveNameList.ContainsKey(moveLines[loopY].Keys[loopX]))
                {
                    Console.WriteLine($">>>> {loopX,4} {loopY,4} {lastMoveNameList[moveLines[loopY].Keys[loopX]]}");

                    using StringFormat drawFormat = new() { FormatFlags = StringFormatFlags.DirectionVertical };
                    SizeF stringSize = graphics.MeasureString(lastMoveNameList[moveLines[loopY].Keys[loopX]], stripeFont);

                    for (float txtLoop = (SpacerSizeY / 2) + BlockSizeY; txtLoop < imageSize.Height; txtLoop += stringSize.Width + PINSTRIPE_SPACE)
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