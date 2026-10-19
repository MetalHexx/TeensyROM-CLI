using Spectre.Console.Cli;
using TeensyRom.Cli.Helpers;
using TeensyRom.Cli.Services;

namespace TeensyRom.Cli.Commands.Main
{
    internal class PortListSettings : CommandSettings
    {
        public static string Example => "ports";
        public static string Description => "List the TeensyROM devices tr-web knows.";
    }

    internal class PortListCommand(IDeviceService devices, IPlayerService player) : AsyncCommand<PortListSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, PortListSettings settings)
        {
            player.StopStream();

            RadHelper.WriteMenu("Port List", "Lists the TeensyROM devices tr-web knows, online and offline.", []);

            var found = await devices.GetDevicesAsync();

            RadHelper.WriteDynamicTable(
                ["Device", "State", "Connection"],
                found.Select(d => new[] { d.Name, d.State, d.Connection }));

            return 0;
        }
    }
}
