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
    internal class RandomStreamCommand(ISerialStateContext serial, ICachedStorageService storage, ILoggingService logService, ISettingsService settingsService, IPlayerService player) : AsyncCommand<RandomStreamCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, RandomStreamCommandSettings settings)
        {
            player.StopStream();

            RadHelper.WriteMenu("Random Stream", "Randomly streams files from the specified directory and it's subdirectories.",
            [
               "SIDs will stream continuously on play length.",
               "Games, images and demos can be streamed with a timer.",
               "Options selected for timers will persist across commands (will move to settings)",
               "Cache files to fatten your stream. ;)"
            ]);
            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice);
            settings.Directory = CommandHelper.PromptForDirectoryPath(settings.Directory, "/");
            var filterType = CommandHelper.PromptForFilterType(settings.Filter);

            if (filterType is TeensyFilterType.All or TeensyFilterType.Games or TeensyFilterType.Images)
            {   
                player.SetStreamTime(CommandHelper.PromptGameTimer());

                if (filterType is TeensyFilterType.All)
                {
                    var overrideSidTIme = PromptHelper.ChoicePrompt("Override SID Time", ["No", "Yes"]) switch
                    {
                        "No" => false,
                        "Yes" => true,
                        _ => false
                    };
                    player.OverrideSidTIme(overrideSidTIme);
                }
            }

            if (!settings.ValidateSettings()) return -1;

            await player.PlayRandom(storageType, settings.Directory, filterType);

            return 0;
        }
    }
}