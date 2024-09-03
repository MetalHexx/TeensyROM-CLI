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

    internal class PlayerCommand(IPlayerService player, ITypeResolver resolver) : AsyncCommand<PlayerCommandSettings>
    {
        public override Task<int> ExecuteAsync(CommandContext context, PlayerCommandSettings settings)
        {
            string choice;

            do
            {
                choice = string.Empty;

                var playerSettings = player.GetPlayerSettings();

                var mode = playerSettings.PlayMode is PlayMode.CurrentDirectory
                    ? "Current Directory"
                    : "Random";

                RadHelper.WriteDynamicTable(["Player Settings", "Value", "Description"],
                [
                    ["Play State", playerSettings.PlayState.ToString(), "The current known state of the player."],
                    ["Play Mode", mode, "Next file launch is random or next in current directory."],
                    ["Current Directory", playerSettings.CurrentItem?.Path.GetUnixParentPath() ?? "---", "The directory of the currently playing file."],
                    ["Current File", playerSettings.CurrentItem?.Name ?? "---", "The currently playing file."],
                    ["Filter Type", playerSettings.FilterType.ToString(), "This is the current filter."],
                    ["Play Timer", playerSettings.PlayTimer?.ToString() ?? "---", "Optional play timer used for Games, Images and SIDs." ],
                    ["Override SID Timer", playerSettings.SidTimerOverride.ToString(), "Override the SID length with the Play Timer"],
                ]);

                choice = PromptHelper.ChoicePrompt("Player", new List<string> { "Next", "Mode", "Current Filter", "Play Timer", "Refresh Player", "Leave Player" });

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

                    case "Play Mode":
                        var playMode = PromptHelper.ChoicePrompt("Play Mode", ["Random", "Current Directory"]) switch
                        {
                            "Random" => PlayMode.Random,
                            "Current Directory" => PlayMode.CurrentDirectory,
                            _ => PlayMode.Random
                        };
                        player.SetPlayMode(playMode);
                        break;

                    case "Current Filter":
                        var filter = CommandHelper.PromptForFilterType("");
                        player.SetFilter(filter);
                        break;

                    case "Play Timer":
                        var timer = CommandHelper.PromptGameTimer();
                        var overrideSidTIme = PromptHelper.Confirm("Override SID Time", false);
                        player.OverrideSidTIme(overrideSidTIme);
                        player.SetStreamTime(timer);
                        break;
                }

            } while (choice != "Leave Player");

            return Task.FromResult(0);
        }
    }
}
