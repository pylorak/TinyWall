using System;
using System.Runtime.Serialization;

namespace TinyWall.Interface
{
    // The default value must have value Zero.
    public enum AppExceptionTimer
    {
        Permanent = 0,
        Until_Reboot = -1,
        For_5_Minutes = 5,
        For_30_Minutes = 30,
        For_1_Hour = 60,
        For_4_Hours = 240,
        For_9_Hours = 540,
        For_24_Hours = 1140,
        Invalid
    }

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public class FirewallExceptionV3
    {
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

        public FirewallExceptionV3()
        {
            Timer = AppExceptionTimer.Permanent;
            CreationDate = DateTime.Now;
            RegenerateId();
        }

        public FirewallExceptionV3(ExceptionSubject subject, ExceptionPolicy policy) :
            this()
        {
            this.Subject = subject;
            this.Policy = policy;
        }

        public void RegenerateId()
        {
            Id = Guid.NewGuid();
        }

        public override string ToString()
        {
            return Subject.ToString();
        }
    }
}
