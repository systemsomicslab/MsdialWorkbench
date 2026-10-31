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
using System.Reactive.Disposables;
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
        private readonly CancellationTokenSource _lifetimeSource;
        private readonly CancellationToken _lifetime;

        public CompoundSearchModel(IFileBean fileBean, T peakSpot, ICompoundSearchUsecase<ICompoundResult, T> compoundSearchService, PlotComparedMsSpectrumUsecase plotComparedMsSpectrumService, SetAnnotationUsecase setAnnotationService) {
            File = fileBean ?? throw new ArgumentNullException(nameof(fileBean));
            _peakSpot = peakSpot;
            _compoundSearchService = compoundSearchService;
            _plotService = plotComparedMsSpectrumService;
            _setAnnotationService = setAnnotationService;
            _isBusy = new BusyNotifier();
            _lifetimeSource = new CancellationTokenSource();
            _lifetime = _lifetimeSource.Token;
            Disposables.Add(Disposable.Create(_lifetimeSource.Dispose));
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
            try {
                _lifetime.ThrowIfCancellationRequested();
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime);
                token = linked.Token;
                using (_isBusy.ProcessStart()) {
                    var results = await Task.Run(() => _compoundSearchService is ICancellableCompoundSearchUsecase<ICompoundResult, T> cancellable
                        ? cancellable.Search(_peakSpot, token) : _compoundSearchService.Search(_peakSpot), token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    if (!disposedValue) {
                        CompoundResults = results;
                    }
                }
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) {
                // Window closure completes the observable normally, including a delayed initial search.
            }
        }

        protected override void Dispose(bool disposing) {
            // WPF queues resource disposal; cancellation must reach scoring as soon as the window closes.
            if (disposing && !disposedValue) { _lifetimeSource.Cancel(); }
            base.Dispose(disposing);
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
