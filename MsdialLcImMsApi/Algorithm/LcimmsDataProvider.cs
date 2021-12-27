using CompMs.Common.DataObj;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.MsdialLcImMsApi.Algorithm
{
    public class LcimmsAccumulateDataProvider : BaseDataProvider
    {
        public LcimmsAccumulateDataProvider(IEnumerable<RawSpectrum> spectrums) : base(spectrums) {

        }

        public LcimmsAccumulateDataProvider(AnalysisFileBean file, bool isGuiProcess = false, int retry = 5) : base(LoadMeasurement(file, false, isGuiProcess, retry).AccumulatedSpectrumList) {
            
        }

        public LcimmsAccumulateDataProvider(RawMeasurement rawObj) : base(rawObj.AccumulatedSpectrumList) {

        }
    }

    public class LcimmsAccumulateDataProviderFactory : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        public LcimmsAccumulateDataProviderFactory(int retry = 5, bool isGuiProcess = false) {
            this.retry = retry;
            this.isGuiProcess = isGuiProcess;
        }

        private readonly int retry;
        private readonly bool isGuiProcess;

        public LcimmsAccumulateDataProvider Create(AnalysisFileBean file) {
            return new LcimmsAccumulateDataProvider(file, isGuiProcess, retry);
        }

        public LcimmsAccumulateDataProvider Create(RawMeasurement rawMeasurement) {
            return new LcimmsAccumulateDataProvider(rawMeasurement);
        }

        IDataProvider IDataProviderFactory<AnalysisFileBean>.Create(AnalysisFileBean source) => Create(source);
        IDataProvider IDataProviderFactory<RawMeasurement>.Create(RawMeasurement source) => Create(source);
    }

}
