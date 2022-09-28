using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ChartDrawingUiTest.Chart
{
    internal class ScatterControlSlimTestViewModel1 : ViewModelBase
    {
        public ScatterControlSlimTestViewModel1() {
            var xs = Enumerable.Range(0, Number).Select(x => new DataPoint() { X = x * (2 * Math.PI) / Number, Y = Math.Sin(x * (2 * Math.PI) / Number), Type = (int)(x * (2 * Math.PI) / Number)});
            Series = new ObservableCollection<DataPoint>(xs);

            var horizontalAxis = ContinuousAxisManager<double>.Build(Series, p => p.X);
            horizontalAxis.ChartMargin = new RelativeMargin(0.05);
            HorizontalAxis = horizontalAxis;
            var verticalAxis = ContinuousAxisManager<double>.Build(Series, p => p.Y);
            verticalAxis.ChartMargin = new RelativeMargin(0.05);
            VerticalAxis = verticalAxis;
            Brushes = new List<BrushContainer>
            {
                new BrushContainer { Brush = new ConstantBrushMapper(System.Windows.Media.Brushes.Black), Label = "Black", },
                new BrushContainer { Brush = new ConstantBrushMapper(System.Windows.Media.Brushes.Red), Label = "Red", },
                new BrushContainer { Brush = new KeyBrushMapper<DataPoint, int>(new Dictionary<int, Color> { [0] = Colors.Red, [1] = Colors.Orange, [2] = Colors.Yellow, [3] = Colors.Green, [4] = Colors.Blue, [5] = Colors.Indigo, [6] = Colors.Purple, }, dp => dp.Type), Label = "Rainbow", },
            };
        }

        public ObservableCollection<DataPoint> Series { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public int Number {
            get => number;
            set => SetProperty(ref number, value);
        }
        private int number = 10000;

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

        public List<BrushContainer> Brushes { get; }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(Number)) {
                Series.Clear();
                foreach (var x in Enumerable.Range(0, Number)) {
                    Series.Add(new DataPoint() { X = x * (2 * Math.PI) / Number, Y = Math.Sin(x * (2 * Math.PI) / Number), Type = (int)(x * (2 * Math.PI) / Number)});
                }
            }
        }
    }

    internal class BrushContainer {
        public IBrushMapper Brush { get; set; /*init;*/ }
        public string Label { get; set; /*init;*/ }
    }
}
