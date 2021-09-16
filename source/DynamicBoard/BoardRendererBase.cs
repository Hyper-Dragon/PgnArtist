global using DynamicBoard.Assets;
global using DynamicBoard.Helpers;
global using Microsoft.Extensions.Logging;
global using System;
global using System.Collections.Generic;
global using System.Drawing;
global using System.Drawing.Imaging;
global using System.IO;
global using System.Runtime.Versioning;
global using System.Threading;
global using System.Threading.Tasks;

namespace DynamicBoard;

public abstract class BoardRendererBase : IBoardRenderer
{
    private readonly ILogger _logger;

    public BoardRendererBase(ILogger logger)
    {
        _logger = logger;
        _logger?.LogTrace("Board Render Base Called");
    }
    public abstract Task<byte[]> GetPngImageFromFenAsync(in string fenString, in int imageSize, in bool isFromWhitesPerspective = true);
    public abstract Task<byte[]> GetPngImageDiffFromFenAsync(in string fenString, in string compFenString, in int imageSize, in bool isFromWhitesPerspective = true);
}

