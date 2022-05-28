using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.MsdialLcMsApi.Algorithm
{
    public class PeakSpotting {

        private readonly double _initialProgress;
        private readonly double _progressMax;

        public PeakSpotting(double initialProgress, double progressMax) {
            _initialProgress = initialProgress;
            _progressMax = progressMax;
        }

        public List<ChromatogramPeakFeature> Run(IDataProvider provider, MsdialLcmsParameter param, CancellationToken token, Action<int> reportAction) {
            var coreProcess = new PeakSpottingCore(param);
            var chromatogramRange = new ChromatogramRange(param.RetentionTimeBegin, param.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            return coreProcess.Execute3DFeatureDetection(provider, param.NumThreads, token, reportAction?.FromRange(_initialProgress, _progressMax), chromatogramRange);
        }
    }
}
