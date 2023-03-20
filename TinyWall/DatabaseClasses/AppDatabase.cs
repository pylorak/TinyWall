using Microsoft.Samples;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace pylorak.TinyWall.DatabaseClasses
{
    [DataContract(Namespace = "TinyWall")]
    class AppDatabase : ISerializable<AppDatabase>
    {
        [DataMember(Name = "KnownApplications")]
        private readonly List<Application> _KnownApplications;

        public static string DBPath
        {
            get { return System.IO.Path.Combine(Utils.AppDataPath, "profiles.json"); }
        }

        public static AppDatabase Load()
        {
            return SerialisationHelper.DeserialiseFromFile(DBPath, new AppDatabase());
        }

        public void Save(string filePath)
        {
            SerialisationHelper.SerialiseToFile(this, filePath);
        }

        [JsonConstructor]
        public AppDatabase(List<Application> knownApplications)
        {
            _KnownApplications = knownApplications;
        }

        public AppDatabase() :
            this(new List<Application>())
        { }

        public List<Application> KnownApplications
        {
            get { return _KnownApplications; }
        }

        public Application? GetApplicationByName(string name)
        {
            foreach (Application app in _KnownApplications)
            {
                if (app.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return app;
            }

            return null;
        }

        public List<FirewallExceptionV3> FastSearchMachineForKnownApps()
        {
            var ret = new List<FirewallExceptionV3>();

            foreach (DatabaseClasses.Application app in KnownApplications)
            {
                if (app.HasFlag("TWUI:Special"))
                    continue;

                foreach (SubjectIdentity id in app.Components)
                {
                    List<ExceptionSubject> subjects = id.SearchForFile();
                    foreach (var subject in subjects)
                    {
                        ret.Add(id.InstantiateException(subject));
                    }
                }
            }

            return ret;
        }

        internal Application? TryGetApp(ExecutableSubject fromSubject, out FirewallExceptionV3? fwex, bool matchSpecial)
        {
            foreach (var app in KnownApplications)
            {
                if (!matchSpecial && app.HasFlag("TWUI:Special"))
                    continue;

                foreach (var id in app.Components)
                {
                    if (id.DoesExecutableSatisfy(fromSubject))
                    {
                        fwex = id.InstantiateException(fromSubject);
                        return app;
                    }
                }
            }

            fwex = null;
            return null;
        }

        internal List<FirewallExceptionV3> GetExceptionsForApp(ExceptionSubject fromSubject, bool guiPrompt, out Application? app)
        {
            app = null;
            var exceptions = new List<FirewallExceptionV3>();

            if (fromSubject is AppContainerSubject)
            {
                exceptions.Add(new FirewallExceptionV3(fromSubject, new TcpUdpPolicy(true)));
                return exceptions;
            }
            else if (fromSubject is ExecutableSubject exeSubject)
            {
                // Try to find an application this subject might belong to
                app = TryGetApp(exeSubject, out FirewallExceptionV3? _, false);
                if (app == null)
                {
                    exceptions.Add(new FirewallExceptionV3(exeSubject, new TcpUdpPolicy(true)));
                    return exceptions;
                }

                // Now that we have the app, try to instantiate firewall exceptions
                // for all components.
                string pathHint = System.IO.Path.GetDirectoryName(exeSubject.ExecutablePath);
                foreach (SubjectIdentity id in app.Components)
                {
                    List<ExceptionSubject> foundSubjects = id.SearchForFile(pathHint);
                    foreach (ExceptionSubject subject in foundSubjects)
                    {
                        var tmp = id.InstantiateException(subject);
                        if (fromSubject.Equals(subject))
                            // Make sure original subject is at index 0
                            exceptions.Insert(0, tmp);
                        else
                            exceptions.Add(tmp);
                    }
                }

                // If we have found dependencies, ask the user what to do
                if ((exceptions.Count > 1) && guiPrompt)
                {

                    // Try to get localized name
                    string localisedAppName = Resources.Exceptions.ResourceManager.GetString(app.Name);
                    localisedAppName = string.IsNullOrWhiteSpace(localisedAppName) ? app.Name : localisedAppName;

                    Utils.SplitFirstLine(string.Format(CultureInfo.InvariantCulture, Resources.Messages.UnblockApp, localisedAppName), out string firstLine, out string contentLines);

                    var dialog = new TaskDialogue
                    {
                        CustomMainIcon = Resources.Icons.firewall,
                        WindowTitle = Resources.Messages.TinyWall,
                        MainInstruction = firstLine,
                        Content = contentLines,
                        DefaultButton = 1,
                        ExpandedControlText = Resources.Messages.UnblockAppShowRelated,
                        ExpandFooterArea = true,
                        AllowDialogCancellation = false,
                        UseCommandLinks = true
                    };

                    var button1 = new TaskDialogueButton(101, Resources.Messages.UnblockAppUnblockAllRecommended);
                    var button2 = new TaskDialogueButton(102, Resources.Messages.UnblockAppUnblockOnlySelected);
                    var button3 = new TaskDialogueButton(103, Resources.Messages.UnblockAppCancel);
                    dialog.Buttons = new TaskDialogueButton[] { button1, button2, button3 };

                    string fileListStr = exceptions.Aggregate(string.Empty, (current, fwex) => current + (fwex.Subject.ToString() + Environment.NewLine));
                    dialog.ExpandedInformation = fileListStr.Trim();

                    switch (dialog.Show())
                    {
                        case 101:
                            break;
                        case 102:
                            // Remove all exceptions with a different subject than the input argument
                            for (int i = exceptions.Count - 1; i >= 0; --i)
                            {
                                if (exceptions[i].Subject is ExecutableSubject exesub)
                                {
                                    if (!exesub.ExecutablePath.Equals(exeSubject.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                                    {
                                        exceptions.RemoveAt(i);
                                        continue;
                                    }
                                }
                                else
                                {
                                    exceptions.RemoveAt(i);
                                    continue;
                                }
                            }
                            exceptions.RemoveRange(1, exceptions.Count - 1);
                            break;
                        case 103:
                            exceptions.Clear();
                            break;
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            return exceptions;
        }

        public JsonTypeInfo<AppDatabase> GetJsonTypeInfo()
        {
            return SourceGenerationContext.Default.AppDatabase;
        }
    }
}
