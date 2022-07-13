using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Chart;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ChartDrawingUiTest.Chart
{
    internal class SpotLinkedAnnotatorTestViewModel1 : ViewModelBase
    {
        public SpotLinkedAnnotatorTestViewModel1() {
            Series = new ObservableCollection<DataPoint>();
            Linkers = new ObservableCollection<SpotLinker>();
            Annotators = new ObservableCollection<SpotAnnotator>();
            Collatz(CalculationMaximum, Series, Linkers, Annotators);

            HorizontalAxis = new AutoContinuousAxisManager<DataPoint, double>(Series, p => p.X)
            {
                ChartMargin = new RelativeMargin(0.05)
            };
            VerticalAxis = new AutoContinuousAxisManager<DataPoint, double>(Series, p => p.Y)
            {
                ChartMargin = new RelativeMargin(0.05)
            };

            var spotPalette = new Dictionary<int, Brush>
            {
                { 0, Brushes.DarkGray },
                { 1, Brushes.LightGray },
            };
            SpotBrush = new DelegateBrushMapper<DataPoint>(point => spotPalette.TryGetValue(point.Type, out var x) ? x : Brushes.Black);
            var linkPalette = new Dictionary<int, Brush>
            {
                { 1, Brushes.Blue },
                { 2, Brushes.Red },
                { 3, Brushes.Blue },
                { 4, Brushes.Red },
            };
            LinkerBrush = new DelegateBrushMapper<ISpotLinker>(linker => linkPalette.TryGetValue(linker.Type, out var x) ? x : Brushes.Black);
            LinkLabelBrush = new ConstantBrushMapper(Brushes.DarkMagenta);
            SpotLabelBrush = new ConstantBrushMapper<SpotAnnotator>(Brushes.DarkGoldenrod);
        }

        public ObservableCollection<DataPoint> Series { get; }

        public ObservableCollection<SpotLinker> Linkers { get; }

        public ObservableCollection<SpotAnnotator> Annotators { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public IBrushMapper SpotBrush { get; }

        public IBrushMapper LinkerBrush { get; }

        public IBrushMapper LinkLabelBrush { get; }

        public IBrushMapper SpotLabelBrush { get; }

        public int CalculationMaximum {
            get => _calculationMaximum;
            set => SetProperty(ref _calculationMaximum, value);
        }
        private int _calculationMaximum = 100;

        public DataPoint SelectedItem {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }
        private DataPoint selectedItem;

        public Point? SelectedPoint {
            get => selectedPoint;
            set => SetProperty(ref selectedPoint, value);
        }
        private Point? selectedPoint;

        public DataPoint FocusedItem {
            get => focusedItem;
            set => SetProperty(ref focusedItem, value);
        }
        private DataPoint focusedItem;

        public Point? FocusedPoint {
            get => focusedPoint;
            set => SetProperty(ref focusedPoint, value);
        }
        private Point? focusedPoint;

        private static void Collatz(int n, ObservableCollection<DataPoint> series, ObservableCollection<SpotLinker> linkers, ObservableCollection<SpotAnnotator> annotators) {
            series.Clear();
            linkers.Clear();
            annotators.Clear();
            var series_ = new List<DataPoint>();
            foreach (var x in Enumerable.Range(1, n)) {
                var dp = new DataPoint() { X = -1, Y = x, Type = x % 2, };
                series_.Add(dp);
                annotators.Add(new SpotAnnotator(dp, x.ToString(), dp.Type));
            }
            var edges = Enumerable.Repeat(0, n + 1).Select(_ => new List<int>()).ToArray();
            series_[0].X = 0;
            for (var i = 2; i <= n; i++) {
                if (i % 2 == 0) {
                    linkers.Add(new SpotLinker(series_[i - 1], series_[i / 2 - 1], "/2", 1) { LabelHorizontalOffset = -3d, ArrowHorizontalOffset = 30d, ArrowVerticalOffset = 10d,  Placement = LinkerLabelPlacementMode.MiddleLeft, });
                    linkers.Add(new SpotLinker(series_[i / 2 - 1], series_[i - 1], "/2", 2) { LabelHorizontalOffset = -3d, ArrowHorizontalOffset = 30d, ArrowVerticalOffset = 10d, Placement = LinkerLabelPlacementMode.MiddleLeft, });
                    edges[i / 2].Add(i);
                }
                else {
                    if (i * 3 + 1 <= n) {
                        linkers.Add(new SpotLinker(series_[i - 1], series_[i * 3], "x3+1", 3) { LabelHorizontalOffset = 3d, ArrowHorizontalOffset = 30d, ArrowVerticalOffset = 10d, Placement = LinkerLabelPlacementMode.MiddleRight, });
                        linkers.Add(new SpotLinker(series_[i * 3], series_[i - 1], "x3+1", 4) { LabelHorizontalOffset = 3d, ArrowHorizontalOffset = 30d, ArrowVerticalOffset = 10d, Placement = LinkerLabelPlacementMode.MiddleRight, });
                        edges[i * 3 + 1].Add(i);
                    }
                }
            }
            var stack = new Stack<(int, int)>();
            stack.Push((1, 0));
            series_[0].X = 0;
            while (stack.Count > 0) {
                var (current, depth) = stack.Pop();
                series_[current - 1].X = depth;
                foreach (var e in edges[current]) {
                    stack.Push((e, depth + 1));
                }
            }

            foreach (var point in series_) {
                series.Add(point);
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(CalculationMaximum)) {
                Collatz(CalculationMaximum, Series, Linkers, Annotators);
            }
        }
    }
}
