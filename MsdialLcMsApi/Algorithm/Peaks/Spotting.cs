using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcMsApi.Algorithm.Peaks
{
    public class Spotting
    {
        private readonly MsdialLcmsParameter _parameter;
        private readonly IupacDatabase _iupacDatabase;

        public Spotting(MsdialLcmsParameter parameter, IupacDatabase iupacDatabase) {
            _parameter = parameter;
            _iupacDatabase = iupacDatabase;
        }

        public Task<List<ChromatogramPeakFeature>> RunAsync(IDataProvider provider, int initialProgress, int totalProgress, Action<int> reportAction = null, CancellationToken token = default) {
            var chromPeakFeatures = new PeakSpotting(initialProgress, initialProgress + totalProgress).Run(provider, _parameter, token, reportAction);
            IsotopeEstimator.Process(chromPeakFeatures, _parameter, _iupacDatabase);
            return Task.FromResult(chromPeakFeatures);
        }
    }
}
