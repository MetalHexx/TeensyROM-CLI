using Spectre.Console;
using System.Reactive.Linq;
using System.Text;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Common;
using TeensyRom.Core.Logging;

namespace TeensyRom.Cli.Services
{
    internal interface ICliLoggingService : ILoggingService
    {
        bool Enabled { get; set; }
    }
    internal class CliLoggingService : LoggingService, ICliLoggingService
    {
        public bool Enabled { get; set; } = true;
        public override void Log(string message, string hExColor)
        {
            var sb = new StringBuilder();

            var log = message
                .SplitAtCarriageReturn()
                .Select(line => WithColor(line, hExColor))
                .Aggregate(sb, (acc, line) => acc.AppendWithLimit(line))
                .ToString()
                .DropLastNewLine();

            _logs.OnNext(log);

            if (Enabled)
            {
                AnsiConsole.Markup($"{log}\r\n\r\n");
            }
            base.Log(message, hExColor);
        }

        private string WithColor(string message, string hexColor)
        {
            message = message.EscapeBrackets();
            message = $"[{hexColor}]{message}[/]\r\n";
            return message;
        }
    }
}
