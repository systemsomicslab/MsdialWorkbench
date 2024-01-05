using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class CheckChromatogramsModel : BindableBase
    {
        private readonly EicLoader _loader;
        private readonly ObservableCollection<ChromatogramPeakFeatureModel> _peaks;
        private readonly PeakPickBaseParameter _peakPickParameter;
        private readonly AdvancedProcessOptionBaseParameter _advancedProcessParameter;
        private readonly List<PeakFeatureSearchValue> _displaySettingValueCandidates;
        private readonly ObservableCollection<PeakFeatureSearchValueModel> _displaySettingValues;

        public CheckChromatogramsModel(EicLoader loader, ObservableCollection<ChromatogramPeakFeatureModel> peaks, PeakPickBaseParameter peakPickParameter, AdvancedProcessOptionBaseParameter advancedProcessParameter) {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _peaks = peaks;
            _peakPickParameter = peakPickParameter;
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

        public bool InsertTic {
            get => _insertTic;
            set => SetProperty(ref _insertTic, value);
        }
        private bool _insertTic;
        public bool InsertBpc {
            get => _insertBpc;
            set => SetProperty(ref _insertBpc, value);
        }
        private bool _insertBpc;

        public bool InsertHighestEic {
            get => _insertHighestEic;
            set => SetProperty(ref _insertHighestEic, value);
        }
        private bool _insertHighestEic;

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
            var displayChroms = new List<DisplayChromatogram>();
            var contents = new List<string>();
            if (InsertTic) {
                var tic = _loader.LoadTic();
                displayChroms.Add(new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC"));
                contents.Add("TIC");
            }
            if (InsertBpc) {
                var bpc = _loader.LoadBpc();
                displayChroms.Add(new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC"));
                contents.Add("BPC");
            }
            if (InsertHighestEic) {
                var maxPeakMz = _peaks.Argmax(n => n.Intensity).Mass;
                var eic = _loader.LoadEicTrace(maxPeakMz, _peakPickParameter.MassSliceWidth);
                displayChroms.Add(new DisplayChromatogram(eic, new Pen(Brushes.Blue, 1.0), "EIC of m/z " + Math.Round(maxPeakMz, 5).ToString()));
                contents.Add("highest peak m/z's EIC");
            }
            if (!displayEICs.IsEmptyOrNull()) {
                var counter = 0;
                foreach (var set in displayEICs.Where(v => v.Mass > 0 && v.MassTolerance > 0)) {
                    var eic = _loader.LoadEicTrace(set.Mass, set.MassTolerance);
                    var subtitle = $"[{Math.Round(set.Mass - set.MassTolerance, 4)}-{Math.Round(set.Mass + set.MassTolerance, 4)}]";
                    var chrom = new DisplayChromatogram(eic, new Pen(ChartBrushes.GetChartBrush(counter), 1.0), set.Title + "; " + subtitle);
                    counter++;
                    displayChroms.Add(chrom);
                }
                if (counter == 1) {
                    contents.Add("selected EIC");
                }
                else if (counter > 1) {
                    contents.Add("selected EICs");
                }
            }
            string title = string.Empty;
            if (contents.Count == 1) {
                title = contents.First();
            }
            else if (contents.Count > 1) {
                title = $"{string.Join(", ", contents.Take(contents.Count - 1))} and {contents.Last()}";
            }
            Chromatograms = new ChromatogramsModel(title, displayChroms, title, "Retention time [min]", "Absolute ion abundance");
        }

        public void Clear() {
            foreach (var m in DisplayEicSettingValues) {
                m.ClearChromatogramSearchQuery();
            }
        }
    }
}
