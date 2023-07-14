using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
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
    internal class SpectrumFeatureFiltering
    {
        private readonly Dictionary<ICollectionView, AttachedPeakFilters<Ms1BasedSpectrumFeature>> _viewToFilterMethods = new Dictionary<ICollectionView, AttachedPeakFilters<Ms1BasedSpectrumFeature>>();
        private readonly Dictionary<ICollectionView, CompositeDisposable> _viewToDisposables = new Dictionary<ICollectionView, CompositeDisposable>();
        private bool _disposedValue;

        public void AttachFilter(ICollectionView view, PeakFilterModel peakFilterModel, PeakSpotTagSearchQueryBuilderModel tagSearchQueryBuilder, IMatchResultEvaluator<Ms1BasedSpectrumFeature> evaluator) {
            var pred = CreateFilter(peakFilterModel, evaluator);
            AttachFilterCore(pred.Invoke, view);
        }

        public void AttachFilter(ValueFilterModel filterModel, Func<Ms1BasedSpectrumFeature, double> convert, ICollectionView view) {
            bool predicate(Ms1BasedSpectrumFeature filterable) => filterModel.Contains(convert(filterable));
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
                        filterModel.Minimum = view.SourceCollection.Cast<Ms1BasedSpectrumFeature>().DefaultIfEmpty().Min(convert);
                        filterModel.Maximum = view.SourceCollection.Cast<Ms1BasedSpectrumFeature>().DefaultIfEmpty().Max(convert);
                    }).AddTo(_viewToDisposables[view]);
            }
        }

        public void AttachFilter(KeywordFilterModel filterModel, Func<Ms1BasedSpectrumFeature, string> convert, ICollectionView view) {
            bool predicate(Ms1BasedSpectrumFeature filterable) => filterModel.Match(convert(filterable));
            AttachFilterCore(predicate, view, filterModel.ObserveProperty(m => m.IsEnabled, isPushCurrentValueAtFirst: false), filterModel.IsEnabled);
        }

        private void AttachFilterCore(Predicate<Ms1BasedSpectrumFeature> predicate, ICollectionView view) {
            if (!_viewToFilterMethods.ContainsKey(view)) {
                _viewToFilterMethods[view] = new AttachedPeakFilters<Ms1BasedSpectrumFeature>(view);
            }
            _viewToFilterMethods[view].Attatch(predicate);
        }

        private void AttachFilterCore(Predicate<Ms1BasedSpectrumFeature> predicate, ICollectionView view, IObservable<bool> enabled, bool initial) {
            if (!_viewToFilterMethods.ContainsKey(view)) {
                _viewToFilterMethods[view] = new AttachedPeakFilters<Ms1BasedSpectrumFeature>(view);
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

        private Predicate<Ms1BasedSpectrumFeature> CreateFilter(PeakFilterModel peakFilterModel, IMatchResultEvaluator<Ms1BasedSpectrumFeature> evaluator) {
            return spectrumFeature => peakFilterModel.AnnotationFilter(spectrumFeature, evaluator);
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                }
                var views = _viewToFilterMethods.Keys.ToArray();
                foreach (var view in views) {
                    DetatchFilter(view);
                }
                _viewToFilterMethods.Clear();
                _viewToDisposables.Clear();
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
