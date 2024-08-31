using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeensyRom.Core.Logging;

namespace TeensyRom.Cli.Helpers
{
    internal class CliLogColorStrategy : ILogColorStategy
    {
        public string WithColor(string message, string hexColor)
        {
            message = message.EscapeBrackets();
            message = $"[{hexColor}]{message}[/]\r\n";
            return message;
        }
    }
}
