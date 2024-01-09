using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class QuantMassEicLoader : IChromatogramLoader<Ms1BasedSpectrumFeature>
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

        public double MzTolerance => _peakPickParameter.MassSliceWidth;

        async Task<DataObj.Chromatogram> IChromatogramLoader<Ms1BasedSpectrumFeature>.LoadChromatogramAsync(Ms1BasedSpectrumFeature target, CancellationToken token) {
            if (target != null) {
                var eic = await LoadEicCoreAsync(target.QuantifiedChromatogramPeak.PeakFeature, token).ConfigureAwait(false);
                if (eic.Count == 0) {
                    return new DataObj.Chromatogram(new List<PeakItem>(), new List<PeakItem>(), null, string.Empty, Colors.Black, _chromXType, _chromXUnit);
                }
                token.ThrowIfCancellationRequested();
                Task<List<PeakItem>> peakEicTask = Task.Run(() => LoadEicPeakCore(target.QuantifiedChromatogramPeak.PeakFeature, eic), token);
                Task<PeakItem> focusedEicTask = Task.Run(() => LoadEicFocusedCore(target.QuantifiedChromatogramPeak.PeakFeature, eic), token);
                await Task.WhenAll(peakEicTask, focusedEicTask).ConfigureAwait(false);
                var peakEic = peakEicTask.Result;
                return new DataObj.Chromatogram(eic, peakEic, focusedEicTask.Result, string.Empty, Colors.Black, _chromXType, _chromXUnit, $"EIC of {target.QuantifiedChromatogramPeak.PeakFeature.Mass:N4} tolerance [Da]: {MzTolerance:F} Max intensity: {peakEic.Max(peak => peak.Intensity):F0}");
            }
            return new DataObj.Chromatogram(new List<PeakItem>(), new List<PeakItem>(), null, string.Empty, Colors.Black, _chromXType, _chromXUnit);
        }

        private async Task<List<PeakItem>> LoadEicCoreAsync(IChromatogramPeakFeature peakFeature, CancellationToken token) {
            var rawSpectra = await _rawSpectraTask.ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            var ms1Peaks = rawSpectra.GetMs1ExtractedChromatogram(peakFeature.Mass, _peakPickParameter.MassSliceWidth, GetChromatogramRange(peakFeature));
            token.ThrowIfCancellationRequested();
            return ms1Peaks.Smoothing(_peakPickParameter.SmoothingMethod, _peakPickParameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new PeakItem(peak))
                .ToList();
        }

        private List<PeakItem> LoadEicPeakCore(IChromatogramPeakFeature target, List<PeakItem> eic) {
            return eic.Where(peak => target.ChromXsLeft.RT.Value <= peak.Time && peak.Time <= target.ChromXsRight.RT.Value).ToList();
        }

        private PeakItem LoadEicFocusedCore(IChromatogramPeakFeature target, List<PeakItem> eic) {
            return eic.Argmin(peak => Math.Abs(target.ChromXsTop.RT.Value - peak.Time));
        }

        private static readonly double PEAK_WIDTH_FACTOR = 3d;
        private ChromatogramRange GetChromatogramRange(IChromatogramPeakFeature target) {
            if (_isConstantRange) {
                return _chromatogramRange;
            }
            var width = target.PeakWidth(_chromXType) * PEAK_WIDTH_FACTOR;
            var center = target.ChromXsTop.RT.Value;
            return new ChromatogramRange(center - width / 2, center + width / 2, _chromXType, _chromXUnit);
        }
    }
}
