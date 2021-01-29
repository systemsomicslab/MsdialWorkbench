using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IAnnotator
    {
        MsScanMatchResult Annotate(IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, MsRefSearchParameterBase parameter = null);
        List<MsScanMatchResult> FindCandidates(IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, MsRefSearchParameterBase parameter = null);
        MsScanMatchResult CalculateScore(IMSScanProperty scan, IReadOnlyList<IsotopicPeak> scanIsotopes, MoleculeMsReference reference, IReadOnlyList<IsotopicPeak> referenceIsotopes, MsRefSearchParameterBase parameter = null);
        MoleculeMsReference Refer(MsScanMatchResult result);
        List<MoleculeMsReference> Search(IMSScanProperty scan, MsRefSearchParameterBase parameter = null);
        void Validate(MsScanMatchResult result, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> scanIsotopes, MoleculeMsReference reference, IReadOnlyList<IsotopicPeak> referenceIsotopes, MsRefSearchParameterBase parameter = null);
    }
}
