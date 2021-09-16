namespace PgnArtist.Generic;

public abstract class HeaderBase
{
    protected readonly GlobalSettings _globalSettings;

    public HeaderBase(GlobalSettings globalSettings)
    {
        _globalSettings = globalSettings;
    }

    protected virtual string[] DisplayTitleImpl()
    {
        throw new NotImplementedException("This console app has not been fully implemented!");
    }

    public void DisplayTitle(bool clearConsole)
    {
        if (_globalSettings.ShouldDisplayHeader)
        {
            if (!Console.IsOutputRedirected && clearConsole)
            {
                Console.Clear();
            }

            DisplayTitleImpl().
                    ToList<string>().
                    ForEach((line) =>
                    {
                        Console.WriteLine(
                        line.Substring(0, Console.IsOutputRedirected ? line.Length : Math.Min(Console.WindowWidth - 2, line.Length)));
                    });
        }
    }
}

