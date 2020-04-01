using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Algorithm {

	/// <summary>
	/// The current chromatogram deconvolution can consider up to 4 coeluting metabolites.
	/// The target deconvoluted chromatogram is always defined as 'C' in this enum.
	/// The left edge's scan number and right edge's scan number of target chromatogram are determined.
	/// The adjacent model chromatograms 
	/// , in which the peak right edge (or peak right edge) is inside of the RT range of target peak,
	/// are considered as 'coeluting' components. 
	/// B is the left model chromatogram to C
	/// A is second left model chromatogram to C
	/// D is the right model chrom to C
	/// E is second right chromatogram to C
	/// </summary>
    public enum Ms2DecPattern { C, BC, CD, ABC, BCD, CDE, ABCD, BCDE, ABCDE }

	/// <summary>
	/// Now, the model quality is determined by ad-hoc criteria considering 
	/// A) ideal slope, B) peak shape, and C) abundance information
	/// </summary>
    public enum ModelQuality { High, Middle, Low }

    /// <summary>
    /// stores the sharpness value and retention time in each data point
    /// </summary>
    public class Ms2DecBin {
        public double TotalSharpnessValue { get; set; }
        public float RetentionTime { get; set; }
        public List<PeakSpot> PeakSpots { get; set; }

        public Ms2DecBin() { TotalSharpnessValue = 0; PeakSpots = new List<PeakSpot>(); }
    }

	/// <summary>
	/// Definition of model chromatogram
	/// </summary>
    public class ModelChromatogram {
        public List<Peak> Peaks { get; set; }

        public int ChromScanOfPeakTop { get; set; }
        public int ChromScanOfPeakLeft { get; set; }
        public int ChromScanOfPeakRight { get; set; }

        public List<float> ModelMzList { get; set; }

        public double IdealSlopeValue { get; set; }
        public double SharpnessValue { get; set; }
        public double MaximumPeakTopValue { get; set; }

        public ModelChromatogram() {
            Peaks = new List<Peak>();
            ModelMzList = new List<float>();
            ChromScanOfPeakTop = 0;
            ChromScanOfPeakLeft = 0;
            ChromScanOfPeakRight = 0;
            IdealSlopeValue = 0;
            MaximumPeakTopValue = 0;
            SharpnessValue = 0;
        }
    }

    public class RegionMarker {
        public int ID { get; set; }
        public int ScanBegin { get; set; }
        public int ScanEnd { get; set; }
    }

    public class PeakSpot {
        public ModelQuality Quality { get; set; }
        public int PeakSpotID { get; set; }
        public int ChromatogramID { get; set; }

        public PeakSpot() { Quality = ModelQuality.Low; }
    }

	/// <summary>
	/// Model chromatogram vector.
	/// This vector currently can contain up to 4 model chromatograms in addition to one targeted model.
	/// </summary>
    public class ModelChromVector {
        public Ms2DecPattern Ms2DecPattern { get; set; }

        public int TargetScanLeftInModelChromVector { get; set; }
        public int TargetScanTopInModelChromVector { get; set; }
        public int TargetScanRightInModelChromVector { get; set; }

        public List<int> ChromScanList { get; set; }
        public List<float> RtArray { get; set; }
        public List<float> TargetIntensityArray { get; set; }

        public List<float> OneLeftIntensityArray { get; set; }
        public List<float> OneRightIntensityArray { get; set; }

        public List<float> TwoLeftIntensityArray { get; set; }
        public List<float> TwoRightInetnsityArray { get; set; }

        public List<float> ModelMzList { get; set; }

        public ModelChromVector() {
            Ms2DecPattern = Ms2DecPattern.C;
            ChromScanList = new List<int>();
            RtArray = new List<float>();
            TargetIntensityArray = new List<float>();
            OneLeftIntensityArray = new List<float>();
            OneRightIntensityArray = new List<float>();
            TwoLeftIntensityArray = new List<float>();
            TwoRightInetnsityArray = new List<float>();
            ModelMzList = new List<float>();
        }
    }
    
    public sealed class MS2Dec {
        private MS2Dec() { }

        /// <summary>
        /// MS2dec deconvolution algotirhm
        /// </summary>
        public static MS2DecResult Process(List<List<double[]>> peaklistList, AnalysisParametersBean param, 
		                                   DataSummaryBean dataSummary, int targetScanNumber) {
            //Peak detections in MS/MS chromatogras
            var peakSpots = getPeakSpots(peaklistList, param, dataSummary); if (peakSpots.Count == 0) return null;

			//Maps the values of peak shape, symmetry, and qualiy of detected peaks into the array 
			//where the length is equal to the scan number
            var ms2decBinArray = getMs2DecBinArray(peakSpots, peaklistList[0]);

			//apply matched filter to extract 'metabolite components' 
			//where peaks having slightly (1-2 scan point diff) different retention times are merged. 
            var matchedFilterArray = getMatchedFileterArray(ms2decBinArray, param.SigmaWindowValue);
            
			//making model chromatograms by considering their peak qualities
			var modelChromatograms = getModelChromatograms(peaklistList, peakSpots, ms2decBinArray, matchedFilterArray, param, dataSummary.AveragePeakWidth);

            //What we have to do in DIA-MS data is first to link the peak tops of MS1 chromatogram and of MS2 chromatogram.
            //in the current program, if the peak top defference between MS1 and MS2 chromatograms is less than 1 scan point, they are recognized as the same metabolite.
            var minimumID = 0;						 
            var minimumDiff = double.MaxValue;		
            for (int i = 0; i < modelChromatograms.Count; i++) {
                var modelChrom = modelChromatograms[i];
                if (Math.Abs(targetScanNumber - modelChrom.ChromScanOfPeakTop) < minimumDiff) {
                    minimumDiff = Math.Abs(targetScanNumber - modelChrom.ChromScanOfPeakTop);
                    minimumID = i;
                }
            }

			//Run deconvolution program if the target feature related to MS1 precursor peak can be found in MS/MS chromatograms
            if (minimumDiff <= 2) {
				//considering adjacent model chromatograms to be considered as 'co-eluting' metabolites
                var modelChromVector = getModelChromatogramVector(minimumID, modelChromatograms, ms2decBinArray);

				//triming MS/MS chromatograms where the retention time range is equal to the range of model chromatogram vector
                var chromatograms = getMs2Chromatograms(modelChromVector, peaklistList, param);
                if (chromatograms.Count == 0) return null;
                return MS2DecCalc.Execute(modelChromVector, chromatograms);
            }
            else {
                return null;
            }
        }

        private static List<List<Peak>> getMs2Chromatograms(ModelChromVector modelChromVector, List<List<double[]>> peaklistList, AnalysisParametersBean param) {
            var peaksList = new List<List<Peak>>();
            foreach (var peaklist in peaklistList) {
                var peaks = getTrimedAndSmoothedPeaklist(peaklist, modelChromVector.ChromScanList[0], modelChromVector.ChromScanList[modelChromVector.ChromScanList.Count - 1]);
                var baselineCorrectedPeaks = getBaselineCorrectedPeaklist(peaks, modelChromVector.TargetScanTopInModelChromVector);
                var peaktopInt = baselineCorrectedPeaks[modelChromVector.TargetScanTopInModelChromVector].Intensity;

                if (peaktopInt <= param.AmplitudeCutoff) continue;

                peaksList.Add(baselineCorrectedPeaks);
            }

            return peaksList;
        }

		//Building model chromatogram vector
        private static ModelChromVector getModelChromatogramVector(int modelID, List<ModelChromatogram> modelChromatograms, Ms2DecBin[] ms2DecBins) {
            
            var modelChromLeft = modelChromatograms[modelID].ChromScanOfPeakLeft;
            var modelChromRight = modelChromatograms[modelID].ChromScanOfPeakRight;
            var isTwoLeftModel = false;
            var isOneLeftModel = false;
            var isTwoRightModel = false;
            var isOneRightModel = false;

            if (modelID > 1 && modelChromatograms[modelID - 2].ChromScanOfPeakRight > modelChromLeft) { isTwoLeftModel = true; }
            if (modelID > 0 && modelChromatograms[modelID - 1].ChromScanOfPeakRight > modelChromLeft) { isOneLeftModel = true; }
            if (modelID < modelChromatograms.Count - 2 && modelChromatograms[modelID + 2].ChromScanOfPeakLeft < modelChromRight) { isTwoRightModel = true; }
            if (modelID < modelChromatograms.Count - 1 && modelChromatograms[modelID + 1].ChromScanOfPeakLeft < modelChromRight) { isOneRightModel = true; }

            if (isTwoLeftModel) modelChromLeft = Math.Min(modelChromatograms[modelID].ChromScanOfPeakLeft, Math.Min(modelChromatograms[modelID - 2].ChromScanOfPeakLeft, modelChromatograms[modelID - 1].ChromScanOfPeakLeft));
            else if (isOneLeftModel) modelChromLeft = Math.Min(modelChromatograms[modelID].ChromScanOfPeakLeft, modelChromatograms[modelID - 1].ChromScanOfPeakLeft);

            if (isTwoRightModel) modelChromRight = Math.Max(modelChromatograms[modelID].ChromScanOfPeakRight, Math.Max(modelChromatograms[modelID + 2].ChromScanOfPeakRight, modelChromatograms[modelID + 1].ChromScanOfPeakRight));
            else if (isOneRightModel) modelChromRight = Math.Max(modelChromatograms[modelID].ChromScanOfPeakRight, modelChromatograms[modelID + 1].ChromScanOfPeakRight);

            var modelVector = new ModelChromVector() { ModelMzList = modelChromatograms[modelID].ModelMzList };
            for (int i = modelChromLeft; i <= modelChromRight; i++) {
                modelVector.ChromScanList.Add(i);
                modelVector.RtArray.Add(ms2DecBins[i].RetentionTime);

                if (modelChromatograms[modelID].ChromScanOfPeakLeft > i || modelChromatograms[modelID].ChromScanOfPeakRight < i) modelVector.TargetIntensityArray.Add(0);
                else modelVector.TargetIntensityArray.Add((float)modelChromatograms[modelID].Peaks[i - modelChromatograms[modelID].ChromScanOfPeakLeft].Intensity);

                if (isTwoLeftModel) {
                    if (modelChromatograms[modelID - 2].ChromScanOfPeakLeft > i || modelChromatograms[modelID - 2].ChromScanOfPeakRight < i) modelVector.TwoLeftIntensityArray.Add(0);
                    else modelVector.TwoLeftIntensityArray.Add((float)modelChromatograms[modelID - 2].Peaks[i - modelChromatograms[modelID - 2].ChromScanOfPeakLeft].Intensity);
                }

                if (isOneLeftModel || isTwoLeftModel) {
                    if (modelChromatograms[modelID - 1].ChromScanOfPeakLeft > i || modelChromatograms[modelID - 1].ChromScanOfPeakRight < i) modelVector.OneLeftIntensityArray.Add(0);
                    else modelVector.OneLeftIntensityArray.Add((float)modelChromatograms[modelID - 1].Peaks[i - modelChromatograms[modelID - 1].ChromScanOfPeakLeft].Intensity);
                }

                if (isTwoRightModel) {
                    if (modelChromatograms[modelID + 2].ChromScanOfPeakLeft > i || modelChromatograms[modelID + 2].ChromScanOfPeakRight < i) modelVector.TwoRightInetnsityArray.Add(0);
                    else modelVector.TwoRightInetnsityArray.Add((float)modelChromatograms[modelID + 2].Peaks[i - modelChromatograms[modelID + 2].ChromScanOfPeakLeft].Intensity);
                }

                if (isOneRightModel || isTwoRightModel) {
                    if (modelChromatograms[modelID + 1].ChromScanOfPeakLeft > i || modelChromatograms[modelID + 1].ChromScanOfPeakRight < i) modelVector.OneRightIntensityArray.Add(0);
                    else modelVector.OneRightIntensityArray.Add((float)modelChromatograms[modelID + 1].Peaks[i - modelChromatograms[modelID + 1].ChromScanOfPeakLeft].Intensity);
                }
            }
            modelVector.Ms2DecPattern = getMs2DecPattern(isOneLeftModel, isTwoLeftModel, isOneRightModel, isTwoRightModel);
            modelVector.TargetScanLeftInModelChromVector = modelChromatograms[modelID].ChromScanOfPeakLeft - modelChromLeft;
            modelVector.TargetScanTopInModelChromVector = modelVector.TargetScanLeftInModelChromVector + modelChromatograms[modelID].ChromScanOfPeakTop - modelChromatograms[modelID].ChromScanOfPeakLeft;
            modelVector.TargetScanRightInModelChromVector = modelVector.TargetScanLeftInModelChromVector + modelChromatograms[modelID].ChromScanOfPeakRight - modelChromatograms[modelID].ChromScanOfPeakLeft;

            return modelVector;
        }

        private static Ms2DecPattern getMs2DecPattern(bool isOneLeftModel, bool isTwoLeftModel, bool isOneRightModel, bool isTwoRightModel) {
            if (!isOneLeftModel && !isTwoLeftModel && !isOneRightModel && !isTwoRightModel) return Ms2DecPattern.C;
            if (isOneLeftModel && !isTwoLeftModel && !isOneRightModel && !isTwoRightModel) return Ms2DecPattern.BC;
            if (!isOneLeftModel && !isTwoLeftModel && isOneRightModel && !isTwoRightModel) return Ms2DecPattern.CD;
            if (isOneLeftModel && !isTwoLeftModel && isOneRightModel && !isTwoRightModel) return Ms2DecPattern.BCD;
            if (isOneLeftModel && isTwoLeftModel && !isOneRightModel && !isTwoRightModel) return Ms2DecPattern.ABC;
            if (!isOneLeftModel && !isTwoLeftModel && isOneRightModel && isTwoRightModel) return Ms2DecPattern.CDE;
            if (isOneLeftModel && isTwoLeftModel && isOneRightModel && !isTwoRightModel) return Ms2DecPattern.ABCD;
            if (isOneLeftModel && !isTwoLeftModel && isOneRightModel && isTwoRightModel) return Ms2DecPattern.BCDE;
            if (isOneLeftModel && isTwoLeftModel && isOneRightModel && isTwoRightModel) return Ms2DecPattern.ABCDE;
            return Ms2DecPattern.C;
        }

        private static List<ModelChromatogram> getModelChromatograms(List<List<double[]>> peaklistList, List<PeakAreaBean> peakSpots, Ms2DecBin[] ms2DecBinArray, double[] matchedFilterArray, AnalysisParametersBean param, double averagePeakWidth) {
            var regionMarkers = getRegionMarkers(matchedFilterArray);
            var modelChromatograms = new List<ModelChromatogram>();
         //   Debug.WriteLine("Regions count: {regionMarkers.Count}");
            foreach (var region in regionMarkers) {
                var peakAreas = new List<PeakAreaBean>();
                for (int i = region.ScanBegin; i <= region.ScanEnd; i++) {
                    foreach (var peakSpot in ms2DecBinArray[i].PeakSpots.Where(n => n.Quality == ModelQuality.High))
                        peakAreas.Add(peakSpots[peakSpot.PeakSpotID]);
                }
                if (peakAreas.Count == 0) {
                    for (int i = region.ScanBegin; i <= region.ScanEnd; i++) {
                        foreach (var peakSpot in ms2DecBinArray[i].PeakSpots.Where(n => n.Quality == ModelQuality.Middle))
                            peakAreas.Add(peakSpots[peakSpot.PeakSpotID]);
                    }
                }
                if (peakAreas.Count == 0) {
                    for (int i = region.ScanBegin; i <= region.ScanEnd; i++) {
                        foreach (var peakSpot in ms2DecBinArray[i].PeakSpots.Where(n => n.Quality == ModelQuality.Low))
                            peakAreas.Add(peakSpots[peakSpot.PeakSpotID]);
                    }
                }
                var modelChrom = getModelChromatogram(peaklistList, peakAreas, ms2DecBinArray, param, averagePeakWidth);
                if (modelChrom != null) modelChromatograms.Add(modelChrom);
            }

            return modelChromatograms;
        }

        private static ModelChromatogram getModelChromatogram(List<List<double[]>> peaklistList, List<PeakAreaBean> peakAreas, Ms2DecBin[] ms2DecBins, AnalysisParametersBean param, double averagePeakWidth) {
            if (peakAreas == null || peakAreas.Count == 0) return null;
            var maxSharpnessValue = peakAreas.Max(n => n.ShapenessValue);
            var maxIdealSlopeValue = peakAreas.Max(n => n.IdealSlopeValue);
            var modelChrom = new ModelChromatogram() { SharpnessValue = maxSharpnessValue, IdealSlopeValue = maxIdealSlopeValue };
            var firstFlg = false;

            var peaklist = new List<Peak>();
            var peaklists = new List<List<Peak>>();
            var baselineCorrectedPeaklist = new List<Peak>();

            foreach (var peak in peakAreas.Where(n => n.ShapenessValue >= maxSharpnessValue * 0.9).OrderByDescending(n => n.ShapenessValue)) {
                if (firstFlg == false) {
                    modelChrom.ChromScanOfPeakTop = peak.ScanNumberAtPeakTop;
                    modelChrom.ChromScanOfPeakLeft = peak.ScanNumberAtLeftPeakEdge;
                    modelChrom.ChromScanOfPeakRight = peak.ScanNumberAtRightPeakEdge;
                    modelChrom.ModelMzList.Add(peak.AccurateMass);

                    peaklist = getTrimedAndSmoothedPeaklist(peaklistList[peak.PeakID], modelChrom.ChromScanOfPeakLeft, modelChrom.ChromScanOfPeakRight);
                    baselineCorrectedPeaklist = getBaselineCorrectedPeaklist(peaklist, modelChrom.ChromScanOfPeakTop - modelChrom.ChromScanOfPeakLeft);
                    peaklists.Add(baselineCorrectedPeaklist);
                    firstFlg = true;
                }
                else {
                    modelChrom.ModelMzList.Add(peak.AccurateMass);
                    peaklist = getTrimedAndSmoothedPeaklist(peaklistList[peak.PeakID], modelChrom.ChromScanOfPeakLeft, modelChrom.ChromScanOfPeakRight);
                    baselineCorrectedPeaklist = getBaselineCorrectedPeaklist(peaklist, modelChrom.ChromScanOfPeakTop - modelChrom.ChromScanOfPeakLeft);
                    peaklists.Add(baselineCorrectedPeaklist);
                }
            }

            double mzCount = modelChrom.ModelMzList.Count;
            foreach (var peak in peaklists[0]) {
                modelChrom.Peaks.Add(new Peak() { Mz = peak.Mz, Intensity = peak.Intensity /= mzCount, RetentionTime = peak.RetentionTime, ScanNumber = peak.ScanNumber });
            }
            if (peaklist.Count > 1) {
                for (int i = 1; i < peaklists.Count; i++) {
                    for (int j = 0; j < peaklists[i].Count; j++) {
                        modelChrom.Peaks[j].Intensity += peaklists[i][j].Intensity /= mzCount;
                    }
                }
            }
            modelChrom = getRefinedModelChromatogram(modelChrom, ms2DecBins, param, averagePeakWidth);

            return modelChrom;
        }

        private static ModelChromatogram getRefinedModelChromatogram(ModelChromatogram modelChrom, Ms2DecBin[] ms2DecBins, AnalysisParametersBean param, double averagePeakWidth) {
            double maxIntensity = double.MinValue;
            int peakTopID = -1, peakLeftID = -1, peakRightID = -1;
            for (int i = 0; i < modelChrom.Peaks.Count; i++)
                if (modelChrom.Peaks[i].Intensity > maxIntensity) { maxIntensity = modelChrom.Peaks[i].Intensity; peakTopID = i; }

            modelChrom.MaximumPeakTopValue = maxIntensity;
            //left spike check
            for (int i = peakTopID; i > 0; i--) {
                if (peakTopID - i < averagePeakWidth * 0.5) continue;
                if (modelChrom.Peaks[i - 1].Intensity >= modelChrom.Peaks[i].Intensity) { peakLeftID = i; break; }
            }
            if (peakLeftID < 0) peakLeftID = 0;

            //right spike check
            for (int i = peakTopID; i < modelChrom.Peaks.Count - 1; i++) {
                if (i - peakTopID < averagePeakWidth * 0.5) continue;
                if (modelChrom.Peaks[i].Intensity <= modelChrom.Peaks[i + 1].Intensity) { peakRightID = i; break; }
            }
            if (peakRightID < 0) peakRightID = modelChrom.Peaks.Count - 1;

            modelChrom.ChromScanOfPeakTop = peakTopID + modelChrom.ChromScanOfPeakLeft;
            modelChrom.ChromScanOfPeakRight = peakRightID + modelChrom.ChromScanOfPeakLeft;
            modelChrom.ChromScanOfPeakLeft = peakLeftID + modelChrom.ChromScanOfPeakLeft;

            var peaks = new List<Peak>();
            for (int i = peakLeftID; i <= peakRightID; i++)
                peaks.Add(modelChrom.Peaks[i]);

            //final curation
            if (peakTopID - peakLeftID < 3) return null;
            if (peakRightID - peakTopID < 3) return null;

            var peaktopRT = modelChrom.Peaks[peakTopID].RetentionTime;
            var peaktopMz = modelChrom.ModelMzList[0];

            modelChrom.Peaks = peaks;

            return modelChrom;
        }

        private static List<RegionMarker> getRegionMarkers(double[] matchedFilterArray) {
            var regionMarkers = new List<RegionMarker>();
            var scanBegin = 0;
            var scanBeginFlg = false;
            var margin = 5;

            for (int i = margin; i < matchedFilterArray.Length - margin; i++) {

                if (matchedFilterArray[i] > 0 && matchedFilterArray[i - 1] < matchedFilterArray[i] && scanBeginFlg == false) {
                    scanBegin = i; scanBeginFlg = true;
                }
                else if (scanBeginFlg == true) {
                    if (matchedFilterArray[i] <= 0) {
                        regionMarkers.Add(new RegionMarker() { ID = regionMarkers.Count, ScanBegin = scanBegin, ScanEnd = i - 1 });

                        scanBeginFlg = false;
                        continue;
                    }
                    else if (matchedFilterArray[i] >= 0 && matchedFilterArray[i - 1] > matchedFilterArray[i] && matchedFilterArray[i] < matchedFilterArray[i + 1]) {
                        regionMarkers.Add(new RegionMarker() { ID = regionMarkers.Count, ScanBegin = scanBegin, ScanEnd = i });
                        scanBegin = i + 1;
                        scanBeginFlg = true;
                        i++;
                    }
                    //else if (matchedFilterArray[i] >= 0 && 
                    //    ((matchedFilterArray[i - 1] >= matchedFilterArray[i] && matchedFilterArray[i] < matchedFilterArray[i + 1]) || 
                    //    (matchedFilterArray[i - 1] > matchedFilterArray[i] && matchedFilterArray[i] <= matchedFilterArray[i + 1]))) {
                    //    regionMarkers.Add(new RegionMarker() { ID = regionMarkers.Count, ScanBegin = scanBegin, ScanEnd = i });
                    //    scanBegin = i + 1;
                    //    scanBeginFlg = true;
                    //    i++;
                    //}
                }
            }

            return regionMarkers;
        }

        private static Ms2DecBin[] getMs2DecBinArray(List<PeakAreaBean> peakSpots, List<double[]> peaklist) {

            var ms2DecBins = new Ms2DecBin[peaklist.Count];

            for (int i = 0; i < ms2DecBins.Length; i++) {
                ms2DecBins[i] = new Ms2DecBin();
                ms2DecBins[i].RetentionTime = (float)peaklist[i][1];
            }

            for (int i = 0; i < peakSpots.Count; i++) {
                var spot = peakSpots[i];
                var scan = peakSpots[i].ScanNumberAtPeakTop;
                var model = new PeakSpot() { PeakSpotID = i, ChromatogramID = spot.PeakID };

                if (spot.IdealSlopeValue > 0.999) model.Quality = ModelQuality.High;
                else if (spot.IdealSlopeValue > 0.9) model.Quality = ModelQuality.Middle;
                else model.Quality = ModelQuality.Low;

                if (model.Quality == ModelQuality.High || model.Quality == ModelQuality.Middle) {
                    if (ms2DecBins[scan].TotalSharpnessValue < spot.ShapenessValue) {
                        ms2DecBins[scan].TotalSharpnessValue = spot.ShapenessValue;
                    }
                    //ms2DecBins[scan].TotalSharpnessValue += spot.ShapenessValue;
                }

                ms2DecBins[scan].PeakSpots.Add(model);
            }

            return ms2DecBins;
        }

        private static double[] getMatchedFileterArray(Ms2DecBin[] ms2DecBinArray, double sigma) {
            var halfPoint = 10.0; // currently this value should be enough for LC
            var matchedFilterArray = new double[ms2DecBinArray.Length];
            var matchedFilterCoefficient = new double[2 * (int)halfPoint + 1];

            for (int i = 0; i < matchedFilterCoefficient.Length; i++)
                matchedFilterCoefficient[i] = (1 - Math.Pow((-halfPoint + i) / sigma, 2)) * Math.Exp(-0.5 * Math.Pow((-halfPoint + i) / sigma, 2));

            for (int i = 0; i < ms2DecBinArray.Length; i++) {
                var sum = 0.0;
                for (int j = -1 * (int)halfPoint; j <= (int)halfPoint; j++) {
                    if (i + j < 0) sum += 0;
                    else if (i + j > ms2DecBinArray.Length - 1) sum += 0;
                    else sum += ms2DecBinArray[i + j].TotalSharpnessValue * matchedFilterCoefficient[(int)(j + halfPoint)];
                }
                matchedFilterArray[i] = sum;
            }

            return matchedFilterArray;
        }

        /// <summary>
        /// Get the result of peak spotting
        /// </summary>
        private static List<PeakAreaBean> getPeakSpots(List<List<double[]>> peaklistList, AnalysisParametersBean param, DataSummaryBean dataSummary) {
            var peakSpots = new List<PeakAreaBean>();

            for (int i = 0; i < peaklistList.Count; i++) {
                var minDatapoints = param.MinimumDatapoints;
                var minAmps = param.MinimumAmplitude;
                var results = PeakDetection.PeakDetectionVS1(minDatapoints, minAmps, peaklistList[i]);
                //var results = PeakDetection.GetDetectedPeakInformationListFromDifferentialBasedPeakDetectionAlgorithm(
                //    param.MinimumDatapoints, param.MinimumAmplitude, param.AmplitudeNoiseFactor, param.SlopeNoiseFactor, param.PeaktopNoiseFactor, (int)dataSummary.AveragePeakWidth, peaklistList[i]
                //    );
                if (results == null || results.Count == 0) continue;
                //save as peakarea bean
                foreach (var result in results) {
                    var peakSpot = new PeakAreaBean() {
                        PeakID = i,
                        AccurateMass = (float)peaklistList[i][result.ScanNumAtPeakTop][2], //mz
                        RtAtPeakTop = result.RtAtPeakTop,
                        RtAtLeftPeakEdge = result.RtAtLeftPeakEdge,
                        RtAtRightPeakEdge = result.RtAtRightPeakEdge,
                        ScanNumberAtPeakTop = result.ScanNumAtPeakTop,
                        ScanNumberAtLeftPeakEdge = result.ScanNumAtLeftPeakEdge,
                        ScanNumberAtRightPeakEdge = result.ScanNumAtRightPeakEdge,
                        IntensityAtPeakTop = result.IntensityAtPeakTop,
                        IntensityAtLeftPeakEdge = result.IntensityAtLeftPeakEdge,
                        IntensityAtRightPeakEdge = result.IntensityAtRightPeakEdge,
                        IdealSlopeValue = result.IdealSlopeValue,
                        ShapenessValue = result.ShapnessValue
                    };

                    peakSpots.Add(peakSpot);
                }
            }

            return peakSpots;
        }

        private static List<Peak> getTrimedAndSmoothedPeaklist(List<double[]> peaklist, int chromScanOfPeakLeft, int chromScanOfPeakRight) {
            var mPeaklist = new List<Peak>();

            for (int i = chromScanOfPeakLeft; i <= chromScanOfPeakRight; i++) {
                var peak = new Peak() {
                    ScanNumber = (int)peaklist[i][0],
                    RetentionTime = peaklist[i][1],
                    Mz = peaklist[i][2],
                    Intensity = peaklist[i][3]
                };

                mPeaklist.Add(peak);
            }

            return mPeaklist;
        }

        private static List<Peak> getBaselineCorrectedPeaklist(List<Peak> peaklist, int peakTop) {
            var baselineCorrectedPeaklist = new List<Peak>();

            //find local minimum of left and right edge
            var minimumLeftID = 0;
            var minimumValue = double.MaxValue;
            for (int i = peakTop; i >= 0; i--)
                if (peaklist[i].Intensity < minimumValue) {
                    minimumValue = peaklist[i].Intensity;
                    minimumLeftID = i;
                }

            var minimumRightID = peaklist.Count - 1;
            minimumValue = double.MaxValue;
            for (int i = peakTop; i < peaklist.Count; i++)
                if (peaklist[i].Intensity < minimumValue) {
                    minimumValue = peaklist[i].Intensity;
                    minimumRightID = i;
                }

            double coeff = (peaklist[minimumRightID].Intensity - peaklist[minimumLeftID].Intensity) / (peaklist[minimumRightID].RetentionTime - peaklist[minimumLeftID].RetentionTime);
            double intercept = (peaklist[minimumRightID].RetentionTime * peaklist[minimumLeftID].Intensity - peaklist[minimumLeftID].RetentionTime * peaklist[minimumRightID].Intensity) / (peaklist[minimumRightID].RetentionTime - peaklist[minimumLeftID].RetentionTime);
            double correctedIntensity = 0;
            for (int i = 0; i < peaklist.Count; i++) {
                correctedIntensity = peaklist[i].Intensity - (int)(coeff * peaklist[i].RetentionTime + intercept);
                if (correctedIntensity >= 0)
                    baselineCorrectedPeaklist.Add(new Peak() { ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = correctedIntensity });
                else
                    baselineCorrectedPeaklist.Add(new Peak() { ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = 0 }); ;
            }

            return baselineCorrectedPeaklist;
        }
    }
}
