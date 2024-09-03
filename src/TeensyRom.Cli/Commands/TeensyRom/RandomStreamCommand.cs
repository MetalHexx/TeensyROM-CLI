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
    internal class RandomStreamCommand(ISerialStateContext serial, ICachedStorageService storage, ILoggingService logService, ISettingsService settingsService, IPlayerService player, ITypeResolver resolver) : AsyncCommand<RandomStreamCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, RandomStreamCommandSettings settings)
        {
            var playerCommand = resolver.Resolve(typeof(PlayerCommand)) as PlayerCommand;

            player.StopStream();

            RadHelper.WriteMenu("Random Stream", "Randomly streams files from the specified directory and it's subdirectories.",
            [
               "SIDs will stream continuously on play length or set timer.",
               "Games, images and demos can also be streamed with a timer.",
               "Cache files to fatten your stream. ;)"
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
            settings.Directory = CommandHelper.PromptForDirectoryPath(settings.Directory, "/");
            var filterType = CommandHelper.PromptForFilterType(settings.Filter);

            if (filterType is TeensyFilterType.All or TeensyFilterType.Games or TeensyFilterType.Images)
            {                   
                player.SetStreamTime(CommandHelper.PromptGameTimer());

                if (filterType is TeensyFilterType.All)
                {
                    var sidTimer = CommandHelper.PromptSidTimer("");
                    player.SetSidTimer(sidTimer);
                }
            }

            if (!settings.ValidateSettings()) return -1;

            await player.PlayRandom(storageType, settings.Directory, filterType);

            if(playerCommand is not null) 
            {
                await playerCommand.ExecuteAsync(context, new());
            }
            return 0;
        }
    }
}