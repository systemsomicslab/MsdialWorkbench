using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting {
    public class DisplayEicSettingModel : BindableBase {
        public ObservableCollection<PeakFeatureSearchValue> DiplayEicSettingValues { get; }
        public DisplayEicSettingModel(ParameterBase param) {
            if (param.DiplayEicSettingValues is null) {
                param.DiplayEicSettingValues = new List<PeakFeatureSearchValue>();
            }

            var values = param.DiplayEicSettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0).ToList();
            values.AddRange(Enumerable.Range(0, 100).Select(_ => new PeakFeatureSearchValue()));
            DiplayEicSettingValues = new ObservableCollection<PeakFeatureSearchValue>(values);
        }
    }
}
