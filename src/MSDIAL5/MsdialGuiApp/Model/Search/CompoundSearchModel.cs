using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Search
{
    interface ICompoundSearchModel : INotifyPropertyChanged, IDisposable {
        IReadOnlyList<CompoundSearcher> CompoundSearchers { get; }

        CompoundSearcher SelectedCompoundSearcher { get; set; }
        
        IFileBean File { get; }

        IPeakSpotModel PeakSpot { get; }

        MsSpectrumModel MsSpectrumModel { get; }

        ICompoundResult SelectedCompoundResult { get; set; }

        MoleculeMsReference SelectedReference { get; set; }

        MsScanMatchResult SelectedMatchResult { get; set; }

        CompoundResultCollection Search();

        void SetConfidence();

        void SetUnsettled();

        void SetUnknown();
    }

    internal class CompoundSearchModel : DisposableModelBase, ICompoundSearchModel
    {
        private readonly SetAnnotationService _setAnnotationService;
        private readonly PlotComparedMsSpectrumService _plotService;
        private readonly ICompoundSearchService<ICompoundResult, PeakSpotModel> _compoundSearchService;
        private readonly PeakSpotModel _peakSpot;

        public CompoundSearchModel(IFileBean fileBean, PeakSpotModel peakSpot, ICompoundSearchService<ICompoundResult, PeakSpotModel> compoundSearchService, PlotComparedMsSpectrumService plotComparedMsSpectrumService, SetAnnotationService setAnnotationService) {
            File = fileBean ?? throw new ArgumentNullException(nameof(fileBean));
            _peakSpot = peakSpot;
            _compoundSearchService = compoundSearchService;
            _plotService = plotComparedMsSpectrumService;
            _setAnnotationService = setAnnotationService;
            SelectedCompoundSearcher = CompoundSearchers.FirstOrDefault();

            this.ObserveProperty(m => SelectedReference)
                .Subscribe(_plotService.UpdateReference).AddTo(Disposables);
            this.ObserveProperty(m => SelectedCompoundSearcher)
                .SkipNull()
                .Select(s => new Ms2ScanMatching(s.MsRefSearchParameter))
                .Subscribe(_plotService.UpdateMatchingScorer).AddTo(Disposables);
        }

        public IReadOnlyList<CompoundSearcher> CompoundSearchers => _compoundSearchService.CompoundSearchers;

        public CompoundSearcher SelectedCompoundSearcher {
            get => _compoundSearchService.SelectedCompoundSearcher;
            set {
                if (_compoundSearchService.SelectedCompoundSearcher != value) {
                    _compoundSearchService.SelectedCompoundSearcher = value;
                    OnPropertyChanged(nameof(SelectedCompoundSearcher));
                }
            }
        }
        
        public IFileBean File { get; }

        public IPeakSpotModel PeakSpot => _peakSpot.PeakSpot;

        public MsSpectrumModel MsSpectrumModel => _plotService.MsSpectrumModel;

        public ICompoundResult SelectedCompoundResult {
            get => _selectedCompoundResult;
            set => SetProperty(ref _selectedCompoundResult, value);
        }
        private ICompoundResult _selectedCompoundResult;

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
                Results = _compoundSearchService.Search(_peakSpot),
            };
        }

        public void SetConfidence() {
            _setAnnotationService.SetConfidence(SelectedCompoundResult);
        }

        public void SetUnsettled() {
            _setAnnotationService.SetUnsettled(SelectedCompoundResult);
        }

        public void SetUnknown() {
            _setAnnotationService.SetUnknown();
        }

        internal new IList<IDisposable> Disposables => base.Disposables;
    }
}
