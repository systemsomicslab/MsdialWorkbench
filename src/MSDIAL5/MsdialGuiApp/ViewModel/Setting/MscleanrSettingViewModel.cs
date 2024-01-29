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
            this.BlankRatioMinimum = model.BlankRatioMinimum.ToString();
            this.RSDMaximum = model.RSDMaximum.ToString();
            this.RMDMinimum = model.RMDMinimum.ToString();
            this.RMDMaximum = model.RMDMaximum.ToString();
            MscleanrFilterCommand.RaiseCanExecuteChanged();
            //BlankRatioMinimum = model.BlankRatioMinimum.ToString();
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

        [Required(ErrorMessage = "Minimum blank ratio must be a numerical value between 0 and 1.")]
        [Range(0.0, 1.0, ErrorMessage = "Minimum blank ratio must be a numerical value between 0 and 1.")]
        [RegularExpression(@"\d*.?\d+", ErrorMessage = "Minimum blank ratio must be a numerical value between 0 and 1.")]
        public string BlankRatioMinimum {
            get => _blankRatioMinimum;
            set
            {
                if (SetProperty(ref _blankRatioMinimum, value))
                {
                    if (!ContainsError(nameof(BlankRatioMinimum)))
                    {
                        model.BlankRatioMinimum = double.Parse(value);
                    }
                    MscleanrFilterCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private string _blankRatioMinimum = string.Empty;

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

        [Required(ErrorMessage = "Maximum RSD must be a positive number value.")]
        [Range(0.0, double.MaxValue, ErrorMessage = "Maximum RSD must be a positive number value.")]
        [RegularExpression(@"\d*.?\d+", ErrorMessage = "Maximum RSD must be a positive number value.")]
        public string RSDMaximum {
            get => _RSDMaximum;
            set
            {
                if (SetProperty(ref _RSDMaximum, value))
                {
                    if (!ContainsError(nameof(RSDMaximum)))
                    {
                        model.RSDMaximum = double.Parse(value);
                    }
                    MscleanrFilterCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private string _RSDMaximum = string.Empty;

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

        [Required(ErrorMessage = "Minimum RMD must be a positive number value.")]
        [Range(0.0, double.MaxValue, ErrorMessage = "Minimum RMD must be a positive number value.")]
        [RegularExpression(@"\d*.?\d+", ErrorMessage = "Minimum RMD must be a positive number value.")]
        public string RMDMinimum {
            get => _RMDMinimum;
            set
            {
                if (SetProperty(ref _RMDMinimum, value))
                {
                    if (!ContainsError(nameof(RMDMinimum)))
                    {
                        model.RMDMinimum = double.Parse(value);
                    }
                    MscleanrFilterCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private string _RMDMinimum = string.Empty;

        [Required(ErrorMessage = "Maximum RMD must be a positive number value.")]
        [Range(0.0, double.MaxValue, ErrorMessage = "Maximum RMD must be a positive number value.")]
        [RegularExpression(@"\d*.?\d+", ErrorMessage = "Maximum RMD must be a positive number value.")]
        public string RMDMaximum {
            get => _RMDMaximum;
            set
            {
                if (SetProperty(ref _RMDMaximum, value))
                {
                    if (!ContainsError(nameof(RMDMaximum)))
                    {
                        model.RMDMaximum = double.Parse(value);
                    }
                    MscleanrFilterCommand.RaiseCanExecuteChanged();
                }
            }

        }
        private string _RMDMaximum = string.Empty;

        public DelegateCommand MscleanrFilterCommand => mscleanrFilterCommand ??= new DelegateCommand(model.RunMscleanrFilter, () => !HasValidationErrors);//, Model.CanNormalize));
        private DelegateCommand? mscleanrFilterCommand;

    }
}
