using System.Windows.Forms;

namespace PKSoft
{
    internal partial class TransparentLabel : Label
    {
        internal TransparentLabel()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }
    }
}
