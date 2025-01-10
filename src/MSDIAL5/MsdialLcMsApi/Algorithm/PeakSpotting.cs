using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.MsdialLcMsApi.Algorithm
{
    public sealed class PeakSpotting {

        private readonly double _initialProgress;
        private readonly double _progressMax;
        private readonly AnalysisFileBean _analysisFile;

        public PeakSpotting(AnalysisFileBean file, double initialProgress, double progressMax) {
            _initialProgress = initialProgress;
            _progressMax = progressMax;
            _analysisFile = file;
        }

        [Obsolete("zzz")]
        public List<ChromatogramPeakFeature> Run(IDataProvider provider, MsdialLcmsParameter param, IProgress<int>? progress, CancellationToken token) {
            var coreProcess = new PeakSpottingCore(param);
            var chromatogramRange = new ChromatogramRange(param.RetentionTimeBegin, param.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            //return coreProcess.Execute3DFeatureDetection(provider, param.NumThreads, token, reportAction?.FromRange(_initialProgress, _progressMax), chromatogramRange);
            return coreProcess.Execute3DFeatureDetectionAsync(_analysisFile, provider, chromatogramRange, param.NumThreads == 1 ? 1 : 2, ReportProgress.FromRange(progress, _initialProgress, _progressMax), token).Result;
        }
    }
}
