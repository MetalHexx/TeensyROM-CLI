using Spectre.Console;
using Spectre.Console.Cli;
using TeensyRom.Cli.Helpers;
using TeensyRom.Cli.Services;
using TeensyRom.Core.Common;
using TeensyRom.Core.Player;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.Main.Launcher
{
    internal class PlayerSettings : LaunchSettings, ITeensyCommandSettings, IRequiresConnection
    {
        public new void ClearSettings() { }
    }

    internal class PlayerCommand : Command<PlayerSettings>
    {
        private readonly IPlayerService _player;
        private readonly ICachedStorageService _storage;

        public PlayerCommand(IPlayerService player, ICachedStorageService storage)
        {
            _player = player;
            _storage = storage;
            _player.FileLaunched.Subscribe(DisplayLaunchedFile);
        }
        public override int Execute(CommandContext context, PlayerSettings settings)
        {
            string choice;

            RadHelper.WriteMenu("Stream Player", "There are many paths your stream can take...");
            WriteHelp();

            do
            {
                var playerSettings = _player.GetPlayerSettings();

                choice = string.Empty;

                choice = PromptHelper.ChoicePrompt("Player Controls", ["Next", "Previous", "Favorite", "Mode", "Filter", "Timer", "Pin Directory", "Help", "Back"]);

                switch (choice)
                {
                    case "Next":
                        _player.PlayNext();
                        break;

                    case "Previous":
                        _player.PlayPrevious();
                        break;

                    case "Favorite":
                        HandleFavorite(playerSettings);
                        break;

                    case "Mode":
                        var playMode = PromptHelper.ChoicePrompt("Play Mode", ["Random", "Current Directory"]);
                        if (playMode == "Random")
                        {
                            _player.SetRandomMode(playerSettings.ScopePath);
                            break;
                        }
                        var directoryPath = playerSettings.CurrentItem is null
                        ? "/"
                            : playerSettings.CurrentItem.Path.GetUnixParentPath();

                        _player.SetDirectoryMode(directoryPath);
                        break;

                    case "Filter":
                        var filter = CommandHelper.PromptForFilterType("");
                        _player.SetFilter(filter);
                        break;

                    case "Pin Directory":
                        var path = CommandHelper.PromptForDirectoryPath("");

                        if (!path.IsValidUnixPath())
                        {
                            RadHelper.WriteError("Not a valid Unix path");
                            break;
                        }
                        _player.SetDirectoryScope(path);
                        break;

                    case "Timer":
                        var timer = CommandHelper.PromptGameTimer("");
                        var sidTimerSelection = CommandHelper.PromptSidTimer("");
                        _player.SetSidTimer(sidTimerSelection);
                        _player.SetStreamTime(timer);
                        break;

                    case "Help":
                        AnsiConsole.WriteLine(RadHelper.ClearHack);
                        WriteHelp();
                        break;
                }

            } while (choice != "Back");

            return 0;
        }

        private void HandleFavorite(Services.PlayerSettings playerSettings)
        {
            AnsiConsole.WriteLine(RadHelper.ClearHack);

            if (playerSettings.CurrentItem is null)
            {
                RadHelper.WriteLine("No file is currently playing");
                return;
            }
            if(playerSettings.CurrentItem.IsFavorite)
            {
                var shouldRemove = PromptHelper.Confirm("Remove Favorite?", false);

                if (shouldRemove)
                {
                    RadHelper.WriteLine(RadHelper.ClearHack);
                    _storage.RemoveFavorite(playerSettings.CurrentItem);
                }
                return;
            }
            _storage.SaveFavorite(playerSettings.CurrentItem);
        }

        private void WriteHelp()
        {
            var playerSettings = _player.GetPlayerSettings();

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

            //AnsiConsole.WriteLine();
            RadHelper.WriteDynamicTable(["Setting", "Current Value", "Description"],
            [
                ["Storage Device", playerSettings.StorageType.ToString(), "The storage device the file is stored on."],
                ["Current Directory", playerSettings.CurrentItem?.Path.GetUnixParentPath() ?? "---", "Directory of the playing file."],
                ["File", playerSettings.CurrentItem?.Name ?? "---", "The current file playing."],
                ["Mode", mode, "Play random or stick to a specific directory."],                
                ["Filter", playerSettings.FilterType.ToString(), "The types of files that will be streamed."],
                ["Pinned Directory", playerSettings.ScopePath, "Random mode will launch from this directory and subdirs."],
                ["Search Query", playerSettings.SearchQuery ?? "---", "The current search query your stream is using."],                
                ["Stream Timer", playerSettings.PlayTimer?.ToString() ?? "---", "Continuous play timer for Games, Images and SIDs." ],
                ["SID Timer", sidTimer, "SIDs play time is song length or overriden w/timer."]
            ]);
        }

        private void DisplayLaunchedFile(ILaunchableItem item) 
        {
            try
            {
                AnsiConsole.WriteLine(RadHelper.ClearHack);
                var release = string.IsNullOrWhiteSpace(item.ReleaseInfo) ? "Unknown" : item.ReleaseInfo.EscapeBrackets();

                var body = string.Empty;

                if (item is SongItem song)
                {
                    body = $"\r\nCreator: {song.Creator}\r\nRelease: {release}\r\nLength: {song.PlayLength}\r\nClock: {song.Meta1}\r\nSID: {song.Meta2}";
                }
                var isFavorite = item.IsFavorite ? "Yes" : "No";

                body = $"{body}\r\nFile Name: {item.Name}\r\nPath: {item.Path.GetUnixParentPath().EscapeBrackets()}\r\nFavorite: {isFavorite}\r\n";

                var fileInfoPanel = new Panel(body.EscapeBrackets())
                    .Header($" Now Playing: {item.Title.EscapeBrackets()} ".AddHighlights())
                    .PadLeft(3)
                    .BorderColor(RadHelper.Theme.Secondary.Color)
                    .Border(BoxBorder.Rounded)
                    .Expand();

                AnsiConsole.Write(fileInfoPanel);

                if (item is SongItem && !string.IsNullOrWhiteSpace(item.Description))
                {
                    var stilCommentPanel = new Panel($"\r\n{item.Description.EscapeBrackets().Trim()}\r\n")
                    .Header(" SID Comments ".AddSecondaryColor())
                    .PadLeft(3)
                    .BorderColor(RadHelper.Theme.Primary.Color)
                    .Border(BoxBorder.Rounded)
                    .Expand();

                    AnsiConsole.Write(stilCommentPanel);
                }
            }
            catch (Exception ex)
            {
                
                var x = 0;
            }
            
        }
    }
}
