using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.MsdialGcMsApi.Algorithm {
    public sealed class PeakSpotting {
        private readonly IupacDatabase _iupacDB;
        private readonly MsdialGcmsParameter _parameter;

        public PeakSpotting(IupacDatabase iupacDB, MsdialGcmsParameter parameter) {
            _iupacDB = iupacDB;
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public List<ChromatogramPeakFeature> Run(AnalysisFileBean analysisFile, IDataProvider provider, ReportProgress reporter, CancellationToken token) {
            var coreProcess = new PeakSpottingCore(_parameter);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var chromPeakFeatures = coreProcess.Execute3DFeatureDetection(analysisFile, provider, _parameter.NumThreads, token, reporter, chromatogramRange);
            IsotopeEstimator.Process(chromPeakFeatures, _parameter, _iupacDB);
            return chromPeakFeatures;
        }
    }
}
