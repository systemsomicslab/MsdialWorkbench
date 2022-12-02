using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
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
            IReadOnlyList<CompoundSearcher> compoundSearchers)
            : base(fileBean, peakSpot, msdecResult, compoundSearchers) {

        }

        public override CompoundResultCollection Search() {
            return new CompoundResultCollection
            {
                Results = SearchCore().Select(c => new LcimmsCompoundResult(c)).ToList<ICompoundResult>(),
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
