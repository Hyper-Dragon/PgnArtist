namespace PgnArtistLib;

public class DiagramPgn
{
    private string? _submittedPgn;
    private IEnumerable<Game>? _parsedGames;
    private RenderableGameCollection? _moveData;
    public string SubmittedPgn => _submittedPgn ?? "";


    public async Task<bool> AssignPgn(string pgnText, GameFilter gameFilter)
    {
        _parsedGames = (IEnumerable<Game>?)await ParseAndValidatePgn(pgnText, gameFilter.MaxPly).ConfigureAwait(false);
        _submittedPgn = pgnText;
        return true;
    }

    public async Task<bool> AssignPgn(FileInfo pgnFileInfo, GameFilter gameFilter)
    {
        string preParsedPgn = File.ReadAllText(pgnFileInfo.FullName);
        _parsedGames = await ParseAndValidatePgn(preParsedPgn, gameFilter.MaxPly).ConfigureAwait(false);
        _submittedPgn = preParsedPgn;
        return true;
    }

    private static async Task<IEnumerable<Game>> ParseAndValidatePgn(string preParsedPgn, int maxPly)
    {
        PGNParser parser = new(new PGNParserOptions(maxPlyCountPerGame: maxPly, ignoreVariations: true));
        IEnumerable<Game>? retVal = await parser.GetGamesFromPGNAsync(preParsedPgn.ToString(CultureInfo.InvariantCulture));

        Console.WriteLine($">> Found {parser.GameCount} games and succesfully processed {parser.Completed}");

        return retVal;
    }

    [SupportedOSPlatform("windows")]
    public async Task BuildMoveData(bool isFromWhitesPerspective, GameFilter filter, string initialFen)
    {
        if (_parsedGames is null)
        {
            throw new NullReferenceException("Call 'LoadPgn' BEFORE trying to generate any diagrams.");
        }

        _moveData = await ProcessParsedPgn.BuildMoveImageData(new MoveImageData()
        {
            ParsedGames =  _parsedGames.ToList(),
            Filter = filter,
            IsFromWhitesPerspective = isFromWhitesPerspective
        }, initialFen);
    }

    [SupportedOSPlatform("windows")]
    public async Task<Bitmap> GenerateDiagram(string diagramTitle, float titleSize)
    {
        if (_parsedGames is null || _moveData is null)
        {
            throw new NullReferenceException("Call 'LoadPgn' and then 'BuildMoveData' BEFORE trying to generate any diagrams.");
        }

        return await Task.Run<Bitmap>(() => new DiagramRenderer().RenderMoveImageData(_moveData, diagramTitle, titleSize));
    }

}
