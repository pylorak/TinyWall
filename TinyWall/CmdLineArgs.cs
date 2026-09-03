using System;

namespace pylorak.TinyWall
{
    public class InvalidCommandlineOptionException : Exception
    {
        public string OptionName { get; init; }

        public InvalidCommandlineOptionException(string optionName) : base($"Unrecognized commandline option \"{optionName}\".")
        {
            OptionName = optionName;
        }
    }

    public class MissingCommandlineValueException : Exception
    {
        public string OptionName { get; init; }

        public MissingCommandlineValueException(string optionName) : base($"Commandline option \"{optionName}\" is missing its argument.")
        {
            OptionName = optionName;
        }
    }

    public class MissingCommandlineOptionException : Exception
    {
        public string OptionName { get; init; }

        public MissingCommandlineOptionException(string optionName) : base($"Missing required commandline option \"{optionName}\".")
        {
            OptionName = optionName;
        }
    }

    public class CliArg<TValue>
    {
        public required string Name { get; init; }
        public bool IsRequired { get; init; }
        public TValue? Value { get; set; }
        public void ThrowIfRequiredAndUnassigned()
        {
            if (!IsRequired)
                return;

            if (typeof(TValue) == typeof(string))
            {
                var str = Value as string;
                if (Utils.IsNullOrEmpty(str))
                    throw new MissingCommandlineOptionException(Name);
            }
            else
            {
                if (Value is null)
                    throw new MissingCommandlineOptionException(Name);
            }
        }
    }

