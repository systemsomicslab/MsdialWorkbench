using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.ViewModel.Setting;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    public class DimsIdentifySettingViewModel : IdentifySettingViewModel
    {
        public DimsIdentifySettingViewModel(DimsIdentifySettingModel model) : base(model, new DimsAnnotatorSettingViewModelFactory()) {

        }
    }
}
