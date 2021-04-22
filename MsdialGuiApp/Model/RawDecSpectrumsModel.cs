using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model
{
    public class RawDecSpectrumsModel : ValidatableBase
    {
        public RawDecSpectrumsModel(
            AxisData horizontalData,
            AxisData rawVerticalData,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> rawLoader,
            AxisData decVerticalData,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> decLoader,
            AxisData refVerticalData,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> refLoader,
            string graphTitle) {

            HorizontalData = horizontalData;
            this.rawVerticalData = rawVerticalData;
            this.rawLoader = rawLoader;
            this.decVerticalData = decVerticalData;
            this.decLoader = decLoader;
            this.refVerticalData = refVerticalData;
            this.refLoader = refLoader;
            GraphTitle = graphTitle;

            RawRefSpectrumModels = new MsSpectrumModel<ChromatogramPeakFeatureModel>(
                horizontalData,
                rawVerticalData, rawLoader,
                refVerticalData, refLoader,
                graphTitle);
            DecRefSpectrumModels = new MsSpectrumModel<ChromatogramPeakFeatureModel>(
                HorizontalData,
                decVerticalData, decLoader,
                refVerticalData, refLoader,
                graphTitle);
        }

        public async Task LoadSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            await Task.WhenAll(
                RawRefSpectrumModels.LoadSpectrumAsync(target, token),
                DecRefSpectrumModels.LoadSpectrumAsync(target, token)
            );
        }

        private readonly IMsSpectrumLoader<ChromatogramPeakFeatureModel> rawLoader, decLoader, refLoader;

        public MsSpectrumModel<ChromatogramPeakFeatureModel> RawRefSpectrumModels {
            get => rawRefSpectrumModels;
            set => SetProperty(ref rawRefSpectrumModels, value);
        }
        private MsSpectrumModel<ChromatogramPeakFeatureModel> rawRefSpectrumModels;

        public MsSpectrumModel<ChromatogramPeakFeatureModel> DecRefSpectrumModels {
            get => decRefSpectrumModels;
            set => SetProperty(ref decRefSpectrumModels, value);
        }
        private MsSpectrumModel<ChromatogramPeakFeatureModel> decRefSpectrumModels;

        public AxisData HorizontalData {
            get => horizontalData;
            set => SetProperty(ref horizontalData, value);
        }
        private AxisData horizontalData;

        public AxisData RawVerticalData {
            get => rawVerticalData;
            set => SetProperty(ref rawVerticalData, value);
        }
        private AxisData rawVerticalData;

        public AxisData DecVerticalData {
            get => decVerticalData;
            set => SetProperty(ref decVerticalData, value);
        }
        private AxisData decVerticalData;

        public AxisData RefVerticalData {
            get => refVerticalData;
            set => SetProperty(ref refVerticalData, value);
        }
        private AxisData refVerticalData;

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle;
    }
}
