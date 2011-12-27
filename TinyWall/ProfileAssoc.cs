using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Serialization;

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

        // If Recommended is set to true, this profile will be recommended
        // (and enabled) by default to the user.
        [XmlAttributeAttribute()]
        public bool Recommended;

        // Specifies whether this application should show up
        // in the "special exceptions" list.
        [XmlAttributeAttribute()]
        public bool Special;

        // Human readable name or description of the executable
        public string Description;

        // List of profiles to associate with above executable if all conditions below are met.
        public string[] Profiles;

        /*
        // List of ports to open in addition to profiles
        public string OpenPortOutboundRemoteTCP = string.Empty;
        public string OpenPortListenLocalTCP = string.Empty;
        public string OpenPortOutboundRemoteUDP = string.Empty;
        public string OpenPortListenLocalUDP = string.Empty;
         */
        
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
            /*
            ex.OpenPortListenLocalTCP = this.OpenPortListenLocalTCP;
            ex.OpenPortListenLocalUDP = this.OpenPortListenLocalUDP;
            ex.OpenPortOutboundRemoteTCP = this.OpenPortOutboundRemoteTCP;
            ex.OpenPortOutboundRemoteUDP = this.OpenPortOutboundRemoteUDP;
             */

            Array.Copy(Profiles, ex.Profiles, Profiles.Length);

            return ex;
        }
        
        public static ProfileAssoc FromExecutable(string filepath, string service)
        {
            if (!File.Exists(filepath))
                throw new FileNotFoundException();

            ProfileAssoc exe = new ProfileAssoc();
            exe.Executable = filepath;
            exe.Service = service;

            return exe;
        }

        public bool DoesExecutableSatisfy(ProfileAssoc exe)
        {
            // We have a current/specific executable's information in the parameter "exe".
            // This method determine if the "exe" satisfies all conditions specified in this
            // instance of profile association.

            if (Path.GetFileName(this.Executable) == this.Executable)
            {
                // File name must match
                if (string.Compare(Path.GetFileName(this.Executable), Path.GetFileName(exe.Executable), StringComparison.InvariantCultureIgnoreCase) != 0)
                    return false;
            }
            else
            {
                // File path must match
                if (string.Compare(Utils.ExpandPathVars(this.Executable), Utils.ExpandPathVars(exe.Executable), StringComparison.InvariantCultureIgnoreCase) != 0)
                    return false;
            }

            // Service name must match
            if (!string.IsNullOrEmpty(this.Service) || !string.IsNullOrEmpty(exe.Service))
            {
                if (string.Compare(this.Service, exe.Service, StringComparison.InvariantCultureIgnoreCase) != 0)
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

        public bool ShouldSerializeRecommended()
        {
            return Recommended;
        }
        /*
        public bool ShouldSerializeOpenPortOutboundRemoteTCP()
        {
            return !string.IsNullOrEmpty(OpenPortOutboundRemoteTCP);
        }
        public bool ShouldSerializeOpenPortListenLocalTCP()
        {
            return !string.IsNullOrEmpty(OpenPortListenLocalTCP);
        }
        public bool ShouldSerializeOpenPortOutboundRemoteUDP()
        {
            return !string.IsNullOrEmpty(OpenPortOutboundRemoteUDP);
        }
        public bool ShouldSerializeOpenPortListenLocalUDP()
        {
            return !string.IsNullOrEmpty(OpenPortListenLocalUDP);
        }
         */

        public bool ShouldSerializeSpecial()
        {
            return Special;
        }
    }
}
