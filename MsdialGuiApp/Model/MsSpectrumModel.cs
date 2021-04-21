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
    public class MsSpectrumModel : ValidatableBase
    {
        public MsSpectrumModel(
            AxisData horizontalData,
            AxisData upperVerticalData,
            IMsSpectrumLoader upperLoader,
            AxisData lowerVerticalData,
            IMsSpectrumLoader lowerLoader,
            string graphTitle) {

            HorizontalData = horizontalData;
            UpperVerticalData = upperVerticalData;
            LowerVerticalData = lowerVerticalData;
            GraphTitle = graphTitle;

            this.upperLoader = upperLoader;
            this.lowerLoader = lowerLoader;
        }

        private readonly IMsSpectrumLoader upperLoader, lowerLoader;

        public IList<SpectrumPeak> UpperSpectrum {
            get => upperSpectrum;
            set => SetProperty(ref upperSpectrum, value);
        }
        private IList<SpectrumPeak> upperSpectrum = new List<SpectrumPeak>(0);

        public IList<SpectrumPeak> LowerSpectrum {
            get => lowerSpectrum;
            set => SetProperty(ref lowerSpectrum, value);
        }
        private IList<SpectrumPeak> lowerSpectrum = new List<SpectrumPeak>(0);

        public async Task LoadSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var spectrums = await Task.WhenAll(
                upperLoader.LoadSpectrumAsync(target, token),
                lowerLoader.LoadSpectrumAsync(target, token));

            UpperSpectrum = spectrums[0];
            LowerSpectrum = spectrums[1];

            HorizontalData = new AxisData(
                ContinuousAxisManager<double>.Build(UpperSpectrum.Concat(LowerSpectrum), peak => peak.Mass),
                HorizontalData.Property,
                HorizontalData.Title);
            UpperVerticalData = new AxisData(
                ContinuousAxisManager<double>.Build(UpperSpectrum, peak => peak.Intensity, 0, 0),
                UpperVerticalData.Property,
                UpperVerticalData.Title);
            LowerVerticalData = new AxisData(
                ContinuousAxisManager<double>.Build(LowerSpectrum, peak => peak.Intensity, 0, 0),
                LowerVerticalData.Property,
                LowerVerticalData.Title);
        }

        public AxisData HorizontalData {
            get => horizontalData;
            set => SetProperty(ref horizontalData, value);
        }
        private AxisData horizontalData;

        public AxisData UpperVerticalData {
            get => upperVerticalData;
            set => SetProperty(ref upperVerticalData, value);
        }
        private AxisData upperVerticalData;

        public AxisData LowerVerticalData {
            get => lowerVerticalData;
            set => SetProperty(ref lowerVerticalData, value);
        }
        private AxisData lowerVerticalData;

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle;
    }
}
