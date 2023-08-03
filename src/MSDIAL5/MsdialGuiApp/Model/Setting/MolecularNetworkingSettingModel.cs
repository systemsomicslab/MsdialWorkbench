using CompMs.CommonMVVM;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting {
    public class MolecularNetworkingSettingModel : BindableBase {

        private readonly MsdialLcmsParameter _parameter;
        
        public MolecularNetworkingSettingModel(MsdialLcmsParameter parameter) {
            _parameter = parameter;
        }
        public double MassTolerance
        {
            get => massTolerance;
            set => SetProperty(ref massTolerance, value);
        }
        private double massTolerance;

        public void RunMolecularNetworking() {
            Console.WriteLine(MassTolerance);
        }
    }
}
