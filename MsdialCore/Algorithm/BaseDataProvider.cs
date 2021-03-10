using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialCore.Algorithm
{
    public abstract class BaseDataProvider : IDataProvider
    {
        protected readonly RawMeasurement rawObj;

        protected BaseDataProvider(RawMeasurement rawObj) {
            this.rawObj = rawObj;
        }

        protected BaseDataProvider(AnalysisFileBean file, bool isGuiProcess, int retry)
            :this(LoadMeasurement(file, isGuiProcess, retry)) { }

        protected static RawMeasurement LoadMeasurement(AnalysisFileBean file, bool isGuiProcess, int retry) {
            using (var access = new RawDataAccess(file.AnalysisFilePath, 0, isGuiProcess)) {
                for (var i = 0; i < retry; i++) {
                    var rawObj = DataAccess.GetRawDataMeasurement(access);
                    if (rawObj != null) {
                        return rawObj;
                    }
                    Thread.Sleep(5000);
                }
            }
            throw new FileLoadException($"Loading {file.AnalysisFilePath} failed.");
        }

        public virtual ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
            return rawObj.SpectrumList.Where(spectrum => spectrum.MsLevel == 1).ToList().AsReadOnly();
        }

        public virtual ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
            return rawObj.SpectrumList.Where(spectrum => spectrum.MsLevel == level).ToList().AsReadOnly();
        }

        public virtual ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return rawObj.SpectrumList.AsReadOnly();
        }
    }
}
