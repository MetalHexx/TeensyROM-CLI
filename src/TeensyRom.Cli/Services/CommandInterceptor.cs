using Spectre.Console.Cli;
using TeensyRom.Cli.Helpers;

namespace TeensyRom.Cli.Services
{
    internal class CommandInterceptor : ICommandInterceptor
    {
        public void Intercept(CommandContext context, CommandSettings settings) { }

        public void InterceptResult(CommandContext context, CommandSettings settings, ref int result)
        {
            if (settings is IClearableSettings s)
            {
                s.ClearSettings();
            }
        }
    }
}
