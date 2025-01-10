using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class LoadChromatogramsUsecase : BindableBase
    {
        private readonly ObservableCollection<ChromatogramPeakFeatureModel> _peaks;
        private readonly RawSpectra _rawSpectra;
        private readonly PeakPickBaseParameter _peakPickParameter;
        private readonly IWholeChromatogramLoader _ticLoader;
        private readonly IWholeChromatogramLoader _bpcLoader;
        private readonly IWholeChromatogramLoader _productTicLoader;
        private readonly IWholeChromatogramLoader<int> _productExperimentTicLoader;
        private readonly IWholeChromatogramLoader<(int, MzRange)> _productExperimentEicLoader;
        private readonly IWholeChromatogramLoader<(double, double)> _eicLoader;
        private readonly IWholeChromatogramLoader<(MzRange, MzRange)> _productEicLoader;

        public LoadChromatogramsUsecase(RawSpectra rawSpectra, ChromatogramRange chromatogramRange, PeakPickBaseParameter parameter, IonMode ionMode, ObservableCollection<ChromatogramPeakFeatureModel> peaks)
        {
            _ticLoader = new TicLoader(rawSpectra, chromatogramRange, parameter);
            _bpcLoader = new BpcLoader(rawSpectra, chromatogramRange, parameter);
            _eicLoader = new EicLoader(rawSpectra, parameter, ionMode, chromatogramRange);
            _productTicLoader = new MS2TicLoader(rawSpectra, chromatogramRange, parameter);
            _productEicLoader = new ProductIonChromatogramLoader(rawSpectra, ionMode, chromatogramRange);
            _productExperimentTicLoader = new MS2TicLoader(rawSpectra, chromatogramRange, parameter);
            _productExperimentEicLoader = new ProductIonChromatogramLoader(rawSpectra, ionMode, chromatogramRange);
            _peaks = peaks;
            _rawSpectra = rawSpectra;
            _peakPickParameter = parameter;
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

        public bool InsertMS2Tic {
            get => _insertMS2Tic;
            set => SetProperty(ref _insertMS2Tic, value);
        }
        private bool _insertMS2Tic;

        public async Task<ChromatogramsModel> LoadMS2TicAsync(CancellationToken token) {
            var displayChromatogram = await _productTicLoader.LoadChromatogramAsync(token).ConfigureAwait(false);
            displayChromatogram.Name = $"Total ion chromatogram, MS2";
            return new ChromatogramsModel(string.Empty, displayChromatogram, displayChromatogram.Name, "Time", "Abundance");
        }

        public async Task<ChromatogramsModel> LoadMS2TicAsync(int experimentID, CancellationToken token) {
            var displayChromatogram = await _productExperimentTicLoader.LoadChromatogramAsync(experimentID, token).ConfigureAwait(false);
            displayChromatogram.Name = $"Total ion chromatogram, ExperimentID: {experimentID}";
            return new ChromatogramsModel(string.Empty, displayChromatogram, displayChromatogram.Name, "Time", "Abundance");
        }

        public async Task<ChromatogramsModel> LoadMS2EicAsync(MzRange product, CancellationToken token) {
            var range = MzRange.FromRange(0d, 1e10);
            var displayChromatogram = await _productEicLoader.LoadChromatogramAsync((range, product), token).ConfigureAwait(false);
            displayChromatogram.Name = $"Fragment ion: {product.Mz:F5}±{Math.Round(product.Tolerance, 5)}";
            return new ChromatogramsModel(string.Empty, displayChromatogram, displayChromatogram.Name, "Time", "Abundance");
        }

        public async Task<ChromatogramsModel> LoadMS2EicAsync(int experimentID, MzRange product, CancellationToken token) {
            var displayChromatogram = await _productExperimentEicLoader.LoadChromatogramAsync((experimentID, product), token).ConfigureAwait(false);
            displayChromatogram.Name = $"ExperimentID: {experimentID}, fragment ion: {product.Mz:F5}±{Math.Round(product.Tolerance, 5)}";
            return new ChromatogramsModel(string.Empty, displayChromatogram, displayChromatogram.Name, "Time", "Abundance");
        }

        public async Task<ChromatogramsModel> LoadMS2EicAsync(MzRange precursor, MzRange product, CancellationToken token) {
            var displayChromatogram = await _productEicLoader.LoadChromatogramAsync((precursor, product), token).ConfigureAwait(false);
            displayChromatogram.Name = $"Precursor m/z: {precursor.Mz:F5}±{Math.Round(precursor.Tolerance, 5)}";
            if (product.Tolerance < 10000) {
                displayChromatogram.Name += $" fragment ion: {product.Mz:F5}±{Math.Round(product.Tolerance, 5)}";
            }
            return new ChromatogramsModel(string.Empty, displayChromatogram, displayChromatogram.Name, "Time", "Abundance");
        }

        public async Task<ChromatogramsModel> LoadTicAsync(CancellationToken token) {
            var displayChromatogram = await _ticLoader.LoadChromatogramAsync(token).ConfigureAwait(false);
            displayChromatogram.Name = "Total ion chromatoram";
            return new ChromatogramsModel(string.Empty, displayChromatogram, displayChromatogram.Name, "Time", "Abundance");
        }

        public async Task<ChromatogramsModel> LoadEicAsync(MzRange mzRange, CancellationToken token) {
            var displayChromatogram = await _eicLoader.LoadChromatogramAsync((mzRange.Mz, mzRange.Tolerance), token).ConfigureAwait(false);
            displayChromatogram.Name = $"m/z: {mzRange.Mz:F5}±{Math.Round(mzRange.Tolerance, 5)}";
            return new ChromatogramsModel(string.Empty, displayChromatogram, displayChromatogram.Name, "Time", "Abundance");
        }

        public async Task<ChromatogramsModel> LoadAsync(List<PeakFeatureSearchValue> displayEICs, CancellationToken token) {
            var builder = new ChromatogramsBuilder(_ticLoader, _bpcLoader, _eicLoader, _peaks, _peakPickParameter);
            var tasks = new List<Task>();
            if (InsertTic) {
                tasks.Add(builder.AddTicAsync(token));
            }
            if (InsertBpc) {
                tasks.Add(builder.AddBpcAsync(token));
            }
            if (InsertHighestEic) {
                tasks.Add(builder.AddHighestEicAsync(token));
            }
            if (InsertMS2Tic) {
                tasks.Add(builder.AddMS2TicAsync(_productTicLoader, token));
                var counter = 0;
                foreach (var (level, ID) in _rawSpectra.ExperimentIDs) {
                    if (level == 2) {
                        tasks.Add(builder.AddMS2TicAsync(_productExperimentTicLoader, ID, ChartBrushes.GetChartBrush(counter++), token));
                    }
                }
            }
            if (!displayEICs.IsEmptyOrNull()) {
                tasks.Add(builder.AddEicsAsync(displayEICs, token));
            }
            await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
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
            private readonly object _lock = new();

            public ChromatogramsBuilder(IWholeChromatogramLoader ticLoader, IWholeChromatogramLoader bpcLoader, IWholeChromatogramLoader<(double, double)> eicLoader, IReadOnlyList<ChromatogramPeakFeatureModel> peaks, PeakPickBaseParameter peakPickParameter)
            {
                _displayChroms = [];
                _contents = [];
                _ticLoader = ticLoader;
                _bpcLoader = bpcLoader;
                _eicLoader = eicLoader;
                _peaks = peaks;
                _peakPickParameter = peakPickParameter;
            }

            public ChromatogramsModel? ChromatogramsModel { get; private set; }

            public async Task AddTicAsync(CancellationToken token) {
                var tic = await _ticLoader.LoadChromatogramAsync(token).ConfigureAwait(false);
                var pen = tic.LinePen = new Pen(Brushes.Black, 1.0);
                pen.Freeze();
                tic.Name = "TIC";
                lock (_lock) {
                    _displayChroms.Add(tic);
                    _contents.Add("TIC");
                }
            }

            public async Task AddBpcAsync(CancellationToken token) {
                var bpc = await _bpcLoader.LoadChromatogramAsync(token).ConfigureAwait(false);
                var pen = bpc.LinePen = new Pen(Brushes.Red, 1.0);
                pen.Freeze();
                bpc.Name = "BPC";
                lock (_lock) {
                    _displayChroms.Add(bpc);
                    _contents.Add("BPC");
                }
            }

            public async Task AddHighestEicAsync(CancellationToken token) {
                var maxPeakMz = _peaks.DefaultIfEmpty().Argmax(peak => peak?.Intensity ?? -1d)?.Mass;
                if (maxPeakMz is null) {
                    return;
                }
                var eic = await _eicLoader.LoadChromatogramAsync((maxPeakMz.Value, _peakPickParameter.CentroidMs1Tolerance), token).ConfigureAwait(false);
                var pen = eic.LinePen = new Pen(Brushes.Blue, 1.0);
                pen.Freeze();
                eic.Name = "EIC of m/z " + Math.Round(maxPeakMz.Value, 5).ToString();
                lock (_lock) {
                    _displayChroms.Add(eic);
                    _contents.Add("most abundant ion's EIC");
                }
            }

            public async Task AddMS2TicAsync(IWholeChromatogramLoader productTicLoader, CancellationToken token) {
                var tic = await productTicLoader.LoadChromatogramAsync(token).ConfigureAwait(false);
                var pen = tic.LinePen = new Pen(Brushes.DarkGray, 1.0);
                pen.Freeze();
                tic.Name = "MS2 TIC";
                lock (_lock) {
                    _displayChroms.Add(tic);
                    _contents.Add("MS2 TIC");
                }
            }

            public async Task AddMS2TicAsync(IWholeChromatogramLoader<int> productTicLoader, int experimentID, Brush? brush = null, CancellationToken token = default) {
                var tic = await productTicLoader.LoadChromatogramAsync(experimentID, token).ConfigureAwait(false);
                var pen = tic.LinePen = new Pen(brush ?? Brushes.Cyan, 1.0);
                pen.Freeze();
                tic.Name = $"MS2 TIC of experiment {experimentID}";
                lock (_lock) {
                    _displayChroms.Add(tic);
                    _contents.Add($"MS2 TIC of experiment {experimentID}");
                }
            }

            public async Task AddEicsAsync(List<PeakFeatureSearchValue> displayEICs, CancellationToken token) {
                var counter = 0;
                foreach (var set in displayEICs) {
                    var eic = await _eicLoader.LoadChromatogramAsync((set.Mass, set.MassTolerance), token).ConfigureAwait(false);
                    var title = set.Title;
                    if (!string.IsNullOrEmpty(title)) {
                        title += "; ";
                    }
                    title += $"[{Math.Round(set.Mass - set.MassTolerance, 4)}-{Math.Round(set.Mass + set.MassTolerance, 4)}]";
                    eic.Name = title;
                    var pen = eic.LinePen = new Pen(ChartBrushes.GetChartBrush(counter), 1.0);
                    pen.Freeze();
                    counter++;
                    lock (_lock) {
                        _displayChroms.Add(eic);
                    }
                }
                lock (_lock) {
                    if (counter == 1) {
                        _contents.Add("selected EIC");
                    }
                    else if (counter > 1) {
                        _contents.Add("selected EICs");
                    }
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
