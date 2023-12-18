using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Search
{
    interface ICompoundSearchModel : INotifyPropertyChanged, IDisposable {
        IFileBean File { get; }

        MsSpectrumModel MsSpectrumModel { get; }

        MoleculeMsReference SelectedReference { get; set; }

        MsScanMatchResult SelectedMatchResult { get; set; }

        CompoundResultCollection Search();

        void SetConfidence();

        void SetUnsettled();

        void SetUnknown();
    }

    interface IEsiCompoundSearchModel : ICompoundSearchModel {
        IReadOnlyList<CompoundSearcher> CompoundSearchers { get; }

        CompoundSearcher SelectedCompoundSearcher { get; set; }
    }

    internal class CompoundSearchModel : DisposableModelBase, IEsiCompoundSearchModel
    {
        private readonly MSDecResult _msdecResult;
        private readonly UndoManager _undoManager;
        private readonly IPeakSpotModel _peakSpot;

        public CompoundSearchModel(IFileBean fileBean, IPeakSpotModel peakSpot, MSDecResult msdecResult, CompoundSearcherCollection compoundSearchers, UndoManager undoManager)
            : this(fileBean, peakSpot, msdecResult, compoundSearchers.Items, undoManager) {

        }

        public CompoundSearchModel(IFileBean fileBean, IPeakSpotModel peakSpot, MSDecResult msdecResult, IReadOnlyList<CompoundSearcher> compoundSearchers, UndoManager undoManager) {
            File = fileBean ?? throw new ArgumentNullException(nameof(fileBean));
            _peakSpot = peakSpot ?? throw new ArgumentNullException(nameof(peakSpot));
            CompoundSearchers = compoundSearchers;
            _undoManager = undoManager;
            SelectedCompoundSearcher = CompoundSearchers.FirstOrDefault();
            _msdecResult = msdecResult ?? throw new ArgumentNullException(nameof(msdecResult));

            var referenceSpectrum = this.ObserveProperty(m => m.SelectedReference)
                .SkipNull()
                .Select(c => new MsSpectrum(c.Spectrum))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var scorer = this.ObserveProperty(m => m.SelectedCompoundSearcher)
                .SkipNull()
                .Select(s => new Ms2ScanMatching(s.MsRefSearchParameter))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            GraphLabels msGraphLabels = new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ObservableMsSpectrum upperObservableMsSpectrum = new ObservableMsSpectrum(Observable.Return(new MsSpectrum(msdecResult.Spectrum)), null, Observable.Return((ISpectraExporter)null)).AddTo(Disposables);
            ObservableMsSpectrum lowerObservableMsSpectrum = new ObservableMsSpectrum(referenceSpectrum, new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true)).AddTo(Disposables), Observable.Return((ISpectraExporter)null)).AddTo(Disposables);
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Mass);
            PropertySelector<SpectrumPeak, double> verticalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity);
            ChartHueItem upperSpectrumHueItem = new ChartHueItem(nameof(SpectrumPeak.SpectrumComment), ChartBrushes.GetBrush(Brushes.Blue));
            SingleSpectrumModel upperSpectrumModel = new SingleSpectrumModel(
                upperObservableMsSpectrum,
                upperObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"),
                upperObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"),
                upperSpectrumHueItem,
                msGraphLabels).AddTo(Disposables);
            ChartHueItem lowerSpectrumHueItem = new ChartHueItem(nameof(SpectrumPeak.SpectrumComment), ChartBrushes.GetBrush(Brushes.Red));
            SingleSpectrumModel lowerSpectrumModel = new SingleSpectrumModel(
                lowerObservableMsSpectrum,
                lowerObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"),
                lowerObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"),
                lowerSpectrumHueItem,
                msGraphLabels).AddTo(Disposables);
            MsSpectrumModel = new MsSpectrumModel(upperSpectrumModel, lowerSpectrumModel, scorer)
            {
                GraphTitle = string.Empty,
                HorizontalTitle = "m/z",
                VerticalTitle = "Abundance",
            }.AddTo(Disposables);
        }

        public IReadOnlyList<CompoundSearcher> CompoundSearchers { get; }

        public CompoundSearcher SelectedCompoundSearcher {
            get => _compoundSearcher;
            set => SetProperty(ref _compoundSearcher, value);
        }
        private CompoundSearcher _compoundSearcher;
        
        public IFileBean File { get; }

        public IPeakSpotModel PeakSpot => _peakSpot;

        public MsSpectrumModel MsSpectrumModel { get; }

        public MoleculeMsReference SelectedReference { 
            get => _selectedReference;
            set => SetProperty(ref _selectedReference, value);
        }
        private MoleculeMsReference _selectedReference;

        public MsScanMatchResult SelectedMatchResult {
            get => _selectedMatchResult;
            set => SetProperty(ref _selectedMatchResult, value);
        }
        private MsScanMatchResult _selectedMatchResult;

        public CompoundResultCollection Search() {
            var results = SearchCore();
            return new CompoundResultCollection
            {
                Results = (results as IList<ICompoundResult>) ?? results.ToList(),
            };
        }

        protected virtual IEnumerable<ICompoundResult> SearchCore() {
            return SelectedCompoundSearcher.Search(
                _peakSpot.MSIon,
                _msdecResult,
                new List<RawPeakElement>(),
                new IonFeatureCharacter { IsotopeWeightNumber = 0, } // Assume this is not isotope.
            );
        }

        public void SetConfidence() {
            _peakSpot.SetConfidence(SelectedReference, SelectedMatchResult);
        }

        public void SetUnsettled() {
            _peakSpot.SetUnsettled(SelectedReference, SelectedMatchResult);
        }

        public void SetUnknown() {
            _peakSpot.SetUnknown(_undoManager);
        }
    }
}
