using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm
{
    public interface IPeakDetectionProcess
    {
        List<ChromatogramPeakFeature> Detect(IDataProvider provider);
    }
}
