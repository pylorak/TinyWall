
namespace pylorak.TinyWall
{
    internal enum StartUpMode
    {
        Invalid,
        DevelTool,
        Service,
        Controller,
        SelfHosted,
        Install,
        Uninstall
    }

    internal class CmdLineArgs
    {
        internal bool autowhitelist = false;
        internal bool updatenow = false;
        internal bool startup = false;

        internal StartUpMode ProgramMode = StartUpMode.Invalid;
    }
}
