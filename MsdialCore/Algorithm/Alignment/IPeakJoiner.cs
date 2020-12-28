using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public interface IPeakJoiner
    {
        List<AlignmentSpotProperty> Join(IReadOnlyList<AnalysisFileBean> asnalysisFiles, int referenceId, DataAccessor accessor);
    }
}
