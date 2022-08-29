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
        public DelegateCommand PcaCommand => pcaCommand ?? (pcaCommand = new DelegateCommand(model.RunPca, () => !HasValidationErrors));//, Model.CanNormalize));
        private DelegateCommand pcaCommand;
    }
}
