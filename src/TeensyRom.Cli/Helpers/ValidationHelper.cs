using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeensyRom.Cli.Helpers
{
    internal static class ValidationHelper
    {
        public static bool IsValueValid(this string value, string[] validValues) => validValues.Any(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
    }
}
