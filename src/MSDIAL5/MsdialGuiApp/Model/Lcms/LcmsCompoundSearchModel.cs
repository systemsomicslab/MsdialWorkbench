using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Lcms
{
    internal sealed class LcmsCompoundSearchModel : CompoundSearchModel, ICompoundSearchModel
    {
        private readonly LcmsCompoundSearchService _compoundSearchService;
        private readonly PeakSpotModel _peakSpot;

        public LcmsCompoundSearchModel(
            IFileBean fileBean,
            PeakSpotModel peakSpot,
            LcmsCompoundSearchService compoundSearchService,
            SetAnnotationService setAnnotationService)
            : base(fileBean, peakSpot.PeakSpot, peakSpot.MSDecResult, compoundSearchService.CompoundSearchers, setAnnotationService) {
            _compoundSearchService = compoundSearchService;
            _peakSpot = peakSpot;

            this.ObserveProperty(m => m.SelectedCompoundSearcher).Subscribe(s => compoundSearchService.SelectedCompoundSearcher = s).AddTo(Disposables);
        }

        public override CompoundResultCollection Search() {
            return new CompoundResultCollection
            {
                Results = _compoundSearchService.Search(_peakSpot),
            };
        }
    }

    internal sealed class LcmsCompoundResult : CompoundResult
    {
        public LcmsCompoundResult(MoleculeMsReference msReference, MsScanMatchResult matchResult)
            : base(msReference, matchResult) {

        }

        public LcmsCompoundResult(ICompoundResult compound)
            : this(compound.MsReference, compound.MatchResult) {

        }

        public double RtSimilarity => matchResult.RtSimilarity;
    }
}
