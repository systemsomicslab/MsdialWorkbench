using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialGcMsApi.Algorithm
{
    public sealed class PeakSpotting {
        private readonly IupacDatabase _iupacDB;
        private readonly MsdialGcmsParameter _parameter;

        public PeakSpotting(IupacDatabase iupacDB, MsdialGcmsParameter parameter) {
            _iupacDB = iupacDB;
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public async Task<List<ChromatogramPeakFeature>> RunAsync(AnalysisFileBean analysisFile, IDataProvider provider, ReportProgress reporter, CancellationToken token = default) {
            var coreProcess = new PeakSpottingCore(_parameter);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var chromPeakFeatures = await coreProcess.Execute3DFeatureDetectionAsync(analysisFile, provider, chromatogramRange, _parameter.NumThreads, reporter, token).ConfigureAwait(false);
            IsotopeEstimator.Process(chromPeakFeatures, _parameter, _iupacDB);
            return chromPeakFeatures;
        }
    }
}
