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
        void RunAnnotation(
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<MSDecResult> msdecResult,
            IDataProvider provider,
            int numThread = 1,
            CancellationToken token = default,
            Action<double> reportAction = null);

        Task RunAnnotationAsync(
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<MSDecResult> msdecResult,
            IDataProvider provider,
            int numThread = 1,
            CancellationToken token = default,
            Action<double> reportAction = null);
    }
}
