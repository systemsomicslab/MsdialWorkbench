using CompMs.MsdialCore.DataObj;
using CompMs.Raw.Abstractions;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm
{
    public interface IPeakDetectionProcess
    {
        List<ChromatogramPeakFeature> Detect(AnalysisFileBean analysisFile, IDataProvider provider);
    }
}
