namespace PgnArtist.Generic;

public abstract class CommandLineOptionsBase
{
    [Option(longName: "hp", Required = false, HelpText = "Hide Params", Default = false)]
    public bool HideParams { get; set; }

    [Option(shortName: 's', longName: "silent", Required = false, HelpText = "Silent mode", Default = false)]
    public bool Silent { get; set; }

    public override string ToString()
    {
        if (!HideParams)
        {
            List<(string param, string val)> sl = new();
            StringBuilder sb = new();

            sl.Add(("Start Date (UTC)", DateTime.UtcNow.ToShortDateString()));
            //sl.Add(("Executable", Environment.GetCommandLineArgs()[0]));
            //sl.Add(("Current Directory", Environment.CurrentDirectory));
            //sl.Add(("System Dir", Environment.SystemDirectory));
            //sl.Add(("OS", Environment.OSVersion.ToString()));
            //sl.Add(("Is 64 bit OS", Environment.Is64BitOperatingSystem.ToString()));
            //sl.Add(("Is 64 bit Process", Environment.Is64BitProcess.ToString()));
            //sl.Add(("CLR Version", Environment.Version.ToString()));
            //sl.Add(("Computer Name", Environment.MachineName));
            //sl.Add(("User Domain Name", Environment.UserDomainName));
            //sl.Add(("Username", Environment.UserName));
            //sl.Add(("Is Interactive", Environment.UserInteractive.ToString()));
            //sl.Add(("Window Width", Console.WindowWidth.ToString()));
            //sl.Add(("Buffer Width", Console.BufferWidth.ToString()));
            //sl.Add(("Out Redirected", Console.IsOutputRedirected.ToString()));
            //sl.Add(("Err Redirected", Console.IsErrorRedirected.ToString()));

            foreach (System.Reflection.PropertyInfo? prop in GetType().GetProperties())
            {
                StringBuilder? nameSb = new(prop.Name[0].ToString());
                prop.Name[1..].ToCharArray().ToList().ForEach((letter) => { nameSb.Append((char.IsUpper(letter) ? $" {letter}" : $"{letter}")); });
                sl.Add((nameSb.ToString(), prop.GetValue(this)?.ToString() ?? "-"));
            }

            int maxPropLength = sl.Max((prop) => { return prop.param.Length; });
            int maxValLength = sl.Max((prop) => { return prop.val.Length; });
            sl.ForEach((prop) => { sb.Append($"{Environment.NewLine}{prop.param.PadRight(maxPropLength)} >> { HelpersStatic.FixedWidth(prop.val, Console.WindowWidth - maxPropLength - 5)}"); });

            string? s = sb.ToString();
            return s[(s.IndexOf(Environment.NewLine) + Environment.NewLine.Length)..];
        }
        else
        {
            return "";
        }
    }
}

