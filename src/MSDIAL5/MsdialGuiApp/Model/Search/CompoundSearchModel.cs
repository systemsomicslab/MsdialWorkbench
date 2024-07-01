using CompMs.App.Msdial.Model.Chart;
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

        object? SearchMethod { get; set; }

        ReadOnlyReactivePropertySlim<MsRefSearchParameterBase?> SearchParameter { get; }
        
        IFileBean File { get; }

        ICompoundResult? SelectedCompoundResult { get; set; }

        IReadOnlyList<ICompoundResult>? CompoundResults { get; }

        MsSpectrumModel MsSpectrumModel { get; }

        void SetConfidence();

        void SetUnsettled();

        void SetUnknown();
    }

    internal class CompoundSearchModel<T> : DisposableModelBase, ICompoundSearchModel
    {
        private readonly SetAnnotationUsecase _setAnnotationService;
        private readonly PlotComparedMsSpectrumUsecase _plotService;
        private readonly ICompoundSearchUsecase<ICompoundResult, T> _compoundSearchService;
        private readonly T _peakSpot;
        private readonly BusyNotifier _isBusy;

        public CompoundSearchModel(IFileBean fileBean, T peakSpot, ICompoundSearchUsecase<ICompoundResult, T> compoundSearchService, PlotComparedMsSpectrumUsecase plotComparedMsSpectrumService, SetAnnotationUsecase setAnnotationService) {
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

        public object? SearchMethod {
            get => _compoundSearchService.SearchMethod;
            set {
                if (_compoundSearchService.SearchMethod != value) {
                    _compoundSearchService.SearchMethod = value;
                    OnPropertyChanged(nameof(SearchMethod));
                }
            }
        }

        public ReadOnlyReactivePropertySlim<MsRefSearchParameterBase?> SearchParameter { get; }
        
        public IFileBean File { get; }

        public T PeakSpot => _peakSpot;

        public MsSpectrumModel MsSpectrumModel => _plotService.MsSpectrumModel;

        public ICompoundResult? SelectedCompoundResult {
            get => _selectedCompoundResult;
            set => SetProperty(ref _selectedCompoundResult, value);
        }
        private ICompoundResult? _selectedCompoundResult;

        public IReadOnlyList<ICompoundResult>? CompoundResults {
            get => _compoundResults;
            private set => SetProperty(ref _compoundResults, value);
        }
        private IReadOnlyList<ICompoundResult>? _compoundResults;

        public IObservable<bool> IsBusy => _isBusy;

        public async Task SearchAsync(CancellationToken token) {
            using (_isBusy.ProcessStart()) {
                CompoundResults = await Task.Run(() => _compoundSearchService.Search(_peakSpot), token).ConfigureAwait(false);
            }
        }

        public void SetConfidence() {
            if (SelectedCompoundResult is ICompoundResult result) {
                _setAnnotationService.SetConfidence(result);
            }
        }

        public void SetUnsettled() {
            if (SelectedCompoundResult is ICompoundResult result) {
                _setAnnotationService.SetUnsettled(result);
            }
        }

        public void SetUnknown() {
            _setAnnotationService.SetUnknown();
        }

        internal new IList<IDisposable> Disposables => base.Disposables;
    }
}
