using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm;
using System;
using System.Threading;

namespace CompMs.MsdialLcMsApi.Process
{
    internal sealed class PeakPickProcess
    {
        private readonly IMsdialDataStorage<MsdialLcmsParameter> _storage;

        public PeakPickProcess(IMsdialDataStorage<MsdialLcmsParameter> storage) {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public ChromatogramPeakFeatureCollection Pick(IDataProvider provider, CancellationToken token, Action<int> reportAction) {
            var chromPeakFeatures = new PeakSpotting(0, 30).Run(provider, _storage.Parameter, token, reportAction);
            IsotopeEstimator.Process(chromPeakFeatures, _storage.Parameter, _storage.IupacDatabase);
            return new ChromatogramPeakFeatureCollection(chromPeakFeatures);
        }
    }
}
