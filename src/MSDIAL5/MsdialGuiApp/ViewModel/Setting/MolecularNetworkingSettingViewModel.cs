using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Setting {
    class MolecularNetworkingSettingViewModel : ViewModelBase {
        private readonly MolecularNetworkingSettingModel model;

        public MolecularNetworkingSettingViewModel(MolecularNetworkingSettingModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            this.model = model;
        }
    }
}
