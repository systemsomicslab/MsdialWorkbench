using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;

namespace CompMs.App.Msdial.Model.Lcms
{
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
