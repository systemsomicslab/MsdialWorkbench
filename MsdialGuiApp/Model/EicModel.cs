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

            loader = new EicLoader(this, provider, parameter, chromXType, chromXUnit, rangeBegin, rangeEnd);
        }

        private EicLoader loader;

        public IList<ChromatogramPeakWrapper> Eic {
            get => eic;
            set => SetProperty(ref eic, value);
        }
        private IList<ChromatogramPeakWrapper> eic;

        public IList<ChromatogramPeakWrapper> EicPeak {
            get => eicPeak;
            set => SetProperty(ref eicPeak, value);
        }
        private IList<ChromatogramPeakWrapper> eicPeak;

        public IList<ChromatogramPeakWrapper> EicFocused {
            get => eicFocused;
            set => SetProperty(ref eicFocused, value);
        }
        private IList<ChromatogramPeakWrapper> eicFocused;

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
            await loader.LoadEicAsync(target, token);
            OnPropertyChanged(string.Empty);
        }

        public void LoadEic(ChromatogramPeakFeatureModel target) {
            loader.LoadEic(target);
            OnPropertyChanged(string.Empty);
        }
    }

    class EicLoader
    {
        public EicLoader(
            EicModel model,
            IDataProvider provider,
            ParameterBase parameter,
            ChromXType chromXType,
            ChromXUnit chromXUnit,
            double rangeBegin,
            double rangeEnd) {

            this.model = model;
            this.provider = provider;
            this.parameter = parameter;
            this.chromXType = chromXType;
            this.chromXUnit = chromXUnit;
            this.rangeBegin = rangeBegin;
            this.rangeEnd = rangeEnd;
        }

        private EicModel model;

        private readonly IDataProvider provider;
        private readonly ParameterBase parameter;
        private readonly ChromXType chromXType;
        private readonly ChromXUnit chromXUnit;
        private readonly double rangeBegin, rangeEnd;

        internal async Task LoadEicAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
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
            model.Eic = eic;
            model.EicPeak = peakEic;
            model.EicFocused = focusedEic;
        }

        internal void LoadEic(ChromatogramPeakFeatureModel target) {
            var eic = LoadEicCore(target);
            if (eic.Count == 0) {
                model.Eic = new List<ChromatogramPeakWrapper>();
                model.EicPeak = new List<ChromatogramPeakWrapper>();
                model.EicFocused = new List<ChromatogramPeakWrapper>();
                return;
            }

            model.Eic = eic;
            model.EicPeak = LoadEicPeakCore(target, eic);
            model.EicFocused = LoadEicFocusedCore(target, eic);
        }

        private List<ChromatogramPeakWrapper> LoadEicCore(ChromatogramPeakFeatureModel target) {
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

        private List<ChromatogramPeakWrapper> LoadEicPeakCore(ChromatogramPeakFeatureModel target, List<ChromatogramPeakWrapper> eic) {
            return eic.Where(peak => target.ChromXLeftValue <= peak.ChromXValue && peak.ChromXValue <= target.ChromXRightValue).ToList();
        }

        private List<ChromatogramPeakWrapper> LoadEicFocusedCore(ChromatogramPeakFeatureModel target, List<ChromatogramPeakWrapper> eic) {
            return new List<ChromatogramPeakWrapper> {
                eic.Where(peak => peak.ChromXValue.HasValue)
                   .Argmin(peak => Math.Abs(target.ChromXValue.Value - peak.ChromXValue.Value))
            };
        }
    }
}
