using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Commands.File.LaunchFile;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.Common
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

        public static TimeSpan? PromptGameTimer() 
        {
            string time = PromptHelper.ChoicePrompt("Game/Image Timer", ["No", "3m", "5m", "15m", "30m", "1h", "Turbo Mode"]);

            return time switch
            {
                "3m" => TimeSpan.FromMinutes(3),
                "5m" => TimeSpan.FromMinutes(5),
                "15m" => TimeSpan.FromMinutes(15),
                "30m" => TimeSpan.FromMinutes(30),
                "1h" => TimeSpan.FromHours(1),
                "Turbo Mode" => TimeSpan.FromSeconds(3),
                _ => null
            };
        }

        public static TeensyStorageType PromptForStorageType(string value)
        {
            if (value.Equals(string.Empty))
            {
                value = PromptHelper.ChoicePrompt("Storage Type", new List<string> { "SD", "USB" });
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
                value = PromptHelper.ChoicePrompt("Filter Type", new List<string> { "All", "Music", "Games", "Images" });
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

        public static string PromptForFilePath(string value, string defaultValue = "/music/MUSICIANS/T/Tjelta_Geir/Artillery.sid") 
        {
            return PromptForPath(value, "File Path:", defaultValue);
        }

        public static string PromptForDirectoryPath(string value, string defaultValue = "/music/MUSICIANS/T/Tjelta_Geir/")
        {
            return PromptForPath(value, "Directory Path:", defaultValue);
        }

        private static string PromptForPath(string value, string title, string defaultValue) 
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                value = PromptHelper.DefaultValueTextPrompt(title, 2, defaultValue);
                RadHelper.WriteLine();
            }
            return value;
        }

        public static ILaunchableItem? GetRandomFilePath(this ICachedStorageService storage, StorageScope scope, string directory, TeensyFileType[] filters)
        {
            var launchItem = storage.GetRandomFile(scope, directory, filters);

            if (launchItem is null)
            {
                RadHelper.WriteError("No file found.");
                AnsiConsole.WriteLine();
                return null;
            }
            return launchItem;
        }

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
