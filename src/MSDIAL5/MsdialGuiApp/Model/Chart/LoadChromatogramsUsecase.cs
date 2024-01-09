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
        private readonly EicLoader _loader;
        private readonly ObservableCollection<ChromatogramPeakFeatureModel> _peaks;
        private readonly PeakPickBaseParameter _peakPickParameter;

        public LoadChromatogramsUsecase(EicLoader loader, ObservableCollection<ChromatogramPeakFeatureModel> peaks, PeakPickBaseParameter peakPickParameter) {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
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
            var builder = new ChromatogramsBuilder(_loader, _peaks, _peakPickParameter);
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
            return builder.Build();
        }

        class ChromatogramsBuilder {
            private readonly EicLoader _loader;
            private readonly IReadOnlyList<ChromatogramPeakFeatureModel> _peaks;
            private readonly PeakPickBaseParameter _peakPickParameter;
            private readonly List<DisplayChromatogram> _displayChroms;
            private readonly List<string> _contents;

            public ChromatogramsBuilder(EicLoader loader, IReadOnlyList<ChromatogramPeakFeatureModel> peaks, PeakPickBaseParameter peakPickParameter)
            {
                _displayChroms = new List<DisplayChromatogram>();
                _contents = new List<string>();
                _loader = loader;
                _peaks = peaks;
                _peakPickParameter = peakPickParameter;
            }

            public void AddTic() {
                var tic = _loader.LoadTic();
                _displayChroms.Add(new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC"));
                _contents.Add("TIC");
            }

            public void AddBpc() {
                var bpc = _loader.LoadBpc();
                _displayChroms.Add(new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC"));
                _contents.Add("BPC");
            }

            public void AddHighestEic() {
                var maxPeakMz = _peaks.Argmax(n => n.Intensity).Mass;
                var eic = _loader.LoadEicTrace(maxPeakMz, _peakPickParameter.MassSliceWidth);
                _displayChroms.Add(new DisplayChromatogram(eic, new Pen(Brushes.Blue, 1.0), "EIC of m/z " + Math.Round(maxPeakMz, 5).ToString()));
                _contents.Add("most abundant ion's EIC");
            }

            public void AddEics(List<PeakFeatureSearchValue> displayEICs) {
                var counter = 0;
                foreach (var set in displayEICs) {
                    var eic = _loader.LoadEicTrace(set.Mass, set.MassTolerance);
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

            public ChromatogramsModel Build() {
                string title = BuildTitle();
                return new ChromatogramsModel(title, _displayChroms, title, "Retention time [min]", "Absolute ion abundance");
            }
        }
    }
}
