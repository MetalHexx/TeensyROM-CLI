using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class SettingsCommand(ISerialStateContext serial, IPlayerService player, ISettingsService settingsService) : AsyncCommand<SettingsCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, SettingsCommandSettings _)
        {
            player.StopStream();

            var choice = string.Empty;

            do 
            {
                RadHelper.WriteMenu("Settings", "Change your global settings.", []);

                var settings = settingsService.GetSettings();

                RadHelper.WriteDynamicTable(["Setting", "Value", "Description"],
                [
                    ["Storage Device", settings.StorageType.ToString(), "Default value to use for your selected storage device"],
                    ["Always Prompt Storage", settings.AlwaysPromptStorage.ToString(), "Determines if you're always prompted to select the storage device."],
                    ["Filter", settings.StartupFilter.ToString(), "Default filter to use for streams."],
                ]);

                choice = PromptHelper.ChoicePrompt("Settings", new List<string> { "Storage Device", "Always Prompt Storage", "Default Filter",  "Quit" });

                switch (choice)
                {
                    case "Storage Device":
                        settings.StorageType = CommandHelper.PromptForStorageType(settings.StorageType.ToString(), true);
                        break;

                    case "Always Prompt Storage":
                        settings.AlwaysPromptStorage = PromptHelper.Confirm("Always Prompt Storage", settings.AlwaysPromptStorage);
                        break;

                    case "Default Filter":
                        settings.StartupFilter = CommandHelper.PromptForFilterType("");
                        break;
                }
                settingsService.SaveSettings(settings);

            } while (choice != "Quit");

            RadHelper.WriteLine();
            return 0;
        }
    }
}