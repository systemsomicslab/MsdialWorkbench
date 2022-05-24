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

        public List<ChromatogramPeakFeature> Run(IDataProvider provider, ReportProgress reporter, CancellationToken token) {
            var coreProcess = new PeakSpottingCore() { InitialProgress = reporter.InitialProgress, ProgressMax = reporter.ProgressMax, };
            var chromPeakFeatures = coreProcess.Execute3DFeatureDetection(provider, _parameter, _parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min, _parameter.NumThreads, token, reporter.ReportAction);
            IsotopeEstimator.Process(chromPeakFeatures, _parameter, _iupacDB);
            return chromPeakFeatures;
        }
    }
}
