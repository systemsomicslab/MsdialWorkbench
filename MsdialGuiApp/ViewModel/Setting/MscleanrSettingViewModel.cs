using CompMs.App.Msdial.Model.Normalize;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting {
    class MscleanrSettingViewModel : ViewModelBase {
        public MscleanrSettingViewModel(ParameterBase parameter) {
            
        }

        public DelegateCommand MscleanrFilterCommand => mscleanrFilterCommand ?? (mscleanrFilterCommand = new DelegateCommand(null));//, Model.CanNormalize));
        private DelegateCommand mscleanrFilterCommand;

    }
}
