using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;

namespace CompMs.App.RawDataViewer.Model
{
    public class MsPeakSpotsSummary : DisposableModelBase
    {
        public MsPeakSpotsSummary(DataPoint[] histogram) {
            if (histogram is null) {
                throw new ArgumentNullException(nameof(histogram));
            }

            Histogram = new DataPointCollection(histogram);
            AccumulatedHistogram = Histogram.Accumulate();

            HorizontalAxis = new LogScaleAxisManager<double>(Histogram.Select(item => item.X).ToArray(), new ConstantMargin(30), base_: 2).AddTo(Disposables);
            VerticalAxis = new ContinuousAxisManager<double>(AccumulatedHistogram.Select(item => item.Y).ToArray(), new ConstantMargin(0, 20), new AxisRange(0d, 0d)).AddTo(Disposables);
        }

        public DataPointCollection Histogram { get; }
        public DataPointCollection AccumulatedHistogram { get; }
        public IAxisManager<double> HorizontalAxis { get; }
        public IAxisManager<double> VerticalAxis { get; }
    }
}
