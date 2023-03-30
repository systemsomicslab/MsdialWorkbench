using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Imms
{
    /// <summary>
    /// Interaction logic for ImmsMainView.xaml
    /// </summary>
    public partial class ImmsMainView : UserControl
    {
        public ImmsMainView() {
            InitializeComponent();

            var mouseRDown = Observable.FromEvent<MouseButtonEventHandler, MouseButtonEventArgs>(
                h => (s, e) => h(e),
                h => MouseRightButtonDown += h,
                h => MouseRightButtonDown -= h);
            var mouseRUp = Observable.FromEvent<MouseButtonEventHandler, MouseButtonEventArgs>(
                h => (s, e) => h(e),
                h => MouseRightButtonUp += h,
                h => MouseRightButtonUp -= h);
            var mouseMove = Observable.FromEvent<MouseEventHandler, MouseEventArgs>(
                h => (s, e) => h(e),
                h => MouseMove += h,
                h => MouseMove -= h);
            var drag = mouseMove
                .SkipUntil(mouseRDown)
                .TakeUntil(mouseRUp)
                .Repeat();

            var mouseLeave = mouseRDown.Select(e => e.GetPosition(null))
                .CombineLatest(drag.Select(e => e.GetPosition(null)), (p, q) => (p, q))
                .Where(pq => Math.Abs(pq.p.X - pq.q.X) > SystemParameters.MinimumHorizontalDragDistance
                            || Math.Abs(pq.p.Y - pq.q.Y) > SystemParameters.MinimumVerticalDragDistance);

            var contextMenuOpening = Observable.FromEvent<ContextMenuEventHandler, ContextMenuEventArgs>(
                h => (s, e) => h(e),
                h => ContextMenuOpening += h,
                h => ContextMenuOpening -= h);

            contextMenuOpening
                .TakeUntil(mouseLeave)
                .Repeat()
                .Subscribe(e => e.Handled = true);
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
