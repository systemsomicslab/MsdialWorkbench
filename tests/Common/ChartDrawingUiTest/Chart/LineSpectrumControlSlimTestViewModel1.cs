using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace ChartDrawingUiTest.Chart
{
    public class LineSpectrumControlSlimTestViewModel1 : ViewModelBase
    {
        public LineSpectrumControlSlimTestViewModel1() {
            var xs = Enumerable.Range(0, Number).Select(x => new DataPoint() { X = x * (2 * Math.PI) / Number, Y = Math.Sin(x * (2 * Math.PI) / Number), Type = (int)(x * (2 * Math.PI) / Number)});
            Series = new ObservableCollection<DataPoint>(xs);

            var horizontalAxis = ContinuousAxisManager<double>.Build(Series, p => p.X);
            horizontalAxis.ChartMargin = new RelativeMargin(0.05);
            HorizontalAxis = horizontalAxis;
            var verticalAxis = ContinuousAxisManager<double>.Build(Series, p => p.Y, new AxisRange(0d, 0d));
            verticalAxis.ChartMargin = new RelativeMargin(0.05);
            VerticalAxis = verticalAxis;
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
}
