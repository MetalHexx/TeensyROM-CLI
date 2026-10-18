using Spectre.Console;
using Spectre.Console.Cli;
using TeensyRom.Cli.Services;
using TeensyRom.Cli.Player;
using TeensyRom.Cli.Settings;
using TeensyRom.Cli.Storage.Entities;

namespace TeensyRom.Cli.Helpers
{
    internal static class SpecialChars
    {
        public const string BulletPoint = "\u2022";
    }
    internal static class CommandHelper
    {
        public static TCommand? ResolveCommand<TCommand>(this ITypeResolver resolver) where TCommand : class
        {
            var command = resolver.Resolve(typeof(TCommand)) as TCommand;

            if (command is null)
            {
                RadHelper.WriteError($"Strange. {typeof(TCommand).Name} command was not found.");
                return command;
            }

            return command;
        }

        public static void DisplayCommandTitle(string message)
        {
            RadHelper.WriteHorizonalRule(message, Justify.Left);
        }

        public static TimeSpan? PromptGameTimer(string value)
        {
            if (value.Equals(string.Empty))
            {
                value = PromptHelper.ChoicePrompt("Game/Image Timer", ["None", "3m", "5m", "15m", "30m", "1h", "1m", "30s","15s", "5s"]);
                RadHelper.WriteLine();
            }
            return value switch
            {
                "3m" => TimeSpan.FromMinutes(3),
                "5m" => TimeSpan.FromMinutes(5),
                "15m" => TimeSpan.FromMinutes(15),
                "30m" => TimeSpan.FromMinutes(30),
                "1h" => TimeSpan.FromHours(1),
                "1m" => TimeSpan.FromMinutes(1),
                "30s" => TimeSpan.FromSeconds(30),
                "15s" => TimeSpan.FromSeconds(15),
                "5s" => TimeSpan.FromSeconds(5),
                _ => null
            };
        }

        public static TeensyStorageType PromptForStorageType(string value, bool promptAlways = false)
        {
            if (promptAlways is true || value.Equals(string.Empty))
            {
                if (value == "USB")
                {
                    value = PromptHelper.ChoicePrompt("Storage Type", new List<string> { "USB", "SD" });
                }
                else
                {
                    value = PromptHelper.ChoicePrompt("Storage Type", new List<string> { "SD", "USB" });
                }
                RadHelper.WriteLine();
            }

            return value switch
            {
                "SD" => TeensyStorageType.SD,
                "USB" => TeensyStorageType.USB,
                _ => TeensyStorageType.SD,
            };
        }

        public static TeensyFilterType PromptForFilterType(string value)
        {
            if (value.Equals(string.Empty))
            {
                value = PromptHelper.ChoicePrompt("Filter", new List<string> { "All", "Music", "Games", "Images" });
                RadHelper.WriteLine();
            }

            return value switch
            {
                "All" => TeensyFilterType.All,
                "Music" => TeensyFilterType.Music,
                "Games" => TeensyFilterType.Games,
                "Images" => TeensyFilterType.Images,
                _ => TeensyFilterType.All,
            };
        }

        public static SidTimer PromptSidTimer(string value)
        {
            if (value.Equals(string.Empty))
            {
                value = PromptHelper.ChoicePrompt("Sid Timer", new List<string> { "Song Length", "Timer Override" });
                RadHelper.WriteLine();
            }

            return value switch
            {
                "Song Length" => SidTimer.SongLength,
                "length" => SidTimer.SongLength,
                "override" => SidTimer.TimerOverride,
                "Timer Override" => SidTimer.TimerOverride,
                _ => SidTimer.SongLength,
            };
        }

        public static string PromptForFilePath(string value, string defaultValue = "/music/MUSICIANS/T/Tjelta_Geir/Artillery.sid")
        {
            return PromptForPath(value, "File Path:", defaultValue);
        }

        public static string PromptForDirectoryPath(string value, string defaultValue = "/")
        {
            return PromptForPath(value, "Directory:", defaultValue);
        }

        public static string PromptForPath(string value, string title, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                value = PromptHelper.DefaultValueTextPrompt(title, 2, defaultValue);
                RadHelper.WriteLine();
            }
            return value;
        }

        /// <summary>
        /// False when the launch that was just attempted failed. The player has already said why, so the caller
        /// goes back to its menu instead of opening an empty player.
        /// </summary>
        public static bool IsPlaying(this IPlayerService player) => player.GetState().PlayState is PlayState.Playing;

        public static bool ValidateSettings(this CommandSettings settings)
        {
            var validation = settings.Validate();

            if (!validation.Successful)
            {
                RadHelper.WriteError(validation?.Message ?? "Validation error");
                RadHelper.WriteLine();
                return false;
            }
            return true;
        }
    }
}
