using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Lcms
{
    public class LcmsCompoundSearchModel<T> : CompoundSearchModel<T> where T : IMSIonProperty, IMoleculeProperty
    {
        public LcmsCompoundSearchModel(
            IFileBean fileBean,
            T property,
            MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> annotatorContainer)
            : base(fileBean, property, msdecResult, isotopes, annotatorContainer) {
             
        }

        public override CompoundResultCollection Search() {
            return new LcmsCompoundResultCollection
            {
                Results = SearchCore().Select(c => new LcmsCompoundResult(c)).ToList<ICompoundResult>(),
            };
        }
    }

    public class LcmsCompoundResult : CompoundResult
    {
        public LcmsCompoundResult(MoleculeMsReference msReference, MsScanMatchResult matchResult)
            : base(msReference, matchResult) {

        }

        public LcmsCompoundResult(ICompoundResult compound)
            : this(compound.MsReference, compound.MatchResult) {

        }

        public double RtSimilarity => matchResult.RtSimilarity;
    }

    public class LcmsCompoundResultCollection : CompoundResultCollection
    {

    }
}
