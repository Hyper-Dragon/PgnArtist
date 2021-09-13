using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PgnArtist.Generic.AutoRegisterAttribute;

namespace PgnArtist.Generic
{
    [AutoRegister(RegistrationType.SINGLETON)]
    public sealed class Helpers
    {
        private readonly GlobalSettings _globalSettings;

        private readonly Stopwatch stopwatch = new();

        public Helpers(GlobalSettings globalSettings)
        {
            _globalSettings = globalSettings;
        }

        public void PressToContinue()
        {
            if (_globalSettings.ShouldBeepOnEnd && !Console.IsOutputRedirected) Console.Beep();

            Console.Write("<Press a Key>");
            Console.ReadKey(true);
        }

        public void DisplaySection(string title, bool isHeader)
        {
            if (_globalSettings.ShouldDisplaySectionHeaders)
            {
                int HEAD_LEN = Console.IsOutputRedirected ? 80 : Console.WindowWidth / 2;
                int FOOT_LEN = Console.IsOutputRedirected ? 60 : Console.WindowWidth / 3;

                int midRowLength = (isHeader) ? HEAD_LEN : FOOT_LEN;
                double spacerLength = (title.Length + 2d) / 2d;

                StringBuilder sb = new();

                sb.Append('=', midRowLength - (int)Math.Ceiling(spacerLength) - 1);
                sb.Append($" {title} ");
                sb.Append('=', midRowLength - (int)Math.Floor(spacerLength) - 1);

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(sb.ToString());
                Console.ResetColor();

                //if (!isHeader)
                //{
                //    Console.WriteLine("");
                //}
            }
        }


        public void StartTimedSection(string msg, bool newLineFirst = false, bool newLineAfter = false)
        {
            stopwatch.Reset();
            stopwatch.Start();

            if (newLineFirst) { Console.WriteLine(); }
            Console.WriteLine(msg);
            if (newLineAfter) { Console.WriteLine(); }
        }

        public void EndTimedSection(string msg, bool newLineFirst = false, bool newLineAfter = false)
        {
            stopwatch.Stop();

            if (newLineFirst) { Console.WriteLine(); }
            Console.WriteLine($"{msg} ({stopwatch.Elapsed.Hours}:{stopwatch.Elapsed.Minutes}:{stopwatch.Elapsed.Seconds}:{stopwatch.Elapsed.Milliseconds})");
            if (newLineAfter) { Console.WriteLine(); }
        }

    }
}
