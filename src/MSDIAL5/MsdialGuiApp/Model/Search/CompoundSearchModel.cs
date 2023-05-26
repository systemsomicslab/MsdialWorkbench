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
        IReadOnlyList<CompoundSearcher> CompoundSearchers { get; }

        CompoundSearcher SelectedCompoundSearcher { get; set; }
        
        IFileBean File { get; }

        IPeakSpotModel PeakSpot { get; }

        MsSpectrumModel MsSpectrumModel { get; }

        MoleculeMsReference SelectedReference { get; set; }

        MsScanMatchResult SelectedMatchResult { get; set; }

        CompoundResultCollection Search();

        void SetConfidence();

        void SetUnsettled();

        void SetUnknown();
    }

    internal class CompoundSearchModel : DisposableModelBase, ICompoundSearchModel
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
                .Select(c => c.Spectrum)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var scorer = this.ObserveProperty(m => m.SelectedCompoundSearcher)
                .SkipNull()
                .Select(s => new Ms2ScanMatching(s.MsRefSearchParameter))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            MsSpectrumModel = new MsSpectrumModel(
                Observable.Return(_msdecResult.Spectrum),
                referenceSpectrum,
                new PropertySelector<SpectrumPeak, double>(peak => peak.Mass),
                new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity),
                new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)),
                nameof(SpectrumPeak.SpectrumComment),
                Observable.Return(MsSpectrumModel.GetBrush(Brushes.Blue)),
                Observable.Return(MsSpectrumModel.GetBrush(Brushes.Red)),
                Observable.Return((ISpectraExporter)null),
                Observable.Return((ISpectraExporter)null),
                null,
                scorer).AddTo(Disposables);
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

        public virtual CompoundResultCollection Search() {
            return new CompoundResultCollection
            {
                Results = SearchCore().ToList(),
            };
        }

        protected IEnumerable<ICompoundResult> SearchCore() {
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
