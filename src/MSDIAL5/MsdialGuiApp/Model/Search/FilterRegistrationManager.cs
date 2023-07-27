using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace CompMs.App.Msdial.Model.Search
{
    internal sealed class FilterRegistrationManager<T> : IDisposable where T : IFilterable, IAnnotatedObject, IMoleculeProperty, IChromatogramPeak, ISpectrumPeak
    {
        private bool _disposedValue;
        private readonly List<ValueFilterManager> _valueFilterManagers;
        private readonly List<KeywordFilterManager> _keywordFilterManagers;

        public FilterRegistrationManager(IReadOnlyList<T> spots, FilterEnableStatus status) {
            var valueFilterManagers = new List<ValueFilterManager>();
            if ((status & FilterEnableStatus.Mz) != FilterEnableStatus.None) {
                var MzFilterModel = new ValueFilterModel("m/z range", 0d, 1d);
                valueFilterManagers.Add(new ValueFilterManager(MzFilterModel, FilterEnableStatus.Mz, obj => ((ISpectrumPeak)obj)?.Mass ?? 0d));
            }
            if ((status & FilterEnableStatus.Rt) != FilterEnableStatus.None) {
                var RtFilterModel = new ValueFilterModel("Retention time", 0d, 1d);
                valueFilterManagers.Add(new ValueFilterManager(RtFilterModel, FilterEnableStatus.Rt, obj => ((IChromatogramPeak)obj)?.ChromXs.RT.Value ?? 0d));
            }
            if ((status & FilterEnableStatus.Dt) != FilterEnableStatus.None) {
                var DtFilterModel = new ValueFilterModel("Mobility", 0d, 1d);
                valueFilterManagers.Add(new ValueFilterManager(DtFilterModel, FilterEnableStatus.Dt, obj => ((IChromatogramPeak)obj)?.ChromXs.Drift.Value ?? 0d));
            }
            _valueFilterManagers = valueFilterManagers;
            var keywordFilterManagers = new List<KeywordFilterManager>();
            if ((status & FilterEnableStatus.Metabolite) != FilterEnableStatus.None) {
                var MetaboliteFilterModel = new KeywordFilterModel("Name filter");
                keywordFilterManagers.Add(new KeywordFilterManager(MetaboliteFilterModel, FilterEnableStatus.Metabolite, obj => ((IMoleculeProperty)obj).Name));
            }
            if ((status & FilterEnableStatus.Protein) != FilterEnableStatus.None) {
                var ProteinFilterModel = new KeywordFilterModel("Protein filter", KeywordFilteringType.KeepIfWordIsNull);
                keywordFilterManagers.Add(new KeywordFilterManager(ProteinFilterModel, FilterEnableStatus.Protein, obj => ((IFilterable)obj).Protein));
            }
            if ((status & FilterEnableStatus.Ontology) != FilterEnableStatus.None) {
                var OntologyFilterModel = new KeywordFilterModel("Ontology filter", KeywordFilteringType.ExactMatch);
                keywordFilterManagers.Add(new KeywordFilterManager(OntologyFilterModel, FilterEnableStatus.Ontology, obj => ((IMoleculeProperty)obj).Ontology));
            }
            if ((status & FilterEnableStatus.Adduct) != FilterEnableStatus.None) {
                var AdductFilterModel = new KeywordFilterModel("Adduct filter", KeywordFilteringType.KeepIfWordIsNull);
                keywordFilterManagers.Add(new KeywordFilterManager(AdductFilterModel, FilterEnableStatus.Adduct, obj => ((IFilterable)obj).AdductIonName));
            }
            if ((status & FilterEnableStatus.Comment) != FilterEnableStatus.None) {
                var CommentFilterModel = new KeywordFilterModel("Comment filter");
                keywordFilterManagers.Add(new KeywordFilterManager(CommentFilterModel, FilterEnableStatus.Comment, obj => ((IFilterable)obj).Comment));
            }
            _keywordFilterManagers = keywordFilterManagers;
            PeakSpotNavigatorModel = new PeakSpotNavigatorModel((IReadOnlyList<IFilterable>)spots, valueFilterManagers.Select(pair => pair.Filter).ToArray(), keywordFilterManagers.Select(pair => pair.Filter).ToArray());
            PeakSpotFiltering = new PeakSpotFiltering<T>();
        }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public PeakSpotFiltering<T> PeakSpotFiltering { get; }

        public void AttachFilter(IEnumerable<T> peaks, PeakFilterModel peakFilterModel, IMatchResultEvaluator<T> evaluator, FilterEnableStatus status) {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ICollectionView collection = CollectionViewSource.GetDefaultView(peaks);
                if (collection is ListCollectionView list) {
                    list.IsLiveFiltering = true;
                }
                PeakSpotFiltering.AttachFilter(collection, peakFilterModel, PeakSpotNavigatorModel.TagSearchQueryBuilder, evaluator);
                if ((status & FilterEnableStatus.Amplitude) != FilterEnableStatus.None) {
                    PeakSpotFiltering.AttachFilter(PeakSpotNavigatorModel.AmplitudeFilterModel, obj => ((IFilterable)obj)?.RelativeAmplitudeValue ?? 0d, collection);
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
            private readonly Func<T, double> _convert;

            public ValueFilterManager(ValueFilterModel model, FilterEnableStatus status, Func<T, double> convert)
            {
                _model = model;
                _status = status;
                _convert = convert;
            }

            public ValueFilterModel Filter => _model;

            public IEnumerable<T> Apply(IEnumerable<T> peaks, FilterEnableStatus status) {
                if ((status & _status) == _status) {
                    return peaks.Where(p => _model.Contains(_convert(p)));
                }
                return peaks;
            }

            public void AttchFilter(PeakSpotFiltering<T> filtering, ICollectionView collection) {
                filtering.AttachFilter(_model, _convert, collection);
            }

            public bool TryAttachFilter(PeakSpotFiltering<T> filtering, ICollectionView collection, FilterEnableStatus status) {
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
            private readonly Func<T, string> _convert;

            public KeywordFilterManager(KeywordFilterModel model, FilterEnableStatus status, Func<T, string> convert)
            {
                _model = model;
                _status = status;
                _convert = convert;
            }

            public KeywordFilterModel Filter => _model;

            public void AttchFilter(PeakSpotFiltering<T> filtering, ICollectionView collection) {
                filtering.AttachFilter(_model, _convert, collection);
            }

            public bool TryAttachFilter(PeakSpotFiltering<T> filtering, ICollectionView collection, FilterEnableStatus status) {
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
