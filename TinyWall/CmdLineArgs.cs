namespace pylorak.TinyWall
{
    internal enum StartupCommand
    {
        Invalid,
        Service,
        Controller,
        SelfHosted,
        Install,
        Uninstall,
        DevelTool,
        ProfileCreator,
        DatabaseCreator,
        UpdateCreator,
        ResXOptimizer,
        BatchSigner
    }

    internal class CmdLineArgs
    {
        internal StartupCommand Command = StartupCommand.Invalid;

        internal bool autowhitelist = false;
        internal bool updatenow = false;
        internal bool startup = false;

        // Profile Creator
        internal string? ExecutablePath;
        internal string? OutputFile;

        // Database Creator
        internal string? SourceFolder;
        internal string? OutputFolder;  // also used by other subcommands

        // Update Creator
        internal string? BaseUrl;
        internal string? ProjectDir;

        // ResX Optimizer
        internal string? ResourceDir;

        // Batch Signer
        internal string? CertificateName;
        internal string? PfxPath;
        internal string? PfxPassword;
        internal string? SignDir;
        internal string? SigntoolPath;
        internal string? TimestampUrl;
    }
}
