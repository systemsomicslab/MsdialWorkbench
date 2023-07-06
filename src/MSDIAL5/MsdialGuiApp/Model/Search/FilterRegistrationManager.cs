using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;

namespace CompMs.App.Msdial.Model.Search
{
    internal sealed class FilterRegistrationManager<T> : IDisposable where T : IFilterable, IAnnotatedObject, IMoleculeProperty, IChromatogramPeak, ISpectrumPeak
    {
        private bool _disposedValue;

        public FilterRegistrationManager(IReadOnlyList<T> spots)
        {
            PeakSpotNavigatorModel = new PeakSpotNavigatorModel((IReadOnlyList<IFilterable>)spots);
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
                if ((status & FilterEnableStatus.Rt) != FilterEnableStatus.None) {
                    PeakSpotFiltering.AttachFilter(PeakSpotNavigatorModel.RtFilterModel, obj => ((IChromatogramPeak)obj)?.ChromXs.RT.Value ?? 0d, collection);
                }
                if ((status & FilterEnableStatus.Dt) != FilterEnableStatus.None) {
                    PeakSpotFiltering.AttachFilter(PeakSpotNavigatorModel.DtFilterModel, obj => ((IChromatogramPeak)obj)?.ChromXs.Drift.Value ?? 0d, collection);
                }
                if ((status & FilterEnableStatus.Mz) != FilterEnableStatus.None) {
                    PeakSpotFiltering.AttachFilter(PeakSpotNavigatorModel.MzFilterModel, obj => ((ISpectrumPeak)obj)?.Mass ?? 0d, collection);
                }
                if ((status & FilterEnableStatus.Amplitude) != FilterEnableStatus.None) {
                    PeakSpotFiltering.AttachFilter(PeakSpotNavigatorModel.AmplitudeFilterModel, obj => ((IFilterable)obj)?.RelativeAmplitudeValue ?? 0d, collection);
                }
                if ((status & FilterEnableStatus.Metabolite) != FilterEnableStatus.None) {
                    PeakSpotFiltering.AttachFilter(PeakSpotNavigatorModel.MetaboliteFilterModel, obj => ((IMoleculeProperty)obj).Name, collection);
                }
                if ((status & FilterEnableStatus.Protein) != FilterEnableStatus.None) {
                    PeakSpotFiltering.AttachFilter(PeakSpotNavigatorModel.ProteinFilterModel, obj => ((IFilterable)obj).Protein, collection);
                }
                if ((status & FilterEnableStatus.Comment) != FilterEnableStatus.None) {
                    PeakSpotFiltering.AttachFilter(PeakSpotNavigatorModel.CommentFilterModel, obj => ((IFilterable)obj).Comment, collection);
                }
                if ((status & FilterEnableStatus.Adduct) != FilterEnableStatus.None) {
                    PeakSpotFiltering.AttachFilter(PeakSpotNavigatorModel.AdductFilterModel, obj => ((IFilterable)obj).AdductIonName, collection);
                }
                if ((status & FilterEnableStatus.Ontology) != FilterEnableStatus.None) {
                    PeakSpotFiltering.AttachFilter(PeakSpotNavigatorModel.OntologyFilterModel, obj => ((IMoleculeProperty)obj).Ontology, collection);
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
