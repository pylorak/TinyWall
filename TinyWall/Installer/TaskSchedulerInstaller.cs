using System.Collections;
using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using Microsoft.Win32.TaskScheduler;

namespace PKSoft
{
    internal class TaskSchedulerInstaller : Installer
    {
        private const string TASK_NAME = @"TinyWall Controller";
        private ManualResetEvent FinishEvent = new ManualResetEvent(false);

        private void RegisterTask(Object sender, DoWorkEventArgs e)
        {
            try
            {
                // Get the service on the local machine
                using (TaskService ts = new TaskService())
                {
                    // Create a new task definition and assign properties
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "Starts TinyWall Controller after logon";

                    // Create a trigger that will fire the task at this time every other day
                    td.Triggers.Add(new LogonTrigger());

                    // Create an action that will launch Notepad whenever the trigger fires
                    td.Actions.Add(new ExecAction(Utils.ExecutablePath));

                    // Start task with highest privileges available
                    td.Principal.RunLevel = TaskRunLevel.Highest;

                    td.Settings.DisallowStartIfOnBatteries = false;
                    td.Settings.StopIfGoingOnBatteries = false;
                    td.Settings.ExecutionTimeLimit = TimeSpan.Zero;

                    // Register the task in the root folder
                    ts.RootFolder.RegisterTaskDefinition(TASK_NAME, td);
                }
            }
            finally
            {
                FinishEvent.Set();
            }
        }

        private void UnRegisterTask(Object sender, DoWorkEventArgs e)
        {
            try
            {
                // Get the service on the local machine
                using (TaskService ts = new TaskService())
                {
                    ts.RootFolder.DeleteTask(TASK_NAME);
                }
            }
            finally
            {
                FinishEvent.Set();
            }
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
            RegisterTask(null, null);
/*
            using (BackgroundWorker bgw = new BackgroundWorker())
            {
                bgw.DoWork += new DoWorkEventHandler(RegisterTask);

                FinishEvent.Reset();
                bgw.RunWorkerAsync();
                FinishEvent.WaitOne(TimeSpan.FromSeconds(30));
            }*/
        }

        public override void Uninstall(IDictionary stateSaver)
        {
            base.Uninstall(stateSaver);
            UnRegisterTask(null, null);
/*
            using (BackgroundWorker bgw = new BackgroundWorker())
            {
                bgw.DoWork += new DoWorkEventHandler(UnRegisterTask);

                FinishEvent.Reset();
                bgw.RunWorkerAsync();
                FinishEvent.WaitOne(TimeSpan.FromSeconds(30));
            }*/
        }
    }
}