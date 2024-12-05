using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChartDrawingUiTest.Chart
{
    public class AxisTestViewModel : ViewModelBase
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

        public IAxisManager AxisY {
            get => axisY;
            set => SetProperty(ref axisY, value);
        }
        private IAxisManager axisY;

        public ObservableCollection<IAxisManager> AxisYs { get; }

        private ObservableCollection<DataPoint> series;
        private double minX;
        private double maxX;
        private double minY;
        private double maxY;

        public AxisTestViewModel() {
            var xs = Enumerable.Range(-10000, 20000).Select(x => new DataPoint() { X = x / 1000d, Y = Math.Exp(x / 1000d), Type = x / 200});
            Series = new ObservableCollection<DataPoint>(xs);
            MinX = xs.Min(dp => dp.X);
            MaxX = xs.Max(dp => dp.X);
            MinY = xs.Min(dp => dp.Y);
            MaxY = xs.Max(dp => dp.Y);

            var axisX = new ContinuousAxisManager<double>(xs.Min(dp => dp.X), xs.Max(dp => dp.X));
            axisX.ChartMargin = new ConstantMargin(100, 10);
            var axisY = new ContinuousAxisManager<double>(xs.Min(dp => dp.Y), xs.Max(dp => dp.Y));
            axisY.ChartMargin = new ConstantMargin(20, 50);
            var logAxisY = LogScaleAxisManager<double>.Build(Series, p => p.Y);
            logAxisY.ChartMargin = new ConstantMargin(20, 50);
            var relativeY = new RelativeAxisManager(xs.Min(dp => dp.Y), xs.Max(dp => dp.Y), new ConstantMargin(20, 50));
            var defectY = new DefectAxisManager(100d, new ConstantMargin(20, 50));
            var sqrtY = new SqrtAxisManager(xs.Min(dp => dp.Y), xs.Max(dp => dp.Y), new ConstantMargin(20, 50));
            var constY = ConstantAxisManager.Instance;

            AxisYs = new ObservableCollection<IAxisManager>(new IAxisManager[]
            {
                axisY, logAxisY, relativeY, defectY, sqrtY, constY,
            });

            AxisX = axisX;
            AxisY = axisY;
        }
    }
}
