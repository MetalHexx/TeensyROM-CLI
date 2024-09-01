using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Numerics;
using System.Reactive.Linq;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Commands;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class CacheCommand(ISerialStateContext serial, ICachedStorageService storage, IPlayerService player, IMediator mediator) : AsyncCommand<CacheCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, CacheCommandSettings settings)
        {
            player.StopStream();

            RadHelper.WriteMenu("Cache Files", "Launch random files from storage and discover something new.",
            [
               "Caching enables search and randomization features.",
               "Caching will increase overall performance and stability.",
               "Cache your files when you make changes to storage outside of this app.",
            ]);

            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice);
            settings.Path = CommandHelper.PromptForDirectoryPath(settings.Path, "/");

            if (!settings.ValidateSettings()) return -1;

            RadHelper.WriteTitle("Resetting TR before caching.  Don't mess with your C64 until caching completed.");
            AnsiConsole.WriteLine();

            await mediator.Send(new ResetCommand());

            RadHelper.WriteLine($"Caching files for {storageType}...");
            AnsiConsole.WriteLine();

            await storage.CacheAll(settings.Path);

            return 0;
        }
    }
}