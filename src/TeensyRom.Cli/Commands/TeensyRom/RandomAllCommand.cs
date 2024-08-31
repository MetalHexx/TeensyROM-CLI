using Spectre.Console.Cli;
using System.Numerics;
using System.Reactive.Linq;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class RandomAllCommand(ISerialStateContext serial, ICachedStorageService storage, ILoggingService logService, ISettingsService settingsService, IPlayerService player) : AsyncCommand<RandomAllCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, RandomAllCommandSettings settings)
        {
            player.StopContinuousPlay();

            RadHelper.WriteMenu("Launch Random", "Launch random files from storage and discover something new.",
            [
               "Filter will limit the file types selected.",
               "SIDs will play continuously.",
               "Games will stop continuous play.",
               "For best result, cache your storage."
            ]);
            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice);
            settings.Directory = CommandHelper.PromptForDirectoryPath(settings.Directory, "/test-cache");
            var filterType = CommandHelper.PromptForFilterType(settings.Filter);

            if(!settings.ValidateSettings()) return -1;

            await player.PlayRandom(storageType, settings.Directory, filterType);

            return 0;
        }
    }
}