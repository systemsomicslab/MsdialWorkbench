using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.Raw.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Loader
{
    public class EicLoader : IChromatogramLoader<ChromatogramPeakFeatureModel>, IWholeChromatogramLoader<(double mass, double tolerance)>, IWholeChromatogramLoader<MzRange>
    {
        public EicLoader(AnalysisFileBean file, IDataProvider provider, PeakPickBaseParameter peakPickParameter, IonMode ionMode, ChromXType chromXType, ChromXUnit chromXUnit, double rangeBegin, double rangeEnd, bool isConstantRange = true) {
            _peakPickParameter = peakPickParameter;
            _ionMode = ionMode;
            _isConstantRange = isConstantRange;

            _rawSpectraTask = Task.Run(() => new RawSpectra(provider, ionMode, file.AcquisitionType));
            _chromatogramRange = new ChromatogramRange(rangeBegin, rangeEnd, chromXType, chromXUnit);
        }

        public EicLoader(RawSpectra rawSpectra, PeakPickBaseParameter peakPickParameter, IonMode ionMode, ChromatogramRange chromatogramRange, bool isConstantRange = true) {
            _peakPickParameter = peakPickParameter;
            _ionMode = ionMode;
            _isConstantRange = isConstantRange;

            _rawSpectraTask = Task.FromResult(rawSpectra);
            _chromatogramRange = chromatogramRange;
        }

        protected readonly PeakPickBaseParameter _peakPickParameter;
        private readonly IonMode _ionMode;
        private readonly bool _isConstantRange;
        private readonly Task<RawSpectra> _rawSpectraTask;
        private readonly ChromatogramRange _chromatogramRange;

        private RawSpectra RawSpectra => _rawSpectraTask.Result;

        public double MzTolerance => _peakPickParameter.CentroidMs1Tolerance;

        async Task<PeakChromatogram> IChromatogramLoader<ChromatogramPeakFeatureModel>.LoadChromatogramAsync(ChromatogramPeakFeatureModel? target, CancellationToken token) {
            if (target is not null) {
                var eic = await LoadEicCoreAsync(target, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                var peak = eic.AsPeak(target.ChromXLeftValue, target.ChromXs.GetChromByType(_chromatogramRange.Type).Value, target.ChromXRightValue);
                var description = $"EIC chromatogram of {target.Mass:N4} tolerance [Da]: {MzTolerance:F} Max intensity: {peak?.GetTop().Intensity ?? 0d:F0}";
                return new PeakChromatogram(eic, peak, string.Empty, Colors.Black, description);
            }
            return new PeakChromatogram(new Chromatogram(Array.Empty<ValuePeak>(), _chromatogramRange.Type, _chromatogramRange.Unit), null, string.Empty, Colors.Black);
        }

        private static readonly double PEAK_WIDTH_FACTOR = 3d;
        private ChromatogramRange GetChromatogramRange(ChromatogramPeakFeatureModel target) {
            if (_isConstantRange) {
                return _chromatogramRange;
            }
            var width = target.InnerModel.PeakWidth(_chromatogramRange.Type) * PEAK_WIDTH_FACTOR;
            var center = target.ChromXValue ?? 0d;
            return new ChromatogramRange(center - width / 2, center + width / 2, _chromatogramRange.Type, _chromatogramRange.Unit);
        }

        protected virtual async Task<Chromatogram> LoadEicCoreAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var rawSpectra = await _rawSpectraTask.ConfigureAwait(false);
            using var ms1Peaks = await rawSpectra.GetMS1ExtractedChromatogramAsync(new MzRange(target.Mass, _peakPickParameter.CentroidMs1Tolerance), GetChromatogramRange(target), token).ConfigureAwait(false);
            return ms1Peaks.ChromatogramSmoothing(_peakPickParameter.SmoothingMethod, _peakPickParameter.SmoothingLevel);
        }

        private async Task<ExtractedIonChromatogram> LoadEicCoreAsync(double mass, double massTolerance, CancellationToken token) {
            using var eic = await RawSpectra.GetMS1ExtractedChromatogramAsync(new MzRange(mass, massTolerance), _chromatogramRange, token).ConfigureAwait(false);
            return eic.ChromatogramSmoothing(_peakPickParameter.SmoothingMethod, _peakPickParameter.SmoothingLevel);
        }

        async Task<DisplayChromatogram> IWholeChromatogramLoader<(double mass, double tolerance)>.LoadChromatogramAsync((double mass, double tolerance) state, CancellationToken token) {
            var eic = await LoadEicCoreAsync(state.mass, state.tolerance, token).ConfigureAwait(false);
            return new DisplayExtractedIonChromatogram(eic, state.tolerance, _ionMode);
        }

        async Task<DisplayChromatogram> IWholeChromatogramLoader<MzRange>.LoadChromatogramAsync(MzRange state, CancellationToken token) {
            var eic = await LoadEicCoreAsync(state.Mz, state.Tolerance, token).ConfigureAwait(false);
            return new DisplayExtractedIonChromatogram(eic, state.Tolerance, _ionMode);
        }

        PeakChromatogram IChromatogramLoader<ChromatogramPeakFeatureModel>.EmptyChromatogram => new PeakChromatogram(new Chromatogram(Array.Empty<ValuePeak>(), _chromatogramRange.Type, _chromatogramRange.Unit), null, string.Empty, Colors.Black);

        public static EicLoader BuildForAllRange(AnalysisFileBean file, IDataProvider provider, ParameterBase parameter, ChromXType chromXType, ChromXUnit chromXUnit, double rangeBegin, double rangeEnd) {
            return new EicLoader(file, provider, parameter.PeakPickBaseParam, parameter.IonMode, chromXType, chromXUnit, rangeBegin, rangeEnd);
        }

        public static EicLoader BuildForPeakRange(AnalysisFileBean file, IDataProvider provider, ParameterBase parameter, ChromXType chromXType, ChromXUnit chromXUnit, double rangeBegin, double rangeEnd) {
            return new EicLoader(file, provider, parameter.PeakPickBaseParam, parameter.IonMode, chromXType, chromXUnit, rangeBegin, rangeEnd, isConstantRange: false);
        }
    }
}
