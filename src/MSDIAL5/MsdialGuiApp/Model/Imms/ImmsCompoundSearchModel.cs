using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.Model.Imms
{
    internal sealed class ImmsCompoundSearchModel : CompoundSearchModel, ICompoundSearchModel
    {
        private readonly ImmsCompoundSearchService _compoundSearchService;
        private readonly PeakSpotModel _peakSpot;

        public ImmsCompoundSearchModel(IFileBean fileBean, PeakSpotModel peakSpotModel, ImmsCompoundSearchService compoundSearchService, SetAnnotationService setAnnotationService)
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

    internal sealed class ImmsCompoundResult : CompoundResult
    {
        public ImmsCompoundResult(MoleculeMsReference msReference, MsScanMatchResult matchResult)
            : base(msReference, matchResult) {

        }

        public ImmsCompoundResult(ICompoundResult compound)
            : this(compound.MsReference, compound.MatchResult) {

        }

        public double CollisionCrossSection => msReference.CollisionCrossSection;
        public double CcsSimilarity => matchResult.CcsSimilarity;
    }
}
