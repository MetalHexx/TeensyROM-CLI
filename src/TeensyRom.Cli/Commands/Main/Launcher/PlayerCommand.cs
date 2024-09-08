using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Common;
using TeensyRom.Core.Player;
using TeensyRom.Core.Settings;

namespace TeensyRom.Cli.Commands.Main.Launcher
{
    internal class PlayerSettings : LaunchSettings 
    {
        public new void ClearSettings() { }
    }

    internal class PlayerCommand(IPlayerService player) : Command<PlayerSettings>
    {
        public override int Execute(CommandContext context, PlayerSettings settings)
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

                //TODO: Add ability to turn help on and off and add a setting.

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

                choice = PromptHelper.ChoicePrompt("Player Controls", ["Next", "Previous", "Mode", "Filter", "Timer", "Pin Directory", "Refresh Menu", "Back"]);

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

                    case "Pin Directory":
                        var path = CommandHelper.PromptForDirectoryPath("");

                        if (!path.IsValidUnixPath())
                        {
                            RadHelper.WriteError("Not a valid Unix path");
                            break;
                        }
                        player.SetDirectoryScope(path);
                        break;

                    case "Timer":
                        var timer = CommandHelper.PromptGameTimer("");
                        var sidTimerSelection = CommandHelper.PromptSidTimer("");
                        player.SetSidTimer(sidTimerSelection);
                        player.SetStreamTime(timer);
                        break;
                }

            } while (choice != "Back");

            AnsiConsole.WriteLine();
            return 0;
        }
    }
}
