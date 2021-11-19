using System;
using System.Collections.Generic;
using System.ServiceProcess;
using pylorak.Windows.Services;

namespace pylorak.TinyWall
{
    public class ServicePidMap
    {
        private Dictionary<uint, HashSet<string>> Cache = new Dictionary<uint, HashSet<string>>();

        public ServicePidMap()
        {
            using (var scm = new ServiceControlManager())
            {
                var services = ServiceController.GetServices();
                foreach (var service in services)
                {
                    if (service.Status != ServiceControllerStatus.Running)
                        continue;

                    uint pid = scm.GetServicePid(service.ServiceName) ?? 0;
                    if (pid != 0)
                    {
                        if (!Cache.ContainsKey(pid))
                            Cache.Add(pid, new HashSet<string>());
                        Cache[pid].Add(service.ServiceName);
                    }
                }
            }
        }

        public HashSet<string> GetServicesInPid(uint pid)
        {
            if (!Cache.ContainsKey(pid))
                return new HashSet<string>();
            else
                return new HashSet<string>(Cache[pid]);
        }
    }
}
