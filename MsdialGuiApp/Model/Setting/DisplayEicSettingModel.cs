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
    public class DisplayEicSettingModel : BindableBase {
        private readonly EicLoader _loader;
        private readonly ParameterBase _parameter;

        public DisplayEicSettingModel(EicLoader loader, ParameterBase parameter) {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));

            if (parameter.DiplayEicSettingValues is null) {
                parameter.DiplayEicSettingValues = new List<PeakFeatureSearchValue>();
            }

            var values = parameter.DiplayEicSettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0).ToList();
            values.AddRange(Enumerable.Range(0, 100).Select(_ => new PeakFeatureSearchValue()));
            DiplayEicSettingValues = new ObservableCollection<PeakFeatureSearchValue>(values);
        }

        public ObservableCollection<PeakFeatureSearchValue> DiplayEicSettingValues { get; }

        public ChromatogramsModel PrepareChromatograms() {
            _parameter.DiplayEicSettingValues = DiplayEicSettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0).ToList();
            var displayEICs = _parameter.DiplayEicSettingValues;
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