    public abstract class CliArgsBase
    {
        private static string[] KeepSelectedElements(string[] input, bool[] mask)
        {
            if (input.Length != mask.Length)
                throw new ArgumentException("Input and mask arrays must have same length.");

            // Determine result length
            int len = 0;
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i]) len++;
            }

            // Copy only selected elements to output
            var output = new string[len];
            for (int si = 0, di = 0; si < mask.Length; si++)
            {
                if (mask[si]) output[di++] = input[si];
            }

            return output;
        }

        protected abstract void OnParse(string[] args, bool[] keep);

        public void Parse(ref string[] args)
        {
            var keep = new bool[args.Length];
            OnParse(args, keep);
            args = KeepSelectedElements(args, keep);
        }

    }

    public class ControllerCliArgs : CliArgsBase
    {
        public CliArg<bool> AutoWhitelist { get; init; } = new() { Name = "/autowhitelist" };
        public CliArg<bool> Startup { get; init; } = new() { Name = "/startup" };

        protected override void OnParse(string[] args, bool[] keep)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLowerInvariant();

                if (AutoWhitelist.Name == arg)
                    AutoWhitelist.Value = true;
                else if (Startup.Name == arg)
                    Startup.Value = true;
                else
                    keep[i] = true;
            }

            AutoWhitelist.ThrowIfRequiredAndUnassigned();
            Startup.ThrowIfRequiredAndUnassigned();
        }
    }

    public class ProfileCreatorCliArgs : CliArgsBase
    {
        public CliArg<string> ExecutablePath { get; init; } = new() { Name = "/executable-path", IsRequired = true };
        public CliArg<string> OutputFile { get; init; } = new() { Name = "/output-file", IsRequired = true };

        protected override void OnParse(string[] args, bool[] keep)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLowerInvariant();

                if (ExecutablePath.Name == arg)
                    ExecutablePath.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else if (OutputFile.Name == arg)
                    OutputFile.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else
                    keep[i] = true;
            }

            ExecutablePath.ThrowIfRequiredAndUnassigned();
            OutputFile.ThrowIfRequiredAndUnassigned();
        }
    }

    public class DatabaseCreatorCliArgs : CliArgsBase
    {
        public CliArg<string> SourceFolder { get; init; } = new() { Name = "/source-folder", IsRequired = true };
        public CliArg<string> OutputFolder { get; init; } = new() { Name = "/output-folder", IsRequired = true };

        protected override void OnParse(string[] args, bool[] keep)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLowerInvariant();

                if (SourceFolder.Name == arg)
                    SourceFolder.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else if (OutputFolder.Name == arg)
                    OutputFolder.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else
                    keep[i] = true;
            }

            SourceFolder.ThrowIfRequiredAndUnassigned();
            OutputFolder.ThrowIfRequiredAndUnassigned();
        }
    }

    public class UpdateCreatorCliArgs : CliArgsBase
    {
        public CliArg<string> BaseUrl { get; init; } = new() { Name = "/base-url", IsRequired = true };
        public CliArg<string> ProjectDir { get; init; } = new() { Name = "/project-dir", IsRequired = true };
        public CliArg<string> OutputFolder { get; init; } = new() { Name = "/output-folder", IsRequired = true };

        protected override void OnParse(string[] args, bool[] keep)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLowerInvariant();

                if (BaseUrl.Name == arg)
                    BaseUrl.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else if (ProjectDir.Name == arg)
                    ProjectDir.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else if (OutputFolder.Name == arg)
                    OutputFolder.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else
                    keep[i] = true;
            }

            BaseUrl.ThrowIfRequiredAndUnassigned();
            ProjectDir.ThrowIfRequiredAndUnassigned();
            OutputFolder.ThrowIfRequiredAndUnassigned();
        }
    }

    public class ResXOptimizerCliArgs : CliArgsBase
    {
        public CliArg<string> ResourceDir { get; init; } = new() { Name = "/resource-dir", IsRequired = true };
        public CliArg<string> OutputFolder { get; init; } = new() { Name = "/output-folder", IsRequired = true };
        public CliArg<bool> Compare { get; init; } = new() { Name = "/compare" };

        protected override void OnParse(string[] args, bool[] keep)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLowerInvariant();

                if (ResourceDir.Name == arg)
                    ResourceDir.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else if (OutputFolder.Name == arg)
                    OutputFolder.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else if (Compare.Name == arg)
                    Compare.Value = true;
                else
                    keep[i] = true;
            }

            ResourceDir.ThrowIfRequiredAndUnassigned();
            OutputFolder.ThrowIfRequiredAndUnassigned();
            Compare.ThrowIfRequiredAndUnassigned();
        }
    }

    public class BatchSignerCliArgs : CliArgsBase
    {
        public CliArg<string> CertificateName { get; init; } = new() { Name = "/certificate-name", IsRequired = true };
        public CliArg<string> SignDir { get; init; } = new() { Name = "/sign-dir", IsRequired = true };
        public CliArg<string> SigntoolPath { get; init; } = new() { Name = "/signtool-path", IsRequired = true };
        public CliArg<string> TimestampUrl { get; init; } = new() { Name = "/timestamp-url", IsRequired = true };

        protected override void OnParse(string[] args, bool[] keep)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLowerInvariant();

                if (CertificateName.Name == arg)
                    CertificateName.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else if (SignDir.Name == arg)
                    SignDir.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else if (SigntoolPath.Name == arg)
                    SigntoolPath.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else if (TimestampUrl.Name == arg)
                    TimestampUrl.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else
                    keep[i] = true;
            }

            CertificateName.ThrowIfRequiredAndUnassigned();
            SignDir.ThrowIfRequiredAndUnassigned();
            SigntoolPath.ThrowIfRequiredAndUnassigned();
            TimestampUrl.ThrowIfRequiredAndUnassigned();
        }
    }

    public class RelocationTestCliArgs : CliArgsBase
    {
        public CliArg<string> ExecutablePath { get; init; } = new() { Name = "/executable-path", IsRequired = true };
        public CliArg<string> OutputFile { get; init; } = new() { Name = "/output-file" };

        protected override void OnParse(string[] args, bool[] keep)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLowerInvariant();

                if (ExecutablePath.Name == arg)
                    ExecutablePath.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else if (OutputFile.Name == arg)
                    OutputFile.Value = (++i < args.Length) ? args[i] : throw new MissingCommandlineValueException(arg);
                else
                    keep[i] = true;
            }

            ExecutablePath.ThrowIfRequiredAndUnassigned();
            OutputFile.ThrowIfRequiredAndUnassigned();
        }
    }

    public enum StartupCommand
    {
        // Default to check for uninitilaized values
        Invalid,

        // Only for developers:
        Service,
        Controller,
        SelfHosted,
        Install,
        Uninstall,
        ProfileCreator,
        DatabaseCreator,
        UpdateCreator,
        ResXOptimizer,
        BatchSigner,
        RelocationTest,

        // Only the following are meant for end-users:
        Update
    }

    public class CmdLineArgs
    {
        public StartupCommand Command { get; set; } = StartupCommand.Invalid;

        public ControllerCliArgs Controller { get; } = new();
        public ProfileCreatorCliArgs ProfileCreator { get; } = new();
        public DatabaseCreatorCliArgs DatabaseCreator { get; } = new();
        public UpdateCreatorCliArgs UpdateCreator { get; } = new();
        public ResXOptimizerCliArgs ResXOptimizer { get; } = new();
        public BatchSignerCliArgs BatchSigner { get; } = new();
        public RelocationTestCliArgs RelocationTest { get; } = new();

        public void ParseArgs(string[] args)
        {
            // Determine the main command first. This is done separately from the rest of the args
            // because their validation could depend on the selected command.
            if (args.Length != 0)
            {
                // Leading forward slash is optional for command arg.
                var commandStr = args[0].ToLowerInvariant();
                if (commandStr.StartsWith("/")) commandStr = commandStr.Substring(1);
                Command = commandStr switch
                {
                    "update" => StartupCommand.Update,
                    "service" => StartupCommand.Service,
                    "controller" => StartupCommand.Controller,
                    "selfhosted" => StartupCommand.SelfHosted,
                    "install" => StartupCommand.Install,
                    "uninstall" => StartupCommand.Uninstall,
                    "profile-creator" => StartupCommand.ProfileCreator,
                    "database-creator" => StartupCommand.DatabaseCreator,
                    "update-creator" => StartupCommand.UpdateCreator,
                    "resx-optimizer" => StartupCommand.ResXOptimizer,
                    "batch-signer" => StartupCommand.BatchSigner,
                    "relocation-test" => StartupCommand.RelocationTest,
                    _ when args[0].StartsWith("/") => StartupCommand.Invalid,
                    _ => throw new InvalidCommandlineOptionException(args[0])
                };
            }

            string[] optsOnly;
            if (Command == StartupCommand.Invalid)
            {
                // No command was defined, so we select a fallback default
                Command = Environment.UserInteractive ? StartupCommand.Controller : StartupCommand.Service;
                optsOnly = args;
            }
            else
            {
                // There was a command, consume it from the processed args
                optsOnly = new string[args.Length - 1];
                Array.Copy(args, 1, optsOnly, 0, args.Length - 1);
            }

            switch (Command)
            {
                case StartupCommand.Controller:
                    Controller.Parse(ref optsOnly);
                    break;
                case StartupCommand.SelfHosted:
                    Controller.Parse(ref optsOnly);
                    break;
                case StartupCommand.ProfileCreator:
                    ProfileCreator.Parse(ref optsOnly);
                    break;
                case StartupCommand.DatabaseCreator:
                    DatabaseCreator.Parse(ref optsOnly);
                    break;
                case StartupCommand.UpdateCreator:
                    UpdateCreator.Parse(ref optsOnly);
                    break;
                case StartupCommand.ResXOptimizer:
                    ResXOptimizer.Parse(ref optsOnly);
                    break;
                case StartupCommand.BatchSigner:
                    BatchSigner.Parse(ref optsOnly);
                    break;
                case StartupCommand.RelocationTest:
                    RelocationTest.Parse(ref optsOnly);
                    break;
            }

            // Whatever remains in optsOnly are the unrecognized arguments
            if (optsOnly.Length > 0)
                throw new InvalidCommandlineOptionException(optsOnly[0]);
        }

    }
}
