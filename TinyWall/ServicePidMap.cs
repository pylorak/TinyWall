using System;
using System.Collections.Generic;
using System.ServiceProcess;

namespace PKSoft
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

                    uint pid = scm.GetServicePid(service.ServiceName);
                    if (!Cache.ContainsKey(pid))
                        Cache.Add(pid, new HashSet<string>());
                    Cache[pid].Add(service.ServiceName);
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
