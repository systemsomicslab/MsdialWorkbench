using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IAnnotator<in T, in U>
        : IMatchResultRefer, IRestorableRefer
        where T : IMSProperty
        where U : IMSScanProperty
    {
        MsScanMatchResult Annotate(T property, U scan, IReadOnlyList<IsotopicPeak> isotopes, MsRefSearchParameterBase parameter = null);
        List<MsScanMatchResult> FindCandidates(T property, U scan, IReadOnlyList<IsotopicPeak> isotopes, MsRefSearchParameterBase parameter = null);
        MsScanMatchResult CalculateScore(T property, U scan, IReadOnlyList<IsotopicPeak> isotopes, MoleculeMsReference reference, MsRefSearchParameterBase parameter = null);
        List<MoleculeMsReference> Search(T property, MsRefSearchParameterBase parameter = null);
        void Validate(MsScanMatchResult result, T property, U scan, IReadOnlyList<IsotopicPeak> isotopes, MoleculeMsReference reference, MsRefSearchParameterBase parameter = null);
    }
}
