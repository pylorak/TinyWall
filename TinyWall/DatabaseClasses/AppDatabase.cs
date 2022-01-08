using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Globalization;
using Microsoft.Samples;

namespace pylorak.TinyWall.DatabaseClasses
{
    [DataContract(Namespace = "TinyWall")]
    class AppDatabase
    {
        [DataMember(Name = "KnownApplications")]
        private readonly List<Application> _KnownApplications;

        public static string DBPath
        {
            get { return System.IO.Path.Combine(Utils.AppDataPath, "profiles.xml"); }
        }

        public static AppDatabase Load(string filePath)
        {
            AppDatabase newInstance = SerializationHelper.LoadFromXMLFile<AppDatabase>(filePath);
            return newInstance;
        }

        public void Save(string filePath)
        {
            SerializationHelper.SaveToXMLFile(this, filePath);
        }

        public AppDatabase()
        {
            _KnownApplications = new List<Application>();
        }

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
                    string localizedAppName = Resources.Exceptions.ResourceManager.GetString(app.Name);
                    localizedAppName = string.IsNullOrEmpty(localizedAppName) ? app.Name : localizedAppName;

                    Utils.SplitFirstLine(string.Format(CultureInfo.InvariantCulture, Resources.Messages.UnblockApp, localizedAppName), out string firstLine, out string contentLines);

                    var dialog = new TaskDialog();
                    dialog.CustomMainIcon = Resources.Icons.firewall;
                    dialog.WindowTitle = Resources.Messages.TinyWall;
                    dialog.MainInstruction = firstLine;
                    dialog.Content = contentLines;
                    dialog.DefaultButton = 1;
                    dialog.ExpandedControlText = Resources.Messages.UnblockAppShowRelated;
                    dialog.ExpandFooterArea = true;
                    dialog.AllowDialogCancellation = false;
                    dialog.UseCommandLinks = true;

                    var button1 = new TaskDialogButton(101, Resources.Messages.UnblockAppUnblockAllRecommended);
                    var button2 = new TaskDialogButton(102, Resources.Messages.UnblockAppUnblockOnlySelected);
                    var button3 = new TaskDialogButton(103, Resources.Messages.UnblockAppCancel);
                    dialog.Buttons = new TaskDialogButton[] { button1, button2, button3 };

                    string fileListStr = string.Empty;
                    foreach (FirewallExceptionV3 fwex in exceptions)
                        fileListStr += fwex.Subject.ToString() + Environment.NewLine;
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
    }
}
