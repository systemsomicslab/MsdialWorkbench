using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Common;
using CompMs.MsdialDimsCore.MsmsAll;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialDimsCore {

    public enum ProcessType { MSMSALL }
    public class ProcessError {
        public string Messeage { get; set; } = string.Empty;
        public bool IsErrorOccured { get; set; } = false;
        public ProcessError() { }
        public ProcessError(bool isError, string message) {
            this.Messeage = message;
            this.IsErrorOccured = isError;
        }
    }
    public class ProcessFile {
        public void Run(string filepath, MsdialDimsParameter param, ProcessType type = ProcessType.MSMSALL) {
            switch (type) {
                case ProcessType.MSMSALL:
                    var msmsAllProcess = new MsmsAllProcess(filepath, param);
                    var error = msmsAllProcess.Run();
                    break;
            }
        }

        public static void Run(
            AnalysisFileBean file,
            MsdialDataStorage container,
            bool isGuiProcess = false,
            Action<int> reportAction = null,
            CancellationToken token = default) {

            var param = (MsdialDimsParameter)container.ParameterBase;
            var mspDB = container.MspDB.OrderBy(reference => reference.PrecursorMz).ToList();
            var textDB = container.TextDB.OrderBy(reference => reference.PrecursorMz).ToList();
            var isotopeTextDB = container.IsotopeTextDB;
            var iupacDB = container.IupacDatabase;
            var filepath = file.AnalysisFilePath;
            var fileID = file.AnalysisFileId;

            using (var access = new RawDataAccess(filepath, 0, isGuiProcess)) {

                // parse raw data
                Console.WriteLine("Loading spectral information");
                var rawObj = LoadRawMeasurement(access);
                var spectrumList = rawObj.SpectrumList;

                // faeture detections
                Console.WriteLine("Peak picking started");
                var ms1Spectrum = spectrumList
                    .Where(spec => spec.MsLevel == 1 && !spec.Spectrum.IsEmptyOrNull())
                    .Argmax(spec => spec.Spectrum.Length);
                var chromPeaks = DataAccess.ConvertRawPeakElementToChromatogramPeakList(ms1Spectrum.Spectrum);
                var sChromPeaks = DataAccess.GetSmoothedPeaklist(chromPeaks, param.SmoothingMethod, param.SmoothingLevel);
                var peakPickResults = PeakDetection.PeakDetectionVS1(sChromPeaks, param.MinimumDatapoints, param.MinimumAmplitude);
                var chromFeatures = ConvertPeaksToPeakFeatures(peakPickResults, ms1Spectrum, spectrumList);

                if (chromFeatures.Count == 0) return;
                IsotopeEstimator.Process(chromFeatures, param, iupacDB);
                SetSpectrumPeaks(chromFeatures, spectrumList);

                Console.WriteLine("Annotation started");
                foreach (var feature in chromFeatures) {
                    AnnotationProcess.Run(feature, mspDB, textDB, param.MspSearchParam, param.TargetOmics, null, out _, out _);
                }

                new PeakCharacterEstimator(90, 10).Process(spectrumList, chromFeatures, null, param, reportAction);

                var paifile = file.PeakAreaBeanInformationFilePath;
                MsdialSerializer.SaveChromatogramPeakFeatures(paifile, chromFeatures);

                reportAction?.Invoke(100);
            }
        }

        private static RawMeasurement LoadRawMeasurement(RawDataAccess access) {
            foreach (var _ in Enumerable.Range(0, 5)) {
                var rawObj = DataAccess.GetRawDataMeasurement(access);
                if (rawObj != null)
                    return rawObj;
                Thread.Sleep(2000);
            }

            throw new FileLoadException($"Loading {access.Filepath} failed.");
        }

        private static List<ChromatogramPeakFeature> ConvertPeaksToPeakFeatures(List<PeakDetectionResult> peakPickResults, RawSpectrum ms1Spectrum, List<RawSpectrum> allSpectra) {
            var peakFeatures = new List<ChromatogramPeakFeature>();
            var ms2SpecObjects = allSpectra
                .Where(spectra => spectra.MsLevel == 2 && spectra.Precursor != null)
                .OrderBy(spectra => spectra.Precursor.SelectedIonMz).ToList();

            foreach (var result in peakPickResults) {
                var peakFeature = DataAccess.GetChromatogramPeakFeature(result, ChromXType.Mz, ChromXUnit.Mz, ms1Spectrum.Spectrum[result.ScanNumAtPeakTop].Mz);
                var chromScanID = peakFeature.ChromScanIdTop;
                peakFeature.IonMode = ms1Spectrum.ScanPolarity == ScanPolarity.Positive ? IonMode.Positive : IonMode.Negative;
                peakFeature.PrecursorMz = ms1Spectrum.Spectrum[chromScanID].Mz;
                peakFeature.Mass = ms1Spectrum.Spectrum[chromScanID].Mz;
                peakFeature.ChromXs = new ChromXs(peakFeature.Mass, ChromXType.Mz, ChromXUnit.Mz);
                peakFeature.ChromXsTop = new ChromXs(peakFeature.Mass, ChromXType.Mz, ChromXUnit.Mz);
                peakFeature.MS1RawSpectrumIdTop = ms1Spectrum.ScanNumber;
                peakFeature.ScanID = ms1Spectrum.ScanNumber;
                peakFeature.MS2RawSpectrumID2CE = GetMS2RawSpectrumIDs(peakFeature.PrecursorMz, ms2SpecObjects); // maybe, in msmsall, the id count is always one but for just in case
                peakFeature.MS2RawSpectrumID = GetRepresentativeMS2RawSpectrumID(peakFeature.MS2RawSpectrumID2CE, allSpectra);
                peakFeatures.Add(peakFeature);

#if DEBUG
                // check result
                Console.WriteLine($"Peak ID = {peakFeature.PeakID}, Scan ID = {peakFeature.ChromScanIdTop}, MSSpecID = {peakFeature.ChromXsTop.Mz.Value}, Height = {peakFeature.PeakHeightTop}, Area = {peakFeature.PeakAreaAboveZero}");
#endif
            }

            return peakFeatures;
        }

        /// <summary>
        /// currently, the mass tolerance is based on ad hoc (maybe can be added to parameter obj.)
        /// the mass tolerance is considered by the basic quadrupole mass resolution.
        /// </summary>
        /// <param name="precursorMz"></param>
        /// <param name="ms2SpecObjects"></param>
        /// <param name="mzTolerance"></param>
        /// <returns></returns>
        private static Dictionary<int, double> GetMS2RawSpectrumIDs(double precursorMz, List<RawSpectrum> ms2SpecObjects, double mzTolerance = 0.25) {
            var ID2CE = new Dictionary<int, double>();
            var startID = SearchCollection.LowerBound(
                ms2SpecObjects,
                new RawSpectrum { Precursor = new RawPrecursorIon { SelectedIonMz = precursorMz - mzTolerance } },
                (x, y) => x.Precursor.SelectedIonMz.CompareTo(y.Precursor.SelectedIonMz));
            
            for (int i = startID; i < ms2SpecObjects.Count; i++) {
                var spec = ms2SpecObjects[i];
                var specPrecursorMz = spec.Precursor.SelectedIonMz;
                if (specPrecursorMz < precursorMz - mzTolerance) continue;
                if (specPrecursorMz > precursorMz + mzTolerance) break;

                ID2CE[spec.ScanNumber] = spec.CollisionEnergy;
            }
            return ID2CE; /// maybe, in msmsall, the id count is always one but for just in case
        }

        private static int GetRepresentativeMS2RawSpectrumID(Dictionary<int, double> ms2RawSpectrumID2CE, List<RawSpectrum> allSpectra) {
            if (ms2RawSpectrumID2CE.Count == 0) return -1;
            return ms2RawSpectrumID2CE.Argmax(kvp => allSpectra[kvp.Key].TotalIonCurrent).Key;
        }

        private static void SetSpectrumPeaks(List<ChromatogramPeakFeature> chromFeatures, List<RawSpectrum> spectra) {
            foreach (var feature in chromFeatures) {
                if (feature.MS2RawSpectrumID >= 0 && feature.MS2RawSpectrumID < spectra.Count) {
                    var peakElements = spectra[feature.MS2RawSpectrumID].Spectrum;
                    var spectrumPeaks = DataAccess.ConvertToSpectrumPeaks(peakElements);
                    var centroidSpec = SpectralCentroiding.Centroid(spectrumPeaks);
                    feature.Spectrum = centroidSpec;
                }
            }
        }
    }
}
