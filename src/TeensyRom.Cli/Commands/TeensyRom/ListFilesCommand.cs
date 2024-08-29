using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Commands.File.LaunchFile;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class ListFilesCommand(IMediator mediator, ISerialStateContext serial, ICachedStorageService storage, ILoggingService logService, ITypeResolver resolver) : AsyncCommand<ListFilesCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, ListFilesCommandSettings settings)
        {
            var launchFileCommand = resolver.Resolve(typeof(LaunchFileConsoleCommand)) as LaunchFileConsoleCommand;

            var connectionState = await serial.CurrentState.FirstAsync();

            
            if (connectionState is not SerialConnectedState)
            {
                RadHelper.WriteLine("Connecting to TeensyROM...");
                AnsiConsole.WriteLine();
                serial.OpenPort();
                RadHelper.WriteLine("Connection Successful!");
                RadHelper.WriteLine();
            }

            RadHelper.WriteHorizonalRule("List Files", Justify.Left);

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
                settings.FilePath = PromptHelper.DefaultValueTextPrompt("File Path:", 2, "/music/MUSICIANS/T/Tjelta_Geir/");
                RadHelper.WriteLine();
            }
            var cacheItem = await storage.GetDirectory(settings.FilePath);

            var fileName = PromptHelper.ChoicePrompt("Select File", cacheItem.Files.Select(f => f.Name).ToList());
            var file = cacheItem.Files.First(f => f.Name == fileName);

            var launchFileCommandSettings = new LaunchFileCommandSettings
            {
                StorageDevice = settings.StorageDevice,
                FilePath = file.Path
            };

            await launchFileCommand.ExecuteAsync(context, launchFileCommandSettings);

            return 0;
        }
    }
}