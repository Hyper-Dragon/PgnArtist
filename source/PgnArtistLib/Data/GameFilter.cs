namespace PgnArtistLib.Data;

public record GameFilter
{
    public int MaxPly { get; set; } = 1000;
    public int MaxGames { get; set; } = 1000;
    public bool TakeGamesFromEnd { get; set; } = false;
    public string FilterWhite { get; set; } = "";
    public string FilterBlack { get; set; } = "";
    public string FilterEither { get; set; } = "";
    public string FilterECO { get; set; } = "";
    public string FilterOpeningContains { get; set; } = "";
}

