using System;
using System.ComponentModel;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

#if !NET35
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
#endif

namespace pylorak.Windows.Services
{
    public struct PowerEventData
    {
        public PowerEventType Event;
        public Guid Setting;
        public int PayloadInt;
    }

    [InstallerType(typeof(System.ServiceProcess.ServiceProcessInstaller))]
    public abstract class ServiceBase : IDisposable
    {
        private delegate void ServiceMainDelegate(int argCount, IntPtr argPointer);
        private delegate void CommandHandlerDelegate();
        private delegate void StartCommandHandlerDelegate(string[] args);
        private delegate void PowerEventHandlerDelegate(PowerEventData data);

        [StructLayout(LayoutKind.Sequential)]
        private struct SERVICE_TABLE_ENTRY
        {
            public IntPtr Name;
            public ServiceMainDelegate? Callback;

            public static SERVICE_TABLE_ENTRY Zero()
            {
                return new SERVICE_TABLE_ENTRY(IntPtr.Zero, null);
            }

            public SERVICE_TABLE_ENTRY(IntPtr namePtr, ServiceMainDelegate? callback)
            {
                Name = namePtr;
                Callback = callback;
            }
        }

        private struct ServiceStartArgsTuple
        {
            public string[] Args;
            public ManualResetEvent StartedEvent;
        }

        private bool disposed;
        private readonly bool initialized;
        private readonly ServiceCtrlHandlerExDelegate ServiceCtrlHandlerExCallback;
        private readonly StartCommandHandlerDelegate StartCommandHandler;
        private readonly CommandHandlerDelegate StopCommandHandler;
        private readonly CommandHandlerDelegate PauseCommandHandler;
        private readonly CommandHandlerDelegate ContinueCommandHandler;
        private readonly CommandHandlerDelegate ShutdownCommandHandler;
        private readonly PowerEventHandlerDelegate PowerEventHandler;
        private readonly ManualResetEvent StoppedEventHandle = new ManualResetEvent(true);
        private readonly ManualResetEvent StartedEventHandle = new ManualResetEvent(false);
        private readonly SafeHandleAllocHGlobal UnmanagedServiceName;

        public abstract string ServiceName { get; }
        public IntPtr ServiceHandle { get; private set; }

        private SERVICE_STATUS Status = new SERVICE_STATUS() { currentState = ServiceState.Stopped };
        private ServiceState PreviousState = ServiceState.Stopped;

#if NET35
        private Exception? StartExceptionInfo;
#else
        private ExceptionDispatchInfo? StartExceptionInfo;
#endif

        protected ServiceBase()
        {
            ServiceCtrlHandlerExCallback = new ServiceCtrlHandlerExDelegate(ServiceCtrlHandlerEx);
            StartCommandHandler = new StartCommandHandlerDelegate(OnStartWrapper);
            StopCommandHandler = new CommandHandlerDelegate(OnStopWrapper);
            PauseCommandHandler = new CommandHandlerDelegate(OnPauseWrapper);
            ContinueCommandHandler = new CommandHandlerDelegate(OnContinueWrapper);
            ShutdownCommandHandler = new CommandHandlerDelegate(OnShutdownWrapper);
            PowerEventHandler = new PowerEventHandlerDelegate(OnPowerEventWrapper);
            UnmanagedServiceName = SafeHandleAllocHGlobal.FromString(ServiceName);

            initialized = true;
        }

        public bool AutoLog { get; set; } = true;

        private EventLog? _EventLog;
        public virtual EventLog EventLog
        {
            get
            {
                if (_EventLog == null)
                {
                    _EventLog = new EventLog();
                    _EventLog.Source = ServiceName;
                    _EventLog.Log = "Application";
                }
                return _EventLog;
            }
        }

        public ServiceState CurrentState
        {
            get { return Status.currentState; }
        }

        private ServiceAcceptedControl _AcceptedControls = ServiceAcceptedControl.None;
        public virtual ServiceAcceptedControl AcceptedControls
        {
            get { return _AcceptedControls; }
            set
            {
                if (((value & ServiceAcceptedControl.SERVICE_ACCEPT_SHUTDOWN) != 0)
                    && ((value & ServiceAcceptedControl.SERVICE_ACCEPT_PRESHUTDOWN) != 0))
                {
                    throw new ArgumentException("Cannot accept both Shutdown and PreShutdown.");
                }

                _AcceptedControls = value;

                if (!IsStateChangePending(CurrentState))
                {
                    Status.controlsAccepted = value;
                    UpdateServiceStatus();
                }
            }
        }

