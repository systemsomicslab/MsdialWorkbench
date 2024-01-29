using CompMs.App.Msdial.Common;
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

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class LoadChromatogramsUsecase : BindableBase
    {
        private readonly ObservableCollection<ChromatogramPeakFeatureModel> _peaks;
        private readonly PeakPickBaseParameter _peakPickParameter;
        private readonly IWholeChromatogramLoader _ticLoader;
        private readonly IWholeChromatogramLoader _bpcLoader;
        private readonly IWholeChromatogramLoader<(double, double)> _eicLoader;

        public LoadChromatogramsUsecase(IWholeChromatogramLoader ticLoader, IWholeChromatogramLoader bpcLoader, IWholeChromatogramLoader<(double, double)> eicLoader, ObservableCollection<ChromatogramPeakFeatureModel> peaks, PeakPickBaseParameter peakPickParameter) {
            _ticLoader = ticLoader;
            _bpcLoader = bpcLoader;
            _eicLoader = eicLoader;
            _peaks = peaks;
            _peakPickParameter = peakPickParameter;
        }

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

        public ChromatogramsModel Load(List<PeakFeatureSearchValue> displayEICs) {
            var builder = new ChromatogramsBuilder(_ticLoader, _bpcLoader, _eicLoader, _peaks, _peakPickParameter);
            if (InsertTic) {
                builder.AddTic();
            }
            if (InsertBpc) {
                builder.AddBpc();
            }
            if (InsertHighestEic) {
                builder.AddHighestEic();
            }
            if (!displayEICs.IsEmptyOrNull()) {
                builder.AddEics(displayEICs);
            }
            builder.Build();
            return builder.ChromatogramsModel!;
        }

        class ChromatogramsBuilder {
            private readonly List<DisplayChromatogram> _displayChroms;
            private readonly List<string> _contents;
            private readonly IWholeChromatogramLoader _ticLoader;
            private readonly IWholeChromatogramLoader _bpcLoader;
            private readonly IWholeChromatogramLoader<(double, double)> _eicLoader;
            private readonly IReadOnlyList<ChromatogramPeakFeatureModel> _peaks;
            private readonly PeakPickBaseParameter _peakPickParameter;

            public ChromatogramsBuilder(IWholeChromatogramLoader ticLoader, IWholeChromatogramLoader bpcLoader, IWholeChromatogramLoader<(double, double)> eicLoader, IReadOnlyList<ChromatogramPeakFeatureModel> peaks, PeakPickBaseParameter peakPickParameter)
            {
                _displayChroms = new List<DisplayChromatogram>();
                _contents = new List<string>();
                _ticLoader = ticLoader;
                _bpcLoader = bpcLoader;
                _eicLoader = eicLoader;
                _peaks = peaks;
                _peakPickParameter = peakPickParameter;
            }

            public ChromatogramsModel? ChromatogramsModel { get; private set; }

            public void AddTic() {
                var tic = _ticLoader.LoadChromatogram();
                _displayChroms.Add(new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC"));
                _contents.Add("TIC");
            }

            public void AddBpc() {
                var bpc = _bpcLoader.LoadChromatogram();
                _displayChroms.Add(new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC"));
                _contents.Add("BPC");
            }

            public void AddHighestEic() {
                var maxPeakMz = _peaks.DefaultIfEmpty().Argmax(peak => peak?.Intensity ?? -1d)?.Mass;
                if (maxPeakMz is null) {
                    return;
                }
                var eic = _eicLoader.LoadChromatogram((maxPeakMz.Value, _peakPickParameter.MassSliceWidth));
                _displayChroms.Add(new DisplayChromatogram(eic, new Pen(Brushes.Blue, 1.0), "EIC of m/z " + Math.Round(maxPeakMz.Value, 5).ToString()));
                _contents.Add("most abundant ion's EIC");
            }

            public void AddEics(List<PeakFeatureSearchValue> displayEICs) {
                var counter = 0;
                foreach (var set in displayEICs) {
                    var eic = _eicLoader.LoadChromatogram((set.Mass, set.MassTolerance));
                    var title = set.Title;
                    if (!string.IsNullOrEmpty(title)) {
                        title += "; ";
                    }
                    title += $"[{Math.Round(set.Mass - set.MassTolerance, 4)}-{Math.Round(set.Mass + set.MassTolerance, 4)}]";
                    var chrom = new DisplayChromatogram(eic, new Pen(ChartBrushes.GetChartBrush(counter), 1.0), title);
                    counter++;
                    _displayChroms.Add(chrom);
                }
                if (counter == 1) {
                    _contents.Add("selected EIC");
                }
                else if (counter > 1) {
                    _contents.Add("selected EICs");
                }
            }

            private string BuildTitle() {
                string title = string.Empty;
                if (_contents.Count == 1) {
                    title = _contents.First();
                }
                else if (_contents.Count > 1) {
                    title = $"{string.Join(", ", _contents.Take(_contents.Count - 1))} and {_contents.Last()}";
                }
                return title;
            }

            public void Build() {
                string title = BuildTitle();
                ChromatogramsModel = new ChromatogramsModel(title, _displayChroms, title, "Retention time [min]", "Absolute ion abundance");
            }
        }
    }
}
