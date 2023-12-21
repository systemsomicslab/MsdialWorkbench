using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Search;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.Model.Dims
{
    internal sealed class DimsCompoundSearchModel : CompoundSearchModel
    {
        private readonly DimsCompoundSearchService _compoundSearchService;
        private readonly PeakSpotModel _peakSpot;

        public DimsCompoundSearchModel(IFileBean fileBean, PeakSpotModel peakSpotModel, DimsCompoundSearchService compoundSearchService, SetAnnotationService setAnnotationService)
            : base(fileBean, peakSpotModel.PeakSpot, peakSpotModel.MSDecResult, compoundSearchService.CompoundSearchers, setAnnotationService) {
            _peakSpot = peakSpotModel;
            _compoundSearchService = compoundSearchService;

            this.ObserveProperty(m => m.SelectedCompoundSearcher).Subscribe(s => compoundSearchService.SelectedCompoundSearcher = s).AddTo(Disposables);
        }

        public override CompoundResultCollection Search() {
            return new CompoundResultCollection
            {
                Results = _compoundSearchService.Search(_peakSpot),
            };
        }
    }
}
