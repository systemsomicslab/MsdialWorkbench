using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using CompMs.Common.DataObj;
using CompMs.Graphics.Core.GraphAxis;
using CompMs.Graphics.Core.LineChart;
using CompMs.Graphics.Core.Scatter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Parameter;
using Rfx.Riken.OsakaUniv;

using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialDimsCore.Common;

namespace MsdialDimsCoreUiTestApp
{
    internal class MainWindowVM : ViewModelBase
    {
        public DrawingLineChart DrawingMS1
        {
            get => drawingMS1;
            set
            {
                var tmp = drawingMS1;
                if (SetProperty(ref drawingMS1, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(DrawingMS1));
                    if (drawingMS1 != null)
                        drawingMS1.PropertyChanged += (s, e) => OnPropertyChanged(nameof(DrawingMS1));
                }
            }
        }

        public DrawingContinuousHorizontalAxis DrawingMS1HorizontalAxis
        {
            get => drawingMS1HorizontalAxis;
            set => SetProperty(ref drawingMS1HorizontalAxis, value);
        }

        public DrawingContinuousVerticalAxis DrawingMS1VerticalAxis
        {
            get => drawingMS1VerticalAxis;
            set => SetProperty(ref drawingMS1VerticalAxis, value);
        }

        public DrawingLineChart DrawingMS2
        {
            get => drawingMS2;
            set
            {
                var tmp = drawingMS2;
                if (SetProperty(ref drawingMS2, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(DrawingMS2));
                    if (drawingMS2 != null)
                        drawingMS2.PropertyChanged += (s, e) => OnPropertyChanged(nameof(DrawingMS2));
                }
            }
        }

        public DrawingScatter DrawingMS2Scatter
        {
            get => drawingMS2Scatter;
            set => SetProperty(ref drawingMS2Scatter, value);
        }

        public DrawingContinuousHorizontalAxis DrawingMS2HorizontalAxis
        {
            get => drawingMS2HorizontalAxis;
            set => SetProperty(ref drawingMS2HorizontalAxis, value);
        }

        public DrawingContinuousVerticalAxis DrawingMS2VerticalAxis
        {
            get => drawingMS2VerticalAxis;
            set => SetProperty(ref drawingMS2VerticalAxis, value);
        }

        public RawSpectrum SelectedSpectrum
        {
            get => selectedSpectrum;
            set => SetProperty(ref selectedSpectrum, value);
        }

        public List<RawSpectrum> Spectrums
        {
            get => spectrums;
            set => SetProperty(ref spectrums, value);
        }

        public List<ChromatogramPeakFeature> ChromatogramPeakFeatures
        {
            get => chromatogramPeakFeatures;
            set => SetProperty(ref chromatogramPeakFeatures, value);
        }

        private DrawingLineChart drawingMS1;
        private DrawingContinuousHorizontalAxis drawingMS1HorizontalAxis;
        private DrawingContinuousVerticalAxis drawingMS1VerticalAxis;
        private DrawingLineChart drawingMS2;
        private DrawingScatter drawingMS2Scatter;
        private DrawingContinuousHorizontalAxis drawingMS2HorizontalAxis;
        private DrawingContinuousVerticalAxis drawingMS2VerticalAxis;
        private RawSpectrum selectedSpectrum;
        private List<RawSpectrum> spectrums;
        private List<ChromatogramPeakFeature> chromatogramPeakFeatures;

