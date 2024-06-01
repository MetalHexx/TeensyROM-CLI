using Spectre.Console;

namespace TeensyRom.Cli.Helpers
{
    /// <summary>
    /// A helper class to create consistent prompts and reduce noise
    /// </summary>
    internal static class PromptHelper
    {
        /// <summary>
        /// Renders a freeform text prompt entry with a length requirement
        /// </summary>
        /// <param name="prompt">The message prompt to show the user</param>
        /// <param name="length">The minimum length allowed from the users input</param>
        /// <param name="theme">Allows an override of the default theme for special cases</param>
        /// <returns>The user input</returns>
        public static string RequiredLengthTextPrompt(string prompt, int length)
        {
            var theme = RadHelper.Theme;

            return AnsiConsole.Prompt(
                new TextPrompt<string>($"[{theme.Secondary}]{prompt}[/]")
                    .PromptStyle(theme.Primary.ToString())
                    .DefaultValueStyle(theme.Secondary.ToString())
                    .Validate(input =>
                    {
                        if (input.Length < length) return ValidationResult.Error($"[{theme.Error}]Length must be greater than {length}[/]");
                        return ValidationResult.Success();
                    }));
        }

        /// <summary>
        /// Renders a freeform text prompt entry with a length requirement
        /// </summary>
        /// <param name="prompt">The message prompt to show the user</param>
        /// <param name="length">The minimum length allowed from the users input</param>
        /// <param name="theme">Allows an override of the default theme for special cases</param>
        /// <returns>The user input</returns>
        public static string DefaultValueTextPrompt(string prompt, int length, string defaultValue = "")
        {
            var theme = RadHelper.Theme;

            return AnsiConsole.Prompt(
                new TextPrompt<string>($"[{theme.Secondary}]{prompt}[/]")
                    .PromptStyle(theme.Primary.ToString())
                    .DefaultValue(defaultValue)
                    .DefaultValueStyle(theme.Secondary.ToString())
                    .Validate(input =>
                    {
                        if (input.Length < length && string.IsNullOrWhiteSpace(defaultValue))
                        {
                            return ValidationResult.Error($"[{theme.Error}]Length must be greater than {length}[/]");
                        }
                        return ValidationResult.Success();
                    }));
        }

        /// <summary>
        /// Renders a true/false (yes/no) prompt with boolean result
        /// </summary>
        /// <param name="message">The message prompt to show the user</param>
        /// <param name="theme">Allows an override of the default theme for special cases</param>
        /// <returns>A boolean result based on user input</returns>
        public static bool Confirm(string message, bool defaultValue)
        {
            var theme = RadHelper.Theme;

            var yesNo = defaultValue
                ? $"[{theme.Primary}](Y/n)[/]"
                : $"[{theme.Primary}](y/N)[/]";

            var values = defaultValue
                ? new[] { "Yes", "No" }
                : new[] { "No", "Yes" };

            var input = AnsiConsole.Prompt
            (
                new SelectionPrompt<string>()
                    .Title($"[{theme.Secondary}]{message}[/] {yesNo}")
                    .HighlightStyle(theme.Primary.ToString())
                    .AddChoices(values)
            );

            AnsiConsole.MarkupLine($"[{theme.Secondary}]{message}[/][{theme.Primary}]{input}[/]");

            return input.Equals("Yes") ? true : false;
        }

        /// <summary>
        /// Renders a choice prompt with string choices and response
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="choices">The choices to show the user</param>
        /// <param name="theme">Allows an override of the default theme for special cases</param>
        /// <returns>Selected choice</returns>
        public static string ChoicePrompt(string message, List<string> choices)
        {
            var theme = RadHelper.Theme;

            var selection = AnsiConsole.Prompt
            (
                new SelectionPrompt<string>()
                    .Title($"[{theme.Secondary}]{message}: [/]")
                    .HighlightStyle(theme.Primary.ToString())
                    .AddChoices(choices)
            );

            AnsiConsole.MarkupLine($"[{theme.Secondary}]{message}: [/][{theme.Primary}]{selection}[/]");

            return selection;
        }
    }
}