        private bool IsRunningAsService
        {
            get
            {
                return ServiceHandle != IntPtr.Zero;
            }
        }

        private void UpdateServiceStatus()
        {
            if (IsRunningAsService)
            {
                if (!NativeMethods.SetServiceStatus(ServiceHandle, ref Status))
                    throw new Win32Exception();
            }
        }

        public static bool IsStateChangePending(ServiceState state)
        {
            switch (state)
            {
                case ServiceState.ContinuePending:
                case ServiceState.PausePending:
                case ServiceState.StartPending:
                case ServiceState.StopPending:
                    return true;
                case ServiceState.Running:
                case ServiceState.Stopped:
                case ServiceState.Paused:
                    return false;
                default:
                    throw new ArgumentException("Unrecognized value.", nameof(state));
            }
        }

        public void Start(string[] args)
        {
            if (!initialized)
                throw new InvalidOperationException($"{nameof(ServiceBase)} constructor has not been called.");

            StartStateChange(ServiceState.StartPending, args);
        }

        public void Stop()
        {
            StartStateChange(ServiceState.StopPending);
        }

        public void Pause()
        {
            StartStateChange(ServiceState.PausePending);
        }

        public void Continue()
        {
            StartStateChange(ServiceState.ContinuePending);
        }

        protected void StartStateChange(ServiceState newState, string[]? startArgs = null)
        {
            const bool async = true;

            if (!IsStateChangePending(newState))
                throw new ArgumentException("Must specify a pending state.", nameof(newState));

            if ((newState == ServiceState.StartPending) && (startArgs == null))
                throw new ArgumentNullException(nameof(startArgs));

            if (IsStateChangePending(CurrentState))
                throw new InvalidOperationException("Must not be in a pending state.");

            switch (newState)
            {
                case ServiceState.ContinuePending:
                    if (CurrentState == ServiceState.Paused)
                    {
                        PreviousState = CurrentState;
                        SetServiceStatePending(newState);
                        InvokeDelegateAsync(ContinueCommandHandler, async);
                    }
                    break;
                case ServiceState.PausePending:
                    if (CurrentState == ServiceState.Running)
                    {
                        PreviousState = CurrentState;
                        SetServiceStatePending(newState);
                        StartedEventHandle.Reset();
                        InvokeDelegateAsync(PauseCommandHandler, async);
                    }
                    break;
                case ServiceState.StartPending:
                    if (CurrentState == ServiceState.Stopped)
                    {
                        PreviousState = CurrentState;
                        SetServiceStatePending(newState);
                        StoppedEventHandle.Reset();
                        Debug.Assert(startArgs != null);
                        InvokeDelegateAsync(StartCommandHandler, async, startArgs!);
                    }
                    break;
                case ServiceState.StopPending:
                    if ( (CurrentState == ServiceState.Running) || (CurrentState == ServiceState.Paused) )
                    {
                        PreviousState = CurrentState;
                        SetServiceStatePending(newState);
                        StartedEventHandle.Reset();
                        InvokeDelegateAsync(StopCommandHandler, async);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Invalid logic branch reached.");
            }
        }

        public void FailStateChange(int win32ExitCode = 0, int serviceSpecificExitCode = 0)
        {
            SetServiceStateReached(PreviousState, win32ExitCode, serviceSpecificExitCode);
        }

        public void FinishStateChange(int win32ExitCode = 0, int serviceSpecificExitCode = 0)
        {
            if (!IsStateChangePending(CurrentState))
                throw new InvalidOperationException("Must be in a pending state.");

            switch (CurrentState)
            {
                case ServiceState.ContinuePending:
                    // Fall-through
                case ServiceState.StartPending:
                    SetServiceStateReached(ServiceState.Running, win32ExitCode, serviceSpecificExitCode);
                    StartedEventHandle.Set();
                    break;
                case ServiceState.PausePending:
                    SetServiceStateReached(ServiceState.Paused, win32ExitCode, serviceSpecificExitCode);
                    break;
                case ServiceState.StopPending:
                    SetServiceStateReached(ServiceState.Stopped, win32ExitCode, serviceSpecificExitCode);
                    StoppedEventHandle.Set();
                    break;
                default:
                    throw new InvalidOperationException("Invalid logic branch reached.");
            }
            PreviousState = CurrentState;
        }

        public WaitHandle StoppedEvent
        {
            get
            {
                return StoppedEventHandle;
            }
        }

        public WaitHandle StartedEvent
        {
            get
            {
                return StartedEventHandle;
            }
        }

        private void SetServiceStatePending(ServiceState newState, int checkpoint = 0, int waitHintMs = 0)
        {
            if (!IsStateChangePending(newState))
                throw new ArgumentException("Must specify a pending state.", nameof(newState));

            Status.currentState = newState;
            Status.checkPoint = checkpoint;
            Status.waitHint = waitHintMs;
            Status.win32ExitCode = 0;
            Status.serviceSpecificExitCode = 0;
            Status.controlsAccepted &= ~(ServiceAcceptedControl.SERVICE_ACCEPT_PAUSE_CONTINUE | ServiceAcceptedControl.SERVICE_ACCEPT_STOP);
            UpdateServiceStatus();
        }

        private void SetServiceStateReached(ServiceState newState, int win32ExitCode = 0, int serviceSpecificExitCode = 0)
        {
            const int ERROR_SERVICE_SPECIFIC_ERROR = 1066;
            if ((serviceSpecificExitCode != 0) && (win32ExitCode != ERROR_SERVICE_SPECIFIC_ERROR))
                throw new ArgumentException($"Argument {nameof(win32ExitCode)} must be ERROR_SERVICE_SPECIFIC_ERROR if a service-specific error code is specified.");

            if (IsStateChangePending(newState))
                throw new ArgumentException("Must not specify a pending state.", nameof(newState));

            Status.currentState = newState;
            Status.checkPoint = 0;
            Status.waitHint = 0;
            Status.win32ExitCode = win32ExitCode;
            Status.serviceSpecificExitCode = serviceSpecificExitCode;
            Status.controlsAccepted = AcceptedControls;
            UpdateServiceStatus();
        }

        protected void StateChangeProgress(int checkpoint, int waitHintMs)
        {
            if (!IsStateChangePending(CurrentState))
                throw new InvalidOperationException("Service must be in a pending state.");

            Status.waitHint = waitHintMs;
            Status.checkPoint = checkpoint;
            UpdateServiceStatus();
        }

        protected void RequestAdditionalTime(int milliseconds)
        {
            StateChangeProgress(Status.checkPoint + 1, milliseconds);
        }

        private static void InvokeDelegateAsync(CommandHandlerDelegate d, bool async)
        {
            if (async)
            {
#if NET35
                d.BeginInvoke(iar =>
                {
                    try { d.EndInvoke(iar); }
                    catch { }
                }, null);
#else
            Task.Run(() => d.Invoke());
#endif
            }
            else
            {
                d.Invoke();
            }
        }

        private static void InvokeDelegateAsync(StartCommandHandlerDelegate d, bool async, string[] args)
        {
            if (async)
            {
#if NET35
                d.BeginInvoke(args, iar =>
            {
                try { d.EndInvoke(iar); }
                catch { }
            }, null);
#else
            Task.Run(() => d.Invoke(args));
#endif
            }
            else
            {
                d.Invoke(args);
            }
        }

        private static void InvokeDelegateAsync(PowerEventHandlerDelegate d, bool async, PowerEventData data)
        {
            if (async)
            {
#if NET35
                d.BeginInvoke(data, iar =>
                {
                    try { d.EndInvoke(iar); }
                    catch { }
                }, null);
#else
            Task.Run(() => d.Invoke(args));
#endif
            }
            else
            {
                d.Invoke(data);
            }
        }

        private void ProcessPowerEvent(int eventType, IntPtr eventData)
        {
            if (!Enum.IsDefined(typeof(PowerEventType), eventType))
                return;

            PowerEventData ped = new PowerEventData();
            ped.Event = (PowerEventType)eventType;

            if (ped.Event == PowerEventType.PowerSettingChange)
            {
                var data0 = (POWERBROADCAST_SETTING_NODATA)Marshal.PtrToStructure(eventData, typeof(POWERBROADCAST_SETTING_NODATA));
                ped.Setting = data0.PowerSetting;
                if (data0.DataLength == 4)
                {
                    var data1 = (POWERBROADCAST_SETTING_DWORD)Marshal.PtrToStructure(eventData, typeof(POWERBROADCAST_SETTING_DWORD));
                    ped.PayloadInt = data1.Data;
                }
            }

            InvokeDelegateAsync(PowerEventHandler, true, ped);
        }

        private int ServiceCtrlHandlerEx(int command, int eventType, IntPtr eventData, IntPtr eventContext)
        {
            const int NO_ERROR = 0;

            switch ((ServiceControlCommand)command)
            {
                case ServiceControlCommand.SERVICE_CONTROL_INTERROGATE:
                    UpdateServiceStatus();
                    break;
                case ServiceControlCommand.SERVICE_CONTROL_POWEREVENT:
                    ProcessPowerEvent(eventType, eventData);
                    break;
                case ServiceControlCommand.SERVICE_CONTROL_STOP:
                    if (!IsStateChangePending(CurrentState))
                        StartStateChange(ServiceState.StopPending);
                    break;
                case ServiceControlCommand.SERVICE_CONTROL_PAUSE:
                    if (!IsStateChangePending(CurrentState))
                        StartStateChange(ServiceState.PausePending);
                    break;
                case ServiceControlCommand.SERVICE_CONTROL_CONTINUE:
                    if (!IsStateChangePending(CurrentState))
                        StartStateChange(ServiceState.ContinuePending);
                    break;
                case ServiceControlCommand.SERVICE_CONTROL_PRESHUTDOWN:
                // Fall-though
                case ServiceControlCommand.SERVICE_CONTROL_SHUTDOWN:
                    InvokeDelegateAsync(ShutdownCommandHandler, true);
                    break;
            }

            return NO_ERROR;
        }

        protected virtual void OnStart(string[] args)
        {
            FinishStateChange();
        }
        protected virtual void OnContinue()
        {
            FinishStateChange();
        }
        protected virtual void OnPause()
        {
            FinishStateChange();
        }
        protected virtual void OnStop()
        {
            FinishStateChange();
        }

        protected virtual void OnShutdown()
        { }

        protected virtual void OnPowerEvent(PowerEventData data)
        { }

        private void OnPowerEventWrapper(PowerEventData data)
        {
            try
            {
                OnPowerEvent(data);
            }
            catch (Exception e)
            {
                WriteEventLogEntry($"OnPowerEvent() error. {e.Message}", EventLogEntryType.Error);

                // We re-throw the exception so that the advapi32 code can report
                // ERROR_EXCEPTION_IN_SERVICE as it would for native services.
                throw;
            }
        }


        private void OnShutdownWrapper()
        {
            try
            {
                OnShutdown();
            }
            catch (Exception e)
            {
                WriteEventLogEntry($"OnShutdown() error. {e.Message}", EventLogEntryType.Error);

                // We re-throw the exception so that the advapi32 code can report
                // ERROR_EXCEPTION_IN_SERVICE as it would for native services.
                throw;
            }
        }

        private void OnContinueWrapper()
        {
            try
            {
                WriteEventLogEntry("Continuing service...");
                OnContinue();
            }
            catch (Exception e)
            {
                WriteEventLogEntry($"Service failed to continue. {e.Message}", EventLogEntryType.Error);
                FailStateChange(Status.win32ExitCode, Status.serviceSpecificExitCode);

                // We re-throw the exception so that the advapi32 code can report
                // ERROR_EXCEPTION_IN_SERVICE as it would for native services.
                throw;
            }
        }

        private void OnPauseWrapper()
        {
            try
            {
                WriteEventLogEntry("Pausing service...");
                OnPause();
            }
            catch (Exception e)
            {
                WriteEventLogEntry($"Service failed to continue. {e.Message}", EventLogEntryType.Error);
                FailStateChange(Status.win32ExitCode, Status.serviceSpecificExitCode);

                // We re-throw the exception so that the advapi32 code can report
                // ERROR_EXCEPTION_IN_SERVICE as it would for native services.
                throw;
            }
        }

        private void OnStartWrapper(string[] args)
        {
            try
            {
                WriteEventLogEntry("Starting service...");
                OnStart(args);
            }
            catch (Exception e)
            {
                WriteEventLogEntry($"Service failed to start. {e.Message}", EventLogEntryType.Error);
                FailStateChange(Status.win32ExitCode, Status.serviceSpecificExitCode);

                // We re-throw the exception so that the advapi32 code can report
                // ERROR_EXCEPTION_IN_SERVICE as it would for native services.
                throw;
            }
        }

        private void OnStopWrapper()
        {
            try
            {
                WriteEventLogEntry("Stopping service...");
                OnStop();
            }
            catch (Exception e)
            {
                WriteEventLogEntry($"Service failed to stop. {e.Message}", EventLogEntryType.Error);
                FailStateChange(Status.win32ExitCode, Status.serviceSpecificExitCode);

                // We re-throw the exception so that the advapi32 code can report
                // ERROR_EXCEPTION_IN_SERVICE as it would for native services.
                throw;
            }
        }

        // Need to execute the start method on a thread pool thread.
        // Most applications will start asynchronous operations in the
        // OnStart method. If such a method is executed in MainCallback
        // thread, the async operations might get canceled immediately.
        private void ServiceQueuedMainCallback(object state)
        {
            var argsBundle = (ServiceStartArgsTuple)state;

            try
            {
                WriteEventLogEntry("Starting service...");
                OnStart(argsBundle.Args);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
#if NET35
                StartExceptionInfo = e;
#else
                StartExceptionInfo = ExceptionDispatchInfo.Capture(e);
#endif
            }
#pragma warning restore CA1031 // Do not catch general exception types
            argsBundle.StartedEvent.Set();
        }

        public void ServiceMain(int argCount, IntPtr argPointer)
        {
            if (!initialized)
                throw new InvalidOperationException($"{nameof(ServiceBase)} constructor has not been called.");
            if (argCount == 0)
                throw new ArgumentOutOfRangeException(nameof(argCount), "Argument must be larger than zero.");

            // First arg is always the service name. We don't store that here, hence -1.
            var args = new string[argCount - 1];
            unsafe
            {
                char** argsAsPtr = (char**)argPointer.ToPointer();

                for (int index = 0; index < args.Length; ++index)
                {
                    // we increment the pointer first so we skip over the first argument. 
                    argsAsPtr++;
                    args[index] = Marshal.PtrToStringUni((IntPtr)(*argsAsPtr));
                }
            }

            ServiceHandle = NativeMethods.RegisterServiceCtrlHandlerEx(ServiceName, ServiceCtrlHandlerExCallback, IntPtr.Zero);
            if ((long)ServiceHandle == 0)
            {
                var e = new Win32Exception();
                WriteEventLogEntry(e.Message, EventLogEntryType.Error);
                SetServiceStateReached(ServiceState.Stopped, e.NativeErrorCode);
                return;
            }
#if true
            StartStateChange(ServiceState.StartPending, args);
#else
            SetServiceStatePending(ServiceState.StartPending);

            // Need to execute the start method on a thread pool thread.
            // Most applications will start asynchronous operations in the
            // OnStart method. If such a method is executed in the current
            // thread, the async operations might get canceled immediately
            // since NT will terminate this thread right after this function
            // finishes.
            var argBundle = new ServiceStartArgsTuple()
            {
                Args = args,
                StartedEvent = new ManualResetEvent(false),
            };
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ServiceQueuedMainCallback), args);
            argBundle.StartedEvent.WaitOne();
            argBundle.StartedEvent.Close();

