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

        public SpectrumFeatureFiltering()
        {
            var valueFilterManagers = new List<ValueFilterManager>();
            var MzFilterModel = new ValueFilterModel("m/z range", 0d, 1d);
            valueFilterManagers.Add(new ValueFilterManager(MzFilterModel, FilterEnableStatus.Mz, (Ms1BasedSpectrumFeature f) => f?.QuantifiedChromatogramPeak.PeakFeature.Mass ?? 0d));
            var RtFilterModel = new ValueFilterModel("Retention time", 0d, 1d);
            valueFilterManagers.Add(new ValueFilterManager(RtFilterModel, FilterEnableStatus.Rt, (Ms1BasedSpectrumFeature f) => f?.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value ?? 0d));
            var keywordFilterManagers = new List<KeywordFilterManager>();
            var MetaboliteFilterModel = new KeywordFilterModel("Name filter");
            keywordFilterManagers.Add(new KeywordFilterManager(MetaboliteFilterModel, FilterEnableStatus.Metabolite, (Ms1BasedSpectrumFeature f) => f.Molecule.Name));
            var CommentFilterModel = new KeywordFilterModel("Comment filter");
            keywordFilterManagers.Add(new KeywordFilterManager(CommentFilterModel, FilterEnableStatus.Comment, (Ms1BasedSpectrumFeature f) => f.Comment));

            ValueFilterManagers = valueFilterManagers;
            KeywordFilterManagers = keywordFilterManagers;
            AmplitudeFilterModel = new ValueFilterModel("Amplitude filter", 0d, 1d);
            TagSearchQueryBuilder = new PeakSpotTagSearchQueryBuilderModel();
        }

        public List<ValueFilterManager> ValueFilterManagers { get; }
        public List<KeywordFilterManager> KeywordFilterManagers { get; }
        public ValueFilterModel AmplitudeFilterModel { get; }
        public PeakSpotTagSearchQueryBuilderModel TagSearchQueryBuilder { get; }

        public void AttachFilter(ICollectionView view, PeakFilterModel peakFilterModel, IMatchResultEvaluator<Ms1BasedSpectrumFeature> evaluator, FilterEnableStatus status) {
            var pred = CreateFilter(peakFilterModel, evaluator, TagSearchQueryBuilder);
            AttachFilterCore(pred.Invoke, view);
            if ((status & FilterEnableStatus.Amplitude) != FilterEnableStatus.None) {
                AttachFilter(AmplitudeFilterModel, (Ms1BasedSpectrumFeature f) => f?.QuantifiedChromatogramPeak.PeakShape.AmplitudeScoreValue ?? 0d, view);
            }
            foreach (var valueFilterManager in ValueFilterManagers) {
                valueFilterManager.TryAttachFilter(this, view, status);
            }
            foreach (var keywordFilterManager in KeywordFilterManagers) {
                keywordFilterManager.TryAttachFilter(this, view, status);
            }
        }

        public void AttachFilter(ICollectionView view, PeakFilterModel peakFilterModel, PeakSpotTagSearchQueryBuilderModel tagSearchQueryBuilder, IMatchResultEvaluator<Ms1BasedSpectrumFeature> evaluator) {
            var pred = CreateFilter(peakFilterModel, evaluator, tagSearchQueryBuilder);
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

        private Predicate<Ms1BasedSpectrumFeature> CreateFilter(PeakFilterModel peakFilterModel, IMatchResultEvaluator<Ms1BasedSpectrumFeature> evaluator, PeakSpotTagSearchQueryBuilderModel tagSearchQueryBuilder) {
            return spectrumFeature => peakFilterModel.AnnotationFilter(spectrumFeature, evaluator) && spectrumFeature.TagCollection.IsSelected(tagSearchQueryBuilder.CreateQuery());
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

        internal class ValueFilterManager {
            private readonly ValueFilterModel _model;
            private readonly FilterEnableStatus _status;
            private readonly Func<Ms1BasedSpectrumFeature, double> _convert;

            public ValueFilterManager(ValueFilterModel model, FilterEnableStatus status, Func<Ms1BasedSpectrumFeature, double> convert)
            {
                _model = model;
                _status = status;
                _convert = convert;
            }

            public ValueFilterModel Filter => _model;

            public void AttchFilter(SpectrumFeatureFiltering filtering, ICollectionView collection) {
                filtering.AttachFilter(_model, _convert, collection);
            }

            public bool TryAttachFilter(SpectrumFeatureFiltering filtering, ICollectionView collection, FilterEnableStatus status) {
                if ((status & _status) == _status) {
                    AttchFilter(filtering, collection);
                    return true;
                }
                return false;
            }
        }

        internal class KeywordFilterManager : IDisposable {
            private readonly KeywordFilterModel _model;
            private readonly FilterEnableStatus _status;
            private readonly Func<Ms1BasedSpectrumFeature, string> _convert;

            public KeywordFilterManager(KeywordFilterModel model, FilterEnableStatus status, Func<Ms1BasedSpectrumFeature, string> convert)
            {
                _model = model;
                _status = status;
                _convert = convert;
            }

            public KeywordFilterModel Filter => _model;

            public void AttchFilter(SpectrumFeatureFiltering filtering, ICollectionView collection) {
                filtering.AttachFilter(_model, _convert, collection);
            }

            public bool TryAttachFilter(SpectrumFeatureFiltering filtering, ICollectionView collection, FilterEnableStatus status) {
                if ((status & _status) == _status) {
                    AttchFilter(filtering, collection);
                    return true;
                }
                return false;
            }

            public void Dispose() {
                _model.Dispose();
            }
        }
    }
}
