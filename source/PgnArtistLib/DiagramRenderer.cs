﻿namespace PgnArtistLib;

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


    public Bitmap RenderMoveImageData(RenderableGameCollection moveLines, string diagramTitle, float titleSize)
    {

        int gridXLength = moveLines.DisplayGrid.GetLength(0);
        int gridYLength = moveLines.DisplayGrid.GetLength(1);

        // Create font/brush/pen.
        using Font headFont = new(FontFamily.GenericSansSerif, HeadFontSize);
        using Font stripeFont = new(FontFamily.GenericMonospace, SpacerSizeX / 2f);
        using Brush drawBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
        using Brush moveBkgBrush = new SolidBrush(Color.FromArgb(235, 200, 0, 0));
        using Brush transBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
        using Brush bkgStripeBrush = new SolidBrush(Color.FromArgb(5, 255, 255, 255));
        using Pen connectorShadowPen = new(new SolidBrush(Color.FromArgb(200, 0, 0, 0))) { Width = ConnectSize };
        using Pen connectorPen = new(Brushes.Orange) { Width = ConnectSize };

        Graphics graphics;

        CreateDrawingSurface(gridXLength, gridYLength, out Bitmap image, out graphics);

        RenderBackgroundFromStream(graphics,
                                   image.Size,
                                   (new EmbeddedFileProvider(typeof(DiagramPgn).Assembly)).GetFileInfo(DEFAULT_BACKGROUND).CreateReadStream());

        RenderTitle(graphics, transBrush, diagramTitle, titleSize, shadowOffset: BoardShadowOffset);
        RenderTitle(graphics, Brushes.White, diagramTitle, titleSize);
        RenderBackgroundStripes(graphics, bkgStripeBrush, image.Size);
        //RenderPinstripes(graphics, moveLines, drawBrush, stripeFont, image.Size);
        RenderBoards(graphics, moveLines.DisplayGrid, transBrush, BoardShadowOffset);
        RenderMoveText(graphics, moveLines.DisplayGrid, headFont, transBrush, shadowOffset: BoardShadowOffset);
        RenderConnectors(graphics, moveLines.DisplayGrid, connectorShadowPen, shadowOffset: BoardShadowOffset);
        RenderConnectors(graphics, moveLines.DisplayGrid, connectorPen);
        RenderMoveText(graphics, moveLines.DisplayGrid, headFont, moveBkgBrush, textBrush: drawBrush);
        RenderBoards(graphics, moveLines.DisplayGrid, transBrush);

        return image;
    }

    private void CreateDrawingSurface(int gridXLength, int gridYLength, out Bitmap image, out Graphics graphics)
    {
        image = new((gridXLength * BlockSizeX) + SpacerSizeX + (SpacerSizeX / 2),
                    (gridYLength * BlockSizeY) + (SpacerSizeY / 4),
                    PixelFormat.Format32bppArgb);

        graphics = Graphics.FromImage(image);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        graphics.Clear(Color.Black);
    }

    private void RenderMoveText(Graphics graphics, RenderableGameMove[,] moveLines, Font headFont, Brush moveBkgBrush, int shadowOffset = 0, Brush? textBrush = null)
    {
        if (textBrush is null && shadowOffset == 0)
        {
            throw new ArgumentException("You must provide a 'textBrush' if is 'ShadowLayer' is false");
        }

        for (int loopY = 1; loopY < moveLines.GetLength(1); loopY++)
        {
            for (int loopX = 0; loopX < moveLines.GetLength(0); loopX++)
            {
                //Draw Moves
                if (loopY < moveLines.GetLength(1))
                {
                    if (moveLines[loopX, loopY].BoardImage != null)
                    {
                        if (shadowOffset > 0)
                        {
                            graphics.FillRectangle(moveBkgBrush,
                                                   shadowOffset + (SpacerSizeX) + ((loopX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2)) - (BoxWidth / 2),
                                                   shadowOffset + (((loopY - 1) * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2)) - (BoxHeight / 2),
                                                   BoxWidth,
                                                   BoxHeight);
                        }
                        else
                        {
                            graphics.FillRectangle(moveBkgBrush,
                                                   (SpacerSizeX) + ((loopX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2)) - (BoxWidth / 2),
                                                   (((loopY - 1) * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2)) - (BoxHeight / 2),
                                                   BoxWidth,
                                                   BoxHeight);

                            graphics.DrawString($"{(int)Math.Round((loopY + 1) / 2d, MidpointRounding.AwayFromZero)}.{((loopY) % 2d != 0 ? "" : "..")} {moveLines[loopX, loopY].San}",
                                                headFont,
                                                textBrush ?? Brushes.White,
                                                (SpacerSizeX) + ((loopX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2)) - (BoxWidth / 2) + 1,
                                                (((loopY - 1) * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2)) - (BoxHeight / 2) + 1);
                        }
                    }
                }

            }
        }
    }

    private void RenderBoards(Graphics graphics, RenderableGameMove[,] moveLines, Brush transBrush, int shadowOffset = 0)
    {
        for (int loopY = 0; loopY < moveLines.GetLength(1); loopY++)
        {
            for (int loopX = 0; loopX < moveLines.GetLength(0); loopX++)
            {
                //Draw Board Shadow
                if (moveLines[loopX, loopY].BoardImage != null)
                {
                    if (shadowOffset > 0)
                    {
                        graphics.FillRectangle(transBrush,
                                           (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + shadowOffset,
                                           (loopY * BlockSizeY) + (SpacerSizeY / 2) + shadowOffset,
                                           moveLines[loopX, loopY].BoardImage.Width,
                                           moveLines[loopX, loopY].BoardImage.Height);
                    }
                    else
                    {
                        graphics.DrawImage(moveLines[loopX, loopY].BoardImage,
                                           (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2),
                                           (loopY * BlockSizeY) + (SpacerSizeY / 2));
                    }
                }
            }
        }
    }

    private void RenderConnectors(Graphics graphics, RenderableGameMove[,] moveLines, Pen connectorPen, int shadowOffset = 0)
    {
        graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        string lastVisibleFen = "";
        int lastVisibleFenIdx = 0;

        for (int loopY = 0; loopY < moveLines.GetLength(1); loopY++)
        {
            for (int loopX = 0; loopX < moveLines.GetLength(0); loopX++)
            {
                //Draw the TOP connector
                if (loopY - 1 >= 0 && moveLines[loopX, loopY - 1].BoardImage is not null &&
                                      moveLines[loopX, loopY].BoardImage is not null)
                {
                    graphics.DrawLine(connectorPen,
                                      shadowOffset + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2),
                                      ((loopY - 1) * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE),
                                      shadowOffset + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2),
                                      ((loopY - 1) * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2));
                }

                //Draw the BOTTOM connector
                if (loopY + 1 < moveLines.GetLength(1) && moveLines[loopX, loopY + 1].BoardImage is not null)
                {
                    graphics.DrawLine(connectorPen,
                                    shadowOffset + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2),
                                    shadowOffset + (loopY * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2),
                                    shadowOffset + (SpacerSizeX) + (loopX * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2),
                                    shadowOffset + ((loopY + 1) * BlockSizeY) + (SpacerSizeY / 2));
                }
            }
        }

        //Draw the HORIZONTAL connectors
        for (int loopY = 1; loopY < moveLines.GetLength(1); loopY++)
        {
            for (int loopX = 0; loopX < moveLines.GetLength(0); loopX++)
            {
                if (!moveLines[loopX, loopY - 1].IsHidden)
                {
                    lastVisibleFen = moveLines[loopX, loopY - 1].BoardFen;
                    lastVisibleFenIdx = loopX;
                    //Console.WriteLine($"Last Visible FEN {loopX,2}:{loopY - 1,2} {lastVisibleFen}");
                }

                if (!moveLines[loopX, loopY].IsHidden)
                {
                    if (moveLines[loopX, loopY].LastBoardFen == lastVisibleFen)
                    {
                        //Console.WriteLine($"   >>>>> {loopX,2}:{loopY,2} {moveLines[loopX, loopY].LastBoardFen}");

                        graphics.DrawLine(connectorPen,
                                          new Point(shadowOffset + (SpacerSizeX) + ((lastVisibleFenIdx) * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2),
                                                    shadowOffset + ((loopY - 1) * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2)),
                                          new Point(shadowOffset + (SpacerSizeX) + ((Math.Max(0, loopX)) * BlockSizeX) + (SpacerSizeX / 2) + (SQUARE_SIZE / 2),
                                                    shadowOffset + ((loopY - 1) * BlockSizeY) + (SpacerSizeY / 2) + (SQUARE_SIZE) + (SpacerSizeY / 2))
                                          );
                    }
                }
            }

        }
    }

    private void RenderPinstripes(Graphics graphics, RenderableGameCollection renderableGame, Brush drawBrush, Font stripeFont, Size imageSize)
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