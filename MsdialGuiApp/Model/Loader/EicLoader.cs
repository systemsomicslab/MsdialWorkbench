using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Extension;
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
    public class EicLoader : IChromatogramLoader
    {
        protected EicLoader(
            IDataProvider provider,
            ParameterBase parameter,
            ChromXType chromXType,
            ChromXUnit chromXUnit,
            double rangeBegin,
            double rangeEnd,
            bool isConstantRange = true) {

            this.provider = provider;
            this.parameter = parameter;
            this.chromXType = chromXType;
            this.chromXUnit = chromXUnit;
            this.rangeBegin = rangeBegin;
            this.rangeEnd = rangeEnd;
            _isConstantRange = isConstantRange;

            _rawSpectraTask = Task.Run(async () => new RawSpectra(await provider.LoadMs1SpectrumsAsync(default).ConfigureAwait(false), parameter.IonMode, parameter.AcquisitionType));
            _chromatogramRange = new ChromatogramRange(rangeBegin, rangeEnd, chromXType, chromXUnit);
        }

        protected readonly IDataProvider provider;
        protected readonly ParameterBase parameter;
        protected readonly ChromXType chromXType;
        protected readonly ChromXUnit chromXUnit;
        protected readonly double rangeBegin, rangeEnd;
        private readonly bool _isConstantRange;
        private readonly Task<RawSpectra> _rawSpectraTask;
        private readonly ChromatogramRange _chromatogramRange;

        private RawSpectra RawSpectra => _rawSpectraTask.Result;

        public double MzTolerance => parameter.CentroidMs1Tolerance;

        async Task<DataObj.Chromatogram> IChromatogramLoader.LoadChromatogramAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {

            if (target != null) {
                var chromatogram = await Task.Run(async () =>
                {
                    var eic = await LoadEicCoreAsync(target, token).ConfigureAwait(false);
                    if (eic.Count == 0) {
                        return new DataObj.Chromatogram(new List<PeakItem>(), new List<PeakItem>(), null, string.Empty, Colors.Black, chromXType, chromXUnit);
                    }

                    token.ThrowIfCancellationRequested();
                    var eicPeakTask = Task.Run(() => LoadEicPeakCore(target, eic));
                    var eicFocusedTask = Task.Run(() => LoadEicFocusedCore(target, eic));

                    var results = await Task.WhenAll(eicPeakTask, eicFocusedTask).ConfigureAwait(false);
                    var peakEic = results[0];
                    var focusedEic = results[1];
                    return new DataObj.Chromatogram(eic.Select(peak => peak.ConvertToPeakItem()).ToList(), peakEic.Select(peak => peak.ConvertToPeakItem()).ToList(), focusedEic.Select(peak => peak.ConvertToPeakItem()).FirstOrDefault(), string.Empty, Colors.Black, chromXType, chromXUnit, $"EIC chromatogram of {target.Mass:N4} tolerance [Da]: {MzTolerance:F} Max intensity: {peakEic.Max(peak => peak.Intensity):F0}");
                }, token).ConfigureAwait(false);
                return chromatogram;
            }

            token.ThrowIfCancellationRequested();
            return new DataObj.Chromatogram(new List<PeakItem>(), new List<PeakItem>(), null, string.Empty, Colors.Black, chromXType, chromXUnit);
        }

        internal async Task<(List<ChromatogramPeakWrapper>, List<ChromatogramPeakWrapper>, List<ChromatogramPeakWrapper>)>
            LoadEicAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {

            var eic = new List<ChromatogramPeakWrapper>();
            var peakEic = new List<ChromatogramPeakWrapper>();
            var focusedEic = new List<ChromatogramPeakWrapper>();

            if (target != null) {
                await Task.Run(async () =>
                {
                    eic = await LoadEicCoreAsync(target, token).ConfigureAwait(false);
                    if (eic.Count == 0)
                        return;

                    token.ThrowIfCancellationRequested();
                    var eicPeakTask = Task.Run(() => LoadEicPeakCore(target, eic));
                    var eicFocusedTask = Task.Run(() => LoadEicFocusedCore(target, eic));

                    var results = await Task.WhenAll(eicPeakTask, eicFocusedTask).ConfigureAwait(false);
                    peakEic = results[0];
                    focusedEic = results[1];
                }, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            return (eic, peakEic, focusedEic);
        }

        internal List<ChromatogramPeakWrapper> LoadEicTrace(double mass, double massTolerance) {
            var eic = LoadEicCore(mass, massTolerance);
            return eic;
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

        public List<ChromatogramPeakWrapper> LoadHighestEicTrace(List<ChromatogramPeakFeatureModel> targets) {
            return RawSpectra
                .GetMs1ExtractedChromatogramByHighestBasePeakMz(targets, parameter.CentroidMs1Tolerance, _chromatogramRange)
                .Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new ChromatogramPeakWrapper(peak))
                .ToList();
        }

        protected virtual Task<List<ChromatogramPeakWrapper>> LoadEicCoreAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            return Task.Run(async () =>
            {
                var rawSpectra = await _rawSpectraTask.ConfigureAwait(false);
                var ms1Peaks = rawSpectra.GetMs1ExtractedChromatogram(target.Mass, parameter.CentroidMs1Tolerance, GetChromatogramRange(target));
                return ms1Peaks.Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                    .Where(peak => peak != null)
                    .Select(peak => new ChromatogramPeakWrapper(peak))
                    .ToList();
            });
        }

        protected virtual List<ChromatogramPeakWrapper> LoadEicCore(double mass, double massTolerance) {
            return RawSpectra
                .GetMs1ExtractedChromatogram(mass, parameter.CentroidMs1Tolerance, _chromatogramRange)
                .Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new ChromatogramPeakWrapper(peak))
                .ToList();
        }

        protected virtual List<ChromatogramPeakWrapper> LoadEicPeakCore(ChromatogramPeakFeatureModel target, List<ChromatogramPeakWrapper> eic) {
            return eic.Where(peak => target.ChromXLeftValue <= peak.ChromXValue && peak.ChromXValue <= target.ChromXRightValue).ToList();
        }

        protected virtual List<ChromatogramPeakWrapper> LoadEicFocusedCore(ChromatogramPeakFeatureModel target, List<ChromatogramPeakWrapper> eic) {
            return new List<ChromatogramPeakWrapper> {
                eic.Where(peak => peak.ChromXValue.HasValue)
                   .Argmin(peak => Math.Abs(target.ChromXValue.Value - peak.ChromXValue.Value))
            };
        }

        internal List<ChromatogramPeakWrapper>
            LoadTic() {

            var tic = LoadTicCore();
            if (tic.Count == 0) {
                return new List<ChromatogramPeakWrapper>();
            }

            return tic;
        }

        protected virtual List<ChromatogramPeakWrapper> LoadTicCore() {
            var chromatogram = RawSpectra.GetMs1TotalIonChromatogram(_chromatogramRange);
            return chromatogram.Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new ChromatogramPeakWrapper(peak))
                .ToList();
        }

        internal List<ChromatogramPeakWrapper>
            LoadBpc() {

            var bpc = LoadBpcCore();
            if (bpc.Count == 0) {
                return new List<ChromatogramPeakWrapper>();
            }

            return bpc;
        }

        protected virtual List<ChromatogramPeakWrapper> LoadBpcCore() {
            return RawSpectra.GetMs1BasePeakChromatogram(_chromatogramRange)
                .Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new ChromatogramPeakWrapper(peak))
                .ToList();
        }

        public static EicLoader BuildForAllRange(IDataProvider provider, ParameterBase parameter, ChromXType chromXType, ChromXUnit chromXUnit, double rangeBegin, double rangeEnd) {
            return new EicLoader(provider, parameter, chromXType, chromXUnit, rangeBegin, rangeEnd);
        }

        public static EicLoader BuildForPeakRange(IDataProvider provider, ParameterBase parameter, ChromXType chromXType, ChromXUnit chromXUnit, double rangeBegin, double rangeEnd) {
            return new EicLoader(provider, parameter, chromXType, chromXUnit, rangeBegin, rangeEnd, isConstantRange: false);
        }
    }
}
