using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeensyRom.Core.Common;

namespace TeensyRom.Cli.Commands.Common
{
    internal static class CommandValidations
    {
        public static bool IsValueValid(this string value, string[] validValues) => validValues.Contains(value.ToLower());
        public static bool IsValidStorageDevice(this string value) => value.IsValueValid(new[] { "sd", "usb" });
        public static bool IsValidFilter(this string value) => value.IsValueValid(new[] { "all", "music", "games", "images" });

        public static ValidationResult ValidateStorageDevice(this string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !value.IsValidStorageDevice())
            {
                return ValidationResult.Error("Storage device must be 'sd' or 'usb'.");
            }
            return ValidationResult.Success();
        }

        public static ValidationResult ValidateUnixPath(this string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !value.IsValidUnixPath())
            {
                return ValidationResult.Error("Must be a valid unix path.");
            }
            return ValidationResult.Success();
        }

        public static ValidationResult ValidateFilter(this string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !value.IsValidFilter())
            {
                return ValidationResult.Error("Filter must be 'all', 'music', 'games', or 'images'.");
            }
            return ValidationResult.Success();
        }
    }
}
