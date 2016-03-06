using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyWall.Interface;
using TinyWall.Interface.Internal;

namespace PKSoft
{
    [Obsolete]
    internal static class ConfigManager
    {
        internal static ServerConfiguration LoadServerConfig()
        {
            ServerConfiguration ret = null;

            // Construct file path
            string SettingsFile = Path.Combine(ServiceSettings21.AppDataPath, "config");

            if (File.Exists(SettingsFile))
            {
                try
                {
                    ret = ServerConfiguration.Load(SettingsFile);
                }
                catch { }

                if (ret == null)
                {
                    // Try again by loading config file from older versions
                    try
                    {
                        ServiceSettings21 oldSettings = ServiceSettings21.Load();
                        ret = oldSettings.ToNewFormat();
                    }
                    catch { }
                }
            }

            if (ret == null)
            {
                ret = new ServerConfiguration();

                // Allow recommended exception
                DatabaseClasses.AppDatabase db = GlobalInstances.AppDatabase;
                foreach (DatabaseClasses.Application app in db.KnownApplications)
                {
                    if (app.HasFlag("TWUI:Special") && app.HasFlag("TWUI:Recommended"))
                    {
                        ret.ActiveProfile.SpecialExceptions.Add(app.Name);
                    }
                }
            }

            return ret;
        }
    }
}
