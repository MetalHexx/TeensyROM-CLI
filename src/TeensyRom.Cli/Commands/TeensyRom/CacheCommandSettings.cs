using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Common;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class CacheCommandSettings : CommandSettings, ITeensyCommandSettings, IRequiresConnection
    {
        [Description("Storage files to cache files for. (sd or usb)")]
        [CommandOption("-s|--storage")]
        public string StorageDevice { get; set; } = string.Empty;

        [Description("Specific TeensyROM path to cache")]
        [CommandOption("-p|--path")]
        public string Path { get; set; } = string.Empty;

        public void ClearSettings()
        {
            StorageDevice = string.Empty;
            Path = string.Empty;
        }

        public override ValidationResult Validate()
        {
            if (!StorageDevice.Equals(string.Empty) && !StorageDevice.IsValueValid(["sd", "usb"])) 
            {
                return ValidationResult.Error($"Storage device must be 'sd' or 'usb'.");
            }
            if (!Path.Equals(string.Empty) && !Path.IsValidUnixPath())
            {
                return ValidationResult.Error($"Must be a valid unix path.");
            }
            return base.Validate();
        }
    }
}
