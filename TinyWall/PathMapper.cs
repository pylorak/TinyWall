using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using PKSoft;

public enum DriveType
{
    Network,
    Local,
}

public sealed class PathMapper : IDisposable
{
    [SuppressUnmanagedCodeSecurity]
    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int QueryDosDevice(string lpDeviceName, [Out] StringBuilder lpTargetPath, int ucchMax);

    [SuppressUnmanagedCodeSecurity]
    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetVolumePathNamesForVolumeName(string lpszVolumeName, [Out] char[] lpszVolumePathNames, int cchBufferLength, out int lpcchReturnLength);

    public struct DriveCache
    {
        public string Device;
        public string Volume;
        public List<string> PathNames;
    }

    private StringBuilder sbuilder = new StringBuilder(260);
    public DriveCache[] Cache { get; private set; }
    private ManagementEventWatcher DriveWatcher;
    private ManualResetEvent CacheReadyEvent = new ManualResetEvent(false);
    private readonly object locker = new object();

    public PathMapper()
    {
        try
        {
            WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 5 WHERE Targetinstance ISA 'Win32_MountPoint'");
            DriveWatcher = new ManagementEventWatcher(insertQuery);
            DriveWatcher.EventArrived += Watcher_EventArrived;
            DriveWatcher.Start();
        }
        catch { }

        RebuildCache();
    }

    private void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
    {
        RebuildCache();
    }

    private void RebuildCache()
    {
        ThreadPool.QueueUserWorkItem(delegate (object arg)
        {
            const int MAX_PATH = 260;

            lock (locker)
            {
                StringBuilder sb = new StringBuilder(MAX_PATH);
                char[] buf = new char[MAX_PATH];
                List<DriveCache> newCache = new List<DriveCache>();

                foreach (var vol in FindVolumeSafeHandle.EnumerateVolumes())
                {
                    var cacheEntry = new DriveCache();

                    if ( (vol[0] != '\\')
                        || (vol[1] != '\\')
                        || (vol[2] != '?')
                        || (vol[3] != '\\')
                        || (vol[vol.Length - 1] != '\\') )
                    {
                        continue;
                    }
                    cacheEntry.Volume = vol;

                    string qddInput = vol.Substring(4, vol.Length - 5); // Also remove trailing backslash
                    int charCount = QueryDosDevice(qddInput, sb, sb.Capacity);
                    if (charCount > 0)
                    {
                        sb.Append('\\');
                        cacheEntry.Device = sb.ToString();
                    }

                    if (GetVolumePathNamesForVolumeName(vol, buf, buf.Length, out int expectedChars))
                    {
                        cacheEntry.PathNames = new List<string>();
                        int startIdx = 0;
                        int numChars = 0;
                        for (int i = 0; i < expectedChars; ++i)
                        {
                            if ((buf[i] == '\0') && (numChars > 0))
                            {
                                cacheEntry.PathNames.Add(new string(buf, startIdx, numChars));
                                startIdx = i + 1;
                                numChars = 0;
                            }
                            else
                                ++numChars;
                        }
                    }

                    newCache.Add(cacheEntry);
                }

                Cache = newCache.ToArray();
            }
            CacheReadyEvent.Set();
        }, null);
    }

    private DriveCache[] GetCache()
    {
        DriveCache[] ret = null;
        CacheReadyEvent.WaitOne();
        lock (locker)
        {
            ret = Cache;
        }
        return ret;
    }

    public string FromNtPath(string devicePath)
    {
        var drives = GetCache();

        foreach (var drive in drives)
        {
            if (devicePath.StartsWith(drive.Device, StringComparison.InvariantCultureIgnoreCase))
                return ReplaceFirst(devicePath, drive.Device, drive.PathNames[0], sbuilder);
        }
        return devicePath;
    }

    public string ToNtPath(string devicePath)
    {
        var drives = GetCache();

        foreach (var drive in drives)
        {
            if (devicePath.StartsWith(drive.PathNames[0], StringComparison.InvariantCultureIgnoreCase))
                return ReplaceFirst(devicePath, drive.PathNames[0], drive.Device, sbuilder);
        }
        return devicePath;
    }

    private static string GetDevicePath(DriveInfo driveInfo)
    {
        var devicePathBuilder = new StringBuilder(128);
        string ret = QueryDosDevice(GetDriveLetter(driveInfo), devicePathBuilder, devicePathBuilder.Capacity + 1) != 0
            ? devicePathBuilder.ToString()
            : null;
        return ret;
    }

    private static string GetDriveLetter(DriveInfo driveInfo)
    {
        return driveInfo.Name.Substring(0, 2);
    }

    private static string ReplaceFirst(string text, string search, string replace, StringBuilder sb)
    {
        int pos = text.IndexOf(search, StringComparison.InvariantCultureIgnoreCase);
        if (pos < 0)
            return text;
        else
        {
            //return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
            int tmp = pos + search.Length;
            sb.Length = 0;
            sb.Append(text, 0, pos);
            sb.Append(replace);
            sb.Append(text, tmp, text.Length - tmp);
            return sb.ToString();
        }
    }

    public void Dispose()
    {
        DriveWatcher?.Dispose();
        CacheReadyEvent.WaitOne();
        CacheReadyEvent.Close();
    }
}
