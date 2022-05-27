using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Setting {
    public class MscleanrSettingModel : BindableBase {
        private bool blankRatioChecked;
        private string blankRatioMinimum;
        private bool deleteGhostPeaksChecked;
        private bool incorrectMassChecked;
        private bool rsdChecked;
        private string rsdMaximum;
        private bool rmdChecked;
        private string rmdMinimum;
        private string rmdMaximum;

        public MscleanrSettingModel(ParameterBase param) {

        }

        public DelegateCommand MscleanrFilterCommand => mscleanrFilterCommand ?? (mscleanrFilterCommand = new DelegateCommand(null));//, Model.CanNormalize));
        private DelegateCommand mscleanrFilterCommand;

        public bool BlankRatioChecked {
            get => blankRatioChecked;
            set => SetProperty(ref blankRatioChecked, value);
        }
        public string BlankRatioMinimum {
            get => blankRatioMinimum;
            set => SetProperty(ref blankRatioMinimum, value);
        }
        public bool DeleteGhostPeaksChecked {
            get => deleteGhostPeaksChecked;
            set => SetProperty(ref deleteGhostPeaksChecked, value);
        }
        public bool IncorrectMassChecked {
            get => incorrectMassChecked;
            set => SetProperty(ref incorrectMassChecked, value);
        }
        public bool RSDChecked {
            get => rsdChecked;
            set => SetProperty(ref rsdChecked, value);
        }
        public string RSDMaximum {
            get => rsdMaximum;
            set => SetProperty(ref rsdMaximum, value);
        }
        public bool RMDChecked {
            get => rmdChecked;
            set => SetProperty(ref rmdChecked, value);
        }
        public string RMDMinimum {
            get => rmdMinimum;
            set => SetProperty(ref rmdMinimum, value);
        }
        public string RMDMaximum {
            get => rmdMaximum;
            set => SetProperty(ref rmdMaximum, value);
        }


    }
}
