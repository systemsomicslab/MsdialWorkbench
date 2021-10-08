using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartDrawingUiTest.Chart
{
    public class AxisLabelTestViewModel : ViewModelBase
    {
        public ObservableCollection<DataPoint> Series
        {
            get => series;
            set => SetProperty(ref series, value);
        }

        public double MinX
        {
            get => minX;
            set => SetProperty(ref minX, value);
        }

        public double MaxX
        {
            get => maxX;
            set => SetProperty(ref maxX, value);
        }

        public double MinY
        {
            get => minY;
            set => SetProperty(ref minY, value);
        }

        public double MaxY
        {
            get => maxY;
            set => SetProperty(ref maxY, value);
        }

        public IAxisManager AxisX { get; }
        public IAxisManager AxisY { get; }

        public ObservableCollection<LabelType> LabelTypes { get; }

        private ObservableCollection<DataPoint> series;
        private double minX;
        private double maxX;
        private double minY;
        private double maxY;

        public AxisLabelTestViewModel() {
            var xs = Enumerable.Range(-1000, 2001).Select(x => x / 10d).Select(x => new DataPoint() { X = x, Y = x * x * x, });
            Series = new ObservableCollection<DataPoint>(xs);
            MinX = xs.Min(dp => dp.X);
            MaxX = xs.Max(dp => dp.X);
            MinY = xs.Min(dp => dp.Y);
            MaxY = xs.Max(dp => dp.Y);

            var axisX = new ContinuousAxisManager<double>(xs.Min(dp => dp.X), xs.Max(dp => dp.X));
            var axisY = new ContinuousAxisManager<double>(xs.Min(dp => dp.Y), xs.Max(dp => dp.Y));
            axisX.ConstantMargin = 100;
            AxisX = axisX;
            AxisY = axisY;

            LabelTypes = new ObservableCollection<LabelType> {
                LabelType.Standard, LabelType.Order,
                LabelType.Relative, LabelType.Percent,
            };
        }
    }
}
