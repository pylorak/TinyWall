using pylorak.Windows;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace pylorak.TinyWall
{
    internal sealed class ProcessStartEventArgs : EventArgs
    {
        internal uint ProcessId { get; }
        internal uint ParentProcessId { get; }
        internal string ImagePath { get; }
        internal string CommandLine { get; }

        internal ProcessStartEventArgs(uint processId, uint parentProcessId, string imagePath, string commandLine)
        {
            ProcessId = processId;
            ParentProcessId = parentProcessId;
            ImagePath = imagePath;
            CommandLine = commandLine;
        }
    }

    internal sealed class ProcessStartWatcher : IDisposable
    {
        private const string SessionName = "TinyWall Process Monitor";
        private const uint ErrorSuccess = 0;
        private const uint ErrorAlreadyExists = 183;
        private const uint ErrorCancelled = 1223;
        private const uint ErrorInstanceNotFound = 4201;
        private const uint EventTraceControlStop = 1;
        private const uint EventTraceRealTimeMode = 0x00000100;
        private const uint EventTraceSystemLoggerMode = 0x02000000;
        private const uint EventTraceNoPerProcessorBuffering = 0x10000000;
        private const uint EventTraceFlagProcess = 0x00000001;
        private const uint EventTraceFlagNoSystemConfig = 0x10000000;
        private const uint ProcessTraceModeRealTime = 0x00000100;
        private const uint ProcessTraceModeEventRecord = 0x10000000;
        private const uint WnodeFlagTracedGuid = 0x00020000;
        private const byte ProcessStartOpcode = 1;
        private const uint PropertyArrayIndexAll = uint.MaxValue;
        private const ulong InvalidProcessTraceHandle = ulong.MaxValue;

        private static readonly Guid KernelProcessEventProvider = new("3d6fa8d0-fe05-11d0-9dda-00c04fd7ba7c");

        private readonly object SyncRoot = new();
        private readonly EventRecordCallback EventRecordCallbackDelegate;
        private Thread? ConsumerThread;
        private ulong SessionHandle;
        private ulong ConsumerHandle = InvalidProcessTraceHandle;
        private bool IsRunning;
        private bool IsDisposed;

        internal event EventHandler<ProcessStartEventArgs>? ProcessStarted;

        internal ProcessStartWatcher()
        {
            EventRecordCallbackDelegate = EventRecordReceived;
        }

        internal void Start()
        {
            lock (SyncRoot)
            {
                ThrowIfDisposed();
                if (IsRunning)
                    return;

                ulong sessionHandle = 0;
                ulong consumerHandle = InvalidProcessTraceHandle;
                try
                {
                    sessionHandle = StartSession();
                    consumerHandle = OpenConsumer();

                    SessionHandle = sessionHandle;
                    ConsumerHandle = consumerHandle;
                    IsRunning = true;
                    ConsumerThread = new Thread(ConsumeEvents)
                    {
                        IsBackground = true,
                        Name = "TinyWall ETW process monitor"
                    };
                    ConsumerThread.Start();
                }
                catch
                {
                    SessionHandle = 0;
                    ConsumerHandle = InvalidProcessTraceHandle;
                    ConsumerThread = null;
                    IsRunning = false;
                    if (consumerHandle != InvalidProcessTraceHandle)
                        NativeMethods.CloseTrace(consumerHandle);
                    if (sessionHandle != 0)
                        StopSession(sessionHandle);
                    throw;
                }
            }
        }

        internal void Stop()
        {
            Thread? consumerThread;
            ulong sessionHandle;
            ulong consumerHandle;

            lock (SyncRoot)
            {
                if (!IsRunning)
                    return;

                IsRunning = false;
                consumerThread = ConsumerThread;
                ConsumerThread = null;
                sessionHandle = SessionHandle;
                SessionHandle = 0;
                consumerHandle = ConsumerHandle;
                ConsumerHandle = InvalidProcessTraceHandle;
            }

            if (sessionHandle != 0)
                StopSession(sessionHandle);

            if (consumerThread != null && consumerThread != Thread.CurrentThread)
                consumerThread.Join();

            if (consumerHandle != InvalidProcessTraceHandle)
                NativeMethods.CloseTrace(consumerHandle);
        }

        public void Dispose()
        {
            lock (SyncRoot)
            {
                if (IsDisposed)
                    return;
                IsDisposed = true;
            }

            Stop();
        }

        private static ulong StartSession()
        {
            IntPtr properties = AllocateProperties();
            try
            {
                uint status = NativeMethods.StartTraceW(out ulong sessionHandle, SessionName, properties);
                if (status == ErrorAlreadyExists)
                {
                    NativeMethods.ControlTraceW(0, SessionName, properties, EventTraceControlStop);
                    Marshal.StructureToPtr(CreateProperties(), properties, false);
                    WriteSessionName(properties);
                    status = NativeMethods.StartTraceW(out sessionHandle, SessionName, properties);
                }

                ThrowOnError(status, "Unable to start the ETW process trace session.");
                return sessionHandle;
            }
            finally
            {
                Marshal.FreeHGlobal(properties);
            }
        }

        private ulong OpenConsumer()
        {
            var traceLog = new EventTraceLogfile
            {
                LoggerName = SessionName,
                ProcessTraceMode = ProcessTraceModeRealTime | ProcessTraceModeEventRecord,
                EventRecordCallback = Marshal.GetFunctionPointerForDelegate(EventRecordCallbackDelegate)
            };

            ulong handle = NativeMethods.OpenTraceW(ref traceLog);
            if (handle == InvalidProcessTraceHandle)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to open the ETW process trace consumer.");
            return handle;
        }

        private void ConsumeEvents()
        {
            ulong handle;
            lock (SyncRoot)
            {
                handle = ConsumerHandle;
            }

            uint status = NativeMethods.ProcessTrace(new[] { handle }, 1, IntPtr.Zero, IntPtr.Zero);
            if (status != ErrorSuccess && status != ErrorCancelled && status != ErrorInstanceNotFound)
                Utils.LogException(new Win32Exception((int)status, "The ETW process trace consumer stopped unexpectedly."), Utils.LOG_ID_SERVICE);

            ulong abandonedSessionHandle = 0;
            ulong abandonedConsumerHandle = InvalidProcessTraceHandle;
            lock (SyncRoot)
            {
                if (IsRunning && ConsumerThread == Thread.CurrentThread)
                {
                    IsRunning = false;
                    ConsumerThread = null;
                    abandonedSessionHandle = SessionHandle;
                    SessionHandle = 0;
                    abandonedConsumerHandle = ConsumerHandle;
                    ConsumerHandle = InvalidProcessTraceHandle;
                }
            }

            if (abandonedSessionHandle != 0)
                StopSession(abandonedSessionHandle);
            if (abandonedConsumerHandle != InvalidProcessTraceHandle)
                NativeMethods.CloseTrace(abandonedConsumerHandle);
        }

        private void EventRecordReceived(ref EventRecord eventRecord)
        {
            try
            {
                if (eventRecord.EventHeader.ProviderId != KernelProcessEventProvider
                    || eventRecord.EventHeader.EventDescriptor.Opcode != ProcessStartOpcode)
                {
                    return;
                }

                uint processId = ReadUInt32Property(ref eventRecord, "ProcessId", eventRecord.EventHeader.ProcessId);
                uint parentProcessId = ReadUInt32Property(ref eventRecord, "ParentId", 0);
                string commandLine = ReadUnicodeStringProperty(ref eventRecord, "CommandLine");
                string eventImagePath = ReadAnsiStringProperty(ref eventRecord, "ImageFileName");
                string imagePath = ResolveImagePath(processId, commandLine, eventImagePath);

                ProcessStarted?.Invoke(
                    this,
                    new ProcessStartEventArgs(processId, parentProcessId, imagePath, commandLine));
            }
            catch (Exception exception)
            {
                Utils.LogException(exception, Utils.LOG_ID_SERVICE);
            }
        }

        private static unsafe uint ReadUInt32Property(ref EventRecord eventRecord, string propertyName, uint fallback)
        {
            byte[]? value = ReadProperty(ref eventRecord, propertyName);
            return value != null && value.Length >= sizeof(uint)
                ? BitConverter.ToUInt32(value, 0)
                : fallback;
        }

        private static unsafe string ReadAnsiStringProperty(ref EventRecord eventRecord, string propertyName)
        {
            byte[]? value = ReadProperty(ref eventRecord, propertyName);
            if (value == null || value.Length == 0)
                return string.Empty;

            int terminator = Array.IndexOf(value, (byte)0);
            int length = terminator < 0 ? value.Length : terminator;
            return length == 0 ? string.Empty : System.Text.Encoding.Default.GetString(value, 0, length);
        }

        private static unsafe string ReadUnicodeStringProperty(ref EventRecord eventRecord, string propertyName)
        {
            byte[]? value = ReadProperty(ref eventRecord, propertyName);
            if (value == null || value.Length < sizeof(char))
                return string.Empty;

            int length = value.Length - (value.Length % sizeof(char));
            while (length >= sizeof(char) && value[length - 1] == 0 && value[length - 2] == 0)
                length -= sizeof(char);
            return length == 0 ? string.Empty : System.Text.Encoding.Unicode.GetString(value, 0, length);
        }

        private static string ResolveImagePath(uint processId, string commandLine, string eventImagePath)
        {
            string commandImagePath = ExtractExecutablePath(commandLine);
            if (!string.IsNullOrEmpty(commandImagePath))
            {
                commandImagePath = RestoreExecutableExtension(commandImagePath, eventImagePath);
                string normalizedCommandPath = PathMapper.Instance.ConvertPathIgnoreErrors(commandImagePath, PathFormat.Win32);
                if (IsAbsoluteWin32Path(normalizedCommandPath))
                    return normalizedCommandPath;
            }

            if (!string.IsNullOrEmpty(eventImagePath))
            {
                string normalizedEventPath = PathMapper.Instance.ConvertPathIgnoreErrors(eventImagePath, PathFormat.Win32);
                if (IsAbsoluteWin32Path(normalizedEventPath))
                    return normalizedEventPath;
            }

            // Some versions of the classic kernel process event expose only the image's
            // file name. Querying the new process immediately returns its full Unicode
            // native path, which ProcessManager converts to Win32 drive-letter format.
            return ProcessManager.GetProcessPath(processId);
        }

        private static string RestoreExecutableExtension(string commandImagePath, string eventImagePath)
        {
            if (Path.HasExtension(commandImagePath) || string.IsNullOrEmpty(eventImagePath))
                return commandImagePath;

            string eventFileName = Path.GetFileName(eventImagePath);
            if (!Path.HasExtension(eventFileName)
                || !string.Equals(
                    Path.GetFileName(commandImagePath),
                    Path.GetFileNameWithoutExtension(eventFileName),
                    StringComparison.OrdinalIgnoreCase))
            {
                return commandImagePath;
            }

            string? commandDirectory = Path.GetDirectoryName(commandImagePath);
            return string.IsNullOrEmpty(commandDirectory)
                ? eventFileName
                : Path.Combine(commandDirectory, eventFileName);
        }

        private static string ExtractExecutablePath(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return string.Empty;

            int start = 0;
            while (start < commandLine.Length && char.IsWhiteSpace(commandLine[start]))
                ++start;

            if (start == commandLine.Length)
                return string.Empty;

            if (commandLine[start] == '"')
            {
                int endQuote = commandLine.IndexOf('"', start + 1);
                return endQuote < 0
                    ? commandLine.Substring(start + 1)
                    : commandLine.Substring(start + 1, endQuote - start - 1);
            }

            int end = start;
            while (end < commandLine.Length && !char.IsWhiteSpace(commandLine[end]))
                ++end;
            return commandLine.Substring(start, end - start);
        }

        private static bool IsAbsoluteWin32Path(string path)
        {
            return (path.Length >= 3
                    && char.IsLetter(path[0])
                    && path[1] == ':'
                    && (path[2] == '\\' || path[2] == '/'))
                || (path.Length >= 2
                    && path[0] == '\\'
                    && path[1] == '\\');
        }

        private static unsafe byte[]? ReadProperty(ref EventRecord eventRecord, string propertyName)
        {
            fixed (char* propertyNamePtr = propertyName)
            {
                var descriptor = new PropertyDataDescriptor
                {
                    PropertyName = (ulong)propertyNamePtr,
                    ArrayIndex = PropertyArrayIndexAll
                };

                uint status = NativeMethods.TdhGetPropertySize(
                    ref eventRecord,
                    0,
                    IntPtr.Zero,
                    1,
                    ref descriptor,
                    out uint propertySize);
                if (status != ErrorSuccess || propertySize == 0 || propertySize > ushort.MaxValue)
                    return null;

                var value = new byte[propertySize];
                fixed (byte* valuePtr = value)
                {
                    status = NativeMethods.TdhGetProperty(
                        ref eventRecord,
                        0,
                        IntPtr.Zero,
                        1,
                        ref descriptor,
                        propertySize,
                        (IntPtr)valuePtr);
                }

                return status == ErrorSuccess ? value : null;
            }
        }

        private static void StopSession(ulong sessionHandle)
        {
            IntPtr properties = AllocateProperties();
            try
            {
                uint status = NativeMethods.ControlTraceW(sessionHandle, SessionName, properties, EventTraceControlStop);
                if (status != ErrorSuccess && status != ErrorInstanceNotFound)
                    Utils.LogException(new Win32Exception((int)status, "Unable to stop the ETW process trace session."), Utils.LOG_ID_SERVICE);
            }
            finally
            {
                Marshal.FreeHGlobal(properties);
            }
        }

        private static IntPtr AllocateProperties()
        {
            int propertiesSize = Marshal.SizeOf<EventTraceProperties>();
            int nameSize = (SessionName.Length + 1) * sizeof(char);
            IntPtr buffer = Marshal.AllocHGlobal(propertiesSize + nameSize);
            Marshal.Copy(new byte[propertiesSize + nameSize], 0, buffer, propertiesSize + nameSize);
            Marshal.StructureToPtr(CreateProperties(), buffer, false);
            WriteSessionName(buffer);
            return buffer;
        }

        private static EventTraceProperties CreateProperties()
        {
            return new EventTraceProperties
            {
                Wnode = new WnodeHeader
                {
                    BufferSize = (uint)(Marshal.SizeOf<EventTraceProperties>() + ((SessionName.Length + 1) * sizeof(char))),
                    ClientContext = 1,
                    Flags = WnodeFlagTracedGuid,
                    Guid = Guid.NewGuid()
                },
                BufferSize = 64,
                MinimumBuffers = 4,
                MaximumBuffers = 16,
                LogFileMode = EventTraceRealTimeMode | EventTraceSystemLoggerMode | EventTraceNoPerProcessorBuffering,
                FlushTimer = 1,
                EnableFlags = EventTraceFlagProcess | EventTraceFlagNoSystemConfig,
                LoggerNameOffset = (uint)Marshal.SizeOf<EventTraceProperties>()
            };
        }

        private static void WriteSessionName(IntPtr properties)
        {
            int offset = Marshal.SizeOf<EventTraceProperties>();
            char[] name = (SessionName + '\0').ToCharArray();
            Marshal.Copy(name, 0, IntPtr.Add(properties, offset), name.Length);
        }

        private static void ThrowOnError(uint status, string message)
        {
            if (status != ErrorSuccess)
                throw new Win32Exception((int)status, message);
        }

        private void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(ProcessStartWatcher));
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void EventRecordCallback(ref EventRecord eventRecord);

        [StructLayout(LayoutKind.Sequential)]
        private struct WnodeHeader
        {
            internal uint BufferSize;
            internal uint ProviderId;
            internal ulong HistoricalContext;
            internal long TimeStamp;
            internal Guid Guid;
            internal uint ClientContext;
            internal uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EventTraceProperties
        {
            internal WnodeHeader Wnode;
            internal uint BufferSize;
            internal uint MinimumBuffers;
            internal uint MaximumBuffers;
            internal uint MaximumFileSize;
            internal uint LogFileMode;
            internal uint FlushTimer;
            internal uint EnableFlags;
            internal int AgeLimit;
            internal uint NumberOfBuffers;
            internal uint FreeBuffers;
            internal uint EventsLost;
            internal uint BuffersWritten;
            internal uint LogBuffersLost;
            internal uint RealTimeBuffersLost;
            internal IntPtr LoggerThreadId;
            internal uint LogFileNameOffset;
            internal uint LoggerNameOffset;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EventDescriptor
        {
            internal ushort Id;
            internal byte Version;
            internal byte Channel;
            internal byte Level;
            internal byte Opcode;
            internal ushort Task;
            internal ulong Keyword;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EventHeader
        {
            internal ushort Size;
            internal ushort HeaderType;
            internal ushort Flags;
            internal ushort EventProperty;
            internal uint ThreadId;
            internal uint ProcessId;
            internal long TimeStamp;
            internal Guid ProviderId;
            internal EventDescriptor EventDescriptor;
            internal uint KernelTime;
            internal uint UserTime;
            internal Guid ActivityId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EventRecord
        {
            internal EventHeader EventHeader;
            internal uint BufferContext;
            internal ushort ExtendedDataCount;
            internal ushort UserDataLength;
            internal IntPtr ExtendedData;
            internal IntPtr UserData;
            internal IntPtr UserContext;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyDataDescriptor
        {
            internal ulong PropertyName;
            internal uint ArrayIndex;
            internal uint Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EventTraceHeader
        {
            internal ushort Size;
            internal ushort FieldTypeFlags;
            internal uint Version;
            internal uint ThreadId;
            internal uint ProcessId;
            internal long TimeStamp;
            internal Guid Guid;
            internal ulong ProcessorTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EventTrace
        {
            internal EventTraceHeader Header;
            internal uint InstanceId;
            internal uint ParentInstanceId;
            internal Guid ParentGuid;
            internal IntPtr MofData;
            internal uint MofLength;
            internal uint ClientContext;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct TimeZoneInformation
        {
            internal int Bias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string StandardName;
            internal SystemTime StandardDate;
            internal int StandardBias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string DaylightName;
            internal SystemTime DaylightDate;
            internal int DaylightBias;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SystemTime
        {
            internal ushort Year;
            internal ushort Month;
            internal ushort DayOfWeek;
            internal ushort Day;
            internal ushort Hour;
            internal ushort Minute;
            internal ushort Second;
            internal ushort Milliseconds;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct TraceLogfileHeader
        {
            internal uint BufferSize;
            internal uint Version;
            internal uint ProviderVersion;
            internal uint NumberOfProcessors;
            internal long EndTime;
            internal uint TimerResolution;
            internal uint MaximumFileSize;
            internal uint LogFileMode;
            internal uint BuffersWritten;
            internal Guid LogInstanceGuid;
            internal IntPtr LoggerName;
            internal IntPtr LogFileName;
            internal TimeZoneInformation TimeZone;
            internal long BootTime;
            internal long PerfFreq;
            internal long StartTime;
            internal uint ReservedFlags;
            internal uint BuffersLost;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct EventTraceLogfile
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string? LogFileName;
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string LoggerName;
            internal long CurrentTime;
            internal uint BuffersRead;
            internal uint ProcessTraceMode;
            internal EventTrace CurrentEvent;
            internal TraceLogfileHeader LogfileHeader;
            internal IntPtr BufferCallback;
            internal uint BufferSize;
            internal uint Filled;
            internal uint EventsLost;
            internal IntPtr EventRecordCallback;
            internal uint IsKernelTrace;
            internal IntPtr Context;
        }

        private static class NativeMethods
        {
            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static extern uint StartTraceW(out ulong sessionHandle, string sessionName, IntPtr properties);

            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static extern uint ControlTraceW(ulong sessionHandle, string sessionName, IntPtr properties, uint controlCode);

            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            internal static extern ulong OpenTraceW(ref EventTraceLogfile logfile);

            [DllImport("advapi32.dll", ExactSpelling = true)]
            internal static extern uint ProcessTrace(
                [In] ulong[] handleArray,
                uint handleCount,
                IntPtr startTime,
                IntPtr endTime);

            [DllImport("advapi32.dll", ExactSpelling = true)]
            internal static extern uint CloseTrace(ulong traceHandle);

            [DllImport("tdh.dll", ExactSpelling = true)]
            internal static extern uint TdhGetPropertySize(
                ref EventRecord eventRecord,
                uint tdhContextCount,
                IntPtr tdhContext,
                uint propertyDataCount,
                ref PropertyDataDescriptor propertyData,
                out uint propertySize);

            [DllImport("tdh.dll", ExactSpelling = true)]
            internal static extern uint TdhGetProperty(
                ref EventRecord eventRecord,
                uint tdhContextCount,
                IntPtr tdhContext,
                uint propertyDataCount,
                ref PropertyDataDescriptor propertyData,
                uint bufferSize,
                IntPtr buffer);
        }
    }
}
