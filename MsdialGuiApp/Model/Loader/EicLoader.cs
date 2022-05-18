using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
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
        }

        protected readonly IDataProvider provider;
        protected readonly ParameterBase parameter;
        protected readonly ChromXType chromXType;
        protected readonly ChromXUnit chromXUnit;
        protected readonly double rangeBegin, rangeEnd;

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
            var chromatogram = DataAccess.GetEicPeaklistByHighestBasePeakMz(
                        provider.LoadMs1Spectrums(),
                        targets.Select(n => n.InnerModel).ToList(), parameter.CentroidMs1Tolerance,
                        parameter.IonMode,
                        chromXType, chromXUnit,
                        rangeBegin, rangeEnd);
            return new CompMs.Common.Components.Chromatogram(chromatogram)
                .Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new ChromatogramPeakWrapper(peak))
                .ToList();
        }

        protected virtual Task<List<ChromatogramPeakWrapper>> LoadEicCoreAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            return Task.Run(async () =>
            {
                var spectra = await provider.LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
                var rawSpectra = new RawSpectra(spectra, chromXType, chromXUnit, parameter.IonMode);
                var ms1Peaks = rawSpectra.GetMs1Chromatogram(target.Mass, parameter.CentroidMs1Tolerance, rangeBegin, rangeEnd);
                return new CompMs.Common.Components.Chromatogram(ms1Peaks)
                    .Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                    .Where(peak => peak != null)
                    .Select(peak => new ChromatogramPeakWrapper(peak))
                    .ToList();
            });
        }

        protected virtual List<ChromatogramPeakWrapper> LoadEicCore(double mass, double massTolerance) {
            var rawSpectra = new RawSpectra(provider.LoadMs1Spectrums(), chromXType, chromXUnit, parameter.IonMode);
            var chromatogram = rawSpectra.GetMs1Chromatogram(mass, parameter.CentroidMs1Tolerance, rangeBegin, rangeEnd);
            return new CompMs.Common.Components.Chromatogram(chromatogram)
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
            var peaklist = DataAccess.GetTicPeaklist(
                        provider.LoadMs1Spectrums(),
                        parameter.IonMode,
                        chromXType, chromXUnit,
                        rangeBegin, rangeEnd);
            return new CompMs.Common.Components.Chromatogram(peaklist)
                .Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
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
            var chromatogram = DataAccess.GetBpcPeaklist(
                        provider.LoadMs1Spectrums(),
                        parameter.IonMode,
                        chromXType, chromXUnit,
                        rangeBegin, rangeEnd);
            return new CompMs.Common.Components.Chromatogram(chromatogram)
                .Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new ChromatogramPeakWrapper(peak))
                .ToList();
        }
    }
}
