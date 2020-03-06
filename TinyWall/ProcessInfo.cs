
namespace PKSoft
{
    internal class ProcessInfo
    {
        public int Pid;
        public string ExePath;
        public UwpPackage.Package? Package;

        public ProcessInfo(int pid)
        {
            Pid = pid;
        }

        public ProcessInfo(int pid, UwpPackage uwp)
        {
            Pid = pid;
            ExePath = Utils.GetPathOfProcessUseTwService(pid, GlobalInstances.Controller);
            Package = uwp.FindPackage(ProcessManager.GetAppContainerSid(pid));
        }
    }
}

