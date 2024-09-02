using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Serial.State;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class PortListCommand(ISerialStateContext serial, IPlayerService player) : AsyncCommand<PortListCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, PortListCommandSettings settings)
        {
            player.StopStream();

            RadHelper.WriteMenu("Port List", "Troubleshooting tool for listing all the available serial ports on the machine.", []);

            var ports = await serial.Ports.FirstAsync();

            RadHelper.WriteLine("Ports Found: ");
            AnsiConsole.WriteLine();

            foreach (var port in ports) 
            {
                RadHelper.WriteLine(port);
            }
            RadHelper.WriteLine();
            return 0;
        }
    }
}