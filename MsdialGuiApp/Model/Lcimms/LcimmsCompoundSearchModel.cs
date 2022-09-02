using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Lcimms
{
    internal sealed class LcimmsCompoundSearchModel<T> : CompoundSearchModel<T> where T : IMSIonProperty, IMoleculeProperty
    {
        public LcimmsCompoundSearchModel(
            IFileBean fileBean,
            T property,
            MSDecResult msdecResult,
            IReadOnlyList<CompoundSearcher> compoundSearchers)
            :base(fileBean, property, msdecResult, compoundSearchers) {

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
