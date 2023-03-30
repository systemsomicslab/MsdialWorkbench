using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Lcms
{
    /// <summary>
    /// Interaction logic for LcmsMainView.xaml
    /// </summary>
    public partial class LcmsMainView : UserControl
    {
        public LcmsMainView() {
            InitializeComponent();
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
