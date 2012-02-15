using System;
using System.Collections.ObjectModel;

namespace PKSoft
{
    [Serializable]
    public class ApplicationCollection : Collection<Application>
    {
        internal Application GetApplicationByName(string name)
        {
            for (int i = 0; i < this.Count; ++i)
            {
                if (string.Compare(name, this[i].Name, System.StringComparison.OrdinalIgnoreCase) == 0)
                    return this[i];
            }
            return null;
        }

        internal Application TryGetRecognizedApp(string executablePath, string service, out ProfileAssoc file)
        {
            ProfileAssoc exe = ProfileAssoc.FromExecutable(executablePath, service);

            for (int i = 0; i < this.Count; ++i)
            {
                for (int j = 0; j < this[i].FileTemplates.Count; ++j)
                {
                    ProfileAssoc assoc = this[i].FileTemplates[j];
                    if (assoc.DoesExecutableSatisfy(exe))
                    {
                        file = Utils.DeepClone(assoc);
                        file.Executable = executablePath;
                        return this[i];
                    }
                }
            }

            file = null;
            return null;
        }
    }
}
