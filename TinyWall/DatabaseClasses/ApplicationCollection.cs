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

        internal Application TryGetRecognizedApp(string executablePath, string service, out AppExceptionAssoc file)
        {
            AppExceptionAssoc exe = AppExceptionAssoc.FromExecutable(executablePath, service);

            for (int i = 0; i < this.Count; ++i)
            {
                for (int j = 0; j < this[i].FileTemplates.Count; ++j)
                {
                    AppExceptionAssoc assoc = this[i].FileTemplates[j];
                    if (assoc.DoesExecutableSatisfy(exe))
                    {
                        file = assoc.InstantiateWithNewExecutable(executablePath);
                        return this[i];
                    }
                }
            }

            file = null;
            return null;
        }
    }
}
