using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.Model.Lcimms
{
    internal sealed class LcimmsCompoundSearchModel : CompoundSearchModel, ICompoundSearchModel
    {
        private readonly PeakSpotModel _peakSpotModel;
        private readonly LcimmsCompoundSearchService _compoundSearchService;

        public LcimmsCompoundSearchModel(IFileBean fileBean, PeakSpotModel peakSpotModel, LcimmsCompoundSearchService compoundSearchService, UndoManager undoManager)
            : base(fileBean, peakSpotModel.PeakSpot, peakSpotModel.MSDecResult, compoundSearchService.CompoundSearchers, undoManager) {
            _peakSpotModel = peakSpotModel;
            _compoundSearchService = compoundSearchService;

            this.ObserveProperty(m => m.SelectedCompoundSearcher).Subscribe(s => compoundSearchService.SelectedCompoundSearcher = s).AddTo(Disposables);
        }

        public override CompoundResultCollection Search() {
            return new CompoundResultCollection
            {
                Results = _compoundSearchService.Search(_peakSpotModel),
            };
        }
    }

    internal sealed class LcimmsCompoundResult : CompoundResult {
        public LcimmsCompoundResult(MoleculeMsReference msReference, MsScanMatchResult matchResult)
            : base(msReference, matchResult) {

        }

        public LcimmsCompoundResult(ICompoundResult compound)
            : this(compound.MsReference, compound.MatchResult) {

        }

        public double CollisionCrossSection => msReference.CollisionCrossSection;
        public double CcsSimilarity => matchResult.CcsSimilarity;
        public double RtSimilarity => matchResult.RtSimilarity;
    }
}
