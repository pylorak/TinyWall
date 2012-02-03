using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PKSoft
{
    internal enum StartUpMode
    {
        Invalid,
        DevelTool,
        Service,
        Controller,
        SelfHosted
    }

    internal class CmdLineArgs
    {
        internal bool detectnow = false;
        internal bool updatenow = false;

        internal bool install = false;
        internal bool uninstall = false;

        internal StartUpMode ProgramMode = StartUpMode.Invalid;
    }
}
