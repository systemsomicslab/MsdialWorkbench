using CompMs.Common.Algorithm.ChromSmoothing;
using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Common;
using CompMs.MsdialDimsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialDimsCore.MsmsAll {
    
    public class MsmsAllProcess {

        string filepath;
        string specEmptyError = "Spectra object is null or empty.";
        string featureEmptyError = "No peak feature detected.";

        MsdialDimsParameter param;
        
        public MsmsAllProcess(string filepath, MsdialDimsParameter param) {
            this.filepath = filepath;
            this.param = param;
        }

        public ProcessError Run() {
            var spectra = DataAccess.GetAllSpectra(filepath);
            if (spectra == null || spectra.Count == 0) return new ProcessError(true, specEmptyError);


            Console.WriteLine("Importing libraries...");
            var iupacDB = IupacResourceParser.GetIUPACDatabase();
            List<MoleculeMsReference> mspDB = null;
            if (param.TargetOmics == TargetOmics.Metabolomics) {
                mspDB = MspFileParser.MspFileReader(param.MspFilePath);
            } else if (param.TargetOmics == TargetOmics.Lipidomics) {
                var lbmQueries = LbmQueryParcer.GetLbmQueries(true);
                var extension = System.IO.Path.GetExtension(param.MspFilePath);
                if (extension == ".lbm2") {
                    mspDB = MspFileParser.ReadSerializedLbmLibrary(param.MspFilePath, lbmQueries,
                        param.IonMode, param.LipidQueryContainer.SolventType, param.LipidQueryContainer.CollisionType);
                }
                else {
                    mspDB = MspFileParser.LbmFileReader(param.MspFilePath, lbmQueries,
                        param.IonMode, param.LipidQueryContainer.SolventType, param.LipidQueryContainer.CollisionType);
                }
            }

            if (mspDB != null) mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

            Console.WriteLine("Feature detection...");
            // in msms all, the first scan would contain ms1 spectra, but for just in case, 
            // the following parser is used to safely obtain the information
            var ms1Spectrum = getMs1SpectraInMsmsAllData(spectra);
            if (ms1Spectrum == null) return new ProcessError(true, specEmptyError);
            var chromPeaks = DataAccess.ConvertRawPeakElementToChromatogramPeakList(ms1Spectrum.Spectrum);
            var sChromPeaks = DataAccess.GetSmoothedPeaklist(chromPeaks, param.SmoothingMethod, param.SmoothingLevel);
            var peakPickResults = PeakDetection.PeakDetectionVS1(sChromPeaks, param.MinimumDatapoints, param.MinimumAmplitude);
            var chromFeatures = GetChromatogramPeakFeatures(peakPickResults, ms1Spectrum, spectra);

            if (chromFeatures.Count == 0) return new ProcessError(true, featureEmptyError);
            IsotopeEstimator.Process(chromFeatures, param, iupacDB);

            SetSpectrumPeaks(chromFeatures, spectra);

            Console.WriteLine("Annotation started...");
            foreach (var item in chromFeatures.Select((value, index) => new { value, index })) {
                var feature = item.value;
                AnnotationProcess.Run(feature, null, mspDB, null, param.MspSearchParam, param.TargetOmics, null, out _, out _);
                Console.WriteLine("PeakID={0}, Annotation={1}", feature.PeakID, feature.Name);
            }

            new PeakCharacterEstimator(90, 10).Process(spectra, chromFeatures, null, param, null);

            return new ProcessError();
        }

        private void SetSpectrumPeaks(List<ChromatogramPeakFeature> chromFeatures, List<RawSpectrum> spectra) {
            var results = new List<MSDecResult>();
            foreach (var feature in chromFeatures) {
                if (feature.MS2RawSpectrumID < 0 || feature.MS2RawSpectrumID > spectra.Count - 1) {

                }
                else {
                    var peakElements = spectra[feature.MS2RawSpectrumID].Spectrum;
                    var spectrumPeaks = DataAccess.ConvertToSpectrumPeaks(peakElements);
                    var centroidSpec = SpectralCentroiding.Centroid(spectrumPeaks);
                    feature.Spectrum = centroidSpec;
                }

                Console.WriteLine("Peak ID={0}, Scan ID={1}, Spectrum count={2}", feature.PeakID, feature.ScanID, feature.Spectrum.Count);
            }
        }

        private List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(List<PeakDetectionResult> peakPickResults, RawSpectrum ms1Spectrum, List<RawSpectrum> allSpectra) {
            var peakFeatures = new List<ChromatogramPeakFeature>();
            var ms2SpecObjects = allSpectra.Where(n => n.MsLevel == 2 && n.Precursor != null).OrderBy(n => n.Precursor.SelectedIonMz).ToList();
            
            foreach (var result in peakPickResults) {

                // here, the chrom scan ID should be matched to the scan number of RawSpectrum Element
                var peakFeature = DataAccess.GetChromatogramPeakFeature(result, ChromXType.Mz, ChromXUnit.Mz, ms1Spectrum.Spectrum[result.ScanNumAtPeakTop].Mz);
                var chromScanID = peakFeature.ChromScanIdTop;
                peakFeature.ChromXs.RT = new RetentionTime(0);
                peakFeature.ChromXsTop.RT = new RetentionTime(0);
                peakFeature.IonMode = ms1Spectrum.ScanPolarity == ScanPolarity.Positive ? IonMode.Positive : IonMode.Negative;
                peakFeature.PrecursorMz = ms1Spectrum.Spectrum[chromScanID].Mz;
                peakFeature.Mass = ms1Spectrum.Spectrum[chromScanID].Mz;
                peakFeature.MS1RawSpectrumIdTop = ms1Spectrum.ScanNumber;
                peakFeature.ScanID = ms1Spectrum.ScanNumber;
                peakFeature.MS2RawSpectrumID2CE = GetMS2RawSpectrumIDs(peakFeature.PrecursorMz, ms2SpecObjects); // maybe, in msmsall, the id count is always one but for just in case
                peakFeature.MS2RawSpectrumID = GetRepresentativeMS2RawSpectrumID(peakFeature.MS2RawSpectrumID2CE, allSpectra);
                peakFeatures.Add(peakFeature);

                // result check
                Console.WriteLine("Peak ID={0}, Scan ID={1}, MZ={2}, MS2SpecID={3}, Height={4}, Area={5}", 
                    peakFeature.PeakID, peakFeature.ChromScanIdTop, peakFeature.ChromXsTop.Mz.Value, peakFeature.MS2RawSpectrumID, peakFeature.PeakHeightTop, peakFeature.PeakAreaAboveZero);
            }

            return peakFeatures;
        }

        private int GetRepresentativeMS2RawSpectrumID(Dictionary<int, double> ms2RawSpectrumID2CE, List<RawSpectrum> allSpectra) {
            if (ms2RawSpectrumID2CE.Count == 0) return -1;

            var maxIntensity = 0.0;
            var maxIntensityID = -1;
            foreach (var pair in ms2RawSpectrumID2CE) {
                var specID = pair.Key;
                var specObj = allSpectra[specID];
                if (specObj.TotalIonCurrent > maxIntensity) {
                    maxIntensity = specObj.TotalIonCurrent;
                    maxIntensityID = specID;
                }
            }
            
            return maxIntensityID;
        }

        /// <summary>
        /// currently, the mass tolerance is based on ad hoc (maybe can be added to parameter obj.)
        /// the mass tolerance is considered by the basic quadrupole mass resolution.
        /// </summary>
        /// <param name="precursorMz"></param>
        /// <param name="allSpectra"></param>
        /// <param name="mzTolerance"></param>
        /// <returns></returns>
        private Dictionary<int, double> GetMS2RawSpectrumIDs(double precursorMz, List<RawSpectrum> ms2SpecObjects, double mzTolerance = 0.25) {
            var ID2CE = new Dictionary<int, double>();
            var startID = GetSpectrumObjectStartIndexByPrecursorMz(precursorMz, mzTolerance, ms2SpecObjects);
            for (int i = startID; i < ms2SpecObjects.Count; i++) {
                var spec = ms2SpecObjects[i];
                var precursorMzObj = spec.Precursor.SelectedIonMz;
                if (precursorMzObj < precursorMz - mzTolerance) continue;
                if (precursorMzObj > precursorMz + mzTolerance) break;

                ID2CE[spec.ScanNumber] = spec.CollisionEnergy;
            }
            return ID2CE; // maybe, in msmsall, the id count is always one but for just in case
        }

        private int GetSpectrumObjectStartIndexByPrecursorMz(double targetedMass, double massTolerance, List<RawSpectrum> ms2SpecObjects) {
            if (ms2SpecObjects.Count == 0) return 0;
            var targetMass = targetedMass - massTolerance;
            int startIndex = 0, endIndex = ms2SpecObjects.Count - 1;
            int counter = 0;

            if (targetMass > ms2SpecObjects[endIndex].Precursor.SelectedIonMz) return endIndex;

            while (counter < 5) {
                if (ms2SpecObjects[startIndex].Precursor.SelectedIonMz <= targetMass && targetMass < ms2SpecObjects[(startIndex + endIndex) / 2].Precursor.SelectedIonMz) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (ms2SpecObjects[(startIndex + endIndex) / 2].Precursor.SelectedIonMz <= targetMass && targetMass < ms2SpecObjects[endIndex].Precursor.SelectedIonMz) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }


        private RawSpectrum getMs1SpectraInMsmsAllData(List<RawSpectrum> spectra) {
            var maxSpecCount = -1.0;
            var maxSpecCountID = -1;

            foreach (var item in spectra.Select((value, index) => new { value, index })
                .Where(n => n.value.MsLevel == 1 && n.value.Spectrum != null && n.value.Spectrum.Length > 0)) {
                var spec = item.value;
                if (spec.Spectrum.Length > maxSpecCount) {
                    maxSpecCount = spec.Spectrum.Length;
                    maxSpecCountID = item.index;
                }
            }

            if (maxSpecCountID < 0) return null;
            return spectra[maxSpecCountID];
        }
    }
}
