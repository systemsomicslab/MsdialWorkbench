using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Imms
{
    internal sealed class ImmsCompoundSearchModel : CompoundSearchModel, IEsiCompoundSearchModel
    {
        public ImmsCompoundSearchModel(IFileBean fileBean, IPeakSpotModel peakSpot, MSDecResult msdecResult, IReadOnlyList<CompoundSearcher> compoundSearchers, UndoManager undoManager)
            : base(fileBean, peakSpot, msdecResult, compoundSearchers, undoManager) {

        }

        protected override IEnumerable<ICompoundResult> SearchCore() {
            return base.SearchCore().Select(c => new ImmsCompoundResult(c));
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
