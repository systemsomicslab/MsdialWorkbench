using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Table;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAnalysisPeakTableModel : PeakSpotTableModelBase<Ms1BasedSpectrumFeature>
    {
        private readonly IReactiveProperty<Ms1BasedSpectrumFeature> _target;

        public GcmsAnalysisPeakTableModel(IReadOnlyList<Ms1BasedSpectrumFeature> spectrumFeature, IReactiveProperty<Ms1BasedSpectrumFeature> target, PeakSpotNavigatorModel peakSpotNavigatorModel)
            : base(spectrumFeature, target) {
            MassMin = spectrumFeature.Select(s => s.QuantifiedChromatogramPeak.PeakFeature.Mass).DefaultIfEmpty().Min();
            MassMax = spectrumFeature.Select(s => s.QuantifiedChromatogramPeak.PeakFeature.Mass).DefaultIfEmpty().Max();
            RtMin = spectrumFeature.Select(s => s.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value).DefaultIfEmpty().Min();
            RtMax = spectrumFeature.Select(s => s.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value).DefaultIfEmpty().Max();
            RiMin = spectrumFeature.Select(s => s.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RI.Value).DefaultIfEmpty().Min();
            RiMax = spectrumFeature.Select(s => s.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RI.Value).DefaultIfEmpty().Max();
            _target = target;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public double RiMin { get; }
        public double RiMax { get; }

        public override void MarkAllAsConfirmed() {
            foreach (var peak in PeakSpots) {
                peak.Confirmed = true;
            }
        }

        public override void SwitchTag(PeakSpotTag tag) {
            if (_target.Value is null) {
                return;
            }
            _target.Value.SwitchPeakSpotTag(tag);
        }
    }
}
