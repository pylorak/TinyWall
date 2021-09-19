using System.Collections.Generic;

namespace PKSoft
{
    internal class ProcessInfo
    {
        public int Pid;
        public string ExePath;
        public UwpPackage.Package? Package;
        public HashSet<string> Services;

        public ProcessInfo(int pid)
        {
            Pid = pid;
        }

        public ProcessInfo(int pid, UwpPackage uwp, ServicePidMap service_pids = null)
        {
            Pid = pid;
            ExePath = Utils.GetPathOfProcessUseTwService(pid, GlobalInstances.Controller);
            if (uwp != null)
                Package = uwp.FindPackage(ProcessManager.GetAppContainerSid(pid));
            if (service_pids != null)
                Services = service_pids.GetServicesInPid((uint)pid);
        }
    }
}

