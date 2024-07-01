using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChartDrawingUiTest.Chart
{
    internal class NestedPropertiesViewModel : BindableBase
    {
        public NestedPropertiesViewModel()
        {
            var xs = Enumerable.Range(0, 6).Select(x => x * (2 * Math.PI) / 6).Select(x => new DataPoint() { X = x, Y = Math.Sin(x), Type = (int)x});
            Series = new ObservableCollection<DataPoint>(xs);
            WrappedSeries = new ObservableCollection<DataPointWrapper>(xs.Select(x => new DataPointWrapper(x)));

            var horizontalAxis = ContinuousAxisManager<double>.Build(xs, p => p.X);
            horizontalAxis.ChartMargin = new RelativeMargin(0.05);
            HorizontalAxis = horizontalAxis;
            var verticalAxis = ContinuousAxisManager<double>.Build(xs, p => p.Y, new AxisRange(0d, 0d));
            verticalAxis.ChartMargin = new RelativeMargin(0.05);
            VerticalAxis = verticalAxis;
        }

        public ObservableCollection<DataPoint> Series { get; }
        public ObservableCollection<DataPointWrapper> WrappedSeries { get; }
        public ContinuousAxisManager<double> HorizontalAxis { get; }
        public ContinuousAxisManager<double> VerticalAxis { get; }
    }

    internal class DataPointWrapper {
        public DataPointWrapper(DataPoint dp) {
            Dp = dp;
        }

        public DataPoint Dp { get; }
    }
}
