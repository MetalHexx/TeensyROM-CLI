using Spectre.Console;
using Spectre.Console.Cli;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Common;
using TeensyRom.Core.Player;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class PlayerCommandSettings : CommandSettings, ITeensyCommandSettings, IRequiresConnection
    {
        public void ClearSettings() { }
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
                    ["Next", "---", "Lauches the next file based on the mode and position in history."],
                    ["Mode", mode, "Next file is random, from search results, or next in the current directory."],
                    ["Filter", playerSettings.FilterType.ToString(), "Filter used to determine next random file."],
                    ["Random Scope", playerSettings.ScopeDirectory, "Path to scope random selection.  Includes subdirs."],
                    ["Timer", playerSettings.PlayTimer?.ToString() ?? "---", "Timer used for Games, Images and SIDs." ],
                    ["SID Timer", sidTimer, "Use song length or override w/timer."],
                    ["Current Directory", playerSettings.CurrentItem?.Path.GetUnixParentPath() ?? "---", "Directory of the playing file."],                    
                    ["File", playerSettings.CurrentItem?.Name ?? "---", "The file playing."],                                        
                ]);

                choice = PromptHelper.ChoicePrompt("Player Controls", new List<string> { "Next", "Mode", "Filter", "Timer", "Scope", "Refresh Menu", "Leave Player" });

                switch (choice)
                {
                    case "Next":
                        player.PlayNext();
                        break;

                    case "Previous":
                        //player.LaunchItem(playerSettings.StorageType, playerSettings.FilterType, playerSettings.PlayMode, playerSettings.PlayState);
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

                    case "Scope":
                        var path = CommandHelper.PromptForDirectoryPath("");

                        if (!path.IsValidUnixPath()) 
                        {
                            RadHelper.WriteError("Not a valid Unix path");
                            break;
                        }
                        player.SetScope(path);
                        break;

                    case "Timer":
                        var timer = CommandHelper.PromptGameTimer();
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
