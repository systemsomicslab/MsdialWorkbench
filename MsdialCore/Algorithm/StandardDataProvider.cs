using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm
{
    public class StandardDataProvider : BaseDataProvider
    {
        public StandardDataProvider(RawMeasurement rawObj) : base(rawObj) {

        }

        public StandardDataProvider(AnalysisFileBean file, bool isGuiProcess, int retry) : this(LoadMeasurement(file, isGuiProcess, retry)) {

        }

        private ReadOnlyCollection<RawSpectrum> ms1Cache = null;
        public override ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
            if (ms1Cache != null)
                return ms1Cache;
            return ms1Cache = rawObj.SpectrumList.Where(spectrum => spectrum.MsLevel == 1).ToList().AsReadOnly();
        }

        private ReadOnlyCollection<RawSpectrum> spectrumCache = null;
        public override ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            if (spectrumCache != null)
                return spectrumCache;
            return spectrumCache = rawObj.SpectrumList.AsReadOnly();
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
            return new StandardDataProvider(file, isGuiProcess, retry);
        }

        public IDataProvider Create(RawMeasurement rawMeasurement) {
            return new StandardDataProvider(rawMeasurement);
        }
    }
}
