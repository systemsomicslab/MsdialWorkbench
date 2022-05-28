using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
    public class EicLoader {
        public EicLoader(
            IDataProvider provider,
            ParameterBase parameter,
            ChromXType chromXType,
            ChromXUnit chromXUnit,
            double rangeBegin,
            double rangeEnd) {

            this.provider = provider;
            this.parameter = parameter;
            this.chromXType = chromXType;
            this.chromXUnit = chromXUnit;
            this.rangeBegin = rangeBegin;
            this.rangeEnd = rangeEnd;

            _rawSpectraTask = Task.Run(async () => new RawSpectra(await provider.LoadMs1SpectrumsAsync(default).ConfigureAwait(false), parameter.IonMode, parameter.AcquisitionType));
            _chromatogramRange = new ChromatogramRange(rangeBegin, rangeEnd, chromXType, chromXUnit);
        }

        protected readonly IDataProvider provider;
        protected readonly ParameterBase parameter;
        protected readonly ChromXType chromXType;
        protected readonly ChromXUnit chromXUnit;
        protected readonly double rangeBegin, rangeEnd;
        private readonly Task<RawSpectra> _rawSpectraTask;
        private readonly ChromatogramRange _chromatogramRange;

        private RawSpectra _rawSpectra => _rawSpectraTask.Result;

        public double MzTolerance => parameter.CentroidMs1Tolerance;

        internal async Task<(List<ChromatogramPeakWrapper>, List<ChromatogramPeakWrapper>, List<ChromatogramPeakWrapper>)>
            LoadEicAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {

            var eic = new List<ChromatogramPeakWrapper>();
            var peakEic = new List<ChromatogramPeakWrapper>();
            var focusedEic = new List<ChromatogramPeakWrapper>();

            if (target != null) {
                await Task.Run(async () => {
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

        internal List<ChromatogramPeakWrapper> LoadHighestEicTrace(List<ChromatogramPeakFeatureModel> targets) {
            return _rawSpectra
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
                var ms1Peaks = rawSpectra.GetMs1ExtractedChromatogram(target.Mass, parameter.CentroidMs1Tolerance, _chromatogramRange);
                return ms1Peaks.Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                    .Where(peak => peak != null)
                    .Select(peak => new ChromatogramPeakWrapper(peak))
                    .ToList();
            });
        }

        protected virtual List<ChromatogramPeakWrapper> LoadEicCore(double mass, double massTolerance) {
            return _rawSpectra
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
            var chromatogram = _rawSpectra.GetMs1TotalIonChromatogram(_chromatogramRange);
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
            return _rawSpectra.GetMs1BasePeakChromatogram(_chromatogramRange)
                .Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new ChromatogramPeakWrapper(peak))
                .ToList();
        }
    }
}
