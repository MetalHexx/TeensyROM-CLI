using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class LaunchFileConsoleCommand(IMediator mediator, ISerialStateContext serial, ILoggingService logService, IPlayerService player, ICachedStorageService storage, ISettingsService settingsService) : AsyncCommand<LaunchFileCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, LaunchFileCommandSettings settings)
        {
            player.StopStream();

            RadHelper.WriteMenu("Launch File", "Launch a specific file.",
            [
               "When playing a SID, or you have a Game/Image timer enabled, the next file in the directory will be played.",
               "Parent directory will be cached on first visit.",
            ]);

            var globalSettings = settingsService.GetSettings();

            settings.StorageDevice = string.IsNullOrWhiteSpace(settings.StorageDevice)
                ? globalSettings.StorageType.ToString()
                : settings.StorageDevice;

            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice, promptAlways: globalSettings.AlwaysPromptStorage);

            if (globalSettings.AlwaysPromptStorage)
            {
                storage.SwitchStorage(storageType);
            }

            settings.FilePath = CommandHelper.PromptForFilePath(settings.FilePath);

            if (!settings.ValidateSettings()) return -1;

            await player.LaunchItem(storageType, settings.FilePath);

            AnsiConsole.WriteLine();
            return 0;
        }
    }
}