using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public class ImmsRepresentativeDataProvider : IDataProvider
    {
        private readonly RawMeasurement rawObj;
        private readonly List<RawSpectrum> representativeSpectrum;

        public ImmsRepresentativeDataProvider(AnalysisFileBean file, bool isGuiProcess = false, int retry = 5) {
            rawObj = LoadMeasurement(file, isGuiProcess, retry);
        }

        public ImmsRepresentativeDataProvider(RawMeasurement rawObj) {
            this.rawObj = rawObj;
            this.representativeSpectrum = SelectRepresentative(rawObj.SpectrumList).OrderBy(spectrum => spectrum.DriftTime).ToList();
        }

        private static RawMeasurement LoadMeasurement(AnalysisFileBean file, bool isGuiProcess, int retry) {
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

        private static List<RawSpectrum> SelectRepresentative(List<RawSpectrum> rawSpectrums) {
            var scannumber = rawSpectrums
                .Where(spectrum => spectrum.MsLevel == 1)
                .GroupBy(spectrum => spectrum.ScanNumber)
                .Argmax(spectrums => spectrums.Sum(spectrum => spectrum.TotalIonCurrent)).Key;
            return rawSpectrums.Where(spectrum => spectrum.ScanNumber == scannumber).ToList();
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return new ReadOnlyCollection<RawSpectrum>(rawObj.SpectrumList);
        }

        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
            return new ReadOnlyCollection<RawSpectrum>(representativeSpectrum);
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
            return new ReadOnlyCollection<RawSpectrum>(rawObj.SpectrumList
                .Where(spectrum => spectrum.MsLevel == level)
                .OrderBy(spectrum => spectrum.DriftTime)
                .ToList());
        }
    }
}
