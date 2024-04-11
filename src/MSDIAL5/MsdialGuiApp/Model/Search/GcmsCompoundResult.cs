using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;

namespace CompMs.App.Msdial.Model.Search
{
    internal sealed class GcmsCompoundResult : CompoundResult
    {
        public GcmsCompoundResult(MoleculeMsReference msReference, MsScanMatchResult matchResult) : base(msReference, matchResult) {

        }

        public double RiSimilarity => matchResult.RiSimilarity;

        public double RtSimilarity => matchResult.RtSimilarity;
    }
}
