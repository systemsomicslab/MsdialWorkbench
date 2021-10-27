using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Setting;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    public class LcmsIdentifySettingViewModel : IdentifySettingViewModel
    {
        public LcmsIdentifySettingViewModel(LcmsIdentifySettingModel model) : base(model, new LcmsAnnotatorSettingViewModelFactory()) {

        }
    }
}
