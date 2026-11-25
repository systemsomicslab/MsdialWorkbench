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
        private readonly int _numThreads;

        /// <param name="numThreads">
        /// The threads this file's peak spotting may use, not the run's "Number of threads": several
        /// files are spotted at once (see FileProcess.PeakSpottingThreadsPerFile). 1 or less takes the
        /// single-thread path of PeakSpottingCore, which yields a different peak list by design.
        /// The multi-thread path writes each mass slice to its own index, so its peaks do not depend
        /// on how many threads it is given.
        /// </param>
        public PeakSpotting(IupacDatabase iupacDB, MsdialGcmsParameter parameter, int numThreads) {
            _iupacDB = iupacDB;
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _numThreads = numThreads;
        }

        public List<ChromatogramPeakFeature> Run(AnalysisFileBean analysisFile, IDataProvider provider, ReportProgress reporter, CancellationToken token) {
            var coreProcess = new PeakSpottingCore(_parameter);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var chromPeakFeatures = coreProcess.Execute3DFeatureDetection(analysisFile, provider, _numThreads, token, reporter, chromatogramRange);
            IsotopeEstimator.Process(chromPeakFeatures, _parameter, _iupacDB);
            return chromPeakFeatures;
        }
    }
}
