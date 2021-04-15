using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model
{
    public class EicModel : ValidatableBase
    {
        public EicModel(
            AxisData horizontalData,
            AxisData verticalData,
            IDataProvider provider,
            ParameterBase parameter,
            ChromXType chromXType,
            ChromXUnit chromXUnit,
            double rangeBegin,
            double rangeEnd) {

            HorizontalData = horizontalData;
            VerticalData = verticalData;

            loader = new EicLoader(provider, parameter, chromXType, chromXUnit, rangeBegin, rangeEnd);
        }

        internal EicModel(
            AxisData horizontalData,
            AxisData verticalData,
            EicLoader loader) {

            HorizontalData = horizontalData;
            VerticalData = verticalData;

            this.loader = loader;
        }

        private readonly EicLoader loader;

        public IList<ChromatogramPeakWrapper> Eic {
            get => eic;
            set => SetProperty(ref eic, value);
        }
        private IList<ChromatogramPeakWrapper> eic = new List<ChromatogramPeakWrapper>(0);

        public IList<ChromatogramPeakWrapper> EicPeak {
            get => eicPeak;
            set => SetProperty(ref eicPeak, value);
        }
        private IList<ChromatogramPeakWrapper> eicPeak = new List<ChromatogramPeakWrapper>(0);

        public IList<ChromatogramPeakWrapper> EicFocused {
            get => eicFocused;
            set => SetProperty(ref eicFocused, value);
        }
        private IList<ChromatogramPeakWrapper> eicFocused = new List<ChromatogramPeakWrapper>(0);

        public AxisData HorizontalData {
            get => horizontalData;
            set => SetProperty(ref horizontalData, value);
        }
        private AxisData horizontalData;

        public AxisData VerticalData {
            get => verticalData;
            set => SetProperty(ref verticalData, value);
        }
        private AxisData verticalData;

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle;

        public async Task LoadEicAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            (Eic, EicPeak, EicFocused) = await loader.LoadEicAsync(target, token);
        }

        public void LoadEic(ChromatogramPeakFeatureModel target) {
            (Eic, EicPeak, EicFocused) = loader.LoadEic(target);
        }
    }

    class EicLoader
    {
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

        internal async Task<(List<ChromatogramPeakWrapper>, List<ChromatogramPeakWrapper>, List<ChromatogramPeakWrapper>)>
            LoadEicAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {

            var eic = new List<ChromatogramPeakWrapper>();
            var peakEic = new List<ChromatogramPeakWrapper>();
            var focusedEic = new List<ChromatogramPeakWrapper>();

            if (target != null) {
                await Task.Run(async () => {
                    eic = LoadEicCore(target);
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

        internal (List<ChromatogramPeakWrapper>, List<ChromatogramPeakWrapper>, List<ChromatogramPeakWrapper>)
            LoadEic(ChromatogramPeakFeatureModel target) {

            var eic = LoadEicCore(target);
            if (eic.Count == 0) {
                return (
                    new List<ChromatogramPeakWrapper>(),
                    new List<ChromatogramPeakWrapper>(),
                    new List<ChromatogramPeakWrapper>());
            }

            return (
                eic,
                LoadEicPeakCore(target, eic),
                LoadEicFocusedCore(target, eic));
        }

        protected virtual List<ChromatogramPeakWrapper> LoadEicCore(ChromatogramPeakFeatureModel target) {
            return DataAccess.GetSmoothedPeaklist(
                DataAccess.GetMs1Peaklist(
                        provider.LoadMs1Spectrums(),
                        target.Mass, parameter.CentroidMs1Tolerance,
                        parameter.IonMode,
                        chromXType, chromXUnit,
                        rangeBegin, rangeEnd),
                    parameter.SmoothingMethod, parameter.SmoothingLevel)
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
    }
}
