﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeensyRom.Cli.Helpers
{
    internal interface ITeensyCommandSettings
    {
        void ClearSettings();
    }

    internal interface IRequiresConnection { }
}