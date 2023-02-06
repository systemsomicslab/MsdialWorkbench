using CompMs.Common.DataObj;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.RawDataHandler.Core;

namespace CompMs.MsdialLcImMsApi.Algorithm
{
    public class LcimmsAccumulateDataProvider : BaseDataProvider
    {
        public LcimmsAccumulateDataProvider(AnalysisFileBean file, bool isGuiProcess = false, int retry = 5) : 
            base(SpectrumParser.GetAccumulatedMs1Spectrum(LoadMeasurement(file, false, false, isGuiProcess, retry).SpectrumList)) {
            
        }

        //public LcimmsAccumulateDataProvider(RawMeasurement rawObj) : base(rawObj.AccumulatedSpectrumList) {

        //}

        public LcimmsAccumulateDataProvider(RawMeasurement rawObj) : base(SpectrumParser.GetAccumulatedMs1Spectrum(rawObj.SpectrumList)) {

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
