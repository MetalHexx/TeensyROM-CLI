using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Commands.File.LaunchFile;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Storage.Entities;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class LaunchFileConsoleCommand(IMediator mediator, ISerialStateContext serial, ILoggingService logService, IPlayerService player) : AsyncCommand<LaunchFileCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, LaunchFileCommandSettings settings)
        {
            player.StopContinuousPlay();

            RadHelper.WriteMenu("Launch File", "Launch a specific file.",
            [
               "If the file is a SID, on completion, a random SID will be played next.",
               "Parent directories will be cached on first play.",               
            ]);

            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice);
            settings.FilePath = CommandHelper.PromptForFilePath(settings.FilePath);

            if (!settings.ValidateSettings()) return -1;

            await player.LaunchItem(storageType, settings.FilePath);

            AnsiConsole.WriteLine();
            return 0;
        }
    }
}