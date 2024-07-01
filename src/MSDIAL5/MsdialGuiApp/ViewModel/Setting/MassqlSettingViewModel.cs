using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using System;

namespace CompMs.App.Msdial.ViewModel.Setting {
    class MassqlSettingViewModel : ViewModelBase {
        private readonly MassqlSettingModel model;

        public MassqlSettingViewModel(MassqlSettingModel model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            this.model = model;
        }

        public string Massql {
            get => model.Massql;
            set {
                if (model.Massql != value)
                {
                    model.Massql = value;
                    OnPropertyChanged(nameof(Massql));
                }
            }
        }

        public DelegateCommand MassqlSearchCommand => massqlSearchCommand ??= new DelegateCommand(model.SendMassql);//, Model.CanNormalize));
        private DelegateCommand? massqlSearchCommand;

    }
}
