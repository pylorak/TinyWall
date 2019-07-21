using System;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.Serialization;
using TinyWall.Interface.Internal;

namespace TinyWall.Interface
{
    public enum SubjectType
    {
        Invalid,
        Global,
        Executable,
        Service
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public abstract class ExceptionSubject : IEquatable<ExceptionSubject>
    {
        public abstract SubjectType SubjectType { get; }

        public abstract bool Equals(ExceptionSubject other);

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            ExceptionSubject other = obj as ExceptionSubject;
            if (other == null)
                return false;
            else
                return Equals(other);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public static ExceptionSubject Construct(string arg1, string arg2 = null)
        {
            if (string.IsNullOrEmpty(arg1))
                throw new ArgumentNullException();

            // Try GlobalSubject
            if (arg1.Equals("*"))
            {
                if (!string.IsNullOrEmpty(arg2))
                    throw new ArgumentException();
                return GlobalSubject.Instance;
            }

            // Try ExecutableSubject
            if (string.IsNullOrEmpty(arg2))
            {
                return new ExecutableSubject(arg1);
            }

            // Try ServiceSubject
            return new ServiceSubject(arg1, arg2);
        }
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public class GlobalSubject : ExceptionSubject
    {
        public static GlobalSubject Instance { get; } = new GlobalSubject();

        public override SubjectType SubjectType
        {
            get
            {
                return Interface.SubjectType.Global;
            }
        }

        public override bool Equals(ExceptionSubject other)
        {
            if (null == other)
                return false;

            if (other.GetType() != this.GetType())
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode();
        }

        public override string ToString()
        {
            return "Global";
        }
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public class ExecutableSubject : ExceptionSubject
    {
        public override SubjectType SubjectType
        {
            get
            {
                return Interface.SubjectType.Executable;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public string ExecutablePath { get; private set; }

        public string ExecutableName
        {
            get { return System.IO.Path.GetFileName(ExecutablePath); }
        }

        public ExecutableSubject(string filePath)
        {
            this.ExecutablePath = filePath;
        }

        [NonSerialized]
        private string _HashSha1;
        public string HashSha1
        {
            get
            {
                if (string.IsNullOrEmpty(_HashSha1))
                {
                    _HashSha1 = Hasher.HashFileSha1(this.ExecutablePath);
                }
                return _HashSha1;
            }
        }

        [NonSerialized]
        private string _CertSubject;
        public string CertSubject
        {
            get
            {
                if (string.IsNullOrEmpty(_CertSubject))
                {
                    try
                    {
                        X509Certificate cert = X509Certificate.CreateFromSignedFile(this.ExecutablePath);
                        _CertSubject = cert.Subject;
                    }
                    catch { }
                }
                return _CertSubject;
            }
        }

        [NonSerialized]
        private bool? _CertValid;
        public bool CertValid
        {
            get
            {
                if (!_CertValid.HasValue)
                {
                    _CertValid = WinTrust.VerifyFileAuthenticode(this.ExecutablePath);
                }
                return _CertValid.Value;
            }
        }

        public bool IsSigned
        {
            get
            {
                return !string.IsNullOrEmpty(this.CertSubject);
            }
        }

        public static string ResolvePath(string path)
        {
            string ret = path;
            ret = Environment.ExpandEnvironmentVariables(Parser.RecursiveParser.ResolveString(ret));
            if (NetworkPath.IsNetworkPath(ret))
            {
                if (!NetworkPath.IsUncPath(ret))
                {
                    try
                    {
                        ret = NetworkPath.GetUncPath(ret);
                    }
                    catch { }
                }
            }

            return ret;
        }

        public virtual ExecutableSubject ToResolved()
        {
            return new ExecutableSubject(ResolvePath(ExecutablePath));
        }

        public override bool Equals(ExceptionSubject other)
        {
            if (null == other)
                return false;

            if (other.GetType() != this.GetType())
                return false;

            ExecutableSubject o = other as ExecutableSubject;
            if (!string.Equals(ExecutablePath, o.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                const int OFFSET_BASIS = unchecked((int)2166136261u);
                const int FNV_PRIME = 16777619;

                int hash = OFFSET_BASIS;
                if (null != ExecutablePath)
                    hash = (hash ^ ExecutablePath.GetHashCode()) * FNV_PRIME;

                return hash;
            }
        }

        public override string ToString()
        {
            return ExecutableName;
        }
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public class ServiceSubject : ExecutableSubject
    {
        public override SubjectType SubjectType
        {
            get
            {
                return Interface.SubjectType.Service;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public string ServiceName { get; private set; }

        public ServiceSubject(string filePath, string serviceName) :
            base(filePath)
        {
            if (string.IsNullOrEmpty(serviceName))
                throw new ArgumentException();

            this.ServiceName = serviceName;
        }

        public override ExecutableSubject ToResolved()
        {
            return new ServiceSubject(ResolvePath(ExecutablePath), ServiceName);
        }

        public override bool Equals(ExceptionSubject other)
        {
            if (!base.Equals(other))
                return false;

            ServiceSubject o = other as ServiceSubject;
            if (!string.Equals(ServiceName, o.ServiceName, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                const int OFFSET_BASIS = unchecked((int)2166136261u);
                const int FNV_PRIME = 16777619;

                int hash = OFFSET_BASIS;
                if (null != ExecutablePath)
                    hash = (hash ^ ExecutablePath.GetHashCode()) * FNV_PRIME;
                if (null != ServiceName)
                    hash = (hash ^ ServiceName.GetHashCode()) * FNV_PRIME;

                return hash;
            }
        }

        public override string ToString()
        {
            return $"Service: {ServiceName}";
        }
    }
}
