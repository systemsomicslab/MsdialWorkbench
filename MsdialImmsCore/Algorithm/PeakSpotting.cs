using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public class PeakSpotting
    {

        private PeakSpottingCore CoreProcess;

        public PeakSpotting(double initialProgress, double progressMax) {
            CoreProcess = new PeakSpottingCore()
            {
                InitialProgress = initialProgress,
                ProgressMax = progressMax,
            };
        }

        public List<ChromatogramPeakFeature> Run(RawMeasurement rawObj, MsdialImmsParameter param, Action<int> reportAction = null) {
            return CoreProcess.Execute3DFeatureDetection(rawObj, param, param.DriftTimeBegin, param.DriftTimeEnd, ChromXType.Drift, ChromXUnit.Msec, reportAction);
        }
    }
}
