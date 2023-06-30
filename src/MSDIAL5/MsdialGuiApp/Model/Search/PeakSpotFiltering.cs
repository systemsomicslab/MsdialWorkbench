using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Search
{
    internal sealed class PeakSpotFiltering : IDisposable
    {
        private readonly Dictionary<ICollectionView, List<PeakFilterModel>> _viewToFilters = new Dictionary<ICollectionView, List<PeakFilterModel>>();
        private readonly Dictionary<ICollectionView, Predicate<object>> _viewToPredicate = new Dictionary<ICollectionView, Predicate<object>>();
        private bool _disposedValue;

        public void AttachFilter(PeakSpotNavigatorModel peakSpotNavigatorModel, ICollectionView view, PeakFilterModel peakFilterModel, FilterEnableStatus status, IMatchResultEvaluator<IFilterable> evaluator) {
            var pred = CreateFilter(peakSpotNavigatorModel, peakFilterModel, status, evaluator);
            bool predicate(object obj) => obj is IFilterable filterable && pred.Invoke(filterable);
            if (_viewToPredicate.ContainsKey(view)) {
                view.Filter -= _viewToPredicate[view];
                _viewToPredicate[view] += predicate;
            }
            else {
                _viewToPredicate[view] = predicate;
            }
            view.Filter += _viewToPredicate[view];
            if (!_viewToFilters.ContainsKey(view)) {
                _viewToFilters.Add(view, new List<PeakFilterModel>());
            }
            _viewToFilters[view].Add(peakFilterModel);
        }

        public bool DetatchFilter(ICollectionView view) {
            if (_viewToPredicate.ContainsKey(view)) {
                view.Filter -= _viewToPredicate[view];
                _viewToPredicate.Remove(view);
                _viewToFilters.Remove(view);
                return true;
            }
            return false;
        }

        private Predicate<IFilterable> CreateFilter(PeakSpotNavigatorModel peakSpotNavigatorModel, PeakFilterModel peakFilterModel, FilterEnableStatus status, IMatchResultEvaluator<IFilterable> evaluator) {
            var results = CreateFilters(peakSpotNavigatorModel, peakFilterModel, status, evaluator);
            bool Pred(IFilterable filterable) {
                return results.All(pred => pred(filterable));
            }
            return Pred;
        }

        private List<Predicate<IFilterable>> CreateFilters(PeakSpotNavigatorModel peakSpotNavigatorModel, PeakFilterModel peakFilterModel, FilterEnableStatus status, IMatchResultEvaluator<IFilterable> evaluator) {
            List<Predicate<IFilterable>> results = new List<Predicate<IFilterable>>
            {
                filterable => peakFilterModel.PeakFilter(filterable, evaluator) && filterable.TagCollection.IsSelected(peakSpotNavigatorModel.TagSearchQueryBuilder.CreateQuery())
            };
            if ((status & FilterEnableStatus.Rt) != FilterEnableStatus.None) {
                results.Add(peakSpotNavigatorModel.RtFilter);
            }
            if ((status & FilterEnableStatus.Dt) != FilterEnableStatus.None) {
                results.Add(peakSpotNavigatorModel.DtFilter);
            }
            if ((status & FilterEnableStatus.Mz) != FilterEnableStatus.None) {
                results.Add(peakSpotNavigatorModel.MzFilter);
            }
            if ((status & FilterEnableStatus.Amplitude) != FilterEnableStatus.None) {
                results.Add(peakSpotNavigatorModel.AmplitudeFilter);
            }
            if ((status & FilterEnableStatus.Metabolite) != FilterEnableStatus.None) {
                results.Add(filterable => peakSpotNavigatorModel.MetaboliteFilter(filterable, peakSpotNavigatorModel.MetaboliteFilterKeywords));
            }
            if ((status & FilterEnableStatus.Protein) != FilterEnableStatus.None) {
                results.Add(filterable => peakSpotNavigatorModel.ProteinFilter(filterable, peakSpotNavigatorModel.ProteinFilterKeywords));
            }
            if ((status & FilterEnableStatus.Comment) != FilterEnableStatus.None) {
                results.Add(filterable => peakSpotNavigatorModel.CommentFilter(filterable, peakSpotNavigatorModel.CommentFilterKeywords));
            }

            if ((status & FilterEnableStatus.Adduct) != FilterEnableStatus.None) {
                results.Add(filterable => peakSpotNavigatorModel.AdductFilter(filterable, peakSpotNavigatorModel.AdductFilterKeywords));
            }

            if ((status & FilterEnableStatus.Ontology) != FilterEnableStatus.None) {
                results.Add(filterable => peakSpotNavigatorModel.OntologyFilter(filterable, peakSpotNavigatorModel.OntologyFilterKeywords));
            }
            return results;
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {

                }
                var views = _viewToFilters.Keys.ToArray();
                foreach (var view in views) {
                    DetatchFilter(view);
                }
                _viewToFilters.Clear();
                _viewToPredicate.Clear();
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
