using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CompMs.App.Msdial.View.Dims
{
    /// <summary>
    /// Interaction logic for DimsMainView.xaml
    /// </summary>
    public partial class DimsMainView : UserControl
    {
        public DimsMainView() {
            InitializeComponent();
        }

        private void TabControl_PairwisePlotViewer_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sender == TabControl_PairwisePlotViewer) {
                var item = e.AddedItems[0];
                // if (item == TabItem_RtMzPairwisePlotPeakView) {
                //     OnAnalysisViewSelected();
                // }
                // if (item == TabItem_RtMzPairwisePlotAlignmentView) {
                //     OnAlignmentViewSelected();
                // }
            }
        }

        private void TabItem_RtMzPairwisePlotPeakView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (sender is TabItem item) {
                if (e.NewValue != null) {
                    item.IsSelected = true;
                }
            }
        }

        private void TabItem_RtMzPairwisePlotAlignmentView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (sender is TabItem item) {
                if (e.NewValue != null) {
                    item.IsSelected = true;
                }
            }
        }

        private void OnAnalysisViewSelected() {
            if (TabItem_EicViewer != null) {
                TabItem_EicViewer.IsSelected = true;
            }
            if (TabItem_MeasurementVsReference != null) {
                TabItem_MeasurementVsReference.IsSelected = true;
            }
        }

        private void OnAlignmentViewSelected() {
            if (TabItem_BarChartViewer != null) {
                TabItem_BarChartViewer.IsSelected = true;
            }
            if (TabItem_RepresentativeVsReference != null) {
                TabItem_RepresentativeVsReference.IsSelected = true;
            }
        }

        private Point mrStart;
        private void MainControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            mrStart = e.GetPosition((IInputElement)sender);
        }

        private void MainControl_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            var mrCurrent = Mouse.GetPosition(this);

            if (Math.Abs(mrStart.X - mrCurrent.X) > SystemParameters.MinimumHorizontalDragDistance
                || Math.Abs(mrStart.Y - mrCurrent.Y) > SystemParameters.MinimumVerticalDragDistance)
                e.Handled = true;
        }
    }
}
