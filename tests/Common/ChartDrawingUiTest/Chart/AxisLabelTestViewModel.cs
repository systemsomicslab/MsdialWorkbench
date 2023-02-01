using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;

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
        public IAxisManager AxisY {
            get => _axisY;
            set => SetProperty(ref _axisY, value);
        }
        private IAxisManager _axisY;

        public ObservableCollection<LabelType> LabelTypes { get; }
        public ObservableCollection<object> AxisTypes { get; }

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

            AxisX = new ContinuousAxisManager<double>(xs.Min(dp => dp.X), xs.Max(dp => dp.X), new ConstantMargin(100));
            LabelTypes = new ObservableCollection<LabelType> {
                LabelType.Standard, LabelType.Order,
                LabelType.Relative, LabelType.Percent,
            };
            AxisTypes = new ObservableCollection<object>
            {
                new LabelItem { Item = new ContinuousAxisManager<double>(xs.Min(dp => dp.Y), xs.Max(dp => dp.Y)) { LabelType = LabelType.Standard, }, Label = "ContinuousStandard"},
                new LabelItem { Item = new ContinuousAxisManager<double>(xs.Min(dp => dp.Y), xs.Max(dp => dp.Y)) { LabelType = LabelType.Order, }, Label = "ContinuousOrder", },
                new LabelItem { Item = new ContinuousAxisManager<double>(xs.Min(dp => dp.Y), xs.Max(dp => dp.Y)) { LabelType = LabelType.Relative, }, Label = "ContinuousRelative", },
                new LabelItem { Item = new ContinuousAxisManager<double>(xs.Min(dp => dp.Y), xs.Max(dp => dp.Y)) { LabelType = LabelType.Percent, }, Label = "ContinuousPercent", },
                new LabelItem { Item = new LogScaleAxisManager<double>(Math.Max(1e-10, xs.Min(dp => dp.Y)), xs.Max(dp => dp.Y)) { LabelType = LabelType.Standard, }, Label = "LogStandard"},
                new LabelItem { Item = new LogScaleAxisManager<double>(Math.Max(1e-10, xs.Min(dp => dp.Y)), xs.Max(dp => dp.Y)) { LabelType = LabelType.Relative, }, Label = "LogRelative", },
                new LabelItem { Item = new SqrtAxisManager(Math.Max(1e-10, xs.Min(dp => dp.Y)), xs.Max(dp => dp.Y)) { LabelType = LabelType.Standard, }, Label = "SqrtStandard"},
                new LabelItem { Item = new SqrtAxisManager(Math.Max(1e-10, xs.Min(dp => dp.Y)), xs.Max(dp => dp.Y)) { LabelType = LabelType.Relative, }, Label = "SqrtRelative", },
            };
            AxisY = ((LabelItem)AxisTypes.First()).Item;
        }

        class LabelItem {
            public IAxisManager Item { get; set; }
            public string Label { get; set; }
        }
    }
}
