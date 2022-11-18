using CompMs.App.Msdial.Model.Core;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.Model.Setting {
    internal sealed class FragmentQuerySettingModel : BindableBase {
        private readonly AdvancedProcessOptionBaseParameter _parameter;
        private readonly IAnalysisModel _analysisModel;
        private readonly IAlignmentModel _alignmentModel;

        public FragmentQuerySettingModel(AdvancedProcessOptionBaseParameter parameter, IAnalysisModel analysisModel, IAlignmentModel alignmentModel) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _analysisModel = analysisModel;
            _alignmentModel = alignmentModel;

            if (parameter.FragmentSearchSettingValues is null) {
                parameter.FragmentSearchSettingValues = new List<PeakFeatureSearchValue>();
            }
            var values = parameter.FragmentSearchSettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0 && n.RelativeIntensityCutoff > 0).ToList();
            SearchOption = new ReactivePropertySlim<AndOr>(parameter.AndOrAtFragmentSearch);

            IsAlignSpotViewSelected = new ReactivePropertySlim<bool>();

            values.AddRange(Enumerable.Range(0, 100).Select(_ => new PeakFeatureSearchValue
            {
                PeakFeatureQueryLevel = PeakFeatureQueryLevel.MS2,
            }));
            FragmentQuerySettingValues = new ObservableCollection<PeakFeatureSearchValue>(values);
        }

        public ObservableCollection<PeakFeatureSearchValue> FragmentQuerySettingValues { get; }
        public ReactivePropertySlim<AndOr> SearchOption { get; }
        public ReactivePropertySlim<bool> IsAlignSpotViewSelected { get; }

        public void ClearListMethod() {
            FragmentQuerySettingValues.Clear();
        }

        public void Search() {
            _parameter.FragmentSearchSettingValues = FragmentQuerySettingValues.Where(v => v.Mass > 0 && v.MassTolerance > 0 && v.RelativeIntensityCutoff > 0).ToList();
            _parameter.AndOrAtFragmentSearch = SearchOption.Value;
            if (IsAlignSpotViewSelected.Value) {
                if (_alignmentModel is null) {
                    MessageBox.Show("Please select an alignment result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _alignmentModel.SearchFragment();
            } else {
                if (_analysisModel is null) {
                    MessageBox.Show("Please select an analysis file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _analysisModel.SearchFragment();
            }
        }
    }
}
