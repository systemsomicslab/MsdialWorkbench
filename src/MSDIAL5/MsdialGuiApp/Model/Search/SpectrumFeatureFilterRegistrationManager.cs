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

        public SpectrumFeatureFilterRegistrationManager(IReadOnlyList<Ms1BasedSpectrumFeature> spectrumFeature, SpectrumFeatureFiltering spectrumFeatureFiltering) {
            PeakSpotFiltering = spectrumFeatureFiltering;
            PeakSpotNavigatorModel = new PeakSpotNavigatorModel(spectrumFeature, spectrumFeatureFiltering.ValueFilterManagers.Select(pair => pair.Filter).ToArray(), spectrumFeatureFiltering.KeywordFilterManagers.Select(pair => pair.Filter).ToArray(), spectrumFeatureFiltering.AmplitudeFilterModel, spectrumFeatureFiltering.TagSearchQueryBuilder);
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
}
