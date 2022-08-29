using System;
using CompMs.CommonMVVM;
using CompMs.App.Msdial.Model.Setting;

namespace CompMs.App.Msdial.ViewModel.Setting {
    class PcaSettingViewModel : ViewModelBase {
        private readonly PcaSettingModel model;

        public PcaSettingViewModel(PcaSettingModel model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            this.model = model;
            this.MaxPcNumber = model.MaxPcNumber.ToString();
        }
        public string MaxPcNumber
        {
            get => _maxPcNumber;
            set
            {
                if (SetProperty(ref _maxPcNumber, value))
                {
                    if (!ContainsError(nameof(MaxPcNumber)))
                    {
                        model.MaxPcNumber = int.Parse(value);
                    }
                }
            }
        }
        private string _maxPcNumber;

        public bool IsIdentifiedImportedInStatistics {
            get => _isIdentifiedImportedInStatistics;
            set
            {
                if (SetProperty(ref _isIdentifiedImportedInStatistics, value)) {
                    model.IsIdentifiedImportedInStatistics = value;
                }
            }
        }
        private bool _isIdentifiedImportedInStatistics;

        public bool IsAnnotatedImportedInStatistics {
            get => _isAnnotatedImportedInStatistics;
            set
            {
                if (SetProperty(ref _isAnnotatedImportedInStatistics, value)) {
                    model.IsAnnotatedImportedInStatistics = value;
                }
            }
        }
        private bool _isAnnotatedImportedInStatistics;

        public bool IsUnknownImportedInStatistics
        {
            get => _isUnknownImportedInStatistics;
            set
            {
                if (SetProperty(ref _isUnknownImportedInStatistics, value))
                {
                    model.IsUnknownImportedInStatistics = value;
                }
            }
        }
        private bool _isUnknownImportedInStatistics;

        public DelegateCommand PcaCommand => pcaCommand ?? (pcaCommand = new DelegateCommand(model.RunPca, () => !HasValidationErrors));//, Model.CanNormalize));
        private DelegateCommand pcaCommand;
    }
}
