using CompMs.Common.DataObj;
using Msdial.Gcms.Dataprocess.Utility;
using NSSplash;
using NSSplash.impl;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Msdial.Gcms.Dataprocess.Algorithm {

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
	public enum Ms1DecPattern { C, BC, CD, ABC, BCD, CDE, ABCD, BCDE, ABCDE }

	/// <summary>
	/// Now, the model quality is determined by ad-hoc criteria considering 
	/// A) ideal slope, B) peak shape, and C) abundance information
	/// </summary>
    public enum ModelQuality { High, Middle, Low }

	/// <summary>
    /// stores the sharpness value and retention time in each data point
    /// </summary>
    public class GcmsDecBin
    {
        public double TotalSharpnessValue { get; set; }
        public int RdamScanNumber { get; set; }
        public float RetentionTime { get; set; }
        public List<PeakSpot> PeakSpots { get; set; }

        public GcmsDecBin() { TotalSharpnessValue = 0; PeakSpots = new List<PeakSpot>(); }
    }

    public class PeakSpot
    {
        public ModelQuality Quality { get; set; }
        public int PeakSpotID { get; set; }

        public PeakSpot() { Quality = ModelQuality.Low; }
    }

    public class RegionMarker
    {
        public int ID { get; set; }
        public int ScanBegin { get; set; }
        public int ScanEnd { get; set; }
    }

	/// <summary>
	/// Definition of model chromatogram
	/// </summary>
    public class ModelChromatogram
    {
        public List<Peak> Peaks { get; set; }

        public int RdamScanOfPeakTop { get; set; }
        public int ChromScanOfPeakTop { get; set; }
        public int ChromScanOfPeakLeft { get; set; }
        public int ChromScanOfPeakRight { get; set; }
        
        public List<float> ModelMzList { get; set; }

        public double IdealSlopeValue { get; set; }
        public double SharpnessValue { get; set; }
        public double MaximumPeakTopValue { get; set; }

        public float EstimatedNoise { get; set; }
        public float SignalToNoise { get; set; }

        public override string ToString() {
			return string.Format("scan at top:{0}, scan at left:{1}, scan at top:{2}, scan at right:{3}, ideal slope:{4}, sharpness:{5}, max peaktop:{6}\n\t" +
				"model mz:[{7}]\n\t" +
				"peaks:[{8}]", RdamScanOfPeakTop, ChromScanOfPeakLeft, ChromScanOfPeakTop, ChromScanOfPeakRight, IdealSlopeValue, SharpnessValue, MaximumPeakTopValue,
				string.Join("; ", ModelMzList), string.Join(";\n\t", Peaks.Select(p => "mz:" + p.Mz + " int:" + p.Intensity + " rt:" + p.RetentionTime)));
		}

		public ModelChromatogram()
        {
            Peaks = new List<Peak>();
            ModelMzList = new List<float>();
            RdamScanOfPeakTop = 0;
            ChromScanOfPeakTop = 0;
            ChromScanOfPeakLeft = 0;
            ChromScanOfPeakRight = 0;
            IdealSlopeValue = 0;
            MaximumPeakTopValue = 0;
            SharpnessValue = 0;
            SignalToNoise = 0;
            EstimatedNoise = 1.0F;
        }
    }

	/// <summary>
	/// Model chromatogram vector.
	/// This vector currently can contain up to 4 model chromatograms in addition to one targeted model.
	/// </summary>
    public class ModelChromVector
    {
        public Ms1DecPattern Ms1DecPattern { get; set; }

        public int TargetScanLeftInModelChromVector { get; set; }
        public int TargetScanTopInModelChromVector { get; set; }
        public int TargetScanRightInModelChromVector { get; set; }

        public List<int> ChromScanList { get; set; }
        public List<int> RdamScanList { get; set; }
        public List<float> RtArray { get; set; }
        public List<float> TargetIntensityArray { get; set; }
        
        public List<float> OneLeftIntensityArray { get; set; }
        public List<float> OneRightIntensityArray { get; set; }

        public List<float> TwoLeftIntensityArray { get; set; }
        public List<float> TwoRightInetnsityArray { get; set; }

        public List<float> ModelMzList { get; set; }
		public float Sharpness { get; set; }
        public float EstimatedNoise { get; set; }

        public ModelChromVector()
        {
            Ms1DecPattern = Ms1DecPattern.C;
            ChromScanList = new List<int>();
            RdamScanList = new List<int>();
            RtArray = new List<float>();
            TargetIntensityArray = new List<float>();
            OneLeftIntensityArray = new List<float>();
            OneRightIntensityArray = new List<float>();
            TwoLeftIntensityArray = new List<float>();
            TwoRightInetnsityArray = new List<float>();
            ModelMzList = new List<float>();
			Sharpness = 0.0F;
            EstimatedNoise = 1.0F;
        }
    }

    public sealed class Deconvolution
    {
        private Deconvolution() { }

        private const double initialProgress = 30.0;
        private const double progressMax = 30.0;

		/// <summary>
		/// Gcmses the MS 1 dec results.
		/// </summary>
        public static List<MS1DecResult> GetMS1DecResults(List<RawSpectrum> spectrumList, List<PeakAreaBean> peakAreaList, AnalysisParamOfMsdialGcms param, Action<int> reportAction)
        {
            peakAreaList = peakAreaList.OrderBy(n => n.ScanNumberAtPeakTop).ThenBy(n => n.AccurateMass).ToList();

			//Get scan ID dictionary between RDAM scan ID and MS1 chromatogram scan ID.
			//note that there is a possibility that raw data contains MS/MS information as well.
            var rdamToChromDict = getRdamAndMs1chromatogramScanDictionary(spectrumList, param.IonMode);
			//var ticPeaklist = DataAccessGcUtility.GetTicPeaklist(spectrumList, param.IonMode); //delete this after figure prepared (hiroshi)
            
			//Maps the values of peak shape, symmetry, and qualiy of detected peaks into the array 
			//where the length is equal to the scan number
			var gcmsDecBinArray = getGcmsBinArray(spectrumList, peakAreaList, rdamToChromDict, param.IonMode);
            
			//apply matched filter to extract 'metabolite components' 
			//where EIC peaks having slightly (1-2 scan point diff) different retention times are merged. 
			var matchedFilterArray = getMatchedFileterArray(gcmsDecBinArray, param.SigmaWindowValue);
            
			//making model chromatograms by considering their peak qualities
			var modelChromatograms = getModelChromatograms(spectrumList, peakAreaList, gcmsDecBinArray, matchedFilterArray, rdamToChromDict, param);
			//exportArraysForPaper(ticPeaklist, gcmsDecBinArray, matchedFilterArray, modelChromatograms, peakAreaList); //delete this after figure prepared (hiroshi)

			//exclude duplicate model chromatograms which have the complete same retention time's peak tops
            modelChromatograms = getRefinedModelChromatograms(modelChromatograms);

			var ms1DecResults = new List<MS1DecResult>();
            var counter = 0;
            var minIntensity = double.MaxValue;
			var maxIntensity = double.MinValue;
			for (int i = 0; i < modelChromatograms.Count; i++) { //do deconvolution at each model chromatogram area

				//consider adjacent model chromatograms to be considered as 'co-eluting' metabolites
				var modelChromVector = getModelChromatogramVector(i, modelChromatograms, gcmsDecBinArray);

				//to get triming EIC chromatograms where the retention time range is equal to the range of model chromatogram vector
				var ms1Chromatograms = getMs1Chromatograms(spectrumList, modelChromVector, gcmsDecBinArray, modelChromatograms[i].ChromScanOfPeakTop, param);
				var ms1DecResult = MS1Dec.GetMs1DecResult(modelChromVector, ms1Chromatograms); //get MS1Dec result

				//Debug.WriteLine("model count: " + modelChromatograms.Count + "  -- ms1 Count: " + ms1Chromatograms.Count + " -- ms1Dec count: " + ms1DecResults.Count);

				if (ms1DecResult != null && ms1DecResult.Spectrum.Count > 0)
                {
                    ms1DecResult.Ms1DecID = counter;
                    ms1DecResult = getRefinedMs1DecResult(ms1DecResult, param);
					ms1DecResult.Splash = calculateSplash(ms1DecResult);
                    ms1DecResults.Add(ms1DecResult);

					if (ms1DecResult.BasepeakHeight < minIntensity) minIntensity = ms1DecResult.BasepeakHeight;
                    if (ms1DecResult.BasepeakHeight > maxIntensity) maxIntensity = ms1DecResult.BasepeakHeight;

                    counter++;
                }
                progressReports(i, modelChromatograms.Count, reportAction);
            }

			foreach (var ms1DecResult in ms1DecResults) {
				ms1DecResult.AmplitudeScore = (float)((ms1DecResult.BasepeakHeight - minIntensity) / (maxIntensity - minIntensity));

				//calculating purity
				//Debug.WriteLine("tic count: " + spectrumList.FindAll(sp => sp.ScanNum == ms1DecResult.ScanNumber).Count);
				//Debug.WriteLine("deconv count: " + ms1DecResult.Spectrum.Count);
				var tic = spectrumList.Find(sp => sp.ScanNumber == ms1DecResult.ScanNumber).TotalIonCurrent;
				//Debug.WriteLine("TIC: " + tic + " -- RT: " + spectrumList.Find(sp => sp.ScanNum == ms1DecResult.ScanNumber).RTmin);

				var eic = ms1DecResult.Spectrum.Sum(s => s.Intensity);
				ms1DecResult.ModelPeakPurity = (float)(eic / tic);
				//Debug.WriteLine("EIC: " + ms1DecResult.Spectrum.Sum(s => s.Intensity) + " -- RT: " + ms1DecResult.Spectrum.Average(s => s.RetentionTime));
				//Debug.WriteLine("purity: " + ms1DecResult.ModelPeakPurity.ToString() + " \n");
				
			}
			return ms1DecResults;
        }

        public static void ReplaceQuantmassByUserdefinedValue(List<RawSpectrum> spectrumList, List<MS1DecResult> ms1DecResults, 
            AnalysisParamOfMsdialGcms param, List<MspFormatCompoundInformationBean> mspDB) {
            if (ms1DecResults != null && mspDB != null) {
                foreach (var result in ms1DecResults) {
                    var mspID = result.MspDbID;
                    if (mspID >= 0 && mspID < mspDB.Count) {
                        var udQuantmass = mspDB[mspID].QuantMass;
                        if (udQuantmass <= 1) continue;
                        if (udQuantmass < param.MassRangeBegin || udQuantmass > param.MassRangeEnd) continue;
                        RecalculateMs1decResultByDefinedQuantmass(result, spectrumList, udQuantmass, param);
                    }
                }
            }
        }

        private static void exportArraysForPaper(List<double[]> ticPeaklist, GcmsDecBin[] gcmsDecBinArray, double[] matchedFilterArray, List<ModelChromatogram> modelChromatograms, List<PeakAreaBean> peakAreaList)
        {
            var peaklistFilePath = @"C:\Users\hiroshi.tsugawa\Desktop\MSDIAL-peaklists.txt";
            var modelChromsFilePath = @"C:\Users\hiroshi.tsugawa\Desktop\MSDIAL-modelchroms.txt";
            using (var sw = new StreamWriter(peaklistFilePath, false, Encoding.ASCII))
            {
                sw.WriteLine("Scan\tRT\tmz\tTIC\tTotal sharpness value\tPeak spot info\tmatched value");
                for (int i = 0; i < ticPeaklist.Count; i++)
                {
                    var peak = ticPeaklist[i];
                    var gcBin = gcmsDecBinArray[i];
                    var matchedValue = matchedFilterArray[i];

                    var peakSpotString = string.Empty;
                    foreach (var spot in gcBin.PeakSpots){
                        var spotID = spot.PeakSpotID;
                        var quality = spot.Quality.ToString();
                        var spotMz = peakAreaList[spotID].AccurateMass;
                        peakSpotString += "[" + Math.Round(spotMz, 4) + ", " + quality + "] "; 
                    }

                    sw.WriteLine(peak[0] + "\t" + peak[1] + "\t" + peak[2] + "\t" + peak[3] + "\t" + gcBin.TotalSharpnessValue + "\t" + peakSpotString + "\t" + matchedValue);
                }
            }

            using (var sw = new StreamWriter(modelChromsFilePath, false, Encoding.ASCII))
            {
                foreach (var chrom in modelChromatograms)
                {
                    sw.WriteLine("SCAN: " + chrom.ChromScanOfPeakTop);
                    sw.WriteLine("IdealSlope: " + chrom.IdealSlopeValue);
                    var modelMzList = string.Empty;
                    for (int i = 0; i < chrom.ModelMzList.Count; i++)
                    {
                        if (i == chrom.ModelMzList.Count - 1)
                            modelMzList += Math.Round(chrom.ModelMzList[i], 4).ToString();
                        else
                            modelMzList += Math.Round(chrom.ModelMzList[i], 4).ToString() + ", ";
                    }
                    sw.WriteLine("ModelMZs: " + modelMzList);
                    sw.WriteLine("Num Peaks: " + chrom.Peaks.Count);
                    foreach (var peak in chrom.Peaks)
                    {
                        sw.WriteLine(peak.ScanNumber + "\t" + peak.RetentionTime + "\t" + peak.Mz + "\t" + peak.Intensity);
                    }
                    sw.WriteLine();
                }
            }
        }

		private static string calculateSplash(MS1DecResult peak) {
			var ions = new List<Ion>();
			peak.Spectrum.ForEach(it => ions.Add(new Ion(it.Mz, it.Intensity)));
			string splash = new Splash().splashIt(new MSSpectrum(ions));
			return splash;
		}

        private static List<ModelChromatogram> getRefinedModelChromatograms(List<ModelChromatogram> modelChromatograms)
        {
            modelChromatograms = modelChromatograms.OrderBy(n => n.ChromScanOfPeakTop).ToList();

            var chromatograms = new List<ModelChromatogram>() { modelChromatograms[0] };
            for (int i = 1; i < modelChromatograms.Count; i++) {
                if (chromatograms[chromatograms.Count - 1].ChromScanOfPeakTop == modelChromatograms[i].ChromScanOfPeakTop) {
                    if (chromatograms[chromatograms.Count - 1].MaximumPeakTopValue < modelChromatograms[i].MaximumPeakTopValue) {
                        chromatograms.RemoveAt(chromatograms.Count - 1);
                        chromatograms.Add(modelChromatograms[i]);
                    }
                } else {
                    chromatograms.Add(modelChromatograms[i]);
                }
            }

            return chromatograms;
        }

        private static MS1DecResult getRefinedMs1DecResult(MS1DecResult ms1DecResult, AnalysisParamOfMsdialGcms param)
        {
            if (param.AccuracyType == AccuracyType.IsNominal) return ms1DecResult;

            var spectrum = new List<Peak>() { ms1DecResult.Spectrum[0] };
            for (int i = 1; i < ms1DecResult.Spectrum.Count; i++)
            {
                if (i > ms1DecResult.Spectrum.Count - 1)  break;
                if (Math.Round(spectrum[spectrum.Count - 1].Mz, 4) != Math.Round(ms1DecResult.Spectrum[i].Mz, 4))
                    spectrum.Add(ms1DecResult.Spectrum[i]);
                else
                {
                    if (spectrum[spectrum.Count - 1].Intensity < ms1DecResult.Spectrum[i].Intensity)
                    {
                        spectrum.RemoveAt(spectrum.Count - 1);
                        spectrum.Add(ms1DecResult.Spectrum[i]);
                    }
                }
            }
            if (spectrum.Count > 0)
            {
                var maxIntensity = spectrum.Max(n => n.Intensity);
                spectrum = spectrum.Where(n => n.Intensity > maxIntensity * 0.001).ToList();
                ms1DecResult.Spectrum = spectrum;
            }
            
            return ms1DecResult;
        }
        
        public static void RecalculateMs1decResultByDefinedQuantmass(MS1DecResult ms1DecResult, List<RawSpectrum> spectrumList,
            float quantMass, AnalysisParamOfMsdialGcms param) {

            ms1DecResult.BasepeakMz = quantMass;
            ms1DecResult.BasepeakHeight = 1;
            ms1DecResult.BasepeakArea = 1;

            var spectrum = ms1DecResult.Spectrum;
            var chromatogram = ms1DecResult.BasepeakChromatogram;
            var estimatedNoise = ms1DecResult.EstimatedNoise;

            var peaktopRt = ms1DecResult.RetentionTime;
            var rtBegin = chromatogram[0].RetentionTime;
            var rtEnd = chromatogram[chromatogram.Count - 1].RetentionTime;
            var peakWidth = chromatogram[chromatogram.Count - 1].RetentionTime - chromatogram[0].RetentionTime;
            if (peakWidth < 0.02) peakWidth = 0.02;
            var rtTol = peakWidth * 0.5;

            var peaklist = DataAccessGcUtility.GetBaselineCorrectedPeaklistByMassAccuracy(
                spectrumList, peaktopRt, (float)rtBegin, (float)rtEnd, quantMass, param);

            var sPeaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            if (sPeaklist.Count != 0) {

                var maxID = -1;
                var maxInt = double.MinValue;
                var minRtId = -1;
                var minRt = double.MaxValue;

                for (int i = 1; i < sPeaklist.Count - 1; i++) {
                    if (sPeaklist[i].RetentionTime < peaktopRt - rtTol) continue;
                    if (peaktopRt + rtTol < sPeaklist[i].RetentionTime) break;

                    if (maxInt < sPeaklist[i].Intensity &&
                        sPeaklist[i - 1].Intensity <= sPeaklist[i].Intensity &&
                        sPeaklist[i].Intensity >= sPeaklist[i + 1].Intensity) {

                        maxInt = sPeaklist[i].Intensity;
                        maxID = i;
                    }
                    if (Math.Abs(sPeaklist[i].RetentionTime - peaktopRt) < minRt) {
                        minRt = sPeaklist[i].RetentionTime;
                        minRtId = i;
                    }
                }

                if (minRtId == -1) {
                    minRtId = 0;
                }

                if (maxID == -1) {
                    maxID = minRtId;
                    maxInt = sPeaklist[minRtId].Intensity;
                }

                //seeking left edge
                var minLeftInt = sPeaklist[maxID].Intensity;
                var minLeftId = -1;
                for (int i = maxID - 1; i >= 1; i--) {

                    if (i < maxID && minLeftInt < sPeaklist[i].Intensity && sPeaklist[i].Intensity < sPeaklist[i - 1].Intensity) {
                        break;
                    }

                    if (minLeftInt >= sPeaklist[i].Intensity) {
                        minLeftInt = sPeaklist[i].Intensity;
                        minLeftId = i;
                    }
                }

                if (minLeftId == -1) {
                    minLeftId = 0;
                }

                //seeking right edge
                var minRightInt = sPeaklist[maxID].Intensity;
                var minRightId = -1;
                for (int i = maxID + 1; i < sPeaklist.Count - 2; i++) {
                    if (i > maxID && minRightInt < sPeaklist[i].Intensity && sPeaklist[i].Intensity < sPeaklist[i + 1].Intensity) {
                        break;
                    }
                    if (minRightInt >= sPeaklist[i].Intensity) {
                        minRightInt = sPeaklist[i].Intensity;
                        minRightId = i;
                    }
                }

                if (minRightId == -1) {
                    minRightId = sPeaklist.Count - 1;
                }

                var peakAreaAboveZero = 0.0;
                for (int i = minLeftId; i <= minRightId - 1; i++) {
                    peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) *
                        (sPeaklist[i + 1].RetentionTime - sPeaklist[i].RetentionTime) * 0.5;
                }

                var peakHeightFromBaseline = Math.Max(maxInt - sPeaklist[0].Intensity, maxInt - sPeaklist[spectrumList.Count - 1].Intensity);

                ms1DecResult.BasepeakHeight = (float)maxInt;
                ms1DecResult.BasepeakArea = (float)peakAreaAboveZero;

                ms1DecResult.SignalNoiseRatio = (float)(peakHeightFromBaseline / estimatedNoise);
            }
        }

        private static List<List<Peak>> getMs1Chromatograms(List<RawSpectrum> spectrumList, ModelChromVector modelChromVector, GcmsDecBin[] gcmsDecBins, int chromScanOfPeakTop, AnalysisParamOfMsdialGcms param)
        {
            var rdamScan = gcmsDecBins[chromScanOfPeakTop].RdamScanNumber;
            var massBin = param.MassAccuracy; if (param.AccuracyType == AccuracyType.IsNominal) massBin = 0.5F;
            var focusedMs1Spectrum = DataAccessGcUtility.GetCentroidMasasSpectra(spectrumList, param.DataType, rdamScan, massBin, param.AmplitudeCutoff, param.MassRangeBegin, param.MassRangeEnd);
            focusedMs1Spectrum = excludeMasses(focusedMs1Spectrum, param.ExcludedMassList);
            if (focusedMs1Spectrum.Count == 0) return new List<List<Peak>>();

            var rdamScanList = modelChromVector.RdamScanList;
            var peaksList = new List<List<Peak>>();
            foreach (var spec in focusedMs1Spectrum.Where(n => n[1] >= param.AmplitudeCutoff).OrderByDescending(n => n[1]))
            {
                var peaks = getTrimedAndSmoothedPeaklist(spectrumList, modelChromVector.ChromScanList[0], modelChromVector.ChromScanList[modelChromVector.ChromScanList.Count - 1], param.SmoothingLevel, gcmsDecBins, (float)spec[0], param);
                var baselineCorrectedPeaks = getBaselineCorrectedPeaklist(peaks, modelChromVector.TargetScanTopInModelChromVector);

                var peaktopRT = baselineCorrectedPeaks[modelChromVector.TargetScanTopInModelChromVector].RetentionTime;
                var peaktopMz = baselineCorrectedPeaks[modelChromVector.TargetScanTopInModelChromVector].Mz;

                peaksList.Add(baselineCorrectedPeaks);
            }

            return peaksList;
        }

        private static List<double[]> excludeMasses(List<double[]> focusedMs1Spectrum, List<ExcludeMassBean> excludedMassList) {
            if (excludedMassList == null || excludedMassList.Count == 0) return focusedMs1Spectrum;

            var cMasses = new List<double[]>();
            foreach (var pair in focusedMs1Spectrum) {
                var checker = false;
                foreach (var ePair in excludedMassList) {
                    if (Math.Abs(pair[0] - (double)ePair.ExcludedMass) < ePair.MassTolerance) {
                        checker = true;
                        break;
                    }
                }
                if (checker) continue;
                cMasses.Add(pair);
            }
            return cMasses;
        }

        private static ModelChromVector getModelChromatogramVector(int modelID, List<ModelChromatogram> modelChromatograms, GcmsDecBin[] gcmsDecBins)
        {
            var modelChromLeft = modelChromatograms[modelID].ChromScanOfPeakLeft;
            var modelChromRight = modelChromatograms[modelID].ChromScanOfPeakRight;
            var estimatedNoise = modelChromatograms[modelID].EstimatedNoise;
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
            for (int i = modelChromLeft; i <= modelChromRight; i++)
            {
                modelVector.ChromScanList.Add(i);
                modelVector.RdamScanList.Add(gcmsDecBins[i].RdamScanNumber);
                modelVector.RtArray.Add(gcmsDecBins[i].RetentionTime);
                
                if (modelChromatograms[modelID].ChromScanOfPeakLeft > i || modelChromatograms[modelID].ChromScanOfPeakRight < i)
					modelVector.TargetIntensityArray.Add(0);
                else
					modelVector.TargetIntensityArray.Add((float)modelChromatograms[modelID].Peaks[i - modelChromatograms[modelID].ChromScanOfPeakLeft].Intensity);

                if (isTwoLeftModel)
                {
                    if (modelChromatograms[modelID - 2].ChromScanOfPeakLeft > i || modelChromatograms[modelID - 2].ChromScanOfPeakRight < i) modelVector.TwoLeftIntensityArray.Add(0);
                    else modelVector.TwoLeftIntensityArray.Add((float)modelChromatograms[modelID - 2].Peaks[i - modelChromatograms[modelID - 2].ChromScanOfPeakLeft].Intensity);
                }
                
                if (isOneLeftModel || isTwoLeftModel)
                {
                    if (modelChromatograms[modelID - 1].ChromScanOfPeakLeft > i || modelChromatograms[modelID - 1].ChromScanOfPeakRight < i) modelVector.OneLeftIntensityArray.Add(0);
                    else modelVector.OneLeftIntensityArray.Add((float)modelChromatograms[modelID - 1].Peaks[i - modelChromatograms[modelID - 1].ChromScanOfPeakLeft].Intensity);
                }

                if (isTwoRightModel)
                {
                    if (modelChromatograms[modelID + 2].ChromScanOfPeakLeft > i || modelChromatograms[modelID + 2].ChromScanOfPeakRight < i) modelVector.TwoRightInetnsityArray.Add(0);
                    else modelVector.TwoRightInetnsityArray.Add((float)modelChromatograms[modelID + 2].Peaks[i - modelChromatograms[modelID + 2].ChromScanOfPeakLeft].Intensity);
                }

                if (isOneRightModel || isTwoRightModel)
                {
                    if (modelChromatograms[modelID + 1].ChromScanOfPeakLeft > i || modelChromatograms[modelID + 1].ChromScanOfPeakRight < i) modelVector.OneRightIntensityArray.Add(0);
                    else modelVector.OneRightIntensityArray.Add((float)modelChromatograms[modelID + 1].Peaks[i - modelChromatograms[modelID + 1].ChromScanOfPeakLeft].Intensity);
                }
            }
            modelVector.Ms1DecPattern = getMs1DecPattern(isOneLeftModel, isTwoLeftModel, isOneRightModel, isTwoRightModel);
            modelVector.TargetScanLeftInModelChromVector = modelChromatograms[modelID].ChromScanOfPeakLeft - modelChromLeft;
            modelVector.TargetScanTopInModelChromVector = modelVector.TargetScanLeftInModelChromVector + modelChromatograms[modelID].ChromScanOfPeakTop - modelChromatograms[modelID].ChromScanOfPeakLeft;
            modelVector.TargetScanRightInModelChromVector = modelVector.TargetScanLeftInModelChromVector + modelChromatograms[modelID].ChromScanOfPeakRight - modelChromatograms[modelID].ChromScanOfPeakLeft;
            modelVector.EstimatedNoise = estimatedNoise;

            return modelVector;
        }

        private static Ms1DecPattern getMs1DecPattern(bool isOneLeftModel, bool isTwoLeftModel, bool isOneRightModel, bool isTwoRightModel)
        {
            if (!isOneLeftModel && !isTwoLeftModel && !isOneRightModel && !isTwoRightModel) return Ms1DecPattern.C;
            if (isOneLeftModel && !isTwoLeftModel && !isOneRightModel && !isTwoRightModel) return Ms1DecPattern.BC;
            if (!isOneLeftModel && !isTwoLeftModel && isOneRightModel && !isTwoRightModel) return Ms1DecPattern.CD;
            if (isOneLeftModel && !isTwoLeftModel && isOneRightModel && !isTwoRightModel) return Ms1DecPattern.BCD;
            if (isOneLeftModel && isTwoLeftModel && !isOneRightModel && !isTwoRightModel) return Ms1DecPattern.ABC;
            if (!isOneLeftModel && !isTwoLeftModel && isOneRightModel && isTwoRightModel) return Ms1DecPattern.CDE;
            if (isOneLeftModel && isTwoLeftModel && isOneRightModel && !isTwoRightModel) return Ms1DecPattern.ABCD;
            if (isOneLeftModel && !isTwoLeftModel && isOneRightModel && isTwoRightModel) return Ms1DecPattern.BCDE;
            if (isOneLeftModel && isTwoLeftModel && isOneRightModel && isTwoRightModel) return Ms1DecPattern.ABCDE;
            return Ms1DecPattern.C;
        }

        private static List<ModelChromatogram> getModelChromatograms(List<RawSpectrum> spectrumList, 
            List<PeakAreaBean> peakAreaList, GcmsDecBin[] gcmsDecBinArray, double[] matchedFilterArray, Dictionary<int, int> rdamToChromDict, AnalysisParamOfMsdialGcms param)
        {
            var regionMarkers = getRegionMarkers(matchedFilterArray);
            var modelChromatograms = new List<ModelChromatogram>();

            foreach (var region in regionMarkers)
            {
                var peakAreas = new List<PeakAreaBean>();
                for (int i = region.ScanBegin; i <= region.ScanEnd; i++)
                {
                    foreach (var peakSpot in gcmsDecBinArray[i].PeakSpots.Where(n => n.Quality == ModelQuality.High))
                        peakAreas.Add(peakAreaList[peakSpot.PeakSpotID]);
                }
                if (peakAreas.Count == 0)
                {
                    for (int i = region.ScanBegin; i <= region.ScanEnd; i++)
                    {
                        foreach (var peakSpot in gcmsDecBinArray[i].PeakSpots.Where(n => n.Quality == ModelQuality.Middle))
                            peakAreas.Add(peakAreaList[peakSpot.PeakSpotID]);
                    }
                }

				var modelChrom = getModelChromatogram(spectrumList, peakAreas, gcmsDecBinArray, rdamToChromDict, param);
				if (modelChrom != null) {
					modelChromatograms.Add(modelChrom);
				}
            }

            return modelChromatograms;
        }

        /// <summary>
        /// 1. This source code is for making a 'model (artificial)' chromatogram which will be used as a 'template' for a least square method in chromatogram deconvolution.
        /// 2. This idea is very similar to the original method of AMDIS: Stein, S. E. Journal of the American Society for Mass Spectrometry, 1999, 10, 770-781. 
        /// </summary>
        private static ModelChromatogram getModelChromatogram(List<RawSpectrum> spectrumList, List<PeakAreaBean> peakAreas, GcmsDecBin[] gcmsDecBins, Dictionary<int, int> rdamToChromDict, AnalysisParamOfMsdialGcms param)
        {
            if (peakAreas == null || peakAreas.Count == 0) return null;
            var maxSharpnessValue = peakAreas.Max(n => n.ShapenessValue * n.IntensityAtPeakTop);
            var maxIdealSlopeValue = peakAreas.Max(n => n.IdealSlopeValue);
            var modelChrom = new ModelChromatogram() { SharpnessValue = maxSharpnessValue, IdealSlopeValue = maxIdealSlopeValue };
            var firstFlg = false;

            var peaklist = new List<Peak>();
            var peaklists = new List<List<Peak>>();
            var baselineCorrectedPeaklist = new List<Peak>();

            foreach (var peak in peakAreas.Where(n => n.ShapenessValue * n.IntensityAtPeakTop >= maxSharpnessValue * 0.9).OrderByDescending(n => n.ShapenessValue * n.IntensityAtPeakTop))
            {
                if (firstFlg == false)
                {
                    modelChrom.RdamScanOfPeakTop = peak.Ms1LevelDatapointNumber;
                    modelChrom.ChromScanOfPeakTop = rdamToChromDict[modelChrom.RdamScanOfPeakTop];
                    modelChrom.ChromScanOfPeakLeft = modelChrom.ChromScanOfPeakTop - (peak.ScanNumberAtPeakTop - peak.ScanNumberAtLeftPeakEdge);
                    modelChrom.ChromScanOfPeakRight = modelChrom.ChromScanOfPeakTop + (peak.ScanNumberAtRightPeakEdge - peak.ScanNumberAtPeakTop);
                    modelChrom.ModelMzList.Add(peak.AccurateMass);
					modelChrom.SharpnessValue = peak.ShapenessValue;
                    modelChrom.EstimatedNoise = peak.EstimatedNoise;
                    modelChrom.SignalToNoise = peak.SignalToNoise;

                    peaklist = getTrimedAndSmoothedPeaklist(spectrumList, modelChrom.ChromScanOfPeakLeft, modelChrom.ChromScanOfPeakRight, param.SmoothingLevel, gcmsDecBins, peak.AccurateMass, param);
                    baselineCorrectedPeaklist = getBaselineCorrectedPeaklist(peaklist, modelChrom.ChromScanOfPeakTop - modelChrom.ChromScanOfPeakLeft);
                    peaklists.Add(baselineCorrectedPeaklist);
                    firstFlg = true;
                }
                else
                {
                    modelChrom.ModelMzList.Add(peak.AccurateMass);
                    peaklist = getTrimedAndSmoothedPeaklist(spectrumList, modelChrom.ChromScanOfPeakLeft, modelChrom.ChromScanOfPeakRight, param.SmoothingLevel, gcmsDecBins, peak.AccurateMass, param);
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

            modelChrom = getRefinedModelChromatogram(modelChrom, gcmsDecBins, param);

            return modelChrom;
        }

        private static ModelChromatogram getRefinedModelChromatogram(ModelChromatogram modelChrom, GcmsDecBin[] gcmsDecBins, AnalysisParamOfMsdialGcms param)
        {
            double maxIntensity = double.MinValue;
            int peakTopID = -1, peakLeftID = -1, peakRightID = -1;

			for (int i = 0; i < modelChrom.Peaks.Count; i++) {
				if (modelChrom.Peaks[i].Intensity > maxIntensity) {
					maxIntensity = modelChrom.Peaks[i].Intensity;
					peakTopID = i;
				}
			}

            modelChrom.MaximumPeakTopValue = maxIntensity;
            //left spike check
            for (int i = peakTopID; i > 0; i--)
            {
                if (peakTopID - i < param.AveragePeakWidth * 0.5) continue;
                if (modelChrom.Peaks[i - 1].Intensity >= modelChrom.Peaks[i].Intensity) { peakLeftID = i; break; }
            }
            if (peakLeftID < 0) peakLeftID = 0;

            //right spike check
            for (int i = peakTopID; i < modelChrom.Peaks.Count - 1; i++)
            {
                if (i - peakTopID < param.AveragePeakWidth * 0.5) continue;
                if (modelChrom.Peaks[i].Intensity <= modelChrom.Peaks[i + 1].Intensity) { peakRightID = i; break; }
            }
            if (peakRightID < 0) peakRightID = modelChrom.Peaks.Count - 1;

            modelChrom.ChromScanOfPeakTop = peakTopID + modelChrom.ChromScanOfPeakLeft;
            modelChrom.ChromScanOfPeakRight = peakRightID + modelChrom.ChromScanOfPeakLeft;
            modelChrom.ChromScanOfPeakLeft = peakLeftID + modelChrom.ChromScanOfPeakLeft;
            modelChrom.RdamScanOfPeakTop = gcmsDecBins[modelChrom.ChromScanOfPeakTop].RdamScanNumber;

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

        private static List<Peak> getTrimedAndSmoothedPeaklist(List<RawSpectrum> spectrumList, int chromScanOfPeakLeft, int chromScanOfPeakRight, int smoothedMargin, GcmsDecBin[] gcmsDecBins, float focusedMass, AnalysisParamOfMsdialGcms param)
        {
            var peaklist = new List<Peak>();

            int startIndex = 0, leftRemainder = 0, rightRemainder = 0;
            double sum = 0, maxIntensityMz, maxMass;
            float massTol = param.MassAccuracy;

            var chromLeft = chromScanOfPeakLeft - smoothedMargin;
            var chromRight = chromScanOfPeakRight + smoothedMargin;

            if (chromLeft < 0) { leftRemainder = smoothedMargin - chromScanOfPeakLeft; chromLeft = 0; }
            if (chromRight > gcmsDecBins.Length - 1) { rightRemainder = chromScanOfPeakRight + smoothedMargin - (gcmsDecBins.Length - 1); chromRight = gcmsDecBins.Length - 1; }

            for (int i = chromLeft; i <= chromRight; i++)
            {
                var rdamScan = gcmsDecBins[i].RdamScanNumber;
                var spectrum = spectrumList[rdamScan];
                var massSpectra = spectrum.Spectrum;

                sum = 0;
                maxIntensityMz = double.MinValue;
                maxMass = focusedMass;

                startIndex = DataAccessGcUtility.GetMs1StartIndex(focusedMass, massTol, massSpectra);
                for (int j = startIndex; j < massSpectra.Length; j++)
                {
                    if (massSpectra[j].Mz < focusedMass - massTol) continue;
                    else if (focusedMass - massTol <= massSpectra[j].Mz && massSpectra[j].Mz < focusedMass + massTol) {
                        sum += massSpectra[j].Intensity;
                        if (maxIntensityMz < massSpectra[j].Intensity) {
                            maxIntensityMz = massSpectra[j].Intensity;
                            maxMass = massSpectra[j].Mz;
                        }
                    }
                    else if (massSpectra[j].Mz >= focusedMass + massTol) break;
                }
                peaklist.Add(new Peak() { ScanNumber = (int)spectrum.ScanNumber, RetentionTime = spectrum.ScanStartTime, Mz = maxMass, Intensity = sum });
            }

            var smoothedPeaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            for (int i = 0; i < smoothedMargin - leftRemainder; i++) smoothedPeaklist.RemoveAt(0);
            for (int i = 0; i < smoothedMargin - rightRemainder; i++) smoothedPeaklist.RemoveAt(smoothedPeaklist.Count - 1);

            return smoothedPeaklist;
        }

        private static List<Peak> getBaselineCorrectedPeaklist(List<Peak> peaklist, int peakTop)
        {
            var baselineCorrectedPeaklist = new List<Peak>();

            //find local minimum of left and right edge
            var minimumLeftID = 0;
            var minimumValue = double.MaxValue;
            for (int i = peakTop; i >= 0; i--)
                if (peaklist[i].Intensity < minimumValue)
                {
                    minimumValue = peaklist[i].Intensity;
                    minimumLeftID = i;
                }

            var minimumRightID = peaklist.Count - 1;
            minimumValue = double.MaxValue;
            for (int i = peakTop; i < peaklist.Count; i++)
                if (peaklist[i].Intensity < minimumValue)
                {
                    minimumValue = peaklist[i].Intensity;
                    minimumRightID = i;
                }

            double coeff = (peaklist[minimumRightID].Intensity - peaklist[minimumLeftID].Intensity) / (peaklist[minimumRightID].RetentionTime - peaklist[minimumLeftID].RetentionTime);
            double intercept = (peaklist[minimumRightID].RetentionTime * peaklist[minimumLeftID].Intensity - peaklist[minimumLeftID].RetentionTime * peaklist[minimumRightID].Intensity) / (peaklist[minimumRightID].RetentionTime - peaklist[minimumLeftID].RetentionTime);
            double correctedIntensity = 0;
            for (int i = 0; i < peaklist.Count; i++)
            {
                correctedIntensity = peaklist[i].Intensity - (int)(coeff * peaklist[i].RetentionTime + intercept);
                if (correctedIntensity >= 0)
                    baselineCorrectedPeaklist.Add(new Peak() { ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = correctedIntensity });
                else
                    baselineCorrectedPeaklist.Add(new Peak() { ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = 0 }); ;
            }

            return baselineCorrectedPeaklist;
        }

        private static List<RegionMarker> getRegionMarkers(double[] matchedFilterArray)
        {
            var regionMarkers = new List<RegionMarker>();
            var scanBegin = 0;
            var scanBeginFlg = false;
            var margin = 5;

            for (int i = margin; i < matchedFilterArray.Length - margin; i++)
            {
                //Debug.WriteLine("MatchedFilter\tID\t{0}\tValue\t{1}", i, matchedFilterArray[i]);

                if (matchedFilterArray[i] > 0 && matchedFilterArray[i - 1] < matchedFilterArray[i] && scanBeginFlg == false)
                {
                    scanBegin = i; scanBeginFlg = true;
                }
                else if (scanBeginFlg == true)
                {
                    if (matchedFilterArray[i] <= 0)
                    {
                        regionMarkers.Add(new RegionMarker() { ID = regionMarkers.Count, ScanBegin = scanBegin, ScanEnd = i - 1 });
                        
                        scanBeginFlg = false;
                        continue;
                    }
                    else if (matchedFilterArray[i - 1] > matchedFilterArray[i] && matchedFilterArray[i] < matchedFilterArray[i + 1] && matchedFilterArray[i] >= 0)
                    {
                        regionMarkers.Add(new RegionMarker() { ID = regionMarkers.Count, ScanBegin = scanBegin, ScanEnd = i });
                        scanBegin = i + 1;
                        scanBeginFlg = true;
                        i++;
                    }
                }
            }

            return regionMarkers;
        }

        private static Dictionary<int, int> getRdamAndMs1chromatogramScanDictionary(List<RawSpectrum> spectrumList, IonMode ionmode)
        {
            var rdamToChromDictionary = new Dictionary<int, int>();
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            var counter = 0;
            for (int i = 0; i < spectrumList.Count; i++)
            {
                if (spectrumList[i].MsLevel > 1) continue;
                if (spectrumList[i].ScanPolarity != scanPolarity) continue;
                rdamToChromDictionary[spectrumList[i].ScanNumber] = counter;

                counter++;
            }

            return rdamToChromDictionary;
        }

        private static double[] getMatchedFileterArray(GcmsDecBin[] gcmsDecBinArray, double sigma)
        {
            var halfPoint = 10.0; // currently this value should be enough for GC
            var matchedFilterArray = new double[gcmsDecBinArray.Length];
            var matchedFilterCoefficient = new double[2 * (int)halfPoint + 1];

			for (int i = 0; i < matchedFilterCoefficient.Length; i++) {
				matchedFilterCoefficient[i] = (1 - Math.Pow((-halfPoint + i) / sigma, 2)) * Math.Exp(-0.5 * Math.Pow((-halfPoint + i) / sigma, 2));
			}

			for (int i = 0; i < gcmsDecBinArray.Length; i++)
            {
				var sum = 0.0;
                for (int j = -1 * (int)halfPoint; j <= (int)halfPoint; j++)
                {
                    if (i + j < 0) sum += 0;
                    else if (i + j > gcmsDecBinArray.Length - 1) sum += 0;
                    else sum += gcmsDecBinArray[i + j].TotalSharpnessValue * matchedFilterCoefficient[(int)(j + halfPoint)];
                }
                matchedFilterArray[i] = sum;
			}

			return matchedFilterArray;
        }

        private static GcmsDecBin[] getGcmsBinArray(List<RawSpectrum> spectrumList, List<PeakAreaBean> peakAreaList, Dictionary<int, int> rdamScanDict, IonMode ionMode)
        {
            var ms1SpectrumList = getMs1SpectrumList(spectrumList, ionMode);
            var gcmsDecBins = new GcmsDecBin[ms1SpectrumList.Count];
            for (int i = 0; i < gcmsDecBins.Length; i++)
            {
                gcmsDecBins[i] = new GcmsDecBin();
                gcmsDecBins[i].RdamScanNumber = ms1SpectrumList[i].ScanNumber;
                gcmsDecBins[i].RetentionTime = (float)ms1SpectrumList[i].ScanStartTime;
                //Debug.WriteLine("GCMS Bins\tID\t{0}\tRT\t{1}", i, gcmsDecBins[i].RetentionTime);
            }

            for (int i = 0; i < peakAreaList.Count; i++)
            {
                var peak = peakAreaList[i];
                var model = new PeakSpot();
                var scanBin = rdamScanDict[peak.Ms1LevelDatapointNumber];
                model.PeakSpotID = i;
                
                if (peak.IdealSlopeValue > 0.999) model.Quality = ModelQuality.High;
                else if (peak.IdealSlopeValue > 0.9) model.Quality = ModelQuality.Middle;
                else model.Quality = ModelQuality.Low;

                if (model.Quality == ModelQuality.High || model.Quality == ModelQuality.Middle) {
                    if (gcmsDecBins[scanBin].TotalSharpnessValue < peak.ShapenessValue) {
                        gcmsDecBins[scanBin].TotalSharpnessValue = peak.ShapenessValue;
                    }
                    // gcmsDecBins[scanBin].TotalSharpnessValue += peak.ShapenessValue;
                }
              
                gcmsDecBins[scanBin].PeakSpots.Add(model);
            }

            return gcmsDecBins;
        }

        private static List<RawSpectrum> getMs1SpectrumList(List<RawSpectrum> spectrumList, IonMode ionMode)
        {
            var ms1SpectrumList = new List<RawSpectrum>();
            var scanPolarity = ionMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            for (int i = 0; i < spectrumList.Count; i++)
            {
                var spectrum = spectrumList[i];

                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanPolarity != scanPolarity) continue;
                ms1SpectrumList.Add(spectrum);
            }
            return ms1SpectrumList;
        }

        private static void progressReports(float focusedMass, float endMass, Action<int> reportAction)
        {
            var progress = initialProgress + focusedMass / endMass * progressMax;
            reportAction?.Invoke((int)progress);
        }
    }
}
