using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace pylorak.TinyWall
{
    // The default value must have value Zero.
    public enum AppExceptionTimer
    {
        Permanent = 0,
        UNTIL_REBOOT = -1,
        FOR_5_MINUTES = 5,
        FOR_30_MINUTES = 30,
        FOR_1_HOUR = 60,
        FOR_4_HOURS = 240,
        FOR_9_HOURS = 540,
        FOR_24_HOURS = 1140,
        Invalid
    }

    [DataContract(Namespace = "TinyWall")]
    public class FirewallExceptionV3 : ISerializable<FirewallExceptionV3>
    {
        public static FirewallExceptionV3 Default { get; } = new FirewallExceptionV3(GlobalSubject.Instance, new UnrestrictedPolicy());

        [DataMember(EmitDefaultValue = false)]
        public Guid Id { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public DateTime CreationDate { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public AppExceptionTimer Timer { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public ExceptionSubject Subject { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public ExceptionPolicy Policy { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool ChildProcessesInherit { get; set; }

        public FirewallExceptionV3(ExceptionSubject subject, ExceptionPolicy policy)
        {
            Timer = AppExceptionTimer.Permanent;
            CreationDate = DateTime.Now;
            RegenerateId();

            Subject = subject;
            Policy = policy;
        }

        public void RegenerateId()
        {
            Id = Guid.NewGuid();
        }

        public override string ToString()
        {
            return Subject.ToString();
        }

        public JsonTypeInfo<FirewallExceptionV3> GetJsonTypeInfo()
        {
            return SourceGenerationContext.Default.FirewallExceptionV3;
        }
    }
}
