using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Globalization;
using TinyWall.Interface;
using TinyWall.Interface.Internal;
using Microsoft.Samples;

namespace PKSoft.DatabaseClasses
{
    [DataContract(Namespace = "TinyWall")]
    class AppDatabase
    {
        [DataMember(Name = "KnownApplications")]
        private List<Application> _KnownApplications;

        public static string DBPath
        {
            get { return System.IO.Path.Combine(Utils.AppDataPath, "profiles_v3.xml"); }
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

        public Application GetApplicationByName(string name)
        {
            foreach (Application app in _KnownApplications)
            {
                if (app.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return app;
            }

            return null;
        }

        internal Application TryGetApp(ExecutableSubject fromSubject, out FirewallExceptionV3 fwex)
        {
            for (int i = 0; i < KnownApplications.Count; ++i)
            {
                for (int j = 0; j < KnownApplications[i].Components.Count; ++j)
                {
                    SubjectIdentity id = KnownApplications[i].Components[j];
                    if (id.DoesExecutableSatisfy(fromSubject))
                    {
                        fwex = id.InstantiateException(fromSubject);
                        return this.KnownApplications[i];
                    }
                }
            }

            fwex = null;
            return null;
        }

        internal List<FirewallExceptionV3> GetExceptionsForApp(ExecutableSubject fromSubject, bool guiPrompt)
        {
            List<FirewallExceptionV3> exceptions = new List<FirewallExceptionV3>();

            // Try to find an application this subject might belong to
            Application app = null;
            for (int i = 0; i < KnownApplications.Count; ++i)
            {
                for (int j = 0; j < KnownApplications[i].Components.Count; ++j)
                {
                    SubjectIdentity id = KnownApplications[i].Components[j];
                    if (id.DoesExecutableSatisfy(fromSubject))
                    {
                        app = KnownApplications[i];
                        break;
                    }
                }

                if (app != null)
                    break;
            }

            if (app == null)
                return exceptions;

            // Now that we have the app, try to instantiate firewall exceptions
            // for all components.
            string pathHint = System.IO.Path.GetDirectoryName(fromSubject.ExecutablePath);
            foreach (SubjectIdentity id in app.Components)
            {
                List<ExecutableSubject> foundSubjects = id.SearchForFile(pathHint);
                foreach (ExecutableSubject subject in foundSubjects)
                {
                    exceptions.Add(id.InstantiateException(subject));
                }
            }

            // If we have found dependencies, ask the user what to do
            if ((exceptions.Count > 1) && guiPrompt)
            {
                string firstLine, contentLines;

                // Try to get localized name
                string localizedAppName = PKSoft.Resources.Exceptions.ResourceManager.GetString(app.Name);
                localizedAppName = string.IsNullOrEmpty(localizedAppName) ? app.Name : localizedAppName;

                Utils.SplitFirstLine(string.Format(CultureInfo.InvariantCulture, PKSoft.Resources.Messages.UnblockApp, localizedAppName), out firstLine, out contentLines);

                TaskDialog dialog = new TaskDialog();
                dialog.CustomMainIcon = PKSoft.Resources.Icons.firewall;
                dialog.WindowTitle = PKSoft.Resources.Messages.TinyWall;
                dialog.MainInstruction = firstLine;
                dialog.Content = contentLines;
                dialog.DefaultButton = 1;
                dialog.ExpandedControlText = PKSoft.Resources.Messages.UnblockAppShowRelated;
                dialog.ExpandFooterArea = true;
                dialog.AllowDialogCancellation = false;
                dialog.UseCommandLinks = true;

                TaskDialogButton button1 = new TaskDialogButton(101, PKSoft.Resources.Messages.UnblockAppUnblockAllRecommended);
                TaskDialogButton button2 = new TaskDialogButton(102, PKSoft.Resources.Messages.UnblockAppUnblockOnlySelected);
                TaskDialogButton button3 = new TaskDialogButton(103, PKSoft.Resources.Messages.UnblockAppCancel);
                dialog.Buttons = new TaskDialogButton[] { button1, button2, button3 };

                string fileListStr = string.Empty;
                foreach (FirewallExceptionV3 fwex in exceptions)
                    fileListStr += fwex.Subject.ToString() + Environment.NewLine;
                dialog.ExpandedInformation = fileListStr.Trim();

                bool success;
                if (Utils.IsMetroActive(out success))
                {
                    Utils.ShowToastNotif(Resources.Messages.ToastInputNeeded);
                }

                switch (dialog.Show())
                {
                    case 101:
                        break;
                    case 102:
                        // Remove all exceptions with a different subject than the input argument
                        for (int i = exceptions.Count-1; i >= 0; --i)
                        {
                            ExecutableSubject exesub = exceptions[i].Subject as ExecutableSubject;
                            if (null == exesub)
                            {
                                exceptions.RemoveAt(i);
                                continue;
                            }

                            if (!exesub.ExecutablePath.Equals(fromSubject.ExecutablePath, StringComparison.OrdinalIgnoreCase))
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

            return exceptions;
        }
    }
}
