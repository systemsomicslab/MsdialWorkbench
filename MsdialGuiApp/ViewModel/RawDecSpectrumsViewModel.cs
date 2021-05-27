using CompMs.App.Msdial.Model;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.ViewModel
{
    public class RawDecSpectrumsViewModel : ViewModelBase
    {
        public RawDecSpectrumsViewModel(RawDecSpectrumsModel model) {
            this.model = model;

            RawRefSpectrumViewModels = new MsSpectrumViewModel<ChromatogramPeakFeatureModel>(model.RawRefSpectrumModels, "Mass", "Intensity");
            DecRefSpectrumViewModels = new MsSpectrumViewModel<ChromatogramPeakFeatureModel>(model.DecRefSpectrumModels, "Mass", "Intensity");
        }

        private readonly RawDecSpectrumsModel model;

        public MsSpectrumViewModel<ChromatogramPeakFeatureModel> RawRefSpectrumViewModels {
            get => rawRefSpectrumViewModels;
            set => SetProperty(ref rawRefSpectrumViewModels, value);
        }
        private MsSpectrumViewModel<ChromatogramPeakFeatureModel> rawRefSpectrumViewModels;

        public MsSpectrumViewModel<ChromatogramPeakFeatureModel> DecRefSpectrumViewModels {
            get => decRefSpectrumViewModels;
            set => SetProperty(ref decRefSpectrumViewModels, value);
        }
        private MsSpectrumViewModel<ChromatogramPeakFeatureModel> decRefSpectrumViewModels;
    }
}