            if (StartExceptionInfo != null)
            {
                WriteEventLogEntry("Service failed to start.", EventLogEntryType.Error);

                // Inform SCM that the service could not be started successfully.
                // (Unless the service has already provided another failure exit code)
                const int ERROR_EXCEPTION_IN_SERVICE = 1064;
                SetServiceStateReached(
                    ServiceState.Stopped,
                    (Status.win32ExitCode == 0) ? ERROR_EXCEPTION_IN_SERVICE : Status.win32ExitCode,
                    Status.serviceSpecificExitCode
                );
            }
#endif
        }

        private void WriteEventLogEntry(string message, EventLogEntryType errorType = EventLogEntryType.Information)
        {
            // Not being able to log should affect normal operation.
            try
            {
                if (AutoLog)
                    EventLog.WriteEntry(message, errorType);
            }
#region Stuff not to catch
            catch (StackOverflowException)
            {
                throw;
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (ThreadAbortException)
            {
                throw;
            }
#endregion
#pragma warning disable CA1031 // Do not catch general exception types
            catch { }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources
                _EventLog?.Dispose();
                UnmanagedServiceName.Dispose();
                StoppedEventHandle.Close();
                StartedEventHandle.Close();
            }
            
            disposed = true;
        }

        public static void Run(ServiceBase srv)
        {
            Run(new ServiceBase[] { srv });
        }

