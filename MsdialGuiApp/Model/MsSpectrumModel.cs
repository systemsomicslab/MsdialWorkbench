using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model
{
    public class MsSpectrumModel<T>: ValidatableBase
    {
        public MsSpectrumModel(
            AxisData horizontalData,
            AxisData upperVerticalData,
            IMsSpectrumLoader<T> upperLoader,
            AxisData lowerVerticalData,
            IMsSpectrumLoader<T> lowerLoader,
            string graphTitle) {

            HorizontalData = horizontalData;
            UpperVerticalData = upperVerticalData;
            LowerVerticalData = lowerVerticalData;
            GraphTitle = graphTitle;

            this.upperLoader = upperLoader;
            this.lowerLoader = lowerLoader;
        }

        private readonly IMsSpectrumLoader<T> upperLoader;

        private readonly IMsSpectrumLoader<T> lowerLoader;
        private IList<SpectrumPeak> upperSpectrum = new List<SpectrumPeak>(0);
        private IList<SpectrumPeak> lowerSpectrum = new List<SpectrumPeak>(0);
        private AxisData horizontalData;
        private AxisData upperVerticalData;
        private AxisData lowerVerticalData;
        private string graphTitle;

        public IList<SpectrumPeak> UpperSpectrum {
            get => upperSpectrum;
            set => SetProperty(ref upperSpectrum, value);
        }

        public IList<SpectrumPeak> LowerSpectrum {
            get => lowerSpectrum;
            set => SetProperty(ref lowerSpectrum, value);
        }

        public AxisData HorizontalData {
            get => horizontalData;
            set => SetProperty(ref horizontalData, value);
        }

        public AxisData UpperVerticalData {
            get => upperVerticalData;
            set => SetProperty(ref upperVerticalData, value);
        }

        public AxisData LowerVerticalData {
            get => lowerVerticalData;
            set => SetProperty(ref lowerVerticalData, value);
        }

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }

        public async Task LoadSpectrumAsync(T target, CancellationToken token) {
            var spectrums = await Task.WhenAll(
                upperLoader.LoadSpectrumAsync(target, token),
                lowerLoader.LoadSpectrumAsync(target, token));

            UpperSpectrum = spectrums[0];
            LowerSpectrum = spectrums[1];

            HorizontalData = new AxisData(
                ContinuousAxisManager<double>.Build(
                    UpperSpectrum.Concat(LowerSpectrum),
                    peak => peak.Mass,
                    HorizontalData.Axis.Bounds),
                HorizontalData.Property,
                HorizontalData.Title);
            UpperVerticalData = new AxisData(
                ContinuousAxisManager<double>.Build(
                    UpperSpectrum,
                    peak => peak.Intensity,
                    UpperVerticalData.Axis.Bounds),
                UpperVerticalData.Property,
                UpperVerticalData.Title);
            LowerVerticalData = new AxisData(
                ContinuousAxisManager<double>.Build(
                    LowerSpectrum,
                    peak => peak.Intensity,
                    LowerVerticalData.Axis.Bounds),
                LowerVerticalData.Property,
                LowerVerticalData.Title);
        }
    }
}
