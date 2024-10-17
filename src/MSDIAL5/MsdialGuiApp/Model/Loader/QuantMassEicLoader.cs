using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class QuantMassEicLoader : IChromatogramLoader<Ms1BasedSpectrumFeature?>
    {
        private readonly PeakPickBaseParameter _peakPickParameter;
        private readonly ChromXType _chromXType;
        private readonly ChromXUnit _chromXUnit;
        private readonly bool _isConstantRange;
        private readonly Task<RawSpectra> _rawSpectraTask;
        private readonly ChromatogramRange _chromatogramRange;

        public QuantMassEicLoader(AnalysisFileBean file, IDataProvider provider, PeakPickBaseParameter peakPickParameter, IonMode ionMode, ChromXType chromXType, ChromXUnit chromXUnit, double rangeBegin, double rangeEnd, bool isConstantRange = true) {
            _peakPickParameter = peakPickParameter;
            _chromXType = chromXType;
            _chromXUnit = chromXUnit;
            _isConstantRange = isConstantRange;

            _rawSpectraTask = Task.Run(async () => new RawSpectra(await provider.LoadMs1SpectrumsAsync(default).ConfigureAwait(false), ionMode, file.AcquisitionType));
            _chromatogramRange = new ChromatogramRange(rangeBegin, rangeEnd, chromXType, chromXUnit);
        }

        public double MzTolerance => _peakPickParameter.CentroidMs1Tolerance;

        async Task<PeakChromatogram> IChromatogramLoader<Ms1BasedSpectrumFeature?>.LoadChromatogramAsync(Ms1BasedSpectrumFeature? target, CancellationToken token) {
            if (target is not null) {
                var peakFeature = target.QuantifiedChromatogramPeak.PeakFeature;
                Chromatogram eic = await LoadEicCoreAsync(peakFeature, token).ConfigureAwait(false);
                var smoothed = eic.ChromatogramSmoothing(_peakPickParameter.SmoothingMethod, _peakPickParameter.SmoothingLevel);
                token.ThrowIfCancellationRequested();
                var peakOfChromatogram = smoothed.AsPeak(peakFeature.ChromXsLeft.GetChromByType(_chromXType).Value, peakFeature.ChromXsTop.GetChromByType(_chromXType).Value, peakFeature.ChromXsRight.GetChromByType(_chromXType).Value);
                var description = $"EIC of {peakFeature.Mass:N4} tolerance [Da]: {MzTolerance:F} Max intensity: {peakOfChromatogram?.GetTop().Intensity ?? 0d:F0}";
                return new PeakChromatogram(smoothed, peakOfChromatogram, string.Empty, Colors.Black, description);
            }
            return new PeakChromatogram(new Chromatogram(Array.Empty<ValuePeak>(), _chromXType, _chromXUnit), null, string.Empty, Colors.Black);
        }

        private async Task<Chromatogram> LoadEicCoreAsync(IChromatogramPeakFeature peakFeature, CancellationToken token) {
            var rawSpectra = await _rawSpectraTask.ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return rawSpectra.GetMS1ExtractedChromatogram(new MzRange(peakFeature.Mass, _peakPickParameter.CentroidMs1Tolerance), GetChromatogramRange(peakFeature));
        }

        PeakChromatogram IChromatogramLoader<Ms1BasedSpectrumFeature?>.EmptyChromatogram  => new PeakChromatogram(new Chromatogram(Array.Empty<ValuePeak>(), _chromXType, _chromXUnit), null, string.Empty, Colors.Black);

        private static readonly double PEAK_WIDTH_FACTOR = 3d;
        private ChromatogramRange GetChromatogramRange(IChromatogramPeakFeature target) {
            if (_isConstantRange) {
                return _chromatogramRange;
            }
            var width = target.PeakWidth(_chromXType) * PEAK_WIDTH_FACTOR;
            var center = target.ChromXsTop.GetChromByType(_chromXType).Value;
            return new ChromatogramRange(center - width / 2, center + width / 2, _chromXType, _chromXUnit);
        }
    }
}
