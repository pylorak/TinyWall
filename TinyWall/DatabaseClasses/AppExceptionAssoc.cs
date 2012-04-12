using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Win32;
using PKSoft.Parser;

namespace PKSoft
{
    [Serializable]  // Needed for cloning
    public class AppExceptionAssoc
    {
        public AppExceptionAssoc()
        {
        }

        public AppExceptionAssoc(string exec, string service = null)
        {
            m_Executable = exec;
            Service = service;
        }

        // Filename or full path. File must have this name.
        private string m_Executable;
        [XmlAttributeAttribute]
        public string Executable
        {
            get { return m_Executable; }
            set
            {
                m_Executable = value;
                m_PublicKeys = null;
                m_Hashes = null;
            }
        }

        // Short name of the service
        [XmlAttributeAttribute]
        public string Service;

        // Found files after searching in the user's file system.
        [NonSerialized]
        private List<string> _ExecutableRealizations;

        [XmlIgnore]
        public List<string> ExecutableRealizations
        {
            get
            {
                if (_ExecutableRealizations == null)
                    _ExecutableRealizations = new List<string>();

                return _ExecutableRealizations;
            }
            set
            {
                _ExecutableRealizations = value;
            }
        }

        // Description of the firewall exception.
        public FirewallException ExceptionTemplate;

        internal FirewallException CreateException(string fullPath)
        {
            FirewallException res = Utils.DeepClone(ExceptionTemplate);
            res.ExecutablePath = fullPath;
            res.ServiceName = Service;
            res.RegenerateID();
            res.Template = false;
            return res;
        }

        // List of locations that specify where to search for this file.
        // Any string that can be resolved by RecursiveParser is valid.
        public string[] SearchPaths;

        // List of possible public keys.
        // If the array has more than one items, only one needs to apply.
        private string[] m_PublicKeys;
        public string[] PublicKeys
        {
            get
            {
                if ((m_PublicKeys == null) && File.Exists(Executable))
                {
                    try
                    {
                        X509Certificate cert = X509Certificate.CreateFromSignedFile(Executable);
                        m_PublicKeys = new string[] { cert.GetPublicKeyString() };
                    }
                    catch 
                    {
                        m_PublicKeys = new string[0];
                    }
                }

                return m_PublicKeys;
            }
            set { m_PublicKeys = value; }
        }

        // List of possible hash strings.
        // If the array has more than one items, only one needs to apply.
        private string[] m_Hashes;
        public string[] Hashes
        {
            get
            {
                if ((m_Hashes == null) && File.Exists(Executable))
                {
                    using (FileStream fs = new FileStream(Executable, FileMode.Open, FileAccess.Read))
                    using (SHA1Cng hasher = new SHA1Cng())
                    {
                        m_Hashes = new string[] { Utils.HexEncode(hasher.ComputeHash(fs)) };
                    }
                }

                return m_Hashes;
            }
            set { m_Hashes = value; }
        }

        // Tries to get the actual file path based on the search crateria
        // specified by SearchPaths. Writes found files to ExecutableRealizations.
        public bool SearchForFile(string pathHint = null)
        {
            ExecutableRealizations.Clear();

            string exec = PKSoft.Parser.RecursiveParser.ResolveString(this.Executable);
            if (IsValidExecutablePath(exec))
            {
                if (this.DoesExecutableSatisfy(exec, this.Service))
                {
                    ExecutableRealizations.Add(exec);
                }
            }

            if ((ExecutableRealizations.Count == 0) && (pathHint != null))
            {
                string fileName = Path.GetFileName(exec);
                string filePath = Path.Combine(pathHint, fileName);
                if (IsValidExecutablePath(filePath))
                {
                    if (this.DoesExecutableSatisfy(filePath, this.Service))
                    {
                        ExecutableRealizations.Add(filePath);
                    }
                }
            }

            if ((ExecutableRealizations.Count == 0) && (this.SearchPaths != null))
            {
                for (int i = 0; i < this.SearchPaths.Length; ++i)
                {
                    string path = SearchPaths[i];

                    // Recursively resolve variables
                    string filePath = Environment.ExpandEnvironmentVariables(RecursiveParser.ResolveString(path));
                    filePath = Path.Combine(filePath, exec);

                    if (IsValidExecutablePath(filePath))
                    {
                        if (this.DoesExecutableSatisfy(filePath, this.Service))
                        {
                            ExecutableRealizations.Add(filePath);
                        }
                    }
                }
            }

            return ExecutableRealizations.Count > 0;
        }

