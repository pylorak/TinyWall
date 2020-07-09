﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using PKSoft;

public enum PathFormat
{
    NativeNt,
    Volume,
    Win32
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

    private DriveCache[] _cache;
    public DriveCache[] Cache
    {
        get
        {
            DriveCache[] ret = null;
            CacheReadyEvent.WaitOne();
            lock (locker)
            {
                ret = _cache;
            }
            return ret;
        }

        private set
        {
            lock(locker)
            {
                _cache = value;
            }
            CacheReadyEvent.Set();
        }
    }

    private void RebuildCache()
    {
        ThreadPool.QueueUserWorkItem(delegate (object arg)
        {
            const int MAX_PATH = 260;

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
        }, null);
    }

    public string ConvertPathIgnoreErrors(string path, PathFormat target)
    {
        try
        {
            return ConvertPath(path, target);
        }
        catch
        {
            return path;
        }
    }

    public string ConvertPath(string path, PathFormat target)
    {
        string ret = path;
        StringBuilder sb = new StringBuilder();

        ret = ReplaceLeading(ret, @"\SystemRoot", Environment.GetFolderPath(Environment.SpecialFolder.System), sb);
        ret = ReplaceLeading(ret, @"\\?\", string.Empty, sb);
        ret = ReplaceLeading(ret, @"\\.\", string.Empty, sb);
        ret = ReplaceLeading(ret, @"\??\", string.Empty, sb);
        ret = ReplaceLeading(ret, @"UNC\",@"\\", sb);
        ret = ReplaceLeading(ret, @"GLOBALROOT\", string.Empty, sb);
        ret = ReplaceLeading(ret, @"\Device\Mup\", @"\\", sb);

        if ((ret.Length >=2) && (ret[0] == '\\') && (ret[1] == '\\'))
        {   // UNC path, like \\server\share\directory\file
            if (target == PathFormat.Win32)
                return ret;

            switch (target)
            {
                case PathFormat.NativeNt:
                    return @"\Device\Mup\" + ret.Substring(2);
                default:
                    throw new NotSupportedException();
            }
        }
        else if ((ret.Length >= 3) && char.IsLetter(ret[0]) && (ret[1] == ':') && (ret[2] == '\\'))
        {   // Win32 drive letter format, like C:\Windows\explorer.exe

            if (target == PathFormat.Win32)
                return ret;

            var dc = Cache;
            for (int i = 0; i < dc.Length; ++i)
            {
                for (int j = 0; j < dc[i].PathNames.Count; ++j)
                {
                    if (ret.StartsWith(dc[i].PathNames[j], StringComparison.OrdinalIgnoreCase))
                    {
                        string trailing = ret.Substring(dc[i].PathNames[j].Length);
                        switch (target)
                        {
                            case PathFormat.NativeNt:
                                return Path.Combine(dc[i].Device, trailing);
                            case PathFormat.Volume:
                                return Path.Combine(dc[i].Volume, trailing);
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }
            }

            throw new DriveNotFoundException();
        }
        else if (ret.StartsWith("Volume{", StringComparison.OrdinalIgnoreCase))
        {   // Volume GUID path, like \\?\Volume{26a21bda-a627-11d7-9931-806e6f6e6963}\Windows\explorer.exe

            if (target == PathFormat.Volume)
                return path;

            ret = @"\\?\" + ret;
            var dc = Cache;
            for (int i = 0; i < dc.Length; ++i)
            {
                if (ret.StartsWith(dc[i].Volume, StringComparison.OrdinalIgnoreCase))
                {
                    string trailing = ret.Substring(dc[i].Volume.Length);
                    switch (target)
                    {
                        case PathFormat.NativeNt:
                            return Path.Combine(dc[i].Device, trailing);
                        case PathFormat.Win32:
                            if (dc[i].PathNames.Count > 0)
                                return Path.Combine(dc[i].PathNames[0], trailing);
                            else
                                throw new NotSupportedException();
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            throw new DriveNotFoundException();
        }
        else
        {   // Assume native NT device path, like \Device\HarddiskVolume1\Windows\explorer.exe
            if (target == PathFormat.NativeNt)
                return path;

            var dc = Cache;
            for (int i = 0; i < dc.Length; ++i)
            {
                if (ret.StartsWith(dc[i].Device, StringComparison.OrdinalIgnoreCase))
                {
                    string trailing = ret.Substring(dc[i].Device.Length);
                    switch (target)
                    {
                        case PathFormat.Volume:
                            return Path.Combine(dc[i].Volume, trailing);
                        case PathFormat.Win32:
                            if (dc[i].PathNames.Count > 0)
                                return Path.Combine(dc[i].PathNames[0], trailing);
                            else
                                throw new DriveNotFoundException();
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            throw new DriveNotFoundException();
        }
    }

    private static string ReplaceLeading(string haystack, string needle, string replacement, StringBuilder sb)
    {
        int pos = haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        if (pos != 0)
            return haystack;
        else
        {
            sb.Length = 0;
            sb.Append(replacement);
            sb.Append(haystack, needle.Length, haystack.Length - needle.Length);
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