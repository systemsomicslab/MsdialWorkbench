using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Setting
{
    internal sealed class DisplayEicSettingModel : BindableBase {
        private readonly EicLoader _loader;
        private readonly AdvancedProcessOptionBaseParameter _advanceProcessParameter;
        private readonly List<PeakFeatureSearchValue> _displaySettingValueCandidates;
        private readonly ObservableCollection<PeakFeatureSearchValueModel> _displaySettingValueModels;

        public DisplayEicSettingModel(EicLoader loader, AdvancedProcessOptionBaseParameter advanceProcessParameter) {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _advanceProcessParameter = advanceProcessParameter ?? throw new ArgumentNullException(nameof(advanceProcessParameter));

            advanceProcessParameter.DiplayEicSettingValues ??= new List<PeakFeatureSearchValue>();
            var values = advanceProcessParameter.DiplayEicSettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0).ToList();
            values.AddRange(Enumerable.Repeat(0, 100).Select(_ => new PeakFeatureSearchValue()));
            _displaySettingValueCandidates = values;
            _displaySettingValueModels = new ObservableCollection<PeakFeatureSearchValueModel>(values.Select(v => new PeakFeatureSearchValueModel(v)));
            DisplayEicSettingValueModels = new ReadOnlyObservableCollection<PeakFeatureSearchValueModel>(_displaySettingValueModels);
        }

        public ReadOnlyObservableCollection<PeakFeatureSearchValueModel> DisplayEicSettingValueModels { get; }

        public ChromatogramsModel PrepareChromatograms() {
            _advanceProcessParameter.DiplayEicSettingValues.Clear();
            _advanceProcessParameter.DiplayEicSettingValues.AddRange(_displaySettingValueCandidates.Where(n => n.Mass > 0 && n.MassTolerance > 0));
            var displayEICs = _advanceProcessParameter.DiplayEicSettingValues;

            if (!displayEICs.IsEmptyOrNull()) {
                var displayChroms = new List<DisplayChromatogram>();
                var counter = 0;
                foreach (var set in displayEICs.Where(v => v.Mass > 0 && v.MassTolerance > 0)) {
                    var eic = _loader.LoadEicTrace(set.Mass, set.MassTolerance);
                    var subtitle = $"[{Math.Round(set.Mass - set.MassTolerance, 4)}-{Math.Round(set.Mass + set.MassTolerance, 4)}]";
                    var chrom = new DisplayChromatogram(eic, new Pen(ChartBrushes.GetChartBrush(counter), 1.0), set.Title + "; " + subtitle);
                    counter++;
                    displayChroms.Add(chrom);
                }

                ChromatogramsModel chromatogramsModel= new ChromatogramsModel("EIC", displayChroms, "EIC", "Retention time [min]", "Absolute ion abundance");
                return chromatogramsModel;
            }
            return null;
        }
    }
}