        void OnChartPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "DrawingMS1":
                    if (DrawingMS1 != null)
                    {
                        var area = DrawingMS1.InitialArea;
                        DrawingMS1.InitialArea = new Rect(new Point(area.X, Math.Max(0, area.Y)), area.BottomRight);
                    }
                    break;
                case "DrawingMS2":
                    if (DrawingMS2 != null)
                    {
                        var area = DrawingMS2.InitialArea;
                        DrawingMS2.InitialArea = new Rect(new Point(area.X, Math.Max(0, area.Y)), area.BottomRight);
                    }
                    break;
                case "SelectedSpectrum":
                    DrawingMS2 = new DrawingLineChart()
                    {
                        XPositions = SelectedSpectrum.Spectrum.Select(spectra => spectra.Mz).ToArray(),
                        YPositions = SelectedSpectrum.Spectrum.Select(spectra => spectra.Intensity).ToArray(),
                    };
                    /*
                    DrawingMS2Scatter = new DrawingScatter()
                    {
                        XPositions = ChromatogramPeakFeatures.Where(feature => feature.MS2RawSpectrumID == SelectedSpectrum.ScanNumber)
                                                             .Select(feature => feature.ChromXsTop.Mz.Value).ToArray(),
                        YPositions = ChromatogramPeakFeatures.Where(feature => feature.MS2RawSpectrumID == SelectedSpectrum.ScanNumber)
                                                             .Select(feature => feature.PeakHeightTop).ToArray(),
                    };
                    */
                    DrawingMS2HorizontalAxis = new DrawingContinuousHorizontalAxis()
                    {
                        MinX = DrawingMS2.XPositions.Min(),
                        MaxX = DrawingMS2.XPositions.Max(),
                    };
                    DrawingMS2VerticalAxis = new DrawingContinuousVerticalAxis()
                    {
                        MinY = DrawingMS2.YPositions.Min(),
                        MaxY = DrawingMS2.YPositions.Max(),
                    };
                    break;
            }
        }

        public MainWindowVM()
        {
            // testfiles
            var filepath = @"C:\Users\Matsuzawa\workspace\riken\abf\704_Egg2 Egg Yolk.abf";
            var lbmFile = @"C:\Users\Matsuzawa\workspace\riken\MSDIAL_LipidDB_Test.lbm2";
            var param = new MsdialDimsParameter() {
                IonMode = CompMs.Common.Enum.IonMode.Negative,
                MspFilePath = lbmFile, 
                TargetOmics = CompMs.Common.Enum.TargetOmics.Lipidomics,
                LipidQueryContainer = new CompMs.Common.Query.LipidQueryBean() { 
                    SolventType = CompMs.Common.Enum.SolventType.HCOONH4
                },
                MspSearchParam = new CompMs.Common.Parameter.MsRefSearchParameterBase() {
                    WeightedDotProductCutOff = 0.1F, SimpleDotProductCutOff = 0.1F,
                    ReverseDotProductCutOff = 0.4F, MatchedPeaksPercentageCutOff = 0.8F,
                    MinimumSpectrumMatch = 1
                }
            };

            var spectras = DataAccess.GetAllSpectra(filepath);
            var ms1spectra = spectras.Where(spectra => spectra.MsLevel == 1)
                                     .Where(spectra => spectra.Spectrum != null)
                                     .Max(spectra => (length: spectra.Spectrum.Length, spectra: spectra))
                                     .spectra;
            var ms1spectrum = ms1spectra.Spectrum
                                        .Select(peak => (Mz: peak.Mz, Intensity: peak.Intensity));
            DrawingMS1 = new DrawingLineChart()
            {
                XPositions = ms1spectrum.Select(spectra => spectra.Mz).ToArray(),
                YPositions = ms1spectrum.Select(spectra => spectra.Intensity).ToArray(),
            };
            DrawingMS1HorizontalAxis = new DrawingContinuousHorizontalAxis()
            {
                MinX = DrawingMS1.XPositions.Min(),
                MaxX = DrawingMS1.XPositions.Max(),
            };
            DrawingMS1VerticalAxis = new DrawingContinuousVerticalAxis()
            {
                MinY = DrawingMS1.YPositions.Min(),
                MaxY = DrawingMS1.YPositions.Max(),
            };
            var ms2spectra = spectras.Where(spectra => spectra.MsLevel == 2)
                                     .Where(spectra => spectra.Spectrum != null)
                                     .ToList();
            Spectrums = ms2spectra;

            var chromPeaks = ChromMsConverter.ConvertRawPeakElementToChromatogramPeakList(ms1spectra.Spectrum);
            var sChromPeaks = DataAccess.GetSmoothedPeaklist(chromPeaks, param.SmoothingMethod, param.SmoothingLevel);
            var peakPickResults = PeakDetection.PeakDetectionVS1(sChromPeaks, param.MinimumDatapoints, param.MinimumAmplitude);
            ChromatogramPeakFeatures = GetChromatogramPeakFeatures(peakPickResults, ms1spectra, spectras);

            PropertyChanged += OnChartPropertyChanged;
        }

        bool SetProperty<T, U>(ref T property, U value, [CallerMemberName]string propertyname = "") where U : T
        {
            if (value == null && property == null || value.Equals(property)) return false;
            property = value;
            OnPropertyChanged(propertyname);
            return true;
        } 

        private List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(List<PeakDetectionResult> peakPickResults, RawSpectrum ms1Spectrum, List<RawSpectrum> allSpectra) {
            var peakFeatures = new List<ChromatogramPeakFeature>();
            var ms2SpecObjects = allSpectra.Where(n => n.MsLevel == 2 && n.Precursor != null).OrderBy(n => n.Precursor.SelectedIonMz).ToList();
            
            foreach (var result in peakPickResults) {

                // here, the chrom scan ID should be matched to the scan number of RawSpectrum Element
                var peakFeature = DataAccess.GetChromatogramPeakFeature(result, ChromXType.Mz, ChromXUnit.Mz);
                var chromScanID = peakFeature.ChromScanIdTop;
                peakFeature.ChromXs.RT = new RetentionTime(0);
                peakFeature.ChromXsTop.RT = new RetentionTime(0);
                peakFeature.IonMode = ms1Spectrum.ScanPolarity == ScanPolarity.Positive ? CompMs.Common.Enum.IonMode.Positive : CompMs.Common.Enum.IonMode.Negative;
                peakFeature.PrecursorMz = ms1Spectrum.Spectrum[chromScanID].Mz;
                peakFeature.MS1RawSpectrumID = ms1Spectrum.ScanNumber;
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

        /// <summary>
        /// currently, the mass tolerance is based on ad hoc (maybe can be added to parameter obj.)
        /// the mass tolerance is considered by the basic quadrupole mass resolution.
        /// </summary>
        /// <param name="precursorMz"></param>
        /// <param name="allSpectra"></param>
        /// <param name="mzTolerance"></param>
        /// <returns></returns>
        private List<int> GetMS2RawSpectrumIDs(double precursorMz, List<RawSpectrum> ms2SpecObjects, double mzTolerance = 0.25) {
            var IDs = new List<int>();
            var startID = GetSpectrumObjectStartIndexByPrecursorMz(precursorMz, mzTolerance, ms2SpecObjects);
            for (int i = startID; i < ms2SpecObjects.Count; i++) {
                var spec = ms2SpecObjects[i];
                var precursorMzObj = spec.Precursor.SelectedIonMz;
                if (precursorMzObj < precursorMz - mzTolerance) continue;
                if (precursorMzObj > precursorMz + mzTolerance) break;

                IDs.Add(spec.ScanNumber);
            }
            return IDs; // maybe, in msmsall, the id count is always one but for just in case
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
