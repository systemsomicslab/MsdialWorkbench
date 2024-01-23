using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;

namespace CompMs.App.RawDataViewer.Model
{
    public sealed class MsSnDistribution : DisposableModelBase
    {
        public MsSnDistribution(IReadOnlyList<ChromatogramPeakFeature> peaks) {
            Peaks = (peaks as IList<ChromatogramPeakFeature>) ?? peaks.ToArray();

            NoiseMax = peaks.Where(peak => peak.PeakShape.SignalToNoise < 10d).DefaultIfEmpty().Max(item => item?.PeakShape.SignalToNoise) ?? -1;
            PeakMin = peaks.Where(peak => peak.PeakShape.SignalToNoise >= 10d).DefaultIfEmpty().Min(item => item?.PeakShape.SignalToNoise) ?? -1;

            HorizontalAxis = new LogScaleAxisManager<double>(peaks.Select(peak => (double)peak.PeakShape.SignalToNoise).ToArray(), new ConstantMargin(30), base_: 10).AddTo(Disposables);
            VerticalAxis = new LogScaleAxisManager<double>(peaks.Select(peak => peak.PeakFeature.PeakHeightTop).ToArray(), new ConstantMargin(30), base_: 10).AddTo(Disposables);
            Brush = new KeyBrushMapper<ChromatogramPeakFeature, bool>(new Dictionary<bool, Brush> { [false] = Brushes.Green, [true] = Brushes.Black }, peak => peak.PeakShape.SignalToNoise >= 10d);
        }

        public IList<ChromatogramPeakFeature> Peaks { get; }

        public IAxisManager<double> HorizontalAxis { get; }
        public IAxisManager<double> VerticalAxis { get; }

        public IBrushMapper<ChromatogramPeakFeature> Brush { get; }

        public double NoiseMax { get; }
        public double PeakMin { get; }
    }
}
