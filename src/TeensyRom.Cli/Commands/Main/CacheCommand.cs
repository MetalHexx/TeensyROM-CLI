using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using TeensyRom.Cli.Helpers;
using TeensyRom.Cli.Services;
using TeensyRom.Core.Commands;
using TeensyRom.Core.Common;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.Main
{
    internal class CacheSettings : CommandSettings, ITeensyCommandSettings, IRequiresConnection
    {
        [Description("Storage files to cache files for. (sd or usb)")]
        [CommandOption("-s|--storage")]
        public string StorageDevice { get; set; } = string.Empty;

        [Description("Specific TeensyROM path to cache")]
        [CommandOption("-p|--path")]
        public string Path { get; set; } = string.Empty;

        public static string Example => "cache -s sd -p /music/ ";
        public static string Description => "Caches all the files on your storage device to enhance search and streaming features.";

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


    internal class CacheCommand(ISerialStateContext serial, ICachedStorageService storage, IPlayerService player, IMediator mediator, ISettingsService settingsService, ICliLoggingService logService) : AsyncCommand<CacheSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, CacheSettings settings)
        {
            player.StopStream();

            RadHelper.WriteMenu("Cache Files", "Launch random files from storage and discover something new.",
            [
               "Caching enables search and randomization features.",
               "Caching will increase overall performance and stability.",
               "Cache your files when you make changes to storage outside of this app.",
            ]);

            var globalSettings = settingsService.GetSettings();

            settings.StorageDevice = string.IsNullOrWhiteSpace(settings.StorageDevice)
                ? globalSettings.StorageType.ToString()
                : settings.StorageDevice;

            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice, promptAlways: true);

            settings.Path = CommandHelper.PromptForDirectoryPath(settings.Path, "/");

            storage.SwitchStorage(storageType);

            if (!settings.ValidateSettings()) return -1;

            RadHelper.WriteTitle("Resetting TR before caching.  Don't mess with your C64 until caching completed.");
            AnsiConsole.WriteLine();

            await mediator.Send(new ResetCommand());

            RadHelper.WriteLine($"Caching files for {storageType}...");
            AnsiConsole.WriteLine();

            var loggingEnabled = logService.Enabled;

            logService.Enabled = true;

            try
            {
                await storage.CacheAll(settings.Path);
            }
            catch (TeensyException ex)
            {
                RadHelper.WriteError(ex.Message);
                AnsiConsole.WriteLine();
                RadHelper.WriteLine("Problem Caching: Waiting 10 seconds for TeensyROM to reset");
                AnsiConsole.WriteLine();
                await Task.Delay(10000);
                await storage.CacheAll(settings.Path);
            }
            logService.Enabled = loggingEnabled;

            return 0;
        }
    }
}