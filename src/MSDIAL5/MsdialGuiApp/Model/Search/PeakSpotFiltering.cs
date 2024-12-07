using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Search
{
    public class PeakSpotFiltering<T> : IDisposable where T: IFilterable, IAnnotatedObject
    {
        private readonly Dictionary<ICollectionView, AttachedPeakFilters<T>> _viewToFilterMethods = new Dictionary<ICollectionView, AttachedPeakFilters<T>>();
        private readonly Dictionary<ICollectionView, CompositeDisposable> _viewToDisposables = new Dictionary<ICollectionView, CompositeDisposable>();
        private readonly FilterEnableStatus _status;
        private bool _disposedValue;

        public PeakSpotFiltering(FilterEnableStatus status) {
            var valueFilterManagers = new List<ValueFilterManager<T>>();
            if ((status & FilterEnableStatus.Mz) != FilterEnableStatus.None) {
                var MzFilterModel = new ValueFilterModel("m/z range", 0d, 1d);
                valueFilterManagers.Add(new ValueFilterManager<T>(MzFilterModel, FilterEnableStatus.Mz, obj => ((ISpectrumPeak)obj)?.Mass ?? 0d));
            }
            if ((status & FilterEnableStatus.Rt) != FilterEnableStatus.None) {
                var RtFilterModel = new ValueFilterModel("Retention time", 0d, 1d);
                valueFilterManagers.Add(new ValueFilterManager<T>(RtFilterModel, FilterEnableStatus.Rt, obj => ((IChromatogramPeak)obj)?.ChromXs.RT.Value ?? 0d));
            }
            if ((status & FilterEnableStatus.Dt) != FilterEnableStatus.None) {
                var DtFilterModel = new ValueFilterModel("Mobility", 0d, 1d);
                valueFilterManagers.Add(new ValueFilterManager<T>(DtFilterModel, FilterEnableStatus.Dt, obj => ((IChromatogramPeak)obj)?.ChromXs.Drift.Value ?? 0d));
            }
            var keywordFilterManagers = new List<KeywordFilterManager<T>>();
            if ((status & FilterEnableStatus.Metabolite) != FilterEnableStatus.None) {
                var MetaboliteFilterModel = new KeywordFilterModel("Name filter");
                keywordFilterManagers.Add(new KeywordFilterManager<T>(MetaboliteFilterModel, FilterEnableStatus.Metabolite, obj => ((IMoleculeProperty)obj).Name));
            }
            if ((status & FilterEnableStatus.Protein) != FilterEnableStatus.None) {
                var ProteinFilterModel = new KeywordFilterModel("Protein filter", KeywordFilteringType.KeepIfWordIsNull);
                keywordFilterManagers.Add(new KeywordFilterManager<T>(ProteinFilterModel, FilterEnableStatus.Protein, obj => ((IFilterable)obj).Protein));
            }
            if ((status & FilterEnableStatus.Ontology) != FilterEnableStatus.None) {
                var OntologyFilterModel = new KeywordFilterModel("Ontology filter", KeywordFilteringType.ExactMatch);
                keywordFilterManagers.Add(new KeywordFilterManager<T>(OntologyFilterModel, FilterEnableStatus.Ontology, obj => ((IMoleculeProperty)obj).Ontology));
            }
            if ((status & FilterEnableStatus.Adduct) != FilterEnableStatus.None) {
                var AdductFilterModel = new KeywordFilterModel("Adduct filter", KeywordFilteringType.KeepIfWordIsNull);
                keywordFilterManagers.Add(new KeywordFilterManager<T>(AdductFilterModel, FilterEnableStatus.Adduct, obj => ((IFilterable)obj).AdductType.AdductIonName));
            }
            if ((status & FilterEnableStatus.Comment) != FilterEnableStatus.None) {
                var CommentFilterModel = new KeywordFilterModel("Comment filter");
                keywordFilterManagers.Add(new KeywordFilterManager<T>(CommentFilterModel, FilterEnableStatus.Comment, obj => ((IFilterable)obj).Comment));
            }
            var amplitudeFilterModel = new ValueFilterModel("Amplitude filter", 0d, 1d);
            var tagSearchQueryBuilder = new PeakSpotTagSearchQueryBuilderModel();

            ValueFilterManagers = valueFilterManagers;
            KeywordFilterManagers = keywordFilterManagers;
            AmplitudeFilterModel = amplitudeFilterModel;
            TagSearchQueryBuilder = tagSearchQueryBuilder;
            _status = status;
        }

        public List<ValueFilterManager<T>> ValueFilterManagers { get; }
        public List<KeywordFilterManager<T>> KeywordFilterManagers { get; }
        public ValueFilterModel AmplitudeFilterModel { get; }
        public PeakSpotTagSearchQueryBuilderModel TagSearchQueryBuilder { get; }

        public PeakSpotFilter CreateFilter(PeakFilterModel peakFilterModel, IMatchResultEvaluator<T> evaluator, FilterEnableStatus status) {
            return new PeakSpotFilter(this, peakFilterModel, evaluator, status);
        }

        public PeakSpotFilter CreateFilter(PeakFilterModel peakFilterModel, IMatchResultEvaluator<T> evaluator) {
            return new PeakSpotFilter(this, peakFilterModel, evaluator, _status);
        }

        public void AttachFilter(ICollectionView view, PeakFilterModel peakFilterModel, IMatchResultEvaluator<T> evaluator, FilterEnableStatus status) {
            var pred = CreateFilter(peakFilterModel, evaluator, TagSearchQueryBuilder);
            AttachFilterCore(pred.Invoke, view);
            if ((status & FilterEnableStatus.Amplitude) != FilterEnableStatus.None) {
                AttachFilter(AmplitudeFilterModel, obj => ((IFilterable)obj)?.RelativeAmplitudeValue ?? 0d, view);
            }
            foreach (var valueFilterManager in ValueFilterManagers) {
                valueFilterManager.TryAttachFilter(this, view, status);
            }
            foreach (var keywordFilterManager in KeywordFilterManagers) {
                keywordFilterManager.TryAttachFilter(this, view, status);
            }
        }

        public void AttachFilter(ValueFilterModel filterModel, Func<T, double> convert, ICollectionView view) {
            bool predicate(T filterable) => filterModel.Contains(convert(filterable));
            AttachFilterCore(predicate, view, filterModel.ObserveProperty(m => m.IsEnabled, isPushCurrentValueAtFirst: false), filterModel.IsEnabled);
            if (view.SourceCollection is INotifyCollectionChanged notifyCollection) {
                if (!_viewToDisposables.ContainsKey(view)) {
                    _viewToDisposables[view] = new CompositeDisposable();
                }
                notifyCollection.CollectionChangedAsObservable().ToUnit()
                    .StartWith(Unit.Default)
                    .Throttle(TimeSpan.FromSeconds(.05d))
                    .Subscribe(_ =>
                    {
                        filterModel.Minimum = view.SourceCollection.Cast<T>().DefaultIfEmpty().Min(convert);
                        filterModel.Maximum = view.SourceCollection.Cast<T>().DefaultIfEmpty().Max(convert);
                    }).AddTo(_viewToDisposables[view]);
            }
        }

        public void AttachFilter(KeywordFilterModel filterModel, Func<T, string> convert, ICollectionView view) {
            bool predicate(T filterable) => filterModel.Match(convert(filterable));
            AttachFilterCore(predicate, view, filterModel.ObserveProperty(m => m.IsEnabled, isPushCurrentValueAtFirst: false), filterModel.IsEnabled);
        }

        private void AttachFilterCore(Predicate<T> predicate, ICollectionView view) {
            if (!_viewToFilterMethods.ContainsKey(view)) {
                _viewToFilterMethods[view] = new AttachedPeakFilters<T>(view);
            }
            _viewToFilterMethods[view].Attatch(predicate);
        }

        private void AttachFilterCore(Predicate<T> predicate, ICollectionView view, IObservable<bool> enabled, bool initial) {
            if (!_viewToFilterMethods.ContainsKey(view)) {
                _viewToFilterMethods[view] = new AttachedPeakFilters<T>(view);
            }
            _viewToFilterMethods[view].Attatch(predicate, enabled, initial);
        }

        public bool DetatchFilter(ICollectionView view) {
            if (_viewToFilterMethods.ContainsKey(view)) {
                _viewToFilterMethods[view].Detatch();
                _viewToFilterMethods.Remove(view);
                _viewToDisposables[view].Dispose();
                _viewToDisposables.Remove(view);
                return true;
            }
            return false;
        }

        private Predicate<T> CreateFilter(PeakFilterModel peakFilterModel, IMatchResultEvaluator<T> evaluator, PeakSpotTagSearchQueryBuilderModel tagSearchQueryBuilder) {
            return filterable => peakFilterModel.PeakFilter(filterable, evaluator) && filterable.TagCollection.IsSelected(tagSearchQueryBuilder.CreateQuery());
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    foreach (var manager in KeywordFilterManagers) {
                        manager.Dispose();
                    }
                }
                var views = _viewToFilterMethods.Keys.ToArray();
                foreach (var view in views) {
                    DetatchFilter(view);
                }
                _viewToFilterMethods.Clear();
                _viewToDisposables.Clear();
                ValueFilterManagers.Clear();
                KeywordFilterManagers.Clear();
                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        class PeakFilters {
            private readonly ICollectionView _view;
            private readonly List<Predicate<T>> _enabledPredicates;
            private readonly List<Predicate<T>> _disabledPredicates;
            private Predicate<object>? _predicate;
            private readonly CompositeDisposable _disposables;

            public PeakFilters(ICollectionView view) {
                _view = view;
                _enabledPredicates = new List<Predicate<T>>();
                _disabledPredicates = new List<Predicate<T>>();
                _disposables = new CompositeDisposable();
            }

            private void ReloadFilter() {
                _view.Filter -= _predicate;
                _predicate = obj => obj is T t && _enabledPredicates.All(pred => pred.Invoke(t));
                _view.Filter += _predicate;
            }

            public void Attatch(Predicate<T> predicate) {
                _enabledPredicates.Add(predicate);
                ReloadFilter();
            }

            public void Attatch(Predicate<T> predicate, IObservable<bool> enabled, bool initial) {
                if (initial) {
                    _enabledPredicates.Add(predicate);
                    ReloadFilter();
                }
                else {
                    _disabledPredicates.Add(predicate);
                }
                _disposables.Add(enabled.Subscribe(e => {
                    if (e) {
                        _disabledPredicates.Remove(predicate);
                        _enabledPredicates.Add(predicate);
                    }
                    else {
                        _enabledPredicates.Remove(predicate);
                        _disabledPredicates.Add(predicate);
                    }
                    ReloadFilter();
                }));
            }

            public void Detatch() {
                _view.Filter -= _predicate;
                _enabledPredicates.Clear();
                _disabledPredicates.Clear();
                _predicate = null;
                _disposables.Dispose();
                _disposables.Clear();
            }

            ~PeakFilters() {
                _disposables.Dispose();
            }
        }

        public sealed class PeakSpotFilter {
            private readonly IMatchResultEvaluator<T> _evaluator;
            private readonly FilterEnableStatus _status;
            private readonly PeakFilterModel _peakFilterModel;
            private readonly PeakSpotFiltering<T> _peakSpotFiltering;

            public PeakSpotFilter(PeakSpotFiltering<T> peakSpotFiltering, PeakFilterModel peakFilterModel, IMatchResultEvaluator<T> evaluator, FilterEnableStatus status) {
                _evaluator = evaluator;
                _status = status;
                _peakFilterModel = peakFilterModel;
                _peakSpotFiltering = peakSpotFiltering;
            }

            public IEnumerable<T> Filter(IEnumerable<T> peaks) {
                peaks = peaks.Where(p => _peakFilterModel.PeakFilter(p, _evaluator));
                var query = _peakSpotFiltering.TagSearchQueryBuilder.CreateQuery();
                peaks = peaks.Where(p => p.TagCollection.IsSelected(query));

                if ((_status & FilterEnableStatus.Amplitude) != FilterEnableStatus.None) {
                    peaks = peaks.Where(p => _peakSpotFiltering.AmplitudeFilterModel.Contains(p.RelativeAmplitudeValue));
                }
                foreach (var valueFilterManager in _peakSpotFiltering.ValueFilterManagers) {
                    peaks = valueFilterManager.Apply(peaks, _status);
                }
                foreach (var keywordFilterManager in _peakSpotFiltering.KeywordFilterManagers) {
                    peaks = keywordFilterManager.Apply(peaks, _status);
                }
                return peaks;
            }

            public IEnumerable<T> FilterAnnotatedPeaks(IEnumerable<T> peaks) {
                return peaks.Where(p => _evaluator.IsReferenceMatched(p) || _evaluator.IsAnnotationSuggested(p));
            }
        } 
    }
}
