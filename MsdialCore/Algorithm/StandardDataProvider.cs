using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm
{
    class StandardDataProvider : BaseDataProvider
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
}
