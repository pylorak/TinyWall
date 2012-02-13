using System;
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
    [Serializable]  // Needed for ICloneable implementation 
    public class ProfileAssoc : ICloneable
    {
        // Filename or full path. File must have this name.
        private string m_Executable;
        [XmlAttributeAttribute()]
        public string Executable
        {
            get { return m_Executable; }
            set
            {
                m_Executable = value;
                PublicKeys = null;
                HashesSHA1 = null;
                MinVersion = null;
                MaxVersion = null;
            }
        }

        // Short name of the service
        [XmlAttributeAttribute()]
        public string Service;

        // Human readable name or description of the executable
        public string Description;

        // List of profiles to associate with above executable if all conditions below are met.
        public string[] Profiles;

        // List of locations that specify where to search for this file.
        // File path or registry location in the format "reg:<RegKey>:<RegValue>"
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

        // List of possible hash strings. If the array has more than one items,
        // only one needs to apply.
        private string[] m_HashesSHA1;
        public string[] HashesSHA1
        {
            get
            {
                if ((m_HashesSHA1 == null) && File.Exists(Executable))
                {
                    // Calculate hash, .Net will return it in binary form
                    byte[] hashBytes;
                    using (FileStream fs = new FileStream(Executable, FileMode.Open, FileAccess.Read))
                    using (SHA1Cng sha1 = new SHA1Cng())
                    {
                        hashBytes = sha1.ComputeHash(fs);
                    }

                    // Convert the byte array to a hexadecimal string
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("X2", CultureInfo.InvariantCulture));
                        // To force the hex string to lower-case letters instead of
                        // upper-case, use he following line instead:
                        // sb.Append(hashBytes[i].ToString("x2")); 
                    }
                    m_HashesSHA1 = new string[] { sb.ToString() };
                }

                return m_HashesSHA1;
            }
            set { m_HashesSHA1 = value; }
        }

        // Tries to get the actual file path based on the search crateria
        // specified by SearchPaths. Returns a collection of all files found.
        public ProfileAssocCollection SearchForFile()
        {
            ProfileAssocCollection foundFiles = new ProfileAssocCollection();

            string exec = PKSoft.Parser.RecursiveParser.ResolveString(this.Executable);
            if (IsValidExecutablePath(exec))
            {
                ProfileAssoc foundFile = ProfileAssoc.FromExecutable(exec, this.Service);
                if (this.DoesExecutableSatisfy(foundFile))
                {
                    foundFile = Utils.DeepClone(this);
                    foundFile.Executable = exec;
                    foundFiles.Add(foundFile);
                }
            }
            else
            {
                if (this.SearchPaths == null)
                    return foundFiles;

                for (int i = 0; i < this.SearchPaths.Length; ++i)
                {
                    string path = SearchPaths[i];

                    // Recursively resolve variables
                    string filePath = Environment.ExpandEnvironmentVariables(RecursiveParser.ResolveString(path));
                    filePath = Path.Combine(filePath, exec);

                    if (IsValidExecutablePath(filePath))
                    {
                        ProfileAssoc foundFile = ProfileAssoc.FromExecutable(filePath, this.Service);
                        if (this.DoesExecutableSatisfy(foundFile))
                        {
                            string resolvedPath = foundFile.Executable;
                            foundFile = Utils.DeepClone(this);
                            foundFile.Executable = resolvedPath;
                            foundFiles.Add(foundFile);
                        }
                    }
                }
            }

            return foundFiles;
        }

        public static bool IsValidExecutablePath(string path)
        {
            return
                string.IsNullOrEmpty(path)  // All files
                || path.Equals("System", StringComparison.OrdinalIgnoreCase)    // System-process
                || (File.Exists(path) && Path.IsPathRooted(path));  // File path on filesystem
        }

        // If present, the profiles for this file will only apply
        // if the executable's product version field is within this range.
        // Both, either one or none may be omitted.
        private string m_MinVersion;
        [XmlAttributeAttribute()]
        public string MinVersion
        {
            get
            {
                if ((m_MinVersion == null) && File.Exists(Executable))
                {
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Executable);
                    m_MinVersion = fvi.ProductVersion;
                }
                return m_MinVersion;
            }
            set { m_MinVersion = value; }
        }

        private string m_MaxVersion;
        [XmlAttributeAttribute()]
        public string MaxVersion
        {
            get
            {
                if ((m_MaxVersion == null) && File.Exists(Executable))
                {
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Executable);
                    m_MaxVersion = fvi.ProductVersion;
                }
                return m_MaxVersion;
            }
            set { m_MaxVersion = value; }
        }

        public AppExceptionSettings ToExceptionSetting()
        {
            AppExceptionSettings ex = new AppExceptionSettings(Executable);
            ex.ServiceName = Service;
            ex.CreationDate = DateTime.Now;
            ex.Timer = AppExceptionTimer.Permanent;
            ex.Profiles = new string[Profiles.Length];

            Array.Copy(Profiles, ex.Profiles, Profiles.Length);

            return ex;
        }
        
        public static ProfileAssoc FromExecutable(string filePath, string service)
        {
            if (!IsValidExecutablePath(filePath))
                throw new FileNotFoundException();

            ProfileAssoc exe = new ProfileAssoc();
            exe.Executable = filePath;
            exe.Service = service;

            return exe;
        }

        public bool DoesExecutableSatisfy(ProfileAssoc exe)
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

            // File version must be in specified range
            if (!string.IsNullOrEmpty(this.MinVersion))
            {
                if (string.IsNullOrEmpty(exe.MinVersion))
                    return false;

                Version verThis = new Version(this.MinVersion);
                Version verExe = new Version(exe.MinVersion);
                if (verExe < verThis)
                    return false;
            }
            if (!string.IsNullOrEmpty(this.MaxVersion))
            {
                if (string.IsNullOrEmpty(exe.MaxVersion))
                    return false;

                Version verThis = new Version(this.MaxVersion);
                Version verExe = new Version(exe.MaxVersion);
                if (verExe > verThis)
                    return false;
            }

            // Do we have an SHA1 match? Either one of the listed hashes in this instance is sufficient.
            if ((this.HashesSHA1 != null) && (this.HashesSHA1.Length > 0))
            {
                if ((exe.HashesSHA1 == null) || (exe.HashesSHA1.Length == 0))
                    // We need a hash, but the executable doesn't have one.
                    return false;

                bool sha1match = false;
                for (int i = 0; i < this.HashesSHA1.Length; ++i)
                {
                    if (string.Compare(HashesSHA1[i], exe.HashesSHA1[0], StringComparison.InvariantCultureIgnoreCase) == 0)
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
                    if (string.Compare(PublicKeys[i], exe.PublicKeys[0], StringComparison.InvariantCultureIgnoreCase) == 0)
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

        public object Clone()
        {
            return Utils.DeepClone(this);
        }
    }
}
