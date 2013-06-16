using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PKSoft
{
    internal partial class MarqueeForm : Form
    {
        internal MarqueeForm()
        {
            InitializeComponent();
        }

        internal static ReferencedBool ShowProgress()
        {
            ReferencedBool bLoadingDone = new ReferencedBool();

            System.Threading.ThreadPool.QueueUserWorkItem((x) =>
            {
                using (MarqueeForm splashForm = new MarqueeForm())
                {
                    splashForm.Show();
                    splashForm.BringToFront();
                    while (!bLoadingDone.Value)
                        System.Windows.Forms.Application.DoEvents();
                    splashForm.Close();
                }
            });

            return bLoadingDone;
        }
    }

    internal class ReferencedBool
    {
        internal bool Value = false;
    }
}
