using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.MsdialCore.Algorithm
{
    public class StandardDataProvider : BaseDataProvider
    {
        public StandardDataProvider(IEnumerable<RawSpectrum> spectrums) : base(spectrums) {

        }

        public StandardDataProvider(RawMeasurement rawObj) : this(rawObj.SpectrumList) {

        }

        public StandardDataProvider(AnalysisFileBean file, bool isImagingMs, bool isGuiProcess, int retry, CancellationToken token = default)
            : base(LoadMeasurementAsync(file, false, isImagingMs, isGuiProcess, retry, token)) {

        }
    }

    public class StandardDataProviderFactory
        : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        public StandardDataProviderFactory(int retry = 5, bool isGuiProcess = false) {
            this.retry = retry;
            this.isGuiProcess = isGuiProcess;
        }
        
        private readonly bool isGuiProcess;
        private readonly int retry = 5;

        public IDataProvider Create(AnalysisFileBean file) {
            return new StandardDataProvider(file, false, isGuiProcess, retry);
        }

        public IDataProvider Create(RawMeasurement rawMeasurement) {
            return new StandardDataProvider(rawMeasurement);
        }
    }
}
