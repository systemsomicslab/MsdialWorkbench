using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CompMs.App.Msdial.Model.Search
{
    [Flags]
    public enum FilterEnableStatus {
        None = 0x0,
        Rt = 0x1,
        Dt = 0x2,
        Mz = 0x4,
        Metabolite = 0x8,
        Protein = 0x10,
        Comment = 0x20,
        Amplitude = 0x40,
        Ontology = 0x80,
        Adduct = 0x100,
        All = ~None,
    }

    public sealed class PeakSpotNavigatorModel : DisposableModelBase
    {
        public PeakSpotNavigatorModel(IReadOnlyList<IFilterable> peakSpots, PeakFilterModel peakFilterModel, IMatchResultEvaluator<MsScanMatchResult> evaluator, FilterEnableStatus status) {
            if (evaluator is null) {
                throw new ArgumentNullException(nameof(evaluator));
            }

            PeakSpots = peakSpots ?? throw new ArgumentNullException(nameof(peakSpots));
            PeakFilterModel = peakFilterModel ?? throw new ArgumentNullException(nameof(peakFilterModel));
            _evaluator = evaluator.Contramap<IFilterable, MsScanMatchResult>(filterable => filterable.MatchResults.Representative);
            AmplitudeLowerValue = 0d;
            AmplitudeUpperValue = 1d;
            if (peakSpots is INotifyCollectionChanged notifyCollection) {
                notifyCollection.CollectionChangedAsObservable().ToUnit()
                    .StartWith(Unit.Default)
                    .Throttle(TimeSpan.FromSeconds(.1d))
                    .Subscribe(_ =>
                    {
                        MzLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.Mass) ?? 0d;
                        MzUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.Mass) ?? 1d;
                        RtLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.ChromXs.RT.Value) ?? 0d;
                        RtUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.ChromXs.RT.Value) ?? 1d;
                        DtLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.ChromXs.Drift.Value) ?? 0d;
                        DtUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.ChromXs.Drift.Value) ?? 1d;
                    }).AddTo(Disposables);
            }
            else {
                //MzLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.Mass) ?? 0d;
                //MzUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.Mass) ?? 1d;
                //RtLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.ChromXs.RT.Value) ?? 0d;
                //RtUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.ChromXs.RT.Value) ?? 1d;
                //DtLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.ChromXs.Drift.Value) ?? 0d;
                //DtUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.ChromXs.Drift.Value) ?? 1d;
            }
            metaboliteFilterKeywords = new List<string>();
            MetaboliteFilterKeywords = metaboliteFilterKeywords.AsReadOnly();
            proteinFilterKeywords = new List<string>();
            ProteinFilterKeywords = proteinFilterKeywords.AsReadOnly();
            commentFilterKeywords = new List<string>();
            CommentFilterKeywords = commentFilterKeywords.AsReadOnly();
            ontologyFilterKeywords = new List<string>();
            OntologyFilterKeywords = ontologyFilterKeywords.AsReadOnly();
            adductFilterKeywords = new List<string>();
            AdductFilterKeywords = adductFilterKeywords.AsReadOnly();

            AttachFilter(peakSpots, peakFilterModel, status: status, evaluator: _evaluator);
        }

        public string SelectedAnnotationLabel {
            get => selectedAnnotationLabel;
            set => SetProperty(ref selectedAnnotationLabel, value);
        }
        private string selectedAnnotationLabel;

        public IReadOnlyList<IFilterable> PeakSpots { get; }
        public ObservableCollection<ICollectionView> PeakSpotsCollection { get; } = new ObservableCollection<ICollectionView>();

        public double AmplitudeLowerValue {
            get => amplitudeLowerValue;
            set => SetProperty(ref amplitudeLowerValue, value);
        }
        private double amplitudeLowerValue;
        public double AmplitudeUpperValue { 
            get => amplitudeUpperValue;
            set => SetProperty(ref amplitudeUpperValue, value);
        }
        private double amplitudeUpperValue;

        public double MzLowerValue {
            get => mzLowerValue;
            set => SetProperty(ref mzLowerValue, value);
        }
        private double mzLowerValue;
        public double MzUpperValue { 
            get => mzUpperValue;
            set => SetProperty(ref mzUpperValue, value);
        }
        private double mzUpperValue;

        public double RtLowerValue {
            get => rtLowerValue;
            set => SetProperty(ref rtLowerValue, value);
        }
        private double rtLowerValue;
        public double RtUpperValue { 
            get => rtUpperValue;
            set => SetProperty(ref rtUpperValue, value);
        }
        private double rtUpperValue;

        public double DtLowerValue {
            get => dtLowerValue;
            set => SetProperty(ref dtLowerValue, value);
        }
        private double dtLowerValue;
        public double DtUpperValue { 
            get => dtUpperValue;
            set => SetProperty(ref dtUpperValue, value);
        }
        private double dtUpperValue;

        private readonly IMatchResultEvaluator<IFilterable> _evaluator;

        public ReadOnlyCollection<string> MetaboliteFilterKeywords { get; }
        private readonly List<string> metaboliteFilterKeywords;

        private readonly SemaphoreSlim metaboliteSemaphore = new SemaphoreSlim(1, 1);
        public async Task SetMetaboliteKeywordsAsync(IEnumerable<string> keywords, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            await metaboliteSemaphore.WaitAsync().ConfigureAwait(false);
            try {
                token.ThrowIfCancellationRequested();
                SetMetaboliteKeywords(keywords);
            }
            finally {
                metaboliteSemaphore.Release();
            }
        }

        private void SetMetaboliteKeywords(IEnumerable<string> keywords) {
            metaboliteFilterKeywords.Clear();
            metaboliteFilterKeywords.AddRange(keywords);
        }

        public ReadOnlyCollection<string> ProteinFilterKeywords { get; }
        private readonly List<string> proteinFilterKeywords;

        private readonly SemaphoreSlim proteinSemaphore = new SemaphoreSlim(1, 1);
        public async Task SetProteinKeywordsAsync(IEnumerable<string> keywords, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            await proteinSemaphore.WaitAsync().ConfigureAwait(false);
            try {
                token.ThrowIfCancellationRequested();
                SetProteinKeywords(keywords);
            }
            finally {
                proteinSemaphore.Release();
            }
        }

        private void SetProteinKeywords(IEnumerable<string> keywords) {
            proteinFilterKeywords.Clear();
            proteinFilterKeywords.AddRange(keywords);
        }

        public ReadOnlyCollection<string> CommentFilterKeywords { get; }
        private readonly List<string> commentFilterKeywords;

        private readonly SemaphoreSlim commentSemaphore = new SemaphoreSlim(1, 1);
        public async Task SetCommentKeywordsAsync(IEnumerable<string> keywords, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            await commentSemaphore.WaitAsync().ConfigureAwait(false);
            try {
                token.ThrowIfCancellationRequested();
                SetCommentKeywords(keywords);
            }
            finally {
                commentSemaphore.Release();
            }
        }

        private void SetCommentKeywords(IEnumerable<string> keywords) {
            commentFilterKeywords.Clear();
            commentFilterKeywords.AddRange(keywords);
        }

        public ReadOnlyCollection<string> OntologyFilterKeywords { get; }
        private readonly List<string> ontologyFilterKeywords;

        private readonly SemaphoreSlim ontologySemaphore = new SemaphoreSlim(1, 1);
        public async Task SetOntologyKeywordsAsync(IEnumerable<string> keywords, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            await ontologySemaphore.WaitAsync().ConfigureAwait(false);
            try {
                token.ThrowIfCancellationRequested();
                SetOntologyKeywords(keywords);
            }
            finally {
                ontologySemaphore.Release();
            }
        }

        private void SetOntologyKeywords(IEnumerable<string> keywords) {
            ontologyFilterKeywords.Clear();
            ontologyFilterKeywords.AddRange(keywords);
        }

        public ReadOnlyCollection<string> AdductFilterKeywords { get; }
        private readonly List<string> adductFilterKeywords;

        private readonly SemaphoreSlim adductSemaphore = new SemaphoreSlim(1, 1);
        public async Task SetAdductKeywordsAsync(IEnumerable<string> keywords, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            await adductSemaphore.WaitAsync().ConfigureAwait(false);
            try {
                token.ThrowIfCancellationRequested();
                SetAdductKeywords(keywords);
            }
            finally {
                adductSemaphore.Release();
            }
        }

        private void SetAdductKeywords(IEnumerable<string> keywords) {
            adductFilterKeywords.Clear();
            adductFilterKeywords.AddRange(keywords);
        }


        public PeakFilterModel PeakFilterModel { get; }

        private readonly Dictionary<ICollectionView, Predicate<object>> _viewToPredicate = new Dictionary<ICollectionView, Predicate<object>>();
        private readonly Dictionary<ICollectionView, List<PeakFilterModel>> _viewToFilters = new Dictionary<ICollectionView, List<PeakFilterModel>>();
        public ObservableCollection<PeakFilterModel> PeakFilters { get; } = new ObservableCollection<PeakFilterModel>();

        public void AttachFilter(ICollectionView view, PeakFilterModel peakFilterModel, FilterEnableStatus status, IMatchResultEvaluator<IFilterable> evaluator = null) {
            if (_viewToPredicate.ContainsKey(view)) {
                view.Filter -= _viewToPredicate[view];
                _viewToPredicate[view] += CreateFilter(peakFilterModel, status, evaluator ?? _evaluator);
            }
            else {
                _viewToPredicate[view] = CreateFilter(peakFilterModel, status, evaluator ?? _evaluator);
            }
            view.Filter += _viewToPredicate[view];
            if (!_viewToFilters.ContainsKey(view)) {
                _viewToFilters.Add(view, new List<PeakFilterModel>());
            }
            _viewToFilters[view].Add(peakFilterModel);
            PeakFilters.Add(peakFilterModel);
            PeakSpotsCollection.Add(view);
        }

        public void AttachFilter(IEnumerable<IFilterable> peaks, PeakFilterModel peakFilterModel, FilterEnableStatus status = ~FilterEnableStatus.None, IMatchResultEvaluator<IFilterable> evaluator = null) {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ICollectionView collection = CollectionViewSource.GetDefaultView(peaks);
                if (collection is ListCollectionView list) {
                    list.IsLiveFiltering = true;
                }
                AttachFilter(collection, peakFilterModel, status, evaluator);
            });
        }

        public void DetatchFilter(ICollectionView view) {
            if (_viewToPredicate.ContainsKey(view)) {
                view.Filter -= _viewToPredicate[view];
                _viewToPredicate.Remove(view);
                foreach (var filter in _viewToFilters[view]) {
                    PeakFilters.Remove(filter);
                }
                _viewToFilters.Remove(view);
                PeakSpotsCollection.Remove(view);
            }
        }

        public void DetatchFilter(IEnumerable<IFilterable> peaks) {
            DetatchFilter(CollectionViewSource.GetDefaultView(peaks));
        }

        private Predicate<object> CreateFilter(PeakFilterModel peakFilterModel, FilterEnableStatus status, IMatchResultEvaluator<IFilterable> evaluator) {
            List<Predicate<IFilterable>> results = new List<Predicate<IFilterable>>();
            results.Add(filterable => peakFilterModel.PeakFilter(filterable, evaluator));
            if ((status & FilterEnableStatus.Rt) != FilterEnableStatus.None) {
                results.Add(RtFilter);
            }
            if ((status & FilterEnableStatus.Dt) != FilterEnableStatus.None) {
                results.Add(DtFilter);
            }
            if ((status & FilterEnableStatus.Mz) != FilterEnableStatus.None) {
                results.Add(MzFilter);
            }
            if ((status & FilterEnableStatus.Amplitude) != FilterEnableStatus.None) {
                results.Add(AmplitudeFilter);
            }
            if ((status & FilterEnableStatus.Metabolite) != FilterEnableStatus.None) {
                results.Add(filterable => MetaboliteFilter(filterable, MetaboliteFilterKeywords));
            }
            if ((status & FilterEnableStatus.Protein) != FilterEnableStatus.None) {
                results.Add(filterable => ProteinFilter(filterable, ProteinFilterKeywords));
            }
            if ((status & FilterEnableStatus.Comment) != FilterEnableStatus.None) {
                results.Add(filterable => CommentFilter(filterable, CommentFilterKeywords));
            }

            if ((status & FilterEnableStatus.Adduct) != FilterEnableStatus.None) {
                results.Add(filterable => AdductFilter(filterable, AdductFilterKeywords));
            }

            if ((status & FilterEnableStatus.Ontology) != FilterEnableStatus.None) {
                results.Add(filterable => OntologyFilter(filterable, OntologyFilterKeywords));
            }


            return (object obj) => obj is IFilterable filterable && results.All(pred => pred(filterable));
        }

        private bool MzFilter(IChromatogramPeak peak) {
            return MzLowerValue <= peak.Mass && peak.Mass <= MzUpperValue;
        }

        private bool RtFilter(IChromatogramPeak peak) {
            return RtLowerValue <= peak.ChromXs.RT.Value && peak.ChromXs.RT.Value <= RtUpperValue;
        }

        private bool DtFilter(IChromatogramPeak peak) {
            return DtLowerValue <= peak.ChromXs.Drift.Value && peak.ChromXs.Drift.Value <= DtUpperValue;
        }

        private bool ProteinFilter(IFilterable peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.Protein?.Contains(keyword) ?? true);
        }

        private bool MetaboliteFilter(IMoleculeProperty peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.Name.Contains(keyword));
        }

        private bool CommentFilter(IFilterable peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => string.IsNullOrEmpty(keyword) || (peak.Comment?.Contains(keyword) ?? false));
        }

        private bool OntologyFilter(IFilterable peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => string.IsNullOrEmpty(keyword) || peak.Ontology == keyword);
        }

        private bool AdductFilter(IFilterable peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.AdductIonName?.Contains(keyword) ?? true);
        }

        private bool AmplitudeFilter(IFilterable peak) {
            var relative = peak.RelativeAmplitudeValue;
            return AmplitudeLowerValue <= relative && relative <= AmplitudeUpperValue;
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                var views = _viewToFilters.Keys.ToArray();
                foreach (var view in views) {
                    DetatchFilter(view);
                }
                _viewToFilters.Clear();
                _viewToPredicate.Clear();
                PeakFilters.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
