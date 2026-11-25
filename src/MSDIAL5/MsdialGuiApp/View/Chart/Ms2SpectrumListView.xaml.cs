using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Chart
{
    /// <summary>
    /// Every MS2 spectrum of the selected peak or alignment spot, drawn as tiles.
    /// </summary>
    public partial class Ms2SpectrumListView : UserControl
    {
        public Ms2SpectrumListView() {
            InitializeComponent();
        }

        // The wheel over a chart zooms it. The zoom behavior leaves the event unhandled,
        // so without this the list around the tiles would scroll at the same time.
        private void Chart_MouseWheel(object sender, MouseWheelEventArgs e) {
            e.Handled = true;
        }
    }
}
