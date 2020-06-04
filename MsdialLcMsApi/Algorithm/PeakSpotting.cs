using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialLcMsApi.Algorithm {
    public class PeakSpotting {

        private PeakSpottingCore CoreProcess;
        public PeakSpotting(double initialProgress, double progressMax) {
            this.CoreProcess = new PeakSpottingCore() { InitialProgress = initialProgress, ProgressMax = progressMax };
        }

        public List<ChromatogramPeakFeature> Run(List<RawSpectrum> spectrumList, MsdialLcmsParameter param, Action<int> reportAction) {
            return CoreProcess.Execute3DFeatureDetection(spectrumList, param, param.RetentionTimeBegin, param.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min, reportAction);
        }
    }
}
