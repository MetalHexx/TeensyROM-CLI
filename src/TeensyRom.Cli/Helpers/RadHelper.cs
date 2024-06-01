using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace TeensyRom.Cli.Helpers
{
    /// <summary>
    /// Helper class that assists with adding some color to cli output
    /// </summary>
    public static class RadHelper
    {
        public static MarkupTheme Theme => new MarkupTheme(
            Primary: new MarkupColor(Color.Fuchsia, "fuchsia"),
            Secondary: new MarkupColor(Color.Aqua, "aqua"),
            Highlight: new MarkupColor(Color.White, "white"),
            Error: new MarkupColor(Color.Red, "red"));

        /// <summary>
        /// Renders the main cli logo
        /// </summary>
        /// <param name="fontPath">Font to use for the logo</param>
        public static void RenderLogo(string text, string fontPath)
        {
            var font = FigletFont.Load(fontPath);

            AnsiConsole.Write(new FigletText(font, text)
                .Color(Theme.Primary.Color));
        }
        /// <summary>
        /// Allows you to AddColumn with a table and pass a delegate
        /// </summary>
        public static Table AddColumn(this Table table, TableColumn column, Action<TableColumn>? configure = null)
        {
            configure?.Invoke(column);
            table.AddColumn(column);
            return table;
        }

        /// <summary>
        /// Formats message strings for cli output
        /// </summary>
        public static string AddHighlights(this string message)
        {
            var stringBuilder = new StringBuilder();
            var theme = Theme;

            foreach (var character in message)
            {
                var charString = character.ToString();
                string markupString = charString switch
                {
                    "." => $"[{theme.Secondary}]{charString}[/]",
                    "*" => $"[{theme.Secondary}]{charString}[/]",
                    "-" => $"[{theme.Secondary}]{charString}[/]",
                    "+" => $"[{theme.Secondary}]{charString}[/]",
                    "/" => $"[{theme.Secondary}]{charString}[/]",
                    "\\" => $"[{theme.Secondary}]{charString}[/]",
                    "_" => $"[{theme.Secondary}]{charString}[/]",
                    _ when Regex.IsMatch(charString, @"\W") => $"[{theme.Highlight}]{charString}[/]",
                    _ when Regex.IsMatch(charString, @"\d") => $"[{theme.Secondary}]{charString}[/]",
                    _ => $"[{theme.Primary}]{charString}[/]",
                };
                stringBuilder.Append(markupString);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Formats path strings for cli output
        /// </summary>
        public static string ToL33tPath(this string message)
        {
            var theme = Theme;
            var stringBuilder = new StringBuilder();

            foreach (var character in message)
            {
                var charString = character.ToString();
                string markupString = charString switch
                {
                    _ when Regex.IsMatch(charString, @"\\") => $"[{theme.Secondary}]{charString}[/]",
                    _ => $"[{theme.Highlight}]{charString}[/]",
                };

                stringBuilder.Append(markupString);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Writes a message with horizontal rule
        /// </summary>
        /// <param name="message"></param>
        public static void WriteHorizonalRule(string message, Justify justify)
        {
            var rule = new Rule($"-={message}=-".AddHighlights());
            rule.Justification = justify;
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();
        }

        /// <summary>
        /// Writes a message
        /// </summary>
        /// <param name="message"></param>
        public static void WriteTitle(string message)
        {
            AnsiConsole.MarkupLine($"-={message}=-".AddHighlights());
        }

        /// <summary>
        /// Writes a message
        /// </summary>
        /// <param name="message"></param>
        public static void WriteLine(string message)
        {
            AnsiConsole.MarkupLine(message.AddHighlights());
        }

        /// <summary>
        /// Displays an error message
        /// </summary>
        /// <param name="message"></param>
        public static void WriteError(string message)
        {
            AnsiConsole.MarkupLine($"[red]{message}[/]");
        }

        /// <summary>
        /// Writes a message with bullet
        /// </summary>
        /// <param name="message"></param>
        public static void WriteBullet(string message, int indent = 1)
        {
            var indentString = " ";

            for (int i = 0; i < indent; i++)
            {
                indentString += " ";
            }

            message = $"{indentString}- {message}";
            AnsiConsole.MarkupLine(message.AddHighlights());
        }

        public static Progress AddTheme(this Progress progress)
        {
            var theme = Theme;
            return progress.Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn()
                    .FinishedStyle(new Style(foreground: theme.Primary.Color))
                    .RemainingStyle(new Style(foreground: theme.Secondary.Color)),
                new PercentageColumn()
                    .CompletedStyle(new Style(foreground: theme.Highlight.Color)),
                new SpinnerColumn(Spinner.Known.Balloon2)
                    .Style(new Style(foreground: theme.Secondary.Color)),
            ]);
        }
    }
}
