using static PgnArtist.Generic.AutoRegisterAttribute;

namespace PgnArtist;

[SupportedOSPlatform("windows")]
[AutoRegister(RegistrationType.SINGLETON)]
public sealed class ConsoleApplication : ConsoleApplicationBase
{
    private readonly DiagramPgn _diagramPgn;

    public ConsoleApplication(Parser parser, Header customProcessor, GlobalSettings globalSettings, Helpers helpers, DiagramPgn diagramPgn) : base(parser, customProcessor, globalSettings, helpers)
    {
        _diagramPgn = diagramPgn;
    }

    protected override async Task<bool> PreRunImplAsync(CommandLineOptions cmdLineOpts)
    {

        if (cmdLineOpts.PgnFilePath is null || !cmdLineOpts.PgnFilePath.Exists)
        {
            Console.WriteLine(">> ERROR :: PGN File Does Not Exist.");
            return false;
        }

        if (!cmdLineOpts.PgnFilePath.FullName.EndsWith(".pgn"))
        {
            Console.WriteLine($">> ERROR :: {cmdLineOpts.PgnFilePath.FullName} must be a '.pgn' file.");
            return false;
        }

        if (cmdLineOpts.DiagramOutFilePath is null)
        {
            Console.WriteLine(">> ERROR :: Output File Is Not Defined.");
            return false;
        }

        if (!cmdLineOpts.DiagramOutFilePath.FullName.EndsWith(".png"))
        {
            Console.WriteLine($">> ERROR :: {cmdLineOpts.DiagramOutFilePath.FullName} must be a '.png' file.");
            return false;
        }

        if (cmdLineOpts.DiagramOutFilePath.Exists && !cmdLineOpts.OverwriteImage)
        {
            Console.WriteLine($">> ERROR :: {cmdLineOpts.DiagramOutFilePath.FullName} already exists.");
            return false;
        }

        _helpers.StartTimedSection($">> Processing PGN {cmdLineOpts.PgnFilePath}");
        _ = await _diagramPgn.AssignPgn(cmdLineOpts.PgnFilePath);
        _helpers.EndTimedSection(">> PGN Processing Done");

        if (cmdLineOpts.DisplayPgn)
        {
            _helpers.DisplaySection("Loadeding PGN", false);
            Console.WriteLine(_diagramPgn.SubmittedPgn);
            _helpers.DisplaySection("PGN Loaded", false);
        }

        return true;
    }

    protected override async Task<int> RunImplAsync(CommandLineOptions cmdOpts)
    {
        GameFilter? filter = new()
        {
            MaxGames = cmdOpts.MaxGames,
            MaxPly = cmdOpts.MaxPly,
            TakeGamesFromEnd = cmdOpts.TakeGamesFromEnd,
            FilterBlack = cmdOpts.FilterBlack,
            FilterWhite = cmdOpts.FilterWhite,
            FilterEither = cmdOpts.FilterEither,
            FilterECO = cmdOpts.FilterECO,
            FilterOpeningContains = cmdOpts.FilterOpeningContains
        };


        _helpers.StartTimedSection(">> Processing Move Data");
        await _diagramPgn.BuildMoveData(!cmdOpts.FlipBoard, filter);
        _helpers.EndTimedSection(">> Processing Complete");

        _helpers.StartTimedSection(">> Generating Diagram");
        System.Drawing.Bitmap? image = await _diagramPgn.GenerateDiagram(cmdOpts.DiagramTitle, cmdOpts.TitleSize);
        _helpers.EndTimedSection(">> Diagram Generated");

        string fileOut = $"{(cmdOpts?.DiagramOutFilePath?.FullName ?? "")}";

        Console.Write($">> Saving Diagram to {fileOut}...");
        image.Save(fileOut, ImageFormat.Png);
        Console.WriteLine($"..DONE");

        return 0;
    }

    protected override async Task<bool> PostRunImplAsync(CommandLineOptions commandLineOptions)
    {
        return await Task.Run<bool>(() =>
        {
            Console.WriteLine(">>>> REPLACE WITH IMPL");
            return true;
        });
    }

}

