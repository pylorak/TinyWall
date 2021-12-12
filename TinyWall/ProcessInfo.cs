using System.Collections.Generic;
using pylorak.Windows;

namespace pylorak.TinyWall
{
    internal class ProcessInfo
    {
        public uint Pid;
        public string Path;
        public UwpPackage.Package? Package;
        public HashSet<string> Services;

        private ProcessInfo(uint pid, string path, UwpPackage.Package? package, HashSet<string> services)
        {
            Pid = pid;
            Path = path;
            Package = package;
            Services = services;
        }

        public static ProcessInfo Create(uint pid, UwpPackage uwp, ServicePidMap servicePids)
        {
            return new ProcessInfo(
                pid,
                Utils.GetPathOfProcessUseTwService(pid, GlobalInstances.Controller),
                uwp.FindPackage(ProcessManager.GetAppContainerSid(pid)),
                servicePids.GetServicesInPid(pid)
            );
        }
        public static ProcessInfo Create(uint pid, string path, UwpPackage uwp, ServicePidMap servicePids)
        {
            return new ProcessInfo(
                pid,
                path,
                uwp.FindPackage(ProcessManager.GetAppContainerSid(pid)),
                servicePids.GetServicesInPid(pid)
            );
        }
        public static ProcessInfo Create(uint pid, string path, string? packageId, UwpPackage uwp, ServicePidMap servicePids)
        {
            return new ProcessInfo(
                pid,
                path,
                uwp.FindPackage(packageId),
                servicePids.GetServicesInPid(pid)
            );
        }
    }
}

