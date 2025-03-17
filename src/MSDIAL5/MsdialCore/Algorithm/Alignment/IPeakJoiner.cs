using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Alignment;

public interface IPeakJoiner
{
    List<AlignmentSpotProperty> Join(IReadOnlyList<AnalysisFileBean> asnalysisFiles, int referenceId, DataAccessor accessor);
}
