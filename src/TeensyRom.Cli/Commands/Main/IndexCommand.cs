using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Helpers;
using TeensyRom.Cli.Services;
using TeensyRom.Cli.Common;
using TeensyRom.Cli.Logging;
using TeensyRom.Cli.Settings;

namespace TeensyRom.Cli.Commands.Main
{
    internal class IndexSettings : CommandSettings, IClearableSettings
    {
        [Description("Storage files to index. (sd or usb)")]
        [CommandOption("-s|--storage")]
        public string StorageDevice { get; set; } = string.Empty;

        [Description("Specific TeensyROM path to index")]
        [CommandOption("-p|--path")]
        public string Path { get; set; } = string.Empty;

        public static string Example => "index -s sd -p /music/ ";
        public static string Description => "Indexes all the files on your storage device to enhance search and streaming features.";

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


    internal class IndexCommand(IStorageService storage, IPlayerService player, ISettingsService settingsService, IDeviceLogStream deviceLog) : AsyncCommand<IndexSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, IndexSettings settings)
        {
            player.StopStream();

            RadHelper.WriteMenu("Index Files", "Indexes your storage for enhanced an enhanced experience.",
            [
               "Indexing enables search and randomization features.",
               "Indexing will increase overall performance and stability.",               
               "Index your files when you make changes to storage outside of this app.",
               "Specifying a directory will refresh the index for that directory and its subdirectories.",
            ]);

            var globalSettings = settingsService.GetSettings();

            settings.StorageDevice = string.IsNullOrWhiteSpace(settings.StorageDevice)
                ? globalSettings.StorageType.ToString()
                : settings.StorageDevice;

            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice, promptAlways: true);

            settings.Path = CommandHelper.PromptForPath(settings.Path, "Directory to Index:", "/");

            storage.SwitchStorage(storageType);

            if (!settings.ValidateSettings()) return -1;

            RadHelper.WriteLine($"Caching files for {storageType}...");
            AnsiConsole.WriteLine();

            var progressLog = await FollowIndexingAsync();

            try
            {
                await storage.Index(settings.Path);
            }
            finally
            {
                if (progressLog is not null) await progressLog.DisposeAsync();
            }
            AnsiConsole.WriteLine("Setting globalSettings.HasIndexed to true");
            globalSettings.HasIndexed = true;
            AnsiConsole.WriteLine("Calling SaveSettings");
            settingsService.SaveSettings(globalSettings);
            AnsiConsole.WriteLine("Completed SaveSettings");

            return 0;
        }

        /// <summary>
        /// tr-web does the indexing and logs its progress to its own log stream. Shows just those lines, whether or not
        /// debug logs are on. Returns null, after saying so, when the stream can't be followed: indexing still runs.
        /// </summary>
        private async Task<IAsyncDisposable?> FollowIndexingAsync()
        {
            try
            {
                return await deviceLog.FollowAsync(WriteIndexingLine);
            }
            catch (TeensyApiException ex)
            {
                RadHelper.WriteError($"Indexing will run without showing its progress. {ex.Message}");
                return null;
            }
        }

        private static void WriteIndexingLine(string line)
        {
            if (!line.Contains("Indexing", StringComparison.Ordinal)) return;

            AnsiConsole.MarkupLine($"[{LogConstants.ExternalColor}]{line.EscapeBrackets()}[/]");
        }
    }
}