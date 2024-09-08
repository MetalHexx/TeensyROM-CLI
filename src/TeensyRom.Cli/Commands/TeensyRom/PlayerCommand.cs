using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Common;
using TeensyRom.Core.Player;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class PlayerCommandSettings : CommandSettings, ITeensyCommandSettings, IRequiresConnection
    {
        [Description("Timer used for Games, Images and SIDs. (No, 3m, 5m, 15m, 30m, 1h, Turbo)")]
        [CommandOption("-t|--timer")]
        public string Timer { get; set; } = string.Empty;

        public void ClearSettings() 
        {
            Timer = string.Empty;
        }

        public override ValidationResult Validate()
        {
            var timerValidation = Timer.ValidateTimer();
            if (!timerValidation.Successful) return timerValidation;

            return base.Validate();
        }
    }

    internal class PlayerCommand(IPlayerService player) : AsyncCommand<PlayerCommandSettings>
    {
        public override Task<int> ExecuteAsync(CommandContext context, PlayerCommandSettings settings)
        {
            string choice;

            do
            {
                RadHelper.WriteMenu("Player Controls", "Use the options below to control behavior of the file launch stream.");

                choice = string.Empty;

                var playerSettings = player.GetPlayerSettings();

                var mode = playerSettings.PlayMode switch 
                {
                    PlayMode.CurrentDirectory => "Current Directory",
                    PlayMode.Random => "Random",
                    PlayMode.Search => "Search Results",
                    _ => "Random"
                };

                var sidTimer = playerSettings.SidTimer is SidTimer.SongLength
                    ? "Song Length"
                    : "Timer Override";

                RadHelper.WriteDynamicTable(["Setting / Action", "Value", "Description"],
                [
                    ["Current Directory", playerSettings.CurrentItem?.Path.GetUnixParentPath() ?? "---", "Directory of the playing file."],
                    ["File", playerSettings.CurrentItem?.Name ?? "---", "The file playing."],
                    ["Storage Device", playerSettings.StorageType.ToString(), "The active storage device."],
                    ["Filter", playerSettings.FilterType.ToString(), "The type of files that will be played."],
                    ["Mode", mode, "The source of the files available to play."],                                        
                    ["Search Query", playerSettings.SearchQuery ?? "---", "The current active search query."],
                    ["Pinned Directory", playerSettings.ScopePath, "Random files will only be played from this directory and subdirs."],
                    ["Timer", playerSettings.PlayTimer?.ToString() ?? "---", "Timer used for Games, Images and SIDs." ],
                    ["SID Timer", sidTimer, "Use song length or override w/timer."],
                                                            
                ]);

                choice = PromptHelper.ChoicePrompt("Player Controls", ["Next", "Previous", "Mode", "Filter", "Timer", "Pinned Directory", "Refresh Menu", "Leave Player"]);

                switch (choice)
                {
                    case "Next":
                        player.PlayNext();
                        break;

                    case "Previous":
                        player.PlayPrevious();
                        break;

                    case "Pause/Stop":
                        break;

                    case "Mode":
                        var playMode = PromptHelper.ChoicePrompt("Play Mode", ["Random", "Current Directory"]);
                        if (playMode == "Random")
                        {
                            player.SetRandomMode(playerSettings.ScopePath);
                            break;
                        }
                        var directoryPath = playerSettings.CurrentItem is null 
                            ? "/" 
                            : playerSettings.CurrentItem.Path.GetUnixParentPath();

                        player.SetDirectoryMode(directoryPath);
                        break;

                    case "Filter":
                        var filter = CommandHelper.PromptForFilterType("");
                        player.SetFilter(filter);
                        break;

                    case "Pinned Directory":
                        var path = CommandHelper.PromptForDirectoryPath("");

                        if (!path.IsValidUnixPath()) 
                        {
                            RadHelper.WriteError("Not a valid Unix path");
                            break;
                        }
                        player.SetDirectoryScope(path);
                        break;

                    case "Timer":
                        var timer = CommandHelper.PromptGameTimer(settings.Timer);
                        var sidTimerSelection = CommandHelper.PromptSidTimer("");
                        player.SetSidTimer(sidTimerSelection);
                        player.SetStreamTime(timer);
                        break;
                }

            } while (choice != "Leave Player");

            return Task.FromResult(0);
        }
    }
}
