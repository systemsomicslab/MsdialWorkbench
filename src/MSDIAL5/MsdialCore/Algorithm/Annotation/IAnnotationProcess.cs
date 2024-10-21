using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IAnnotationProcess
    {
        Task RunAnnotationAsync(
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            MSDecResultCollection msdecResult,
            IDataProvider provider,
            int numThread = 1,
            Action<double>? reportAction = null,
            CancellationToken token = default);
    }
}
