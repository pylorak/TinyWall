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
        Service,
        AppContainer
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    public abstract class ExceptionSubject : IEquatable<ExceptionSubject>
    {
        public abstract SubjectType SubjectType { get; }

        public abstract bool Equals(ExceptionSubject other);

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is ExceptionSubject other)
                return Equals(other);
            else
                return false;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public static ExceptionSubject Construct(string arg1, string? arg2 = null)
        {
            if (string.IsNullOrEmpty(arg1))
                throw new ArgumentException(nameof(arg1));

            // Try GlobalSubject
            if (arg1.Equals("*"))
            {
                if (!string.IsNullOrEmpty(arg2))
                    throw new ArgumentException(nameof(arg2));
                return GlobalSubject.Instance;
            }

            // Try ExecutableSubject
            if (string.IsNullOrEmpty(arg2))
            {
                return new ExecutableSubject(arg1);
            }

            // Try ServiceSubject
            return new ServiceSubject(arg1, arg2!);
        }
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
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
            if (other is null)
                return false;

            return (other is GlobalSubject);
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

        private string? _HashSha1;
        public string HashSha1
        {
            get
            {
                if (_HashSha1 is null)
                    _HashSha1 = Hasher.HashFileSha1(this.ExecutablePath);

                return _HashSha1;
            }
        }

        private string? _CertSubject;
        public string? CertSubject
        {
            get
            {
                if (_CertSubject is null)
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

        private WinTrust.VerifyResult? _CertStatus;
        public bool CertValid
        {
            get
            {
                if (!_CertStatus.HasValue)
                {
                    _CertStatus = WinTrust.VerifyFileAuthenticode(this.ExecutablePath);
                }
                return WinTrust.VerifyResult.SIGNATURE_VALID == _CertStatus;
            }
        }

        public bool IsSigned
        {
            get
            {
                if (!_CertStatus.HasValue)
                {
                    _CertStatus = WinTrust.VerifyFileAuthenticode(this.ExecutablePath);
                }
                return WinTrust.VerifyResult.SIGNATURE_MISSING != _CertStatus;
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
            if (other is null)
                return false;

            if (other is ExecutableSubject o)
                return string.Equals(ExecutablePath, o.ExecutablePath, StringComparison.OrdinalIgnoreCase);
            else
                return false;
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
                throw new ArgumentException(nameof(serviceName));

            this.ServiceName = serviceName;
        }

        public override ExecutableSubject ToResolved()
        {
            return new ServiceSubject(ResolvePath(ExecutablePath), ServiceName);
        }

        public override bool Equals(ExceptionSubject other)
        {
            if (other is null)
                return false;

            if (!base.Equals(other))
                return false;

            if (other is ServiceSubject o)
                return string.Equals(ServiceName, o.ServiceName, StringComparison.OrdinalIgnoreCase);
            else
                return false;
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
            return $"{ServiceName} ({ExecutableName})";
        }
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    public class AppContainerSubject : ExceptionSubject
    {
        public override SubjectType SubjectType
        {
            get
            {
                return Interface.SubjectType.AppContainer;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public string Sid { get; private set; }

        [DataMember(EmitDefaultValue = false)]
        public string DisplayName { get; private set; }

        [DataMember(EmitDefaultValue = false)]
        public string Publisher { get; private set; }

        [DataMember(EmitDefaultValue = false)]
        public string PublisherId { get; private set; }

        public AppContainerSubject(string sid, string displayName, string publisher, string publisherId)
        {
            this.Sid = sid;
            this.DisplayName = displayName;
            this.Publisher = publisher;
            this.PublisherId = publisherId;
        }

        public override bool Equals(ExceptionSubject other)
        {
            if (other is null)
                return false;

            if (other is AppContainerSubject o)
                return string.Equals(Sid, o.Sid, StringComparison.OrdinalIgnoreCase);
            else
                return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                const int OFFSET_BASIS = unchecked((int)2166136261u);
                const int FNV_PRIME = 16777619;

                int hash = OFFSET_BASIS;
                if (null != Sid)
                    hash = (hash ^ Sid.GetHashCode()) * FNV_PRIME;

                return hash;
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    // -----------------------------------------------------------------------
}
