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

            RadHelper.WriteMenu("Random Stream", "Randomly streams files from the specified directory. Includes subdirectories.",
            [
               "SIDs will play continuously.",
               "Games will stop continuous play (for now).",
               "Cache your storage to increase randomization variety"
            ]);
            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice);
            settings.Directory = CommandHelper.PromptForDirectoryPath(settings.Directory, "/");
            var filterType = CommandHelper.PromptForFilterType(settings.Filter);

            if(!settings.ValidateSettings()) return -1;

            await player.PlayRandom(storageType, settings.Directory, filterType);

            return 0;
        }
    }
}