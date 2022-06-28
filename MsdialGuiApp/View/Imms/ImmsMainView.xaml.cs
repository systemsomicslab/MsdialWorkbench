using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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

namespace CompMs.App.Msdial.View.Imms
{
    /// <summary>
    /// Interaction logic for ImmsMainView.xaml
    /// </summary>
    public partial class ImmsMainView : UserControl
    {
        public ImmsMainView() {
            InitializeComponent();

            var tabChange = Observable.FromEvent<SelectionChangedEventHandler, SelectionChangedEventArgs>(
                h => (s, e) => h(e),
                h => TabControl_PairwisePlotViewer.SelectionChanged += h,
                h => TabControl_PairwisePlotViewer.SelectionChanged -= h);

            var peakPlotSelect = tabChange
                .Where(e => e.AddedItems[0] == TabItem_RtMzPairwisePlotPeakView)
                .Select(_ => Unit.Default);
            Observable.FromEvent<DependencyPropertyChangedEventHandler, DependencyPropertyChangedEventArgs>(
                    h => (s, e) => h(e),
                    h => TabItem_RtMzPairwisePlotPeakView.DataContextChanged += h,
                    h => TabItem_RtMzPairwisePlotPeakView.DataContextChanged -= h)
                .Do(_ => TabItem_RtMzPairwisePlotPeakView.IsSelected = true)
                .Select(_ => Unit.Default)
                .Merge(peakPlotSelect)
                .Subscribe(_ => OnAnalysisViewSelected());

            var alignmentPlotSelect = tabChange
                .Where(e => e.AddedItems[0] == TabItem_RtMzPairwisePlotAlignmentView)
                .Select(_ => Unit.Default);
            Observable.FromEvent<DependencyPropertyChangedEventHandler, DependencyPropertyChangedEventArgs>(
                    h => (s, e) => h(e),
                    h => TabItem_RtMzPairwisePlotAlignmentView.DataContextChanged += h,
                    h => TabItem_RtMzPairwisePlotAlignmentView.DataContextChanged -= h)
                .Do(_ => TabItem_RtMzPairwisePlotAlignmentView.IsSelected = true)
                .Select(_ => Unit.Default)
                .Merge(alignmentPlotSelect)
                .Subscribe(_ => OnAlignmentViewSelected());

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
    }
}
