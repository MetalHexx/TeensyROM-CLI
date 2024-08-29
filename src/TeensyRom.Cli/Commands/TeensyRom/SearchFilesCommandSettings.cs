using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Common;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class SearchFilesCommandSettings : CommandSettings 
    {
        [Description("Storage device of file to launch. (sd or usb)")]
        [CommandOption("-s|--storage")]
        public string StorageDevice { get; set; } = string.Empty;

        [Description("Search terms.  Use quotes.  Ex: \"iron maiden aces high\"")]
        [CommandOption("-t|--terms")]
        public string Terms { get; set; } = string.Empty;

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
