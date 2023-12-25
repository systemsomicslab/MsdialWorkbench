using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Search
{
    interface ICompoundSearchModel : INotifyPropertyChanged, IDisposable {
        IList SearchMethods { get; }

        object SearchMethod { get; set; }

        ReadOnlyReactivePropertySlim<MsRefSearchParameterBase> SearchParameter { get; }
        
        IFileBean File { get; }

        IPeakSpotModel PeakSpot { get; }

        ICompoundResult SelectedCompoundResult { get; set; }

        MsSpectrumModel MsSpectrumModel { get; }

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
        private readonly SetAnnotationUsecase _setAnnotationService;
        private readonly PlotComparedMsSpectrumUsecase _plotService;
        private readonly ICompoundSearchUsecase<ICompoundResult, PeakSpotModel> _compoundSearchService;
        private readonly PeakSpotModel _peakSpot;
        private readonly BusyNotifier _isBusy;

        public CompoundSearchModel(IFileBean fileBean, PeakSpotModel peakSpot, ICompoundSearchUsecase<ICompoundResult, PeakSpotModel> compoundSearchService, PlotComparedMsSpectrumUsecase plotComparedMsSpectrumService, SetAnnotationUsecase setAnnotationService) {
            File = fileBean ?? throw new ArgumentNullException(nameof(fileBean));
            _peakSpot = peakSpot;
            _compoundSearchService = compoundSearchService;
            _plotService = plotComparedMsSpectrumService;
            _setAnnotationService = setAnnotationService;
            _isBusy = new BusyNotifier();
            SearchParameter = _compoundSearchService.ObserveProperty(m => m.SearchParameter)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            SearchMethod = SearchMethods.OfType<object>().FirstOrDefault();

            this.ObserveProperty(m => SelectedCompoundResult)
                .Subscribe(r => _plotService.UpdateReference(r?.MsReference)).AddTo(Disposables);
            _compoundSearchService.ObserveProperty(m => m.SearchParameter)
                .SkipNull()
                .Select(s => new Ms2ScanMatching(s))
                .Subscribe(_plotService.UpdateMatchingScorer).AddTo(Disposables);
        }

        public IList SearchMethods => _compoundSearchService.SearchMethods;

        public object SearchMethod {
            get => _compoundSearchService.SearchMethod;
            set {
                if (_compoundSearchService.SearchMethod != value) {
                    _compoundSearchService.SearchMethod = value;
                    OnPropertyChanged(nameof(SearchMethod));
                }
            }
        }

        public ReadOnlyReactivePropertySlim<MsRefSearchParameterBase> SearchParameter { get; }
        
        public IFileBean File { get; }

        public IPeakSpotModel PeakSpot => _peakSpot.PeakSpot;

        public MsSpectrumModel MsSpectrumModel => _plotService.MsSpectrumModel;

        public ICompoundResult SelectedCompoundResult {
            get => _selectedCompoundResult;
            set => SetProperty(ref _selectedCompoundResult, value);
        }
        private ICompoundResult _selectedCompoundResult;

        public IReadOnlyList<ICompoundResult> CompoundResults {
            get => _compoundResults;
            private set => SetProperty(ref _compoundResults, value);
        }
        private IReadOnlyList<ICompoundResult> _compoundResults;

        public IObservable<bool> IsBusy => _isBusy;

        public async Task SearchAsync(CancellationToken token) {
            using (_isBusy.ProcessStart()) {
                CompoundResults = await Task.Run(() => _compoundSearchService.Search(_peakSpot), token).ConfigureAwait(false);
            }
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
