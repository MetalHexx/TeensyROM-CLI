using Spectre.Console;
using Spectre.Console.Cli;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Helpers;

namespace TeensyRom.Cli.Commands.Main.Launcher
{
    internal class LaunchSettings : CommandSettings, ITeensyCommandSettings, IRequiresConnection
    {
        public void ClearSettings() { }
    }
    internal class LaunchCommand : AsyncCommand<LaunchSettings>
    {
        private readonly RandomCommand _randomCommand;
        private readonly NavigateCommand _navigateCommand;
        private readonly FileLaunchCommand _fileCommand;
        private readonly PlayerCommand _playerCommand;

        public LaunchCommand(ITypeResolver resolver)
        {
            _randomCommand = (resolver.Resolve(typeof(RandomCommand)) as RandomCommand)!;
            _navigateCommand = (resolver.Resolve(typeof(NavigateCommand)) as NavigateCommand)!;
            _fileCommand = (resolver.Resolve(typeof(FileLaunchCommand)) as FileLaunchCommand)!;
            _playerCommand = (resolver.Resolve(typeof(PlayerCommand)) as PlayerCommand)!;

            if(_randomCommand == null || _navigateCommand == null || _fileCommand == null || _playerCommand == null)
            {
                throw new Exception("Failed to resolve command dependencies");
            }   
        }
        public override async Task<int> ExecuteAsync(CommandContext context, LaunchSettings settings)
        {
            var shouldLeave = false;

            do
            {
                RadHelper.WriteMenu("Launch Menu", "The path to launch files is yours to choose...");

                RadHelper.WriteDynamicTable(["Menu Item", "Description"],
                [
                    ["Random", "Plays a random file."],
                    ["Navigate", "Navigate your storage to find a file to launch."],
                    ["File", "Launch a file directory with a path or DeepSID link"],
                    ["Player", "Go to the player view."],
                    ["Back", "Back to main menu."],
                ]);

                switch (PromptHelper.ChoicePrompt("Launch Menu", ["Random", "Navigate", "File", "Player", "Back"]))
                {
                    case "Random":
                        await _randomCommand.ExecuteAsync(context, new RandomSettings { });
                        break;

                    case "Navigate":
                        await _navigateCommand.ExecuteAsync(context, new NavigateSettings { });
                        break;

                    case "File":
                        await _fileCommand.ExecuteAsync(context, new FileLaunchSettings { });
                        break;

                    case "Player":
                        return _playerCommand.Execute(context, new PlayerSettings { });

                    default:
                        shouldLeave = true;
                        break;
                }
            } while (!shouldLeave);

            AnsiConsole.WriteLine();
            return 0;
        }
    }
}
