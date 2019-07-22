using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using TinyWall.Interface.Parser;
using TinyWall.Interface;

namespace PKSoft.DatabaseClasses
{
    [DataContract(Namespace = "TinyWall")]
    internal sealed class SubjectIdentity
    {
        public SubjectIdentity(ExceptionSubject subject)
        {
            Subject = subject;
        }

        [DataMember(EmitDefaultValue = false)]
        internal ExceptionSubject Subject { get; set; }
        [DataMember(EmitDefaultValue = false)]
        internal ExceptionPolicy Policy { get; set; }

        // List of locations that specify where to search for this file.
        // Any string that can be resolved by RecursiveParser is valid.
        [DataMember(EmitDefaultValue = false)]
        internal List<string> SearchPaths { get; set; }

        // List of possible public keys.
        // If the array has more than one items, only one needs to apply.
        [DataMember(EmitDefaultValue = false)]
        internal List<string> CertificateSubjects { get; set; }

        // List of possible hash strings.
        // If the array has more than one items, only one needs to apply.
        [DataMember(EmitDefaultValue = false)]
        public List<string> AllowedSha1 { get; set; }

        public FirewallExceptionV3 InstantiateException(ExceptionSubject withSubject)
        {
            return new FirewallExceptionV3(withSubject, this.Policy);
        }

        // Tries to get the actual file path based on the search criteria
        // specified by SearchPaths. Writes found files to ExecutableRealizations.
        public List<ExecutableSubject> SearchForFile(string pathHint = null)
        {
            List<ExecutableSubject> ret = new List<ExecutableSubject>();

            // If the subject is not a file, we cannot search for it
            ExecutableSubject exesub = Subject as ExecutableSubject;
            if (null == exesub)
                return ret;

            ExecutableSubject resolvedSubject = exesub.ToResolved();
            if (IsValidExecutablePath(resolvedSubject.ExecutablePath))
            {
                if (this.DoesExecutableSatisfy(resolvedSubject))
                {
                    ret.Add(resolvedSubject);
                }
            }

            List<string> searchPaths = new List<string>();
            if (SearchPaths != null) searchPaths.AddRange(SearchPaths);
            if (!string.IsNullOrEmpty(pathHint)) searchPaths.Add(pathHint);

            if (ret.Count == 0)
            {
                for (int i = 0; i < searchPaths.Count; ++i)
                {
                    string path = searchPaths[i];

                    // Recursively resolve variables
                    string folderPath = Environment.ExpandEnvironmentVariables(RecursiveParser.ResolveString(path));
                    string filePath = Path.Combine(folderPath, exesub.ExecutableName);

                    if (IsValidExecutablePath(resolvedSubject.ExecutablePath))
                    {
                        ExecutableSubject testee = null;
                        if (this.Subject.SubjectType == SubjectType.Service)
                            testee = new ServiceSubject(filePath, (this.Subject as ServiceSubject).ServiceName);
                        else
                            testee = new ExecutableSubject(filePath);

                        if (this.DoesExecutableSatisfy(testee))
                        {
                            ret.Add(testee);
                        }
                    }
                }
            }

            return ret;
        }

        public static bool IsValidExecutablePath(string path)
        {
            return
                path.Equals("*")  // All files
                || path.Equals("System", StringComparison.OrdinalIgnoreCase)    // System-process
                || Path.IsPathRooted(path) && (File.Exists(path));  // File path on filesystem
        }

        public bool DoesExecutableSatisfy(ExceptionSubject subject)
        {
            if (null == subject)
                throw new ArgumentNullException();

            ExecutableSubject reference = this.Subject as ExecutableSubject;
            ExecutableSubject testee = subject as ExecutableSubject;
            if ((testee != null) && (reference != null))
            {
                reference = reference.ToResolved();

                if (reference.ExecutablePath == reference.ExecutableName)   // This condition checks whetehr the reference is just a file name or a full path
                {
                    // File name must match
                    if (string.Compare(reference.ExecutableName, testee.ExecutableName, StringComparison.OrdinalIgnoreCase) != 0)
                        return false;
                }
                else
                {
                    // File path must match
                    if (string.Compare(reference.ExecutablePath, testee.ExecutablePath, StringComparison.OrdinalIgnoreCase) != 0)
                        return false;
                }

                /* For now we don't want to match service names, otherwise enable this block
                ServiceSubject referenceSrv = reference as ServiceSubject;
                ServiceSubject testeeSrv = testee as ServiceSubject;
                if ((testeeSrv != null) && (referenceSrv != null))
                {
                    // Service name must match
                    if (string.Compare(testeeSrv.ServiceName, referenceSrv.ServiceName, StringComparison.OrdinalIgnoreCase) != 0)
                        return false;
                }
                */

                // Do we have an SHA1 match? Either one of the listed hashes in this instance is sufficient.
                if ((this.AllowedSha1 != null) && (this.AllowedSha1.Count > 0))
                {
                    for (int i = 0; i < this.AllowedSha1.Count; ++i)
                    {
                        if (string.Compare(AllowedSha1[i], testee.HashSha1, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return true;
                        }
                    }
                }

                // Do we have a public key match? Either one of the listed keys in this instance is sufficient.
                if ((this.CertificateSubjects.Count > 0) && testee.IsSigned && testee.CertValid)
                {
                    for (int i = 0; i < this.CertificateSubjects.Count; ++i)
                    {
                        if (string.Compare(CertificateSubjects[i], testee.CertSubject, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return true;
                        }
                    }
                }
            }

            // None of the identity proofs could be verified, so let's fail
            return false;
        }

        public override string ToString()
        {
            return Subject.ToString();
        }
    }
}

