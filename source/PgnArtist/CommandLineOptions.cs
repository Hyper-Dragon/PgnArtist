namespace PgnArtist;

public sealed class CommandLineOptions : CommandLineOptionsBase
{
    [Value(index: 0, Required = true, HelpText = "The PGN file to diagram")]
    public FileInfo? PgnFilePath { get; set; }

    [Value(index: 1, Required = true, HelpText = "Diagram filename to write")]
    public FileInfo? DiagramOutFilePath { get; set; }

    [Option(longName: "initialFen", Required = false, HelpText = "Initial Fen", Default = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR")]
    public string InitialFen { get; set; } = "";

    [Option(shortName: 't', longName: "title", Required = false, HelpText = "Header Text", Default = "")]
    public string DiagramTitle { get; set; } = "";

    [Option(longName: "titleSize", Required = false, HelpText = "Title Size (em)", Default = 50f)]
    public float TitleSize { get; set; }

    [Option(longName: "maxPly", Required = false, HelpText = "Max Ply to Render", Default = 1000)]
    public int MaxPly { get; set; }

    [Option(longName: "maxGames", Required = false, HelpText = "Max Games to Render", Default = 1000)]
    public int MaxGames { get; set; }




    [Option(longName: "filterWhite", Required = false, HelpText = "Filter....", Default = "")]
    public string FilterWhite { get; set; } = "";

    [Option(longName: "filterBlack", Required = false, HelpText = "Filter....", Default = "")]
    public string FilterBlack { get; set; } = "";

    [Option(longName: "filterEither", Required = false, HelpText = "Filter....", Default = "")]
    public string FilterEither { get; set; } = "";

    [Option(longName: "filterEco", Required = false, HelpText = "Filter....", Default = "")]
    public string FilterECO { get; set; } = "";

    [Option(longName: "filterOpeningContains", Required = false, HelpText = "Filter....", Default = "")]
    public string FilterOpeningContains { get; set; } = "";




    [Option(longName: "takeGamesFromEnd", Required = false, HelpText = "Take games from the end of the pgn", Default = false)]
    public bool TakeGamesFromEnd { get; set; }

    [Option(shortName: 'd', longName: "displaypgn", Required = false, HelpText = "Display PGN", Default = false)]
    public bool DisplayPgn { get; set; }

    [Option(shortName: 'o', longName: "overwriteImage", Required = false, HelpText = "Overwrite Image", Default = false)]
    public bool OverwriteImage { get; set; }

    [Option(shortName: 'f', longName: "flipBoard", Required = false, HelpText = "Flip Board", Default = false)]
    public bool FlipBoard { get; set; }
}

