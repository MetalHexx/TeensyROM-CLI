using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Logging;

namespace TeensyRom.Cli.Services
{
    internal class AlertService : IAlertService
    {
        public IObservable<string> CommandErrors => throw new NotImplementedException();

        public void Publish(string error)
        {
            RadHelper.WriteLine(error);
        }
    }
}
