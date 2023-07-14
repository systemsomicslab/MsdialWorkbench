using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace CompMs.App.Msdial.Model.Search
{
    internal sealed class SpectrumFeatureFilterRegistrationManager : IDisposable
    {
        private bool _disposedValue;
        private readonly List<ValueFilterManager> _valueFilterManagers;
        private readonly List<KeywordFilterManager> _keywordFilterManagers;

        public SpectrumFeatureFilterRegistrationManager(IReadOnlyList<Ms1BasedSpectrumFeature> spectrumFeature) {
            var valueFilterManagers = new List<ValueFilterManager>();
            var RtFilterModel = new ValueFilterModel("Retention time", 0d, 1d);
            valueFilterManagers.Add(new ValueFilterManager(RtFilterModel, FilterEnableStatus.Rt, (Ms1BasedSpectrumFeature f) => f?.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value ?? 0d));
            _valueFilterManagers = valueFilterManagers;
            var keywordFilterManagers = new List<KeywordFilterManager>();
            var MetaboliteFilterModel = new KeywordFilterModel("Name filter");
            keywordFilterManagers.Add(new KeywordFilterManager(MetaboliteFilterModel, FilterEnableStatus.Metabolite, (Ms1BasedSpectrumFeature f) => f.Molecule.Name));
            var OntologyFilterModel = new KeywordFilterModel("Ontology filter", KeywordFilteringType.ExactMatch);
            keywordFilterManagers.Add(new KeywordFilterManager(OntologyFilterModel, FilterEnableStatus.Ontology, (Ms1BasedSpectrumFeature f) => f.Molecule.Ontology));
            var CommentFilterModel = new KeywordFilterModel("Comment filter");
            keywordFilterManagers.Add(new KeywordFilterManager(CommentFilterModel, FilterEnableStatus.Comment, (Ms1BasedSpectrumFeature f) => f.Comment));
            _keywordFilterManagers = keywordFilterManagers;
            PeakSpotNavigatorModel = new PeakSpotNavigatorModel(spectrumFeature, valueFilterManagers.Select(pair => pair.Filter).ToArray(), keywordFilterManagers.Select(pair => pair.Filter).ToArray());
            PeakSpotFiltering = new SpectrumFeatureFiltering();
        }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public SpectrumFeatureFiltering PeakSpotFiltering { get; }

        public void AttachFilter(IEnumerable<Ms1BasedSpectrumFeature> peaks, PeakFilterModel peakFilterModel, IMatchResultEvaluator<Ms1BasedSpectrumFeature> evaluator, FilterEnableStatus status) {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ICollectionView collection = CollectionViewSource.GetDefaultView(peaks);
                if (collection is ListCollectionView list) {
                    list.IsLiveFiltering = true;
                }
                PeakSpotFiltering.AttachFilter(collection, peakFilterModel, PeakSpotNavigatorModel.TagSearchQueryBuilder, evaluator);
                if ((status & FilterEnableStatus.Amplitude) != FilterEnableStatus.None) {
                    PeakSpotFiltering.AttachFilter(PeakSpotNavigatorModel.AmplitudeFilterModel, (Ms1BasedSpectrumFeature f) => f?.QuantifiedChromatogramPeak.PeakShape.AmplitudeOrderValue ?? 0d, collection);
                }
                foreach (var valueFilterManager in _valueFilterManagers) {
                    valueFilterManager.TryAttachFilter(PeakSpotFiltering, collection, status);
                }
                foreach (var keywordFilterManager in _keywordFilterManagers) {
                    keywordFilterManager.TryAttachFilter(PeakSpotFiltering, collection, status);
                }

                PeakSpotNavigatorModel.PeakSpotsCollection.Add(collection);
                PeakSpotNavigatorModel.PeakFilters.Add(peakFilterModel);
            });
        }

        public void DetatchFilter(ICollectionView view) {
            PeakSpotFiltering.DetatchFilter(view);
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    PeakSpotFiltering.Dispose();
                    PeakSpotNavigatorModel.Dispose();
                    foreach (var manager in _keywordFilterManagers) {
                        manager.Dispose();
                    }
                }
                _keywordFilterManagers.Clear();
                _valueFilterManagers.Clear();
                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        class ValueFilterManager {
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

        class KeywordFilterManager : IDisposable {
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
