using CompMs.CommonMVVM;
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

            HorizontalAxis = Log2HistogramAxis.Create(Histogram.Select(item => item.X), new ConstantMargin(30)).AddTo(Disposables);
            VerticalAxis = HistogramCountAxis.Create(AccumulatedHistogram.Select(item => item.Y), new ConstantMargin(0, 20)).AddTo(Disposables);
        }

        public DataPointCollection Histogram { get; }
        public DataPointCollection AccumulatedHistogram { get; }
        public IAxisManager<double> HorizontalAxis { get; }
        public IAxisManager<double> VerticalAxis { get; }
    }
}
