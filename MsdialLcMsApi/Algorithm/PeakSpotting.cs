using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.MsdialLcMsApi.Algorithm
{
    public class PeakSpotting {

        private PeakSpottingCore CoreProcess;
        public PeakSpotting(double initialProgress, double progressMax) {
            this.CoreProcess = new PeakSpottingCore() { InitialProgress = initialProgress, ProgressMax = progressMax };
        }

        public List<ChromatogramPeakFeature> Run(IDataProvider provider, MsdialLcmsParameter param, CancellationToken token, Action<int> reportAction) {
            return CoreProcess.Execute3DFeatureDetection(provider, param, param.RetentionTimeBegin, param.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min, param.NumThreads, token, reportAction);
        }
    }
}
