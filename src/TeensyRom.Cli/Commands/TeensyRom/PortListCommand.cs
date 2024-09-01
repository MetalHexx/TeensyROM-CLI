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

            var ports = await serial.Ports.FirstAsync();

            foreach (var port in ports) 
            {
                RadHelper.WriteLine(port);
            }
            RadHelper.WriteLine();
            return 0;
        }
    }
}