using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Lcms
{
    internal sealed class LcmsCompoundSearchModel : CompoundSearchModel, IEsiCompoundSearchModel
    {
        public LcmsCompoundSearchModel(
            IFileBean fileBean,
            IPeakSpotModel peakSpot,
            MSDecResult msdecResult,
            IReadOnlyList<CompoundSearcher> compoundSearcher,
            UndoManager undoManager)
            : base(fileBean, peakSpot, msdecResult, compoundSearcher, undoManager) {
             
        }

        protected override IEnumerable<ICompoundResult> SearchCore() {
            return base.SearchCore().Select(c => new LcmsCompoundResult(c));
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
