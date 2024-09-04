using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Common;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class SearchFilesCommandSettings : CommandSettings, ITeensyCommandSettings, IRequiresConnection
    {
        [Description("Storage device to search. (sd or usb)")]
        [CommandOption("-s|--storage")]
        public string StorageDevice { get; set; } = string.Empty;

        [Description("Search query.  Ex: \"iron maiden aces high\"")]
        [CommandOption("-q|--query")]
        public string Query { get; set; } = string.Empty;

        public void ClearSettings()
        {
            StorageDevice = string.Empty;
            Query = string.Empty;
        }

        public override ValidationResult Validate()
        {
            if (!StorageDevice.Equals(string.Empty) && !StorageDevice.IsValueValid(["sd", "usb"])) 
            {
                return ValidationResult.Error($"Storage device must be 'sd' or 'usb'.");
            }
            return base.Validate();
        }
    }
}
