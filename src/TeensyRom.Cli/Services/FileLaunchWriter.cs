using Spectre.Console;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Common;
using TeensyRom.Core.Storage.Entities;

namespace TeensyRom.Cli.Services
{
    internal class FileLaunchWriter
    {
        public FileLaunchWriter(IPlayerService player)
        {
            player.FileLaunched.Subscribe(item => 
            {
                AnsiConsole.WriteLine(RadHelper.ClearHack);

                var release = string.IsNullOrWhiteSpace(item.ReleaseInfo) ? "Unknown" : item.ReleaseInfo.EscapeBrackets();

                var body = string.Empty;

                if (item is SongItem song)
                {
                    body = $"\r\n  Title: {song.Title}\r\n  Creator: {song.Creator}\r\n  Release: {release}\r\n  Length: {song.PlayLength}\r\n  Clock: {song.Meta1}\r\n  SID: {song.Meta2}";
                }
                body = $"{body}\r\n  File Name: {item.Name}\r\n  Path: {item.Path.GetUnixParentPath().EscapeBrackets()}\r\n";

                var panel = new Panel(body.EscapeBrackets())
                      .PadTop(2)
                      .BorderColor(RadHelper.Theme.Secondary.Color)
                      .Border(BoxBorder.Rounded)
                      .Expand();

                panel.Header($" Now Playing: {item.Title.EscapeBrackets()} ".AddHighlights());

                AnsiConsole.Write(panel);
            });
        }
    }
}
