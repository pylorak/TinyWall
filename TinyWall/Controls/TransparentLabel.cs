using System.Windows.Forms;

namespace PKSoft
{
    public partial class TransparentLabel : Label
    {
        public TransparentLabel()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }
    }
}
