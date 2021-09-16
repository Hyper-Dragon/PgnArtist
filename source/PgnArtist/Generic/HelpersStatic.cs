namespace PgnArtist.Generic;

public static class HelpersStatic
{
    public static string FixedWidth(string s, int width)
    {
        return new string(s.Take(width).ToArray()).PadRight(width);
    }

    public static void DisplayException(Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error :: Outer Type :: {ex.GetType()}");
        Console.WriteLine($"                    :: {ex.Message}");
        ex.StackTrace?.Split(Environment.NewLine).ToList<string>().ForEach((line) => { Console.WriteLine($"                    :: {line}"); });
        Console.WriteLine($"      :: Inner Type :: {(ex.InnerException is null ? "-" : ex.InnerException?.GetType())}");
        Console.WriteLine($"                    :: {(ex.InnerException is null ? "-" : ex.InnerException?.Message)}");
        ex.InnerException?.StackTrace?.Split(Environment.NewLine).ToList<string>().ForEach((line) => { Console.WriteLine($"                    :: {line}"); });
        Console.ResetColor();
    }

    public static string ValueOrDash(int? valueIn)
    {
        if (!valueIn.HasValue || valueIn.Value == 0)
        {
            return "-";
        }
        else
        {
            return valueIn?.ToString() ?? "";
        }
    }
}

