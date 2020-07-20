using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using CompMs.Common.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Parameter;

using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.Common;

namespace MsdialDimsCoreUiTestApp
{
    internal class MainWindowVM : INotifyPropertyChanged
    {
        public ObservableCollection<AnalysisFileBean> AnalysisFiles {
            get => analysisFiles;
            set => SetProperty(ref analysisFiles, value);
        }

        public ObservableCollection<AlignmentFileBean> AlignmentFiles {
            get => alignmentFiles;
            set => SetProperty(ref alignmentFiles, value);
        }

        public ObservableCollection<ChromatogramPeak> Ms1Peaks {
            get => ms1Peaks;
            set => SetProperty(ref ms1Peaks, value);
        }

        public Rect Ms1Area {
            get => ms1Area;
            set => SetProperty(ref ms1Area, value);
        }

        public Rect Ms2Area {
            get => ms2Area;
            set => SetProperty(ref ms2Area, value);
        }

        public ObservableCollection<Ms2Info> Ms2Features {
            get => ms2Features;
            set => SetProperty(ref ms2Features, value);
        }

        private ObservableCollection<ChromatogramPeak> ms1Peaks;
        private ObservableCollection<Ms2Info> ms2Features;
        private Rect ms1Area, ms2Area;
        private ChromatogramSerializer<ChromatogramSpotInfo> chromSpotSerializer;
        private ObservableCollection<AnalysisFileBean> analysisFiles;
        private ObservableCollection<AlignmentFileBean> alignmentFiles;
        private MsdialDimsParameter param;

        public MainWindowVM()
        {
            // testfiles
            var lbmFile = @"C:\Users\YUKI MATSUZAWA\works\data\lbm\MSDIAL_LipidDB_Test.lbm2";
            var textLibraryFile = @"C:\Users\YUKI MATSUZAWA\works\data\textlib\TestLibrary.txt";
            analysisFiles = new ObservableCollection<AnalysisFileBean> {
                new AnalysisFileBean { AnalysisFileId = 0,
                                       AnalysisFileName = "703_Egg2 Egg White",
                                       AnalysisFilePath = @"C:\Users\YUKI MATSUZAWA\works\data\sciex_msmsall\703_Egg2 Egg White.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },
                new AnalysisFileBean { AnalysisFileId = 0,
                                       AnalysisFileName = "704_Egg2 Egg Yolk",
                                       AnalysisFilePath = @"C:\Users\YUKI MATSUZAWA\works\data\sciex_msmsall\704_Egg2 Egg Yolk.abf",
                                       PeakAreaBeanInformationFilePath = System.IO.Path.GetTempFileName() },

            };
            alignmentFiles = new ObservableCollection<AlignmentFileBean> {
                new AlignmentFileBean {
                    FileName = "Alignment 1",
                    EicFilePath = System.IO.Path.GetTempFileName(),
                },
            };
            param = new MsdialDimsParameter() {
                IonMode = CompMs.Common.Enum.IonMode.Negative,
                MspFilePath = lbmFile,
                TextDBFilePath = textLibraryFile,
                TargetOmics = CompMs.Common.Enum.TargetOmics.Lipidomics,
                LipidQueryContainer = new CompMs.Common.Query.LipidQueryBean() {
                    SolventType = CompMs.Common.Enum.SolventType.HCOONH4,
                    LbmQueries = LbmQueryParcer.GetLbmQueries(true),
                },
                MspSearchParam = new CompMs.Common.Parameter.MsRefSearchParameterBase() {
                    WeightedDotProductCutOff = 0.1F, SimpleDotProductCutOff = 0.1F,
                    ReverseDotProductCutOff = 0.4F, MatchedPeaksPercentageCutOff = 0.8F,
                    MinimumSpectrumMatch = 1
                }
            };

            List<MoleculeMsReference> mspDB = LibraryHandler.ReadLipidMsLibrary(param.MspFilePath, param);
            mspDB.Sort((a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));

            List<MoleculeMsReference> textDB = TextLibraryParser.TextLibraryReader(param.TextDBFilePath, out _);
            textDB.Sort((a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));

            chromSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");

            foreach (var analysisFile in analysisFiles)
                RunAnnotation(analysisFile, mspDB, textDB);
            RunAlignment(analysisFiles, alignmentFiles[0]);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyname) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));

        bool SetProperty<T>(ref T property, T value, [CallerMemberName]string propertyname = "")
        {
            if (value == null && property == null || value.Equals(property)) return false;
            property = value;
            RaisePropertyChanged(propertyname);
            return true;
        }

