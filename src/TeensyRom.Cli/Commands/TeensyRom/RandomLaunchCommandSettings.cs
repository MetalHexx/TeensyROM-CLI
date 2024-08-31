using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using TeensyRom.Cli.Commands.Common;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class RandomLaunchCommandSettings : CommandSettings, ITeensyCommandSettings, IRequiresConnection
    {
        [Description("Storage device of file to launch. (sd or usb)")]
        [CommandOption("-s|--storage")]
        public string StorageDevice { get; set; } = string.Empty;

        [Description("The filter to use (all, music, games, images).")]
        [CommandOption("-f|--filter")]
        public string Filter { get; set; } = string.Empty;

        [Description("The directory to use.")]
        [CommandOption("-d|--directory")]
        public string Directory { get; set; } = string.Empty;

        public void ClearSettings()
        {
            StorageDevice = string.Empty;
            Filter = string.Empty;
            Directory = string.Empty;
        }

        public override ValidationResult Validate()
        {
            var storageValidation = CommandValidations.ValidateStorageDevice(StorageDevice);

            if (!storageValidation.Successful) return storageValidation;
            
            var directoryValidation = CommandValidations.ValidateUnixPath(Directory);
            
            if (!directoryValidation.Successful) return directoryValidation;

            var filterValidation = CommandValidations.ValidateFilter(Filter);
            
            if (!filterValidation.Successful) return filterValidation;

            return base.Validate();
        }
    }
}
