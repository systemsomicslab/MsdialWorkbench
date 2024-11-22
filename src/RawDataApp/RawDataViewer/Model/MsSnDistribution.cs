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

            NoiseMax = peaks.Where(peak => peak.PeakShape.SignalToNoise < 10d).OrderByDescending(item => item?.PeakFeature.PeakHeightTop).FirstOrDefault();
            PeakMin = peaks.Where(peak => peak.PeakShape.SignalToNoise >= 10d).OrderBy(item => item?.PeakFeature.PeakHeightTop).FirstOrDefault();

            HorizontalAxis = new LogScaleAxisManager<double>(peaks.Select(peak => (double)peak.PeakShape.SignalToNoise).ToArray(), new ConstantMargin(30)).AddTo(Disposables);
            VerticalAxis = new LogScaleAxisManager<double>(peaks.Select(peak => peak.PeakFeature.PeakHeightTop).ToArray(), new ConstantMargin(30)).AddTo(Disposables);
            Brush = new KeyBrushMapper<ChromatogramPeakFeature, bool>(new Dictionary<bool, Brush> { [false] = Brushes.Blue, [true] = Brushes.Red }, peak => peak.IsMsmsContained);
        }

        public IList<ChromatogramPeakFeature> Peaks { get; }

        public IAxisManager<double> HorizontalAxis { get; }
        public IAxisManager<double> VerticalAxis { get; }

        public IBrushMapper<ChromatogramPeakFeature> Brush { get; }

        public ChromatogramPeakFeature NoiseMax { get; }
        public ChromatogramPeakFeature PeakMin { get; }
    }
}