        private void RunAnnotation(AnalysisFileBean analysisFileBean, List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB) {
            var spectras = DataAccess.GetAllSpectra(analysisFileBean.AnalysisFilePath);
            var ms1spectra = spectras.Where(spectra => spectra.MsLevel == 1)
                                     .Where(spectra => spectra.Spectrum != null)
                                     .Max(spectra => (spectra.Spectrum.Length, spectra))
                                     .spectra;

            var chromPeaks = ComponentsConverter.ConvertRawPeakElementToChromatogramPeakList(ms1spectra.Spectrum);
            var sChromPeaks = DataAccess.GetSmoothedPeaklist(chromPeaks, param.SmoothingMethod, param.SmoothingLevel);
            var peakPickResults = PeakDetection.PeakDetectionVS1(sChromPeaks, param.MinimumDatapoints, param.MinimumAmplitude);
            var chromatogramPeakFeatures = GetChromatogramPeakFeatures(peakPickResults, ms1spectra, spectras);
            SetSpectrumPeaks(chromatogramPeakFeatures, spectras);

            MsdialSerializer.SaveChromatogramPeakFeatures(analysisFileBean.PeakAreaBeanInformationFilePath, chromatogramPeakFeatures);

            var ms2spectra = spectras.Where(spectra => spectra.MsLevel == 2)
                                     .Where(spectra => spectra.Spectrum != null);


            Ms1Peaks = new ObservableCollection<ChromatogramPeak>(sChromPeaks);
            Ms1Area = new Rect(new Point(sChromPeaks.Min(peak => peak.Mass), sChromPeaks.Min(peak => peak.Intensity)),
                               new Point(sChromPeaks.Max(peak => peak.Mass), sChromPeaks.Max(peak => peak.Intensity)));
            Ms2Area = new Rect(0, 0, 1000, 1);
            var results = chromatogramPeakFeatures.Select(feature => CalculateAndSetAnnotatedReferences(feature, mspDB, textDB, param)).ToList();

            Ms2Features = new ObservableCollection<Ms2Info>(
                chromatogramPeakFeatures.Zip(results, (feature, result) =>
                {
                    var spectrum = ScalingSpectrumPeaks(ComponentsConverter.ConvertToSpectrumPeaks(spectras[feature.MS2RawSpectrumID].Spectrum));
                    var centroid = ScalingSpectrumPeaks(feature.Spectrum);
                    var mspIDs = feature.MSRawID2MspIDs.IsEmptyOrNull() ? new List<int>() : feature.MSRawID2MspIDs[feature.MS2RawSpectrumID];
                    var detectedMsp = mspIDs.Zip(result.Msp, (id, res) => new AnnotationResult{Reference = mspDB[id], Result = res });
                    var detectedText = feature.TextDbIDs.Zip(result.Text, (id, res) => new AnnotationResult { Reference = textDB[id], Result = res });
                    var detected = detectedMsp.Concat(detectedText).ToList();
                    foreach (var det in detected)
                        det.Reference.Spectrum = ScalingSpectrumPeaks(det.Reference.Spectrum);
                    return new Ms2Info
                    {
                        ChromatogramPeakFeature = feature,
                        PeakID = feature.PeakID,
                        Mass = feature.Mass,
                        Intensity = feature.PeakHeightTop,
                        Spectrum = spectrum,
                        Centroids = centroid,
                        Detected = detected,
                        //Annotated = 0
                    };
                })
                );
        }

        private void RunAlignment(IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile) {
            var result = AlignmentProcess.Alignment(analysisFiles, alignmentFile, chromSpotSerializer, param);
            // TODO: save AlignmentResultContainer
        }

        private List<SpectrumPeak> ScalingSpectrumPeaks(IEnumerable<SpectrumPeak> spectrumPeaks)
        {
            if (!spectrumPeaks.Any()) return new List<SpectrumPeak>();
            var min = spectrumPeaks.Min(peak => peak.Intensity);
            var width = spectrumPeaks.Max(peak => peak.Intensity) - min;

            return spectrumPeaks.Select(peak => new SpectrumPeak(peak.Mass, (peak.Intensity - min) / width)).ToList();
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
                peakFeature.IonMode = ms1Spectrum.ScanPolarity == ScanPolarity.Positive ? CompMs.Common.Enum.IonMode.Positive : CompMs.Common.Enum.IonMode.Negative;
                peakFeature.PrecursorMz = ms1Spectrum.Spectrum[chromScanID].Mz;
                peakFeature.MS1RawSpectrumIdTop = ms1Spectrum.ScanNumber;
                peakFeature.ScanID = ms1Spectrum.ScanNumber;
                peakFeature.MS2RawSpectrumIDs = GetMS2RawSpectrumIDs(peakFeature.PrecursorMz, ms2SpecObjects); // maybe, in msmsall, the id count is always one but for just in case
                peakFeature.MS2RawSpectrumID = GetRepresentativeMS2RawSpectrumID(peakFeature.MS2RawSpectrumIDs, allSpectra);
                peakFeatures.Add(peakFeature);

                // result check
                Console.WriteLine("Peak ID={0}, Scan ID={1}, MZ={2}, MS2SpecID={3}, Height={4}, Area={5}",
                    peakFeature.PeakID, peakFeature.ChromScanIdTop, peakFeature.ChromXsTop.Mz.Value, peakFeature.MS2RawSpectrumID, peakFeature.PeakHeightTop, peakFeature.PeakAreaAboveZero);
            }

