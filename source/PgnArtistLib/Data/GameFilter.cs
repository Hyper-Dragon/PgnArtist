namespace PgnArtistLib.Data;

public record GameFilter
{
    public int MaxPly { get; init; } = 1000;
    public int MaxGames { get; init; } = 1000;
    public bool TakeGamesFromEnd { get; init; } = false;
    public string FilterWhite { get; init; } = "";
    public string FilterBlack { get; init; } = "";
    public string FilterEither { get; init; } = "";
    public string FilterECO { get; init; } = "";
    public string FilterOpeningContains { get; init; } = "";
}

