using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialGcMsApi.Algorithm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAnalysisCompoundSearchModel : BindableBase, ICompoundSearchModel
    {
        private bool _disposedValue;
        private readonly UndoManager _undoManager;
        private CompositeDisposable _disposables = new CompositeDisposable();
        private readonly CalculateMatchScore _calculateMatchScore;

        public GcmsAnalysisCompoundSearchModel(Ms1BasedSpectrumFeature spectrumFeature, IFileBean fileBean, CalculateMatchScore calculateMatchScore, UndoManager undoManager)
        {
            SpectrumFeature = spectrumFeature;
            File = fileBean;
            SearchParameter = calculateMatchScore.SearchParameter;
            _calculateMatchScore = calculateMatchScore.With(SearchParameter);
            _undoManager = undoManager;

            var referenceSpectrum = this.ObserveProperty(m => m.SelectedReference)
                .DefaultIfNull(c => new MsSpectrum(c.Spectrum))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(_disposables);
            var scorer = Observable.Return(new Ms2ScanMatching(SearchParameter));
            GraphLabels msGraphLabels = new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ObservableMsSpectrum upperObservableMsSpectrum = new ObservableMsSpectrum(Observable.Return(new MsSpectrum(spectrumFeature.Scan.Spectrum)), null, Observable.Return((ISpectraExporter)null)).AddTo(_disposables);
            ObservableMsSpectrum lowerObservableMsSpectrum = new ObservableMsSpectrum(referenceSpectrum, new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true)).AddTo(_disposables), Observable.Return((ISpectraExporter)null)).AddTo(_disposables);
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Mass);
            PropertySelector<SpectrumPeak, double> verticalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity);
            ChartHueItem upperSpectrumHueItem = new ChartHueItem(nameof(SpectrumPeak.SpectrumComment), ChartBrushes.GetBrush(Brushes.Blue));
            SingleSpectrumModel upperSpectrumModel = new SingleSpectrumModel(
                upperObservableMsSpectrum,
                upperObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"),
                upperObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"),
                upperSpectrumHueItem,
                msGraphLabels).AddTo(_disposables);
            ChartHueItem lowerSpectrumHueItem = new ChartHueItem(nameof(SpectrumPeak.SpectrumComment), ChartBrushes.GetBrush(Brushes.Red));
            SingleSpectrumModel lowerSpectrumModel = new SingleSpectrumModel(
                lowerObservableMsSpectrum,
                lowerObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"),
                lowerObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"),
                lowerSpectrumHueItem,
                msGraphLabels).AddTo(_disposables);
            MsSpectrumModel = new MsSpectrumModel(upperSpectrumModel, lowerSpectrumModel, scorer)
            {
                GraphTitle = string.Empty,
                HorizontalTitle = "m/z",
                VerticalTitle = "Abundance",
            }.AddTo(_disposables);
        }

        public Ms1BasedSpectrumFeature SpectrumFeature { get; }

        public IFileBean File { get; }

        public MsRefSearchParameterBase SearchParameter { get; }

        public RetentionType RetentionType => _calculateMatchScore.RetentionType;

        public MsSpectrumModel MsSpectrumModel { get; }

        public MoleculeMsReference SelectedReference {
            get => _selectedReference;
            set => SetProperty(ref _selectedReference, value);
        }
        private MoleculeMsReference _selectedReference;

        public MsScanMatchResult SelectedMatchResult {
            get => _selectedMatchResut;
            set => SetProperty(ref _selectedMatchResut, value);
        }
        private MsScanMatchResult _selectedMatchResut;

        public CompoundResultCollection Search() {
            var compounds = _calculateMatchScore.CalculateMatches(SpectrumFeature.Scan)
                    .OrderByDescending(r => r.TotalScore)
                    .Select(result => new GcmsCompoundResult(_calculateMatchScore.Reference(result), result))
                    .ToArray<ICompoundResult>();
            foreach (var compound in compounds) {
                compound.MatchResult.Source |= SourceType.Manual;
            }
            return new CompoundResultCollection
            {
                Results = compounds,
            };
        }

        public void SetConfidence() {
            SpectrumFeature.SetConfidence(SelectedReference, SelectedMatchResult);
        }

        public void SetUnknown() {
            SpectrumFeature.SetUnknown(_undoManager);
        }

        public void SetUnsettled() {
            SpectrumFeature.SetUnsettled(SelectedReference, SelectedMatchResult);
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    _disposables?.Dispose();
                }
                _disposables = null;
                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
