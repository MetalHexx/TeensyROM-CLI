using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Common;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class ListFilesCommandSettings : CommandSettings, ITeensyCommandSettings, IRequiresConnection
    {
        [Description("Storage device of file to launch. (sd or usb)")]
        [CommandOption("-s|--storage")]
        public string StorageDevice { get; set; } = string.Empty;

        [Description("The path of the files to list.")]
        [CommandOption("-p|--path")]
        public string FilePath { get; set; } = string.Empty;

        public void ClearSettings()
        {
            StorageDevice = string.Empty;
            FilePath = string.Empty;
        }

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