        public static void Run(ServiceBase[] services)
        {
            int ENTRY_SIZE = Marshal.SizeOf(typeof(SERVICE_TABLE_ENTRY));
            ServiceType serviceType = (services.Length > 1) ? ServiceType.SERVICE_TYPE_WIN32_SHARE_PROCESS : ServiceType.SERVICE_TYPE_WIN32_OWN_PROCESS;
            using var nativeEntriesTable = SafeHandleAllocHGlobal.Alloc((services.Length + 1) * ENTRY_SIZE);
            SERVICE_TABLE_ENTRY[] entries = new SERVICE_TABLE_ENTRY[services.Length];

            IntPtr entriesPointer = nativeEntriesTable.DangerousGetHandle();
            for (int i = 0; i < services.Length; ++i)
            {
                services[i].Status.serviceType = serviceType;
                entries[i] = new SERVICE_TABLE_ENTRY(services[i].UnmanagedServiceName.DangerousGetHandle(), new ServiceMainDelegate(services[i].ServiceMain));
                Marshal.StructureToPtr(entries[i], entriesPointer, true);
                entriesPointer = (IntPtr)((long)entriesPointer + ENTRY_SIZE);
            }
            SERVICE_TABLE_ENTRY lastEntry = SERVICE_TABLE_ENTRY.Zero();
            Marshal.StructureToPtr(lastEntry, entriesPointer, true);

            // Doesn't return while service is running.
            bool res = NativeMethods.StartServiceCtrlDispatcher(nativeEntriesTable.DangerousGetHandle());

            // SCM might terminate the process after this point,
            // no further code is guaranteed to run.

            if (!res)
            {
                const int ERROR_FAILED_SERVICE_CONTROLLER_CONNECT = 1063;
                const int ERROR_SERVICE_ALREADY_RUNNING = 1056;

                var errCode = Marshal.GetLastWin32Error();
                Exception e = errCode switch
                {
                    ERROR_FAILED_SERVICE_CONTROLLER_CONNECT => new InvalidOperationException("Cannot run service code as a non-service process (ERROR_FAILED_SERVICE_CONTROLLER_CONNECT)."),
                    ERROR_SERVICE_ALREADY_RUNNING => new InvalidOperationException("The process alerady registered a service control dispatcher (ERROR_SERVICE_ALREADY_RUNNING)."),
                    _ => new Win32Exception(errCode),
                };
                if (Environment.UserInteractive)
                    ShowMessageBox(e.Message, "Error");
                else
                    Console.WriteLine(e.Message);

                foreach (var service in services)
                {
                    if (service.EventLog.Source.Length != 0)
                        service.WriteEventLogEntry(e.Message, EventLogEntryType.Error);
                }

                throw e;
            }

            foreach (ServiceBase service in services)
            {
                if (service.StartExceptionInfo != null)
                {
#if NET35
                    throw new Exception("Exception thrown during service start. See InnerException for details.", service.StartExceptionInfo);
#else
                    service.StartExceptionInfo.Throw();
#endif
                }
            }
        }

        private static void ShowMessageBox(string message, string title)
        {
            const int MB_OK = 0;
            const int MB_ICONERROR = 0x00000010;
            _ = NativeMethods.MessageBox(IntPtr.Zero, message, title, MB_OK | MB_ICONERROR);
        }
    }
}
