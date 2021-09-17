namespace PgnArtistLib;

public class DiagramPgn
{
    private string? _submittedPgn;
    private IEnumerable<Game<ChessLib.Data.MoveRepresentation.MoveStorage>>? _parsedGames;
    private RenderableGame? _moveData;
    public string SubmittedPgn => _submittedPgn ?? "";


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

    private static async Task<IEnumerable<Game<MoveStorage>>> ParseAndValidatePgn(string preParsedPgn)
    {
        return await (new PGNParser()).GetGamesFromPGNAsync(preParsedPgn.ToString(CultureInfo.InvariantCulture));
    }

    [SupportedOSPlatform("windows")]
    public async Task BuildMoveData(bool isFromWhitesPerspective, GameFilter filter)
    {
        if (_parsedGames is null)
        {
            throw new NullReferenceException("Call 'LoadPgn' BEFORE trying to generate any diagrams.");
        }

        _moveData = await ProcessParsedPgn.BuildMoveImageData(new MoveImageData() { ParsedGames = _parsedGames, Filter = filter, IsFromWhitesPerspective = isFromWhitesPerspective });
    }

    [SupportedOSPlatform("windows")]
    public async Task<Bitmap> GenerateDiagram(string diagramTitle, float titleSize)
    {
        if (_parsedGames is null || _moveData is null)
        {
            throw new NullReferenceException("Call 'LoadPgn' and then 'BuildMoveData' BEFORE trying to generate any diagrams.");
        }

        return await Task.Run<Bitmap>(() => new DiagramRenderer().RenderMoveImageData(_moveData.LastMoveNameList, _moveData.MoveLines, diagramTitle, titleSize));
    }















}
