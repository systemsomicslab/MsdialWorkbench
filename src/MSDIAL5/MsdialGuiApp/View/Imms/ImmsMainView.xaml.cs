using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Imms
{
    /// <summary>
    /// Interaction logic for ImmsMainView.xaml
    /// </summary>
    public partial class ImmsMainView : UserControl
    {
        public ImmsMainView() {
            InitializeComponent();
        }

        private void TabItem_DtMzPairwisePlotPeakView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (sender is TabItem item) {
                if (e.NewValue != null) {
                    item.IsSelected = true;
                }
            }
        }

        private void TabItem_DtMzPairwisePlotAlignmentView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (sender is TabItem item) {
                if (e.NewValue != null) {
                    item.IsSelected = true;
                }
            }
        }
    }
}
