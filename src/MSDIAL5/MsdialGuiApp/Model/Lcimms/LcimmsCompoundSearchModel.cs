using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Lcimms
{
    internal sealed class LcimmsCompoundSearchModel : CompoundSearchModel, ICompoundSearchModel
    {
        public LcimmsCompoundSearchModel(
            IFileBean fileBean,
            IPeakSpotModel peakSpot,
            MSDecResult msdecResult,
            IReadOnlyList<CompoundSearcher> compoundSearchers,
            UndoManager undoManager)
            : base(fileBean, peakSpot, msdecResult, compoundSearchers, undoManager) {

        }

        protected override IEnumerable<ICompoundResult> SearchCore() {
            return base.SearchCore().Select(c => new LcimmsCompoundResult(c));
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
