using System;
using System.IO;

namespace pylorak.TinyWall
{
    public static class AppPaths
    {
        public static string ExecutablePath { get; } = System.Reflection.Assembly.GetEntryAssembly().Location;
        public static string ExecutableFolder { get; } = Path.GetDirectoryName(ExecutablePath);

        public static string PrivateTemp { get; } = Path.Combine(AppDataPath, "temp");

        public static string AppDataPath
        {
            get
            {
#if DEBUG
                return ExecutableFolder;
#else
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TinyWall");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
#endif
            }
        }

        public static string UserDataPath
        {
            get
            {
#if DEBUG
                return ExecutableFolder;
#else
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TinyWall");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
#endif
            }
        }

        public static void EmptyFolder(string folderPath, bool removeBase)
        {
            try
            {
                if (removeBase)
                {
                    Directory.Delete(folderPath, true);
                }
                else
                {
                    var files = Directory.GetFiles(folderPath);
                    foreach (var f in files)
                    {
                        try { File.Delete(f); }
                        catch { }
                    }

                    var dirs = Directory.GetDirectories(folderPath);
                    foreach (var d in dirs)
                    {
                        try { Directory.Delete(d, true); }
                        catch { }
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                // Not an error, ignore
            }
            catch (Exception e)
            {
                // We might be executing in an (un)installer, so never fail, only log.
                Utils.LogException(e, Utils.LOG_ID_INSTALLER);
            }
        }
    }
}
