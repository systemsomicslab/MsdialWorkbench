using CompMs.App.Msdial.Model;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.ViewModel
{
    public class RawDecSpectrumsViewModel : ViewModelBase
    {
        public RawDecSpectrumsViewModel(RawDecSpectrumsModel model) {
            this.model = model;

            RawRefSpectrumViewModels = new MsSpectrumViewModel(model.RawRefSpectrumModels, "Mass", "Intensity");
            DecRefSpectrumViewModels = new MsSpectrumViewModel(model.DecRefSpectrumModels, "Mass", "Intensity");
        }

        private readonly RawDecSpectrumsModel model;

        public MsSpectrumViewModel RawRefSpectrumViewModels {
            get => rawRefSpectrumViewModels;
            set => SetProperty(ref rawRefSpectrumViewModels, value);
        }
        private MsSpectrumViewModel rawRefSpectrumViewModels;

        public MsSpectrumViewModel DecRefSpectrumViewModels {
            get => decRefSpectrumViewModels;
            set => SetProperty(ref decRefSpectrumViewModels, value);
        }
        private MsSpectrumViewModel decRefSpectrumViewModels;
    }
}
