using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using System;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Setting {
    class MscleanrSettingViewModel : ViewModelBase {
        private readonly MscleanrSettingModel model;

        public MscleanrSettingViewModel(MscleanrSettingModel model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            this.model = model;
            BlankRatioMinimum = model.BlankRatioMinimum.ToString();
        }

        public bool BlankRatioChecked {
            get => _blankRatioChecked;
            set
            {
                if (SetProperty(ref _blankRatioChecked, value)) {
                    model.BlankRatioChecked = value;
                }
            }
        }
        private bool _blankRatioChecked;

        //[Required(ErrorMessage = "")]
        //[Range(0, double.MaxValue, ErrorMessage = "Negative value ")]
        //[RegularExpression(@"\d*.?\d+", ErrorMessage = "")]
        public string BlankRatioMinimum {
            get => _blankRatioMinimum;
            set
            {
                if(SetProperty(ref _blankRatioMinimum, value)) {
                    model.BlankRatioMinimumZZZ = double.Parse(value);
                }
            }
        }
        private string _blankRatioMinimum;

        public bool DeleteGhostPeaksChecked {
            get => _deleteGhostPeaksChecked;
            set
            {
                if (SetProperty(ref _deleteGhostPeaksChecked, value)) {
                    model.DeleteGhostPeaksChecked = value;
                }
            }
        }
        private bool _deleteGhostPeaksChecked;

        public bool IncorrectMassChecked {
            get => _incorrectMassChecked;
            set
            {
                if (SetProperty(ref _incorrectMassChecked, value)) {
                    model.IncorrectMassChecked = value;
                }
            }
        }
        private bool _incorrectMassChecked;

        public bool RSDChecked {
            get => _RSDChecked;
            set
            {
                if (SetProperty(ref _RSDChecked, value)) {
                    model.RSDChecked = value;
                }
            }
        }
        private bool _RSDChecked;

        public string RSDMaximum {
            get => _RSDMaximum;
            set
            {
                if (SetProperty(ref _RSDMaximum, value)) {
                    model.RSDMaximum = double.Parse(value);
                }
            }
        }
        private string _RSDMaximum;

        public bool RMDChecked {
            get => _RMDChecked;
            set
            {
                if (SetProperty(ref _RMDChecked, value)) {
                    model.RMDChecked = value;
                }
            }
        }
        private bool _RMDChecked;

        public string RMDMinimum {
            get => _RMDMinimum;
            set
            {
                if (SetProperty(ref _RMDMinimum, value)) {
                    model.RMDMinimum = double.Parse(value);
                }
            }
        }
        private string _RMDMinimum;

        public string RMDMaximum {
            get => _RMDMaximum;
            set
            {
                if (SetProperty(ref _RMDMaximum, value)) {
                    model.RMDMaximum = double.Parse(value);
                }
            }
        }
        private string _RMDMaximum;

        //public string BlankRatioMinimum {
        //    get => _blankRatioMinimum;
        //    set
        //    {
        //        if (SetProperty(ref _blankRatioMinimum, value)) {
        //            model.BlankRatioMinimumZZZ = double.Parse(value);
        //        }
        //    }
        //}
        //private string _blankRatioMinimum;

        public DelegateCommand MscleanrFilterCommand => mscleanrFilterCommand ?? (mscleanrFilterCommand = new DelegateCommand(model.RunMscleanrFilter));//, Model.CanNormalize));
        private DelegateCommand mscleanrFilterCommand;

    }
}
