using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

namespace Toaster
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length != 2)
                return -1;

            try
            {
                string AppModelID = args[0];
                string[] textLines = args[1].Split('|');
                ShowToast(AppModelID, textLines);
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        // Create and show the toast.
        // See the "Toasts" sample for more detail on what can be done with toasts
        private static void ShowToast(string APP_ID, string[] textLines)
        {
            // Get a toast XML template
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            // Fill in the text elements
            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            for (int i = 0; i < textLines.Length; i++)
                stringElements[i].AppendChild(toastXml.CreateTextNode(textLines[i]));

            /*
            for (int i = 0; i < stringElements.Length; i++)
            {
                stringElements[i].AppendChild(toastXml.CreateTextNode("Line " + i));
            }*/

            // Show the toast. Be sure to specify the AppUserModelId on your application's shortcut!
            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
        }
    }
}
