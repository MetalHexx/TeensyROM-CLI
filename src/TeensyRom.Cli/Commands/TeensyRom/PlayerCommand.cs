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
                RadHelper.WriteMenu("Player", "Control the current play stream using the modes listed below");

                choice = string.Empty;

                var playerSettings = player.GetPlayerSettings();

                var mode = playerSettings.PlayMode is PlayMode.CurrentDirectory
                    ? "Current Directory"
                    : "Random";

                var sidTimer = playerSettings.SidTimer is SidTimer.SongLength
                    ? "Song Length"
                    : "Timer Override";

                RadHelper.WriteDynamicTable(["Player Settings", "Value", "Description"],
                [
                    ["State", playerSettings.PlayState.ToString(), "The current known state of the player."],
                    ["Mode", mode, "Next file launch is random or next in current directory."],
                    ["Directory", playerSettings.CurrentItem?.Path.GetUnixParentPath() ?? "---", "The directory of the currently playing file."],
                    ["File", playerSettings.CurrentItem?.Name ?? "---", "The currently playing file."],
                    ["Filter", playerSettings.FilterType.ToString(), "This is the current filter."],
                    ["Timer", playerSettings.PlayTimer?.ToString() ?? "---", "Optional play timer used for Games, Images and SIDs." ],
                    ["SID Timer", sidTimer, "Determines if song length is used or overidden by the set timer."],
                ]);

                choice = PromptHelper.ChoicePrompt("Player", new List<string> { "Next", "Mode", "Filter", "Timer", "Refresh", "Leave Player" });

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
                        var playMode = PromptHelper.ChoicePrompt("Play Mode", ["Random", "Current Directory"]) switch
                        {
                            "Random" => PlayMode.Random,
                            "Current Directory" => PlayMode.CurrentDirectory,
                            _ => PlayMode.Random
                        };
                        player.SetPlayMode(playMode);
                        break;

                    case "Filter":
                        var filter = CommandHelper.PromptForFilterType("");
                        player.SetFilter(filter);
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
