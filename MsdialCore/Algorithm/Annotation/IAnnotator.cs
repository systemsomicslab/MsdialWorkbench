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
        MsScanMatchResult Annotate(IMSScanProperty scan, IMoleculeMsProperty property, IReadOnlyList<IsotopicPeak> isotopes, MsRefSearchParameterBase parameter = null);
        List<MsScanMatchResult> FindCandidates(IMSScanProperty scan, IMoleculeMsProperty property, IReadOnlyList<IsotopicPeak> isotopes, MsRefSearchParameterBase parameter = null);
        MsScanMatchResult CalculateScore(IMSScanProperty scan, IMoleculeMsProperty property, IReadOnlyList<IsotopicPeak> isotopes, MoleculeMsReference reference, MsRefSearchParameterBase parameter = null);
        MoleculeMsReference Refer(MsScanMatchResult result);
        List<MoleculeMsReference> Search(IMoleculeMsProperty property, MsRefSearchParameterBase parameter = null);
        void Validate(MsScanMatchResult result, IMSScanProperty scan, IMoleculeMsProperty property, IReadOnlyList<IsotopicPeak> isotopes, MoleculeMsReference reference, MsRefSearchParameterBase parameter = null);
    }
}