        internal AppExceptionAssoc InstantiateWithNewExecutable(string exec)
        {
            AppExceptionAssoc res = Utils.DeepClone(this);
            res.Executable = exec;
            return res;
        }

        public static bool IsValidExecutablePath(string path)
        {
            return
                string.IsNullOrEmpty(path)  // All files
                || path.Equals("System", StringComparison.OrdinalIgnoreCase)    // System-process
                || (File.Exists(path) && Path.IsPathRooted(path));  // File path on filesystem
        }
        
        public static AppExceptionAssoc FromExecutable(string filePath, string service)
        {
            if (!IsValidExecutablePath(filePath))
                throw new FileNotFoundException();

            return new AppExceptionAssoc(filePath, service);
        }

        public bool DoesExecutableSatisfy(string filePath, string service)
        {
            return DoesExecutableSatisfy(FromExecutable(filePath, service));
        }

        public bool DoesExecutableSatisfy(AppExceptionAssoc exe)
        {
            if (exe == null)
                return false;

            // We have a current/specific executable's information in the parameter "exe".
            // This method determine if the "exe" satisfies all conditions specified in this
            // instance of profile association.

            if (Path.GetFileName(this.Executable) == this.Executable)
            {
                // File name must match
                if (string.Compare(Path.GetFileName(this.Executable), Path.GetFileName(exe.Executable), StringComparison.OrdinalIgnoreCase) != 0)
                    return false;
            }
            else
            {
                // File path must match
                if (string.Compare(PKSoft.Parser.RecursiveParser.ResolveString(this.Executable), PKSoft.Parser.RecursiveParser.ResolveString(exe.Executable), StringComparison.OrdinalIgnoreCase) != 0)
                    return false;
            }

            // Service name must match
            if (!string.IsNullOrEmpty(this.Service) || !string.IsNullOrEmpty(exe.Service))
            {
                if (string.Compare(this.Service, exe.Service, StringComparison.OrdinalIgnoreCase) != 0)
                    return false;
            }

            // Do we have an SHA1 match? Either one of the listed hashes in this instance is sufficient.
            if ((this.Hashes != null) && (this.Hashes.Length > 0))
            {
                if ((exe.Hashes == null) || (exe.Hashes.Length == 0))
                    // We need a hash, but the executable doesn't have one.
                    return false;

                bool sha1match = false;
                for (int i = 0; i < this.Hashes.Length; ++i)
                {
                    if (string.Compare(Hashes[i], exe.Hashes[0], StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        sha1match = true;
                        break;
                    }
                }

                if (!sha1match)
                    return false;
            }

            // Do we have a public key match? Either one of the listed keys in this instance is sufficient.
            if ((this.PublicKeys != null) && (this.PublicKeys.Length > 0))
            {
                if ((exe.PublicKeys == null) || (exe.PublicKeys.Length < 1))
                    // We need a public key, but the executable doesn't have one.
                    return false;

                bool keymatch = false;
                for (int i = 0; i < this.PublicKeys.Length; ++i)
                {
                    if (string.Compare(PublicKeys[i], exe.PublicKeys[0], StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        keymatch = true;
                        break;
                    }
                }

                if (!keymatch)
                    return false;
            }
            
            // None of the above checks failed. So let us accept it.
            return true;
        }

        public override string ToString()
        {
            return this.Executable;
        }
    }
}
