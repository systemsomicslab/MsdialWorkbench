using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Gcms
{
    /// <summary>
    /// Interaction logic for GcmsMainView.xaml
    /// </summary>
    public partial class GcmsMainView : UserControl
    {
        public GcmsMainView()
        {
            InitializeComponent();
        }

        private void PeaksTabItem_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (sender is TabItem item) {
                if (e.NewValue != null) {
                    item.IsSelected = true;
                }
            }
        }
    }
}
