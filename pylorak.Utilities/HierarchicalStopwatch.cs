using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;

namespace pylorak.Utilities
{
    public sealed class HierarchicalStopwatch : Disposable
    {
        [ThreadStatic] private static Stack<HierarchicalStopwatch>? Hierarchy;
        [ThreadStatic] private static Stopwatch? Timer;
        [ThreadStatic] private static StreamWriter? Logfile;
        [ThreadStatic] private static StringBuilder? LogLines;
        [ThreadStatic] private static int IndentLevel;

        public static bool Enable { get; set; } = false;

        [DisallowNull]
        public static string? LogFileBase { get; set; }

        private bool SameLineResult = true;
        private bool HasSubTask = false;
        private long StartTicksSubTask = 0;
        private readonly long StartTicksTask = 0;

        public HierarchicalStopwatch(string taskName)
        {
            if (!Enable)
                return;

            if (Hierarchy == null)
            {
                Hierarchy = new Stack<HierarchicalStopwatch>();
                Timer = new Stopwatch();
                LogLines = new StringBuilder();

                Timer.Start();
            }

            ++IndentLevel;
            if (Hierarchy.Count > 0)
            {
                var parent = Hierarchy.Peek();
                if (parent.SameLineResult)
                    NewLogLine(0, "\n");
                parent.SameLineResult = false;
            }
            Hierarchy.Push(this);

            NewLogLine(IndentLevel, "{0}:", taskName);
            StartTicksTask = Timer!.ElapsedTicks;
        }

        private void NewLogLine(int indent, string format, params object[] args)
        {
            for (int i = 0; i < indent; ++i)
                LogLines!.Append("    ");

            LogLines!.AppendFormat(format, args);
        }

        private void EndSubTask()
        {
            long totalTicks = Timer!.ElapsedTicks;
            long elapsedTimeMs = 1000 * (totalTicks - StartTicksSubTask) / Stopwatch.Frequency;
            if (elapsedTimeMs == 0)
                elapsedTimeMs = 1;

            if (SameLineResult)
                NewLogLine(0, " {0} ms\n", elapsedTimeMs);
            else
                NewLogLine(IndentLevel + 1, "Total: {0} ms\n", elapsedTimeMs);

            FlushResults();
            --IndentLevel;
        }

        private void EndTask()
        {
            long totalTicks = Timer!.ElapsedTicks;
            long elapsedTimeMs = 1000 * (totalTicks - StartTicksTask) / Stopwatch.Frequency;
            if (elapsedTimeMs == 0)
                elapsedTimeMs = 1;

            if (SameLineResult && !HasSubTask)
                NewLogLine(0, " {0} ms\n", elapsedTimeMs);
            else
                NewLogLine(IndentLevel + 1, "Total: {0} ms\n", elapsedTimeMs);

            FlushResults();
            --IndentLevel;
        }

        public void NewSubTask(string taskName)
        {
            if (!Enable)
                return;

            if (HasSubTask)
                EndSubTask();
            else if (SameLineResult)
                NewLogLine(0, "\n");

            HasSubTask = true;
            SameLineResult = true;
            ++IndentLevel;

            NewLogLine(IndentLevel, "{0}:", taskName);
            StartTicksSubTask = Timer!.ElapsedTicks;
        }

        public static void FlushResults()
        {
            if (!Enable)
                return;

            Debug.Assert(!string.IsNullOrEmpty(Thread.CurrentThread.Name));
            if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
                return;

            string threadName = Thread.CurrentThread.Name;

            try
            {
                // Open log file if necessary
                if (Logfile == null)
                {
                    // Name of the current log file
                    var logdir = Path.GetDirectoryName(LogFileBase);
                    var logfile = $"{LogFileBase} {threadName}.log";

                    if (!Directory.Exists(logdir))
                        Directory.CreateDirectory(logdir);

                    // Only log if log file has not yet reached a certain size
                    if (File.Exists(logfile))
                    {
                        var fi = new FileInfo(logfile);
                        if (fi.Length > 512 * 1024)
                        {
                            // Truncate file back to zero
                            using var fs = new FileStream(logfile, FileMode.Truncate, FileAccess.Write);
                        }
                    }

                    Logfile = new StreamWriter(logfile, true, Encoding.UTF8);
                }

                // Do the logging
                Logfile.Write(LogLines!.ToString());
                Logfile.Flush();
                LogLines.Length = 0;
            }
            catch
            {
                // Ignore exceptions - logging should not itself cause new problems
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                // Release managed resources

                if (Enable)
                {
                    if (HasSubTask)
                        EndSubTask();
                    EndTask();

                    Hierarchy!.Pop();
                    if (Hierarchy.Count == 0)
                    {
                        Logfile?.Dispose();
                        Logfile = null;
                    }
                }
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.

            base.Dispose(disposing);
        }
    }
}
