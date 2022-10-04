using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting {
    public class FragmentQuerySettingModel : BindableBase {
        public ObservableCollection<PeakFeatureSearchValue> FragmentQuerySettingValues { get; }
        public ReactivePropertySlim<AndOr> SearchOption { get; }
        public ReactivePropertySlim<bool> IsAlignSpotViewSelected { get; }

        public FragmentQuerySettingModel(ParameterBase param, bool isAlignSpotViewSelected) {
            if (param.FragmentSearchSettingValues is null) {
                param.FragmentSearchSettingValues = new List<PeakFeatureSearchValue>();
            }

            var values = param.FragmentSearchSettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0 && n.RelativeIntensityCutoff > 0).ToList();
            SearchOption = new ReactivePropertySlim<AndOr>(param.AndOrAtFragmentSearch);

            IsAlignSpotViewSelected = new ReactivePropertySlim<bool>(isAlignSpotViewSelected);

            values.AddRange(Enumerable.Range(0, 100).Select(_ => new PeakFeatureSearchValue()
            {
                PeakFeatureQueryLevel = PeakFeatureQueryLevel.MS2,
            }));
            FragmentQuerySettingValues = new ObservableCollection<PeakFeatureSearchValue>(values);
        }

        public void ClearListMethod() {
            FragmentQuerySettingValues.Clear();
        }
    }
}
