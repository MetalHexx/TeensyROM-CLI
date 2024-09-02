using Spectre.Console;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Logging;

namespace TeensyRom.Cli.Commands.TeensyRom.Services
{
    internal class CliAlertService : IAlertService
    {
        public IObservable<string> CommandErrors => throw new NotImplementedException();

        public void Publish(string error)
        {
            RadHelper.WriteLine(error);
            AnsiConsole.WriteLine(RadHelper.ClearHack);
        }
    }
}
