using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;

namespace CompMs.App.Msdial.Model.Lcimms
{
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
