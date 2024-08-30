using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class CacheCommand(ISerialStateContext serial, ICachedStorageService storage) : AsyncCommand<CacheCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, CacheCommandSettings settings)
        {
            AnsiConsole.WriteLine("DEBUG: Entered - CacheCommand!");            
            var connectionState = await serial.CurrentState.FirstAsync();
            
            if (connectionState is not SerialConnectedState)
            {
                RadHelper.WriteLine("Connecting to TeensyROM...");
                AnsiConsole.WriteLine();
                serial.OpenPort();
                RadHelper.WriteLine("Connection Successful!");
                RadHelper.WriteLine();
            }

            RadHelper.WriteHorizonalRule("Search Files", Justify.Left);

            AnsiConsole.MarkupLine($"{RadHelper.AddSecondaryColor("Tips:")}");
            AnsiConsole.MarkupLine($"{RadHelper.AddPrimaryColor("- Caching enables search and randomization features.  Do it! :)")}");
            AnsiConsole.MarkupLine($"{RadHelper.AddPrimaryColor("- Caching will increase overall performance and stability.")}");
            AnsiConsole.MarkupLine($"{RadHelper.AddPrimaryColor("- Cache your files when you make changes to storage outside of this app.")}");

            if (settings.StorageDevice.Equals(string.Empty))
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
            if(settings.StorageDevice.Equals(string.Empty))
            {
                RadHelper.WriteError("Storage device must be 'sd' or 'usb'.");
                return -1;
            }
            RadHelper.WriteLine($"Caching files for {storageType}...");

            settings.Path = PromptHelper.DefaultValueTextPrompt("Path to Cache:", 2, "/");

            var validation = settings.Validate();

            if (!validation.Successful) 
            {
                RadHelper.WriteError(validation?.Message ?? "Validation error");
                return 0;
            }

            await storage.CacheAll(settings.Path);

            return 0;
        }
    }
}