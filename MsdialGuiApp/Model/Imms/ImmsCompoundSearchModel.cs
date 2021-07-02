using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Imms
{
    class ImmsCompoundSearchModel<T> : CompoundSearchModel<T> where T : IMSIonProperty, IMoleculeProperty
    {
        public ImmsCompoundSearchModel(
            IFileBean fileBean,
            T property,
            MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator<IMSIonProperty, IMSScanProperty> annotator,
            MsRefSearchParameterBase parameter = null) : base(
                fileBean,
                property,
                msdecResult,
                isotopes,
                annotator,
                parameter) {
        }

        protected override IEnumerable<CompoundResult> SearchCore() {
            return base.SearchCore().Select(c => new ImmsCompoundResult(c));
        }
    }

    class ImmsCompoundResult : CompoundResult
    {
        public ImmsCompoundResult(MoleculeMsReference msReference, MsScanMatchResult matchResult)
            : base(msReference, matchResult) {

        }

        public ImmsCompoundResult(CompoundResult compound)
            : this(compound.msReference, compound.matchResult) {

        }

        public double CollisionCrossSection => msReference.CollisionCrossSection;
        public double CcsSimilarity => matchResult.CcsSimilarity;
    }
}
