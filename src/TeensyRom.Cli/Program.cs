using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;
using System.Reflection;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Common;
using TeensyRom.Cli.Fonts;
using TeensyRom.Cli.Helpers;
using TeensyRom.Cli.Services;
public class Program
{
    private static async Task<int> Main(string[] args)
    {
        AnsiConsole.WriteLine();
        RadHelper.RenderLogo("TeensyROM", FontConstants.FontPath);

        var host = CliHost.Create();
        var app = CliHost.CreateApp(new TypeRegistrar(host.Services));

        if (!IsHelpOrVersion(args) && !await host.Startup.RunAsync())
        {
            return 1;
        }

        var resultCode = 0;

        while (true)
        {
            try
            {
                if (IsHelpOrVersion(args))
                {
                    app.Run(args);
                    return -99;
                }

                if (args.Length > 0)
                {
                    var help = args.ToList();
                    help.Add("-h");
                    AnsiConsole.WriteLine();

                    resultCode = app.Run(help.ToArray());
                    resultCode = app.Run(args);
                    args = [];
                }

                var menuChoice = PromptHelper.ChoicePrompt("Choose wisely", ["Launch", "Settings", "Index Files", "List Ports", "Generate ASID Patches", "Credits", "Leave"]);

                AnsiConsole.WriteLine();

                if (menuChoice == "Leave") return 0;

                args = menuChoice switch
                {
                    "Launch" => ["launch"],
                    "Index Files" => ["index"],
                    "Settings" => ["settings"],
                    "List Ports" => ["ports"],
                    "Generate ASID Patches" => ["chipsynth"],
                    "Credits" => ["credits"],
                    _ => []
                };
                app.Run(args);

                args = [];
            }
            catch(CommandParseException ex)
            {
                RadHelper.WriteError(ex.Message);
                AnsiConsole.WriteLine();
                args = ["-h"];
            }
            catch (TeensyApiException ex)
            {
                RadHelper.WriteError(ex.Message);
                args = [];
                continue;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.Default);
                LogExceptionToFile(ex);
                continue;
            }
            if (resultCode == -1) return resultCode;
        }
    }

    private static bool IsHelpOrVersion(string[] args) =>
        args.Contains("-h") || args.Contains("--help") || args.Contains("-v") || args.Contains("--version");

    private static readonly object _logFileLock = new object();

    private static void LogExceptionToFile(Exception ex)
    {
        string filePath = Path.Combine(Assembly.GetExecutingAssembly().GetPath(), @"Assets\System\Logs\UnhandledErrorLogs.txt");

        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        }

        try
        {
            lock (_logFileLock)
            {
                File.AppendAllText(filePath, $"{DateTime.Now}{Environment.NewLine}Exception: {ex}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch (Exception logEx)
        {
            Debug.WriteLine("Failed to log exception: " + logEx.Message);
        }
    }
}
