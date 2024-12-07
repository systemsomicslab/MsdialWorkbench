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

        public FilterRegistrationManager(IReadOnlyList<T> spots, PeakSpotFiltering<T> peakSpotFiltering) {
            PeakSpotFiltering = peakSpotFiltering;
            PeakSpotNavigatorModel = new PeakSpotNavigatorModel(spots, peakSpotFiltering.ValueFilterManagers.Select(pair => pair.Filter).ToArray(), peakSpotFiltering.KeywordFilterManagers.Select(pair => pair.Filter).ToArray(), peakSpotFiltering.AmplitudeFilterModel, peakSpotFiltering.TagSearchQueryBuilder);
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
                PeakSpotFiltering.AttachFilter(collection, peakFilterModel, evaluator, status);
                PeakSpotNavigatorModel.PeakSpotsCollection.Add(collection);
                PeakSpotNavigatorModel.PeakFilters.Add(peakFilterModel);
            });
        }

        public void DetatchFilter(ICollectionView view) {
            PeakSpotFiltering.DetatchFilter(view);
            var idx = PeakSpotNavigatorModel.PeakSpotsCollection.IndexOf(view);
            PeakSpotNavigatorModel.PeakSpotsCollection.RemoveAt(idx);
            PeakSpotNavigatorModel.PeakFilters.RemoveAt(idx);
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    PeakSpotNavigatorModel.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }

    public class ValueFilterManager<T> where T: IAnnotatedObject, IFilterable {
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

    public class KeywordFilterManager<T> : IDisposable where T : IAnnotatedObject, IFilterable
    {
        private readonly KeywordFilterModel _model;
            private readonly FilterEnableStatus _status;
        private readonly Func<T, string> _convert;

        public KeywordFilterManager(KeywordFilterModel model, FilterEnableStatus status, Func<T, string> convert) {
                _model = model;
                _status = status;
                _convert = convert;
            }

        public KeywordFilterModel Filter => _model;

            public IEnumerable<T> Apply(IEnumerable<T> peaks, FilterEnableStatus status) {
                if ((status & _status) == _status) {
                return peaks.Where(p => _model.Match(_convert(p)));
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

        public void Dispose() {
            _model.Dispose();
        }
    }
}
