using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm
{
    public class DimsProfilePeakDetectionProcess : IPeakDetectionProcess
    {
        private readonly PeakPickBaseParameter peakPickParameter;
        private readonly ProjectBaseParameter projectParameter;

        public DimsProfilePeakDetectionProcess(PeakPickBaseParameter peakPickParameter, ProjectBaseParameter projectParameter) {
            this.peakPickParameter = peakPickParameter ?? throw new ArgumentNullException(nameof(peakPickParameter));
            this.projectParameter = projectParameter ?? throw new ArgumentNullException(nameof(projectParameter));
        }

        public List<ChromatogramPeakFeature> Detect(AnalysisFileBean analysisFile, IDataProvider provider) {

            var ms1Spectrum = provider.LoadMs1Spectrums().Argmax(spec => spec.Spectrum.Length);
            var chromPeaks = DataAccess.ConvertRawPeakElementToChromatogramPeakList(ms1Spectrum.Spectrum);
            var sChromPeaks = new Chromatogram(chromPeaks, ChromXType.Mz, ChromXUnit.Mz).ChromatogramSmoothing(peakPickParameter.SmoothingMethod, peakPickParameter.SmoothingLevel).AsPeakArray();

            var peakPickResults = PeakDetection.PeakDetectionVS1(sChromPeaks, peakPickParameter.MinimumDatapoints, peakPickParameter.MinimumAmplitude);
            if (peakPickResults.IsEmptyOrNull()) {
                return new List<ChromatogramPeakFeature>();
            }
            return ConvertPeaksToPeakFeatures(peakPickResults, ms1Spectrum, provider, analysisFile.AcquisitionType);
        }

        private static List<ChromatogramPeakFeature> ConvertPeaksToPeakFeatures(List<PeakDetectionResult> peakPickResults, RawSpectrum ms1Spectrum, IDataProvider provider, AcquisitionType type) {
            var peakFeatures = new List<ChromatogramPeakFeature>();
            var ms2SpecObjects = provider.LoadMsNSpectrums(level: 2)
                .Where(spectra => spectra.MsLevel == 2 && spectra.Precursor != null)
                .OrderBy(spectra => spectra.Precursor.SelectedIonMz).ToList();
            IonMode ionMode = ms1Spectrum.ScanPolarity == ScanPolarity.Positive ? IonMode.Positive : IonMode.Negative;

            foreach (var result in peakPickResults) {
                var peakFeature = DataAccess.GetChromatogramPeakFeature(result, ChromXType.Mz, ChromXUnit.Mz, ms1Spectrum.Spectrum[result.ScanNumAtPeakTop].Mz, ionMode);
                var chromScanID = peakFeature.ChromScanIdTop;

                IChromatogramPeakFeature peak = peakFeature;
                peak.Mass = ms1Spectrum.Spectrum[chromScanID].Mz;
                peak.ChromXsTop = new ChromXs(peakFeature.Mass, ChromXType.Mz, ChromXUnit.Mz);

                peakFeature.MS1RawSpectrumIdTop = ms1Spectrum.Index;
                peakFeature.ScanID = ms1Spectrum.ScanNumber;
                switch (type) {
                    case AcquisitionType.AIF:
                    case AcquisitionType.SWATH:
                        peakFeature.MS2RawSpectrumID2CE = GetMS2RawSpectrumIDsDIA(peakFeature.PrecursorMz, ms2SpecObjects); // maybe, in msmsall, the id count is always one but for just in case
                        break;
                    case AcquisitionType.DDA:
                        peakFeature.MS2RawSpectrumID2CE = GetMS2RawSpectrumIDsDDA(peakFeature.PrecursorMz, ms2SpecObjects); // maybe, in msmsall, the id count is always one but for just in case
                        break;
                    default:
                        throw new NotSupportedException(nameof(type));
                }
                peakFeature.MS2RawSpectrumID = GetRepresentativeMS2RawSpectrumID(peakFeature.MS2RawSpectrumID2CE, provider);
                peakFeatures.Add(peakFeature);
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
        /// 
        private static Dictionary<int, double> GetMS2RawSpectrumIDsDIA(double precursorMz, List<RawSpectrum> ms2SpecObjects, double mzTolerance = 0.25) {
            var ID2CE = new Dictionary<int, double>();
            int startID = SearchCollection.LowerBound(
                ms2SpecObjects,
                new RawSpectrum { Precursor = new RawPrecursorIon { IsolationTargetMz = precursorMz - mzTolerance, IsolationWindowUpperOffset = 0, } },
                (x, y) => (x.Precursor.IsolationTargetMz + x.Precursor.IsolationWindowUpperOffset).CompareTo(y.Precursor.IsolationTargetMz + y.Precursor.IsolationWindowUpperOffset));
            
            for (int i = startID; i < ms2SpecObjects.Count; i++) {
                var spec = ms2SpecObjects[i];
                if (spec.Precursor.IsolationTargetMz - precursorMz < - spec.Precursor.IsolationWindowUpperOffset - mzTolerance) continue;
                if (spec.Precursor.IsolationTargetMz - precursorMz > spec.Precursor.IsolationWindowLowerOffset + mzTolerance) break;

                ID2CE[spec.Index] = spec.CollisionEnergy;
            }
            return ID2CE; /// maybe, in msmsall, the id count is always one but for just in case
        }

        /// <summary>
        /// currently, the mass tolerance is based on ad hoc (maybe can be added to parameter obj.)
        /// the mass tolerance is considered by the basic quadrupole mass resolution.
        /// </summary>
        /// <param name="precursorMz"></param>
        /// <param name="ms2SpecObjects"></param>
        /// <param name="mzTolerance"></param>
        /// <returns></returns>
        /// 
        private static Dictionary<int, double> GetMS2RawSpectrumIDsDDA(double precursorMz, List<RawSpectrum> ms2SpecObjects, double mzTolerance = 0.25) {
            var ID2CE = new Dictionary<int, double>();
            int startID = SearchCollection.LowerBound(
                ms2SpecObjects,
                new RawSpectrum { Precursor = new RawPrecursorIon { IsolationTargetMz = precursorMz - mzTolerance, IsolationWindowUpperOffset = 0, } },
                (x, y) => (x.Precursor.IsolationTargetMz).CompareTo(y.Precursor.IsolationTargetMz));
            
            for (int i = startID; i < ms2SpecObjects.Count; i++) {
                var spec = ms2SpecObjects[i];
                if (spec.Precursor.IsolationTargetMz - precursorMz < - mzTolerance) continue;
                if (spec.Precursor.IsolationTargetMz - precursorMz > + mzTolerance) break;

                ID2CE[spec.Index] = spec.CollisionEnergy;
            }
            return ID2CE;
        }

        private static int GetRepresentativeMS2RawSpectrumID(Dictionary<int, double> ms2RawSpectrumID2CE, IDataProvider provider) {
            if (ms2RawSpectrumID2CE.Count == 0) return -1;
            return ms2RawSpectrumID2CE.Argmax(kvp => provider.LoadMsSpectrumFromIndex(kvp.Key).TotalIonCurrent).Key;
        }
    }

    /*
    public class DimsCentroidPeakDetectionProcess : IPeakDetectionProcess
    {
        public List<ChromatogramPeakFeature> Detect(IDataProvider provider) {

            var ms1Spectrum = provider.LoadMs1Spectrums().Argmax(spec => spec.TotalIonCurrent);
            var chromPeaks = DataAccess.ConvertRawPeakElementToChromatogramPeakList(ms1Spectrum.Spectrum);
            var sChromPeaks = DataAccess.GetSmoothedPeaklist(chromPeaks, peakPickParameter.SmoothingMethod, peakPickParameter.SmoothingLevel);

            var peakPickResults = PeakDetection.PeakDetectionVS1(sChromPeaks, peakPickParameter.MinimumDatapoints, peakPickParameter.MinimumAmplitude);
            if (peakPickResults.IsEmptyOrNull()) {
                return new List<ChromatogramPeakFeature>();
            }
            return ConvertPeaksToPeakFeatures(peakPickResults, ms1Spectrum, provider.LoadMsSpectrums(), projectParameter.AcquisitionType);
        }
    }
    */
}