            return peakFeatures;
        }

        private int GetRepresentativeMS2RawSpectrumID(List<int> ms2RawSpectrumIDs, List<RawSpectrum> allSpectra) {
            if (ms2RawSpectrumIDs.Count == 0) return -1;

            var maxIntensity = 0.0;
            var maxIntensityID = -1;
            for (int i = 0; i < ms2RawSpectrumIDs.Count; i++) {
                var specID = ms2RawSpectrumIDs[i];
                var specObj = allSpectra[specID];
                if (specObj.TotalIonCurrent > maxIntensity) {
                    maxIntensity = specObj.TotalIonCurrent;
                    maxIntensityID = specID;
                }
            }
            return maxIntensityID;
        }

        private List<int> GetMS2RawSpectrumIDs(double precursorMz, List<RawSpectrum> ms2SpecObjects, double mzTolerance = 0.25) {
            var IDs = new List<int>();
            var target = new RawSpectrum { Precursor = new RawPrecursorIon { SelectedIonMz = precursorMz - mzTolerance } };
            var startID =  SearchCollection.LowerBound(ms2SpecObjects, target, (a, b) => a.Precursor.SelectedIonMz.CompareTo(b.Precursor.SelectedIonMz));
            for (int i = startID; i < ms2SpecObjects.Count; i++) {
                var spec = ms2SpecObjects[i];
                var precursorMzObj = spec.Precursor.SelectedIonMz;
                if (precursorMzObj < precursorMz - mzTolerance) continue;
                if (precursorMzObj > precursorMz + mzTolerance) break;

                IDs.Add(spec.ScanNumber);
            }
            return IDs; // maybe, in msmsall, the id count is always one but for just in case
        }

        private void SetSpectrumPeaks(List<ChromatogramPeakFeature> chromFeatures, List<RawSpectrum> spectra) {
            foreach (var feature in chromFeatures) {
                if (feature.MS2RawSpectrumID >= 0 && feature.MS2RawSpectrumID <= spectra.Count - 1) {
                    var peakElements = spectra[feature.MS2RawSpectrumID].Spectrum;
                    var spectrumPeaks = ComponentsConverter.ConvertToSpectrumPeaks(peakElements);
                    var centroidSpec = SpectralCentroiding.Centroid(spectrumPeaks);
                    feature.Spectrum = centroidSpec;
                }
                Console.WriteLine("Peak ID={0}, Scan ID={1}, Spectrum count={2}", feature.PeakID, feature.ScanID, feature.Spectrum.Count);
            }
        }

        private (List<MsScanMatchResult> Msp, List<MsScanMatchResult> Text) CalculateAndSetAnnotatedReferences(ChromatogramPeakFeature chromatogramPeakFeature, List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB, MsdialDimsParameter param)
        {
            AnnotationProcess.Run(chromatogramPeakFeature, mspDB, textDB, param.MspSearchParam, param.TargetOmics, out List<MsScanMatchResult> mspResult, out List<MsScanMatchResult> textResult);
            Console.WriteLine("PeakID={0}, Annotation={1}", chromatogramPeakFeature.PeakID, chromatogramPeakFeature.Name);
            return (mspResult, textResult);
        }
    }

    internal class AnnotationResult
    {
        public MoleculeMsReference Reference { get; set; }
        public MsScanMatchResult Result { get; set; }
        public double Score => Result.TotalScore;
    }

    internal class Ms2Info
    {
        public ChromatogramPeakFeature ChromatogramPeakFeature { get; set; }
        public int PeakID { get; set; }
        public double Mass { get; set; }
        public double Intensity { get; set; }
        public List<SpectrumPeak> Spectrum { get; set; }
        public List<SpectrumPeak> Centroids { get; set; }
        public List<AnnotationResult> Detected { get; set; }
        public int Annotated { get; set; }
    }
}
