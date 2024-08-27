using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Commands.File.LaunchFile;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Storage.Entities;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class LaunchFileConsoleCommand(IMediator mediator, ISerialStateContext serial, ILoggingService logService) : AsyncCommand<LaunchFileCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, LaunchFileCommandSettings settings)
        {
            var connectionState = serial.CurrentState.FirstAsync().Wait();

            
            if (connectionState is not SerialConnectedState)
            {
                RadHelper.WriteLine("Connecting to TeensyROM...");
                AnsiConsole.WriteLine();
                serial.OpenPort();
                RadHelper.WriteLine("Connection Successful!");
                RadHelper.WriteLine();
            }

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
                settings.FilePath = PromptHelper.DefaultValueTextPrompt("File Path:", 2, "/music/MUSICIANS/T/Tjelta_Geir/Artillery.sid");
                RadHelper.WriteLine();
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