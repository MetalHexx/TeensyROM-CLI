using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Common;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class LaunchFileCommandSettings : CommandSettings 
    {
        [Description("Storage device of file to launch. (sd or usb)")]
        [CommandOption("-s|--storage")]
        public string StorageDevice { get; set; } = string.Empty;

        [Description("The full path of the file to launch.")]
        [CommandOption("-p|--path")]
        public string FilePath { get; set; } = string.Empty;

        public override ValidationResult Validate()
        {
            if (!StorageDevice.Equals(string.Empty) && !StorageDevice.IsValueValid(["sd", "usb"])) 
            {
                return ValidationResult.Error($"Storage device must be 'sd' or 'usb'.");
            }
            if (!FilePath.Equals(string.Empty) && !FilePath.IsValidUnixPath()) 
            {
                return ValidationResult.Error($"Must be a valid unix path.");
            }
            return base.Validate();
        }
    }
}
