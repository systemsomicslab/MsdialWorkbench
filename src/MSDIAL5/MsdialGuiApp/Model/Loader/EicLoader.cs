using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Loader
{
    public class EicLoader : IChromatogramLoader<ChromatogramPeakFeatureModel>, IWholeChromatogramLoader<(double mass, double tolerance)>
    {
        protected EicLoader(AnalysisFileBean file, IDataProvider provider, PeakPickBaseParameter peakPickParameter, IonMode ionMode, ChromXType chromXType, ChromXUnit chromXUnit, double rangeBegin, double rangeEnd, bool isConstantRange = true) {
            this.provider = provider;
            _peakPickParameter = peakPickParameter;
            this.chromXType = chromXType;
            this.chromXUnit = chromXUnit;
            this.rangeBegin = rangeBegin;
            this.rangeEnd = rangeEnd;
            _isConstantRange = isConstantRange;

            _rawSpectraTask = Task.Run(async () => new RawSpectra(await provider.LoadMs1SpectrumsAsync(default).ConfigureAwait(false), ionMode, file.AcquisitionType));
            _chromatogramRange = new ChromatogramRange(rangeBegin, rangeEnd, chromXType, chromXUnit);
        }

        protected readonly IDataProvider provider;
        protected readonly PeakPickBaseParameter _peakPickParameter;
        protected readonly ChromXType chromXType;
        protected readonly ChromXUnit chromXUnit;
        protected readonly double rangeBegin, rangeEnd;
        private readonly bool _isConstantRange;
        private readonly Task<RawSpectra> _rawSpectraTask;
        private readonly ChromatogramRange _chromatogramRange;

        private RawSpectra RawSpectra => _rawSpectraTask.Result;

        public double MzTolerance => _peakPickParameter.CentroidMs1Tolerance;

        async Task<PeakChromatogram> IChromatogramLoader<ChromatogramPeakFeatureModel>.LoadChromatogramAsync(ChromatogramPeakFeatureModel? target, CancellationToken token) {
            if (target is not null) {
                var eic = await LoadEicCoreAsync(target, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                var peak = eic.AsPeak(target.ChromXLeftValue, target.ChromXs.GetChromByType(chromXType).Value, target.ChromXRightValue);
                var description = $"EIC chromatogram of {target.Mass:N4} tolerance [Da]: {MzTolerance:F} Max intensity: {peak?.GetTop().Intensity ?? 0d:F0}";
                return new PeakChromatogram(eic, peak, string.Empty, Colors.Black, description);
            }
            return new PeakChromatogram(new Chromatogram(Array.Empty<ValuePeak>(), chromXType, chromXUnit), null, string.Empty, Colors.Black);
        }

        private static readonly double PEAK_WIDTH_FACTOR = 3d;
        private ChromatogramRange GetChromatogramRange(ChromatogramPeakFeatureModel target) {
            if (_isConstantRange) {
                return _chromatogramRange;
            }
            var width = target.InnerModel.PeakWidth(chromXType) * PEAK_WIDTH_FACTOR;
            var center = target.ChromXValue ?? 0d;
            return new ChromatogramRange(center - width / 2, center + width / 2, chromXType, chromXUnit);
        }

        protected virtual async Task<Chromatogram> LoadEicCoreAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var rawSpectra = await _rawSpectraTask.ConfigureAwait(false);
            var ms1Peaks = rawSpectra.GetMs1ExtractedChromatogram(target.Mass, _peakPickParameter.CentroidMs1Tolerance, GetChromatogramRange(target));
            return ms1Peaks.SmoothedChromatogram(_peakPickParameter.SmoothingMethod, _peakPickParameter.SmoothingLevel);
        }

        protected Chromatogram LoadEicCore(double mass, double massTolerance) {
            return RawSpectra
                .GetMs1ExtractedChromatogram(mass, massTolerance, _chromatogramRange)
                .SmoothedChromatogram(_peakPickParameter.SmoothingMethod, _peakPickParameter.SmoothingLevel);
        }

        List<PeakItem> IWholeChromatogramLoader<(double mass, double tolerance)>.LoadChromatogram((double mass, double tolerance) state) {
            return LoadEicCore(state.mass, state.tolerance)
                .AsPeakArray()
                .Select(peak => new PeakItem(peak))
                .ToList();
        }

        PeakChromatogram IChromatogramLoader<ChromatogramPeakFeatureModel>.EmptyChromatogram => new PeakChromatogram(new Chromatogram(Array.Empty<ValuePeak>(), chromXType, chromXUnit), null, string.Empty, Colors.Black);

        public static EicLoader BuildForAllRange(AnalysisFileBean file, IDataProvider provider, ParameterBase parameter, ChromXType chromXType, ChromXUnit chromXUnit, double rangeBegin, double rangeEnd) {
            return new EicLoader(file, provider, parameter.PeakPickBaseParam, parameter.IonMode, chromXType, chromXUnit, rangeBegin, rangeEnd);
        }

        public static EicLoader BuildForPeakRange(AnalysisFileBean file, IDataProvider provider, ParameterBase parameter, ChromXType chromXType, ChromXUnit chromXUnit, double rangeBegin, double rangeEnd) {
            return new EicLoader(file, provider, parameter.PeakPickBaseParam, parameter.IonMode, chromXType, chromXUnit, rangeBegin, rangeEnd, isConstantRange: false);
        }
    }
}
