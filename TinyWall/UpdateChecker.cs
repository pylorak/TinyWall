using System;
using System.Globalization;
using System.IO;
using System.Net;

namespace PKSoft
{
    [Serializable]
    public class UpdateModule
    {
        public string Component;
        public string Version;
        public string UpdateURL;
    }

    [Serializable]
    public class UpdateDescriptor
    {
        public string MagicWord = "TinyWall Update Descriptor";
        public UpdateModule[] Modules;
    }

    internal static class UpdateChecker
    {
        private const int UPDATER_VERSION = 2;
        private const string URL_UPDATE_DESCRIPTOR = @"http://tinywall.pados.hu/updates/UpdVer{0}/updesc.xml";

        internal static UpdateDescriptor GetDescriptor()
        {
            string url = string.Format(CultureInfo.InvariantCulture, URL_UPDATE_DESCRIPTOR, UPDATER_VERSION);
            string tmpFile = Path.GetTempFileName();

            try
            {
                using (WebClient HTTPClient = new WebClient())
                {
                    HTTPClient.DownloadFile(url, tmpFile);
                }

                UpdateDescriptor descriptor = SerializationHelper.LoadFromXMLFile<UpdateDescriptor>(tmpFile);
                if (descriptor.MagicWord != "TinyWall Update Descriptor")
                    throw new ApplicationException("Bad update descriptor file.");

                return descriptor;
            }
            finally
            {
                if (File.Exists(tmpFile))
                    File.Delete(tmpFile);
            }
        }

        internal static UpdateModule GetMainAppModule(UpdateDescriptor descriptor)
        {
            for (int i = 0; i < descriptor.Modules.Length; ++i)
            {
                if (descriptor.Modules[i].Component == "TinyWall")
                    return descriptor.Modules[i];
            }

            return null;
        }
        internal static UpdateModule GetHostsFileModule(UpdateDescriptor descriptor)
        {
            for (int i = 0; i < descriptor.Modules.Length; ++i)
            {
                if (descriptor.Modules[i].Component == "HostsFile")
                    return descriptor.Modules[i];
            }

            return null;
        }
    }
}
