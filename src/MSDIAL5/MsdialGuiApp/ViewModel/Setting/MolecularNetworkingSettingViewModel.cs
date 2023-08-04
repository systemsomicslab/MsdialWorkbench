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
        public string MassTolerance
        {
            get => _massTolerance;
            set
            {
                if (SetProperty(ref _massTolerance, value))
                {
                    if (!ContainsError(nameof(MassTolerance)))
                    {
                        model.MassTolerance = double.Parse(value);
                    }                    
                }
            }
        }
        private string _massTolerance;

        public string RelativeAbundanceCutoff {
            get => _relativeAbundanceCutoff;
            set {
                if (SetProperty(ref _relativeAbundanceCutoff, value)) {
                    if (!ContainsError(nameof(RelativeAbundanceCutoff))) {
                        model.RelativeAbundanceCutoff = double.Parse(value);
                    }
                }
            }
        }
        private string _relativeAbundanceCutoff;

        public string SimilarityCutoff {
            get => _similarityCutoff;
            set {
                if (SetProperty(ref _similarityCutoff, value)) {
                    if (!ContainsError(nameof(SimilarityCutoff))) {
                        model.SimilarityCutoff = double.Parse(value);
                    }
                }
            }
        }
        private string _similarityCutoff;

        public DelegateCommand MolecularNetworkingCommand => molecularNetworkingCommand ?? (molecularNetworkingCommand = new DelegateCommand(model.RunMolecularNetworking, () => !HasValidationErrors));//, Model.CanNormalize));
        private DelegateCommand molecularNetworkingCommand;

    }
}
