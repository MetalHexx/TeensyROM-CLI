using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
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

            RadHelper.WriteHorizonalRule("File Launcher", Justify.Left);

            if(settings.StorageDevice.Equals(string.Empty))
            {
                settings.StorageDevice = PromptHelper.ChoicePrompt("Storage Type", ["SD", "USB"]);
                RadHelper.WriteLine();
            }
            var storageType = settings.StorageDevice.ToUpper() switch
            {
                "SD" => TeensyStorageType.SD,
                "USB" => TeensyStorageType.USB,
                _ => TeensyStorageType.SD, 
            };

            if (string.IsNullOrWhiteSpace(settings.FilePath))
            {
                settings.FilePath = PromptHelper.DefaultValueTextPrompt("File Path:", 2, "/test-cache/12_Bars.sid");
                RadHelper.WriteLine();
            }

            var validation = settings.Validate();

            if (!validation.Successful)
            {
                RadHelper.WriteError(validation?.Message ?? "Validation error");
                return 0;
            }

            var fileItem = FileItem.Create(settings.FilePath);

            if (fileItem is ILaunchableItem launchItem)
            {
                var result = await mediator.Send(new LaunchFileCommand(storageType, launchItem));

                if (result.IsSuccess)
                {
                    RadHelper.WriteTitle($"Now Playing: {settings.FilePath}");
                }
                else 
                {
                    RadHelper.WriteError($"Error Launching: {settings.FilePath}");
                }
                AnsiConsole.WriteLine();
                return 0;
            }
            RadHelper.WriteError("File is not launchable.");
            AnsiConsole.WriteLine();
            return 0;
        }
    }
}