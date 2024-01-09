using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class CheckChromatogramsModel : BindableBase
    {
        private readonly AdvancedProcessOptionBaseParameter _advancedProcessParameter;
        private readonly List<PeakFeatureSearchValue> _displaySettingValueCandidates;
        private readonly ObservableCollection<PeakFeatureSearchValueModel> _displaySettingValues;

        public CheckChromatogramsModel(LoadChromatogramsUsecase loadingChromatograms, AdvancedProcessOptionBaseParameter advancedProcessParameter) {
            LoadChromatogramsUsecase = loadingChromatograms ?? throw new ArgumentNullException(nameof(loadingChromatograms));
            _advancedProcessParameter = advancedProcessParameter;
            advancedProcessParameter.DiplayEicSettingValues ??= new List<PeakFeatureSearchValue>();
            var values = advancedProcessParameter.DiplayEicSettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0).ToList();
            values.AddRange(Enumerable.Repeat(0, 100).Select(_ => new PeakFeatureSearchValue()));
            _displaySettingValueCandidates = values;
            _displaySettingValues = new ObservableCollection<PeakFeatureSearchValueModel>(values.Select(v => new PeakFeatureSearchValueModel(v)));
            DisplayEicSettingValues = new ReadOnlyObservableCollection<PeakFeatureSearchValueModel>(_displaySettingValues);
        }

        public ChromatogramsModel Chromatograms {
            get => _chromatograms;
            private set => SetProperty(ref _chromatograms, value);
        }
        private ChromatogramsModel _chromatograms;

        public ReadOnlyObservableCollection<PeakFeatureSearchValueModel> DisplayEicSettingValues { get; }

        public LoadChromatogramsUsecase LoadChromatogramsUsecase { get; }

        public Task ExportAsync(Stream stream, string separator) {
            return Chromatograms.ExportAsync(stream, separator);
        }

        public void Update() {
            foreach (var m in DisplayEicSettingValues) {
                m.Commit();
            }
            _advancedProcessParameter.DiplayEicSettingValues.Clear();
            _advancedProcessParameter.DiplayEicSettingValues.AddRange(_displaySettingValueCandidates.Where(n => n.Mass > 0 && n.MassTolerance > 0));
            var displayEICs = _advancedProcessParameter.DiplayEicSettingValues;

            Chromatograms = LoadChromatogramsUsecase.Load(displayEICs);
        }

        public void Clear() {
            foreach (var m in DisplayEicSettingValues) {
                m.ClearChromatogramSearchQuery();
            }
        }
    }
}
