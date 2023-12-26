using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;

namespace CompMs.App.Msdial.Model.Dims
{
    internal sealed class DimsCompoundResult : CompoundResult
    {
        public DimsCompoundResult(MoleculeMsReference msReference, MsScanMatchResult matchResult) : base(msReference, matchResult) {

        }

        public DimsCompoundResult(ICompoundResult compoundResult) : this(compoundResult.MsReference, compoundResult.MatchResult) {
                
        }
    }
}
