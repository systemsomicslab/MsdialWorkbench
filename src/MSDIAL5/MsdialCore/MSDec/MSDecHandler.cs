using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Query;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using NSSplash;
using NSSplash.impl;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.MSDec {

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
	public enum MsDecPattern { C, BC, CD, ABC, BCD, CDE, ABCD, BCDE, ABCDE }

    /// <summary>
    /// Now, the model quality is determined by ad-hoc criteria considering 
    /// A) ideal slope, B) peak shape, and C) abundance information
    /// </summary>
    public enum ModelQuality { High, Middle, Low }

    public class MsDecBin {
        public double TotalSharpnessValue { get; set; }
        public int RdamScanNumber { get; set; }
        public float RetentionTime { get; set; }
        public List<PeakSpot> PeakSpots { get; set; }

        public MsDecBin() { TotalSharpnessValue = 0; PeakSpots = new List<PeakSpot>(); }
    }

    public class PeakSpot {
        public ModelQuality Quality { get; set; }
        public int PeakSpotID { get; set; }

        public PeakSpot() { Quality = ModelQuality.Low; }
    }

    public class RegionMarker {
        public int ID { get; set; }
        public int ScanBegin { get; set; }
        public int ScanEnd { get; set; }
    }

    /// <summary>
	/// Definition of model chromatogram
	/// </summary>
    public class ModelChromatogram {
        public List<ChromatogramPeak> Peaks { get; set; }

        public int RdamScanOfPeakTop { get; set; }
        public int ChromScanOfPeakTop { get; set; }
        public int ChromScanOfPeakLeft { get; set; }
        public int ChromScanOfPeakRight { get; set; }

        public List<double> ModelMzList { get; set; }

        public double IdealSlopeValue { get; set; }
        public double SharpnessValue { get; set; }
        public double MaximumPeakTopValue { get; set; }

        public float EstimatedNoise { get; set; }
        public float SignalToNoise { get; set; }

        public override string ToString() {
            return string.Format("scan at top:{0}, scan at left:{1}, scan at top:{2}, scan at right:{3}, ideal slope:{4}, sharpness:{5}, max peaktop:{6}\n\t" +
                "model mz:[{7}]\n\t" +
                "peaks:[{8}]", RdamScanOfPeakTop, ChromScanOfPeakLeft, ChromScanOfPeakTop, ChromScanOfPeakRight, IdealSlopeValue, SharpnessValue, MaximumPeakTopValue,
                string.Join("; ", ModelMzList), string.Join(";\n\t", Peaks.Select(p => "mz:" + p.Mass + " int:" + p.Intensity + " rt:" + p.ChromXs.Value)));
        }

        public ModelChromatogram() {
            Peaks = new List<ChromatogramPeak>();
            ModelMzList = new List<double>();
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

    public class ModelChromatogram_temp {
        public ValuePeak[] Peaks { get; set; }

        public int RdamScanOfPeakTop { get; set; }
        public int ChromScanOfPeakTop { get; set; }
        public int ChromScanOfPeakLeft { get; set; }
        public int ChromScanOfPeakRight { get; set; }

        public List<double> ModelMzList { get; set; }

        public double IdealSlopeValue { get; set; }
        public double SharpnessValue { get; set; }
        public double MaximumPeakTopValue { get; set; }

        public float EstimatedNoise { get; set; }
        public float SignalToNoise { get; set; }

        public override string ToString() {
            return string.Format("scan at top:{0}, scan at left:{1}, scan at top:{2}, scan at right:{3}, ideal slope:{4}, sharpness:{5}, max peaktop:{6}\n\t" +
                "model mz:[{7}]\n\t" +
                "peaks:[{8}]", RdamScanOfPeakTop, ChromScanOfPeakLeft, ChromScanOfPeakTop, ChromScanOfPeakRight, IdealSlopeValue, SharpnessValue, MaximumPeakTopValue,
                string.Join("; ", ModelMzList), string.Join(";\n\t", Peaks?.Select(p => "mz:" + p.Mz + " int:" + p.Intensity + " rt:" + p.Time)));
        }

        public ModelChromatogram_temp() {
            Peaks = null;
            ModelMzList = new List<double>();
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
    public class ModelChromVector {
        public MsDecPattern Ms1DecPattern { get; set; }

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

        public List<double> ModelMzList { get; set; }
        public float Sharpness { get; set; }
        public float EstimatedNoise { get; set; }

        public ModelChromVector() {
            Ms1DecPattern = MsDecPattern.C;
            ChromScanList = new List<int>();
            RdamScanList = new List<int>();
            RtArray = new List<float>();
            TargetIntensityArray = new List<float>();
            OneLeftIntensityArray = new List<float>();
            OneRightIntensityArray = new List<float>();
            TwoLeftIntensityArray = new List<float>();
            TwoRightInetnsityArray = new List<float>();
            ModelMzList = new List<double>();
            Sharpness = 0.0F;
            EstimatedNoise = 1.0F;
        }
    }

    public static class MSDecHandler {
        #region gcms
        public static List<MSDecResult> GetMSDecResults(IReadOnlyList<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures, ParameterBase param, ReportProgress reporter) {
            chromPeakFeatures = chromPeakFeatures.OrderBy(n => n.ChromScanIdTop).ThenBy(n => n.Mass).ToList();

            //Get scan ID dictionary between RDAM scan ID and MS1 chromatogram scan ID.
            //note that there is a possibility that raw data contains MS/MS information as well.
            var rdamToChromDict = getRdamAndMs1chromatogramScanDictionary(spectrumList, param.IonMode);
            //var ticPeaklist = DataAccessGcUtility.GetTicPeaklist(spectrumList, param.IonMode); //delete this after figure prepared (hiroshi)

            //Maps the values of peak shape, symmetry, and qualiy of detected peaks into the array 
            //where the length is equal to the scan number
            var gcmsDecBinArray = getMsdecBinArray(spectrumList, chromPeakFeatures, rdamToChromDict, param.IonMode);

            //apply matched filter to extract 'metabolite components' 
            //where EIC peaks having slightly (1-2 scan point diff) different retention times are merged. 
            var matchedFilterArray = getMatchedFileterArray(gcmsDecBinArray, param.SigmaWindowValue);

            //making model chromatograms by considering their peak qualities
            var modelChromatograms = getModelChromatograms(spectrumList, chromPeakFeatures, gcmsDecBinArray, matchedFilterArray, rdamToChromDict, param);
            //exportArraysForPaper(ticPeaklist, gcmsDecBinArray, matchedFilterArray, modelChromatograms, peakAreaList); //delete this after figure prepared (hiroshi)

            //exclude duplicate model chromatograms which have the complete same retention time's peak tops
            modelChromatograms = getRefinedModelChromatograms(modelChromatograms);

            var msdecResults = new List<MSDecResult>();
            var counter = 0;
            var minIntensity = double.MaxValue;
            var maxIntensity = double.MinValue;
            for (int i = 0; i < modelChromatograms.Count; i++) { //do deconvolution at each model chromatogram area

                //consider adjacent model chromatograms to be considered as 'co-eluting' metabolites
                var modelChromVector = getModelChromatogramVector(i, modelChromatograms, gcmsDecBinArray);

                //to get triming EIC chromatograms where the retention time range is equal to the range of model chromatogram vector
                var ms1Chromatograms = getMs1Chromatograms(spectrumList, modelChromVector, gcmsDecBinArray, modelChromatograms[i].ChromScanOfPeakTop, param);
                var msdecResult = MSDecProcess.GetMsDecResult(modelChromVector, ms1Chromatograms); //get MS1Dec result

                //Debug.WriteLine("model count: " + modelChromatograms.Count + "  -- ms1 Count: " + ms1Chromatograms.Count + " -- ms1Dec count: " + ms1DecResults.Count);

                if (msdecResult != null && msdecResult.Spectrum.Count > 0) {
                    msdecResult.ScanID = counter;
                    msdecResult.RawSpectrumID = modelChromatograms[i].RdamScanOfPeakTop;
                    msdecResult.Spectrum = getRefinedMsDecSpectrum(msdecResult.Spectrum, param);
                    msdecResult.Splash = calculateSplash(msdecResult.Spectrum);
                    msdecResults.Add(msdecResult);

                    if (msdecResult.ModelPeakHeight < minIntensity) minIntensity = msdecResult.ModelPeakHeight;
                    if (msdecResult.ModelPeakHeight > maxIntensity) maxIntensity = msdecResult.ModelPeakHeight;

                    counter++;
                }
                reporter.Report(i, modelChromatograms.Count);
            }

            foreach (var ms1DecResult in msdecResults) {
                ms1DecResult.AmplitudeScore = (float)((ms1DecResult.ModelPeakHeight - minIntensity) / (maxIntensity - minIntensity));

                //calculating purity
                //Debug.WriteLine("tic count: " + spectrumList.FindAll(sp => sp.ScanNum == ms1DecResult.ScanNumber).Count);
                //Debug.WriteLine("deconv count: " + ms1DecResult.Spectrum.Count);
                var tic = spectrumList.FirstOrDefault(sp => sp.ScanNumber == ms1DecResult.ScanID)?.TotalIonCurrent ?? 1d;
                //Debug.WriteLine("TIC: " + tic + " -- RT: " + spectrumList.Find(sp => sp.ScanNum == ms1DecResult.ScanNumber).RTmin);

                var eic = ms1DecResult.Spectrum.Sum(s => s.Intensity);
                ms1DecResult.ModelPeakPurity = (float)(eic / tic);
                //Debug.WriteLine("EIC: " + ms1DecResult.Spectrum.Sum(s => s.Intensity) + " -- RT: " + ms1DecResult.Spectrum.Average(s => s.RetentionTime));
                //Debug.WriteLine("purity: " + ms1DecResult.ModelPeakPurity.ToString() + " \n");

            }
            return msdecResults;
        }

        public static QuantifiedChromatogramPeak? GetChromatogramQuantInformation(RawSpectra spectra, MSDecResult result, double targetMz, ParameterBase param) {
            var model = result.ModelPeakChromatogram;
            System.Diagnostics.Debug.Assert(model is null || model.Count > 0, "No model peak chromatogram");
            var startID = model[0].ID;
            var endID = model[model.Count - 1].ID;
            var startRt = model[0].ChromXs.RT.Value;
            var endRt = model[model.Count - 1].ChromXs.RT.Value;
            var offset = .5d;
            ChromatogramRange chromatogramRange = new ChromatogramRange(startRt, endRt, ChromXType.RT, ChromXUnit.Min).ExtendWith(offset).RestrictBy(spectra.StartRt, spectra.EndRt);
            //targetMz = (int)targetMz;
            using var chrom = spectra.GetMS1ExtractedChromatogram(new MzRange(targetMz, param.CentroidMs1Tolerance), chromatogramRange);
            using var smoothedchrom = chrom.ChromatogramSmoothing(param.SmoothingMethod, param.SmoothingLevel);
            var peakResult = smoothedchrom.GetPeakDetectionResultFromRange(startID, endID);
            System.Diagnostics.Debug.Assert(peakResult is not null);
            if (peakResult is null) {
                return null;
            }
            var peak = peakResult.ConvertToPeakFeature(smoothedchrom, targetMz);
            var peakShape = new ChromatogramPeakShape(peakResult);
            return QuantifiedChromatogramPeak.RecalculatedFromChromatogram(peak, peakShape, peakResult, smoothedchrom);
        }

        private static MsDecBin[] getMsdecBinArray(IReadOnlyList<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures, Dictionary<int, int> rdamScanDict, IonMode ionMode) {
            var ms1SpectrumList = getMs1SpectrumList(spectrumList, ionMode);
            var msdecBins = new MsDecBin[ms1SpectrumList.Count];
            for (int i = 0; i < msdecBins.Length; i++) {
                msdecBins[i] = new MsDecBin();
                msdecBins[i].RdamScanNumber = ms1SpectrumList[i].ScanNumber;
                msdecBins[i].RetentionTime = (float)ms1SpectrumList[i].ScanStartTime;
                //Debug.WriteLine("GCMS Bins\tID\t{0}\tRT\t{1}", i, gcmsDecBins[i].RetentionTime);
            }

            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                var peak = chromPeakFeatures[i];
                var model = new PeakSpot();
                var scanBin = rdamScanDict[peak.MS1RawSpectrumIdTop];
                model.PeakSpotID = i;

                if (peak.PeakShape.IdealSlopeValue > 0.999) model.Quality = ModelQuality.High;
                else if (peak.PeakShape.IdealSlopeValue > 0.9) model.Quality = ModelQuality.Middle;
                else model.Quality = ModelQuality.Low;

                if (model.Quality == ModelQuality.High || model.Quality == ModelQuality.Middle) {
                    if (msdecBins[scanBin].TotalSharpnessValue < peak.PeakShape.ShapenessValue) {
                        msdecBins[scanBin].TotalSharpnessValue = peak.PeakShape.ShapenessValue;
                    }
                    // gcmsDecBins[scanBin].TotalSharpnessValue += peak.ShapenessValue;
                }

                msdecBins[scanBin].PeakSpots.Add(model);
            }

            return msdecBins;
        }

        private static List<ModelChromatogram> getModelChromatograms(IReadOnlyList<RawSpectrum> spectrumList,
            List<ChromatogramPeakFeature> chromPeakFeatures, MsDecBin[] msdecBinArray, double[] matchedFilterArray, Dictionary<int, int> rdamToChromDict, ParameterBase param) {
            var regionMarkers = getRegionMarkers(matchedFilterArray);
            var modelChromatograms = new List<ModelChromatogram>();

            foreach (var region in regionMarkers) {
                var peakAreas = new List<ChromatogramPeakFeature>();
                for (int i = region.ScanBegin; i <= region.ScanEnd; i++) {
                    foreach (var peakSpot in msdecBinArray[i].PeakSpots.Where(n => n.Quality == ModelQuality.High))
                        peakAreas.Add(chromPeakFeatures[peakSpot.PeakSpotID]);
                }
                if (peakAreas.Count == 0) {
                    for (int i = region.ScanBegin; i <= region.ScanEnd; i++) {
                        foreach (var peakSpot in msdecBinArray[i].PeakSpots.Where(n => n.Quality == ModelQuality.Middle))
                            peakAreas.Add(chromPeakFeatures[peakSpot.PeakSpotID]);
                    }
                }

                var modelChrom = getModelChromatogram(spectrumList, peakAreas, msdecBinArray, rdamToChromDict, param);
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
        private static ModelChromatogram getModelChromatogram(IReadOnlyList<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> peakAreas,
            MsDecBin[] msdecBins, Dictionary<int, int> rdamToChromDict, ParameterBase param) {
            if (peakAreas == null || peakAreas.Count == 0) return null;
            var maxSharpnessValue = peakAreas.Max(n => n.PeakShape.ShapenessValue * n.PeakHeightTop);
            var maxIdealSlopeValue = peakAreas.Max(n => n.PeakShape.IdealSlopeValue);
            var modelChrom = new ModelChromatogram() { SharpnessValue = maxSharpnessValue, IdealSlopeValue = maxIdealSlopeValue };
            var firstFlg = false;

            var peaklists = new List<List<ChromatogramPeak>>();
            var baselineCorrectedPeaklist = new List<ChromatogramPeak>();

            foreach (var peak in peakAreas.Where(n => n.PeakShape.ShapenessValue * n.PeakHeightTop >= maxSharpnessValue * 0.9)
                .OrderByDescending(n => n.PeakShape.ShapenessValue * n.PeakHeightTop)) {
                if (firstFlg == false) {
                    modelChrom.RdamScanOfPeakTop = peak.MS1RawSpectrumIdTop;
                    modelChrom.ChromScanOfPeakTop = rdamToChromDict[modelChrom.RdamScanOfPeakTop];
                    modelChrom.ChromScanOfPeakLeft = modelChrom.ChromScanOfPeakTop - (peak.ChromScanIdTop - peak.ChromScanIdLeft);
                    modelChrom.ChromScanOfPeakRight = modelChrom.ChromScanOfPeakTop + (peak.ChromScanIdRight - peak.ChromScanIdTop);
                    modelChrom.SharpnessValue = peak.PeakShape.ShapenessValue;
                    modelChrom.EstimatedNoise = peak.PeakShape.EstimatedNoise;
                    modelChrom.SignalToNoise = peak.PeakShape.SignalToNoise;
                    firstFlg = true;
                }
                modelChrom.ModelMzList.Add((float)peak.Mass);
                var peaklist = getTrimedAndSmoothedPeaklist(spectrumList, modelChrom.ChromScanOfPeakLeft, modelChrom.ChromScanOfPeakRight, param.SmoothingLevel, msdecBins, (float)peak.Mass, param);
                baselineCorrectedPeaklist = getBaselineCorrectedPeaklist(peaklist, modelChrom.ChromScanOfPeakTop - modelChrom.ChromScanOfPeakLeft);
                peaklists.Add(baselineCorrectedPeaklist);
            }

            double mzCount = modelChrom.ModelMzList.Count;
            foreach (var peak in peaklists[0]) {
                modelChrom.Peaks.Add(new ChromatogramPeak(peak.ID, peak.Mass, peak.Intensity /= mzCount, peak.ChromXs));
            }

            if (peaklists.Count > 1) {
                for (int i = 1; i < peaklists.Count; i++) {
                    for (int j = 0; j < peaklists[i].Count; j++) {
                        modelChrom.Peaks[j].Intensity += peaklists[i][j].Intensity /= mzCount;
                    }
                }
            }

            modelChrom = getRefinedModelChromatogram(modelChrom, msdecBins, param);

            return modelChrom;
        }


        // only gcms
        public static void ReplaceQuantmassByUserdefinedValue(List<RawSpectrum> spectrumList, List<MSDecResult> msDecResults,
            ParameterBase param, List<MoleculeMsReference> mspDB) {
            if (msDecResults != null && mspDB != null) {
                foreach (var result in msDecResults) {
                    var mspID = result.MspID;
                    if (mspID >= 0 && mspID < mspDB.Count) {
                        var udQuantmass = mspDB[mspID].QuantMass;
                        if (udQuantmass <= 1) continue;
                        if (udQuantmass < param.MassRangeBegin || udQuantmass > param.MassRangeEnd) continue;
                        RecalculateMs1decResultByDefinedQuantmass(result, spectrumList, (float)udQuantmass, param);
                    }
                }
            }
        }


        // no need anymore (I made this for my paper work).
        private static void exportArraysForPaper(List<double[]> ticPeaklist, MsDecBin[] msdecBinArray, double[] matchedFilterArray,
            List<ModelChromatogram> modelChromatograms, List<ChromatogramPeakFeature> chromPeakFeatures) {
            var peaklistFilePath = @"C:\Users\hiroshi.tsugawa\Desktop\MSDIAL-peaklists.txt";
            var modelChromsFilePath = @"C:\Users\hiroshi.tsugawa\Desktop\MSDIAL-modelchroms.txt";
            using (var sw = new StreamWriter(peaklistFilePath, false, Encoding.ASCII)) {
                sw.WriteLine("Scan\tRT\tmz\tTIC\tTotal sharpness value\tPeak spot info\tmatched value");
                for (int i = 0; i < ticPeaklist.Count; i++) {
                    var peak = ticPeaklist[i];
                    var gcBin = msdecBinArray[i];
                    var matchedValue = matchedFilterArray[i];

                    var peakSpotString = string.Empty;
                    foreach (var spot in gcBin.PeakSpots) {
                        var spotID = spot.PeakSpotID;
                        var quality = spot.Quality.ToString();
                        var spotMz = chromPeakFeatures[spotID].Mass;
                        peakSpotString += "[" + Math.Round(spotMz, 4) + ", " + quality + "] ";
                    }

                    sw.WriteLine(peak[0] + "\t" + peak[1] + "\t" + peak[2] + "\t" + peak[3] + "\t" + gcBin.TotalSharpnessValue + "\t" + peakSpotString + "\t" + matchedValue);
                }
            }

            using (var sw = new StreamWriter(modelChromsFilePath, false, Encoding.ASCII)) {
                foreach (var chrom in modelChromatograms) {
                    sw.WriteLine("SCAN: " + chrom.ChromScanOfPeakTop);
                    sw.WriteLine("IdealSlope: " + chrom.IdealSlopeValue);
                    var modelMzList = string.Empty;
                    for (int i = 0; i < chrom.ModelMzList.Count; i++) {
                        if (i == chrom.ModelMzList.Count - 1)
                            modelMzList += Math.Round(chrom.ModelMzList[i], 4).ToString();
                        else
                            modelMzList += Math.Round(chrom.ModelMzList[i], 4).ToString() + ", ";
                    }
                    sw.WriteLine("ModelMZs: " + modelMzList);
                    sw.WriteLine("Num Peaks: " + chrom.Peaks.Count);
                    foreach (var peak in chrom.Peaks) {
                        sw.WriteLine(peak.ID + "\t" + peak.ChromXs.Value + "\t" + peak.Mass + "\t" + peak.Intensity);
                    }
                    sw.WriteLine();
                }
            }
        }

        // nominal GC should be using isnominal as the accuracy type. Otherwise, should be using isaccurate
        private static List<SpectrumPeak> getRefinedMsDecSpectrum(List<SpectrumPeak> originalSpec, ParameterBase param) {
            
            if (param.AccuracyType == AccuracyType.IsNominal) return originalSpec;

            var spectrum = new List<SpectrumPeak>() { originalSpec[0] };
            for (int i = 1; i < originalSpec.Count; i++) {
                if (i > originalSpec.Count - 1) break;
                if (Math.Round(spectrum[spectrum.Count - 1].Mass, 4) != Math.Round(originalSpec[i].Mass, 4))
                    spectrum.Add(originalSpec[i]);
                else {
                    if (spectrum[spectrum.Count - 1].Intensity < originalSpec[i].Intensity) {
                        spectrum.RemoveAt(spectrum.Count - 1);
                        spectrum.Add(originalSpec[i]);
                    }
                }
            }
            if (spectrum.Count > 0) {
                var maxIntensity = spectrum.Max(n => n.Intensity);
                spectrum = spectrum.Where(n => n.Intensity > param.AmplitudeCutoff).ToList();
                return spectrum;
            }

            return spectrum;
        }

        // for gcms only
        public static void RecalculateMs1decResultByDefinedQuantmass(MSDecResult msDecResult, List<RawSpectrum> spectrumList, float quantMass, ParameterBase param) {

            msDecResult.ModelPeakMz = quantMass;
            msDecResult.ModelPeakHeight = 1;
            msDecResult.ModelPeakArea = 1;

            var spectrum = msDecResult.Spectrum;
            var chromatogram = msDecResult.ModelPeakChromatogram;
            var estimatedNoise = msDecResult.EstimatedNoise;

            var peaktopRt = msDecResult.ChromXs.Value;
            var rtBegin = chromatogram[0].ChromXs.Value;
            var rtEnd = chromatogram[chromatogram.Count - 1].ChromXs.Value;
            var peakWidth = chromatogram[chromatogram.Count - 1].ChromXs.Value - chromatogram[0].ChromXs.Value;
            if (peakWidth < 0.02) peakWidth = 0.02;
            var rtTol = peakWidth * 0.5;

            var peaklist = DataAccess.GetBaselineCorrectedPeaklistByMassAccuracy(spectrumList, (float)peaktopRt, (float)rtBegin, (float)rtEnd, quantMass, param);

            var sPeaklist = peaklist.ChromatogramSmoothing(param.SmoothingMethod, param.SmoothingLevel).AsPeakArray();
            if (sPeaklist.Count != 0) {

                var maxID = -1;
                var maxInt = double.MinValue;
                var minRtId = -1;
                var minRt = double.MaxValue;

                for (int i = 1; i < sPeaklist.Count - 1; i++) {
                    if (sPeaklist[i].ChromXs.Value < peaktopRt - rtTol) continue;
                    if (peaktopRt + rtTol < sPeaklist[i].ChromXs.Value) break;

                    if (maxInt < sPeaklist[i].Intensity &&
                        sPeaklist[i - 1].Intensity <= sPeaklist[i].Intensity &&
                        sPeaklist[i].Intensity >= sPeaklist[i + 1].Intensity) {

                        maxInt = sPeaklist[i].Intensity;
                        maxID = i;
                    }
                    if (Math.Abs(sPeaklist[i].ChromXs.Value - peaktopRt) < minRt) {
                        minRt = sPeaklist[i].ChromXs.Value;
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
                        (sPeaklist[i + 1].ChromXs.Value - sPeaklist[i].ChromXs.Value) * 0.5;
                }

                var peakHeightFromBaseline = Math.Max(maxInt - sPeaklist[0].Intensity, maxInt - sPeaklist[spectrumList.Count - 1].Intensity);

                msDecResult.ModelPeakHeight = (float)maxInt;
                msDecResult.ModelPeakArea = (float)peakAreaAboveZero;

                msDecResult.SignalNoiseRatio = (float)(peakHeightFromBaseline / estimatedNoise);
            }
        }

        private static List<List<ChromatogramPeak>> getMs1Chromatograms(IReadOnlyList<RawSpectrum> spectrumList, ModelChromVector modelChromVector,
           MsDecBin[] msdecBins, int chromScanOfPeakTop, ParameterBase param) {
            var rdamScan = msdecBins[chromScanOfPeakTop].RdamScanNumber;
            var massBin = param.CentroidMs1Tolerance; if (param.AccuracyType == AccuracyType.IsNominal) massBin = 0.5F;
            var focusedMs1Spectrum = DataAccess.GetCentroidMassSpectra(spectrumList, param.MSDataType, rdamScan, param.AmplitudeCutoff, param.MassRangeBegin, param.MassRangeEnd);
            focusedMs1Spectrum = ExcludeMasses(focusedMs1Spectrum, param.ExcludedMassList);
            if (focusedMs1Spectrum.Count == 0) return new List<List<ChromatogramPeak>>();

            var rdamScanList = modelChromVector.RdamScanList;
            var peaksList = new List<List<ChromatogramPeak>>();
            foreach (var spec in focusedMs1Spectrum.Where(n => n.Intensity >= param.AmplitudeCutoff).OrderByDescending(n => n.Intensity)) {
                var peaks = getTrimedAndSmoothedPeaklist(spectrumList, modelChromVector.ChromScanList[0], modelChromVector.ChromScanList[modelChromVector.ChromScanList.Count - 1], param.SmoothingLevel, msdecBins, (float)spec.Mass, param);
                var baselineCorrectedPeaks = getBaselineCorrectedPeaklist(peaks, modelChromVector.TargetScanTopInModelChromVector);

                //var peaktopRT = baselineCorrectedPeaks[modelChromVector.TargetScanTopInModelChromVector].RetentionTime;
                //var peaktopMz = baselineCorrectedPeaks[modelChromVector.TargetScanTopInModelChromVector].Mz;

                peaksList.Add(baselineCorrectedPeaks);
            }

            return peaksList;
        }

        private static List<SpectrumPeak> ExcludeMasses(List<SpectrumPeak> focusedMs1Spectrum, List<MzSearchQuery> excludedMassList) {
            if (excludedMassList == null || excludedMassList.Count == 0) return focusedMs1Spectrum;

            var cMasses = new List<SpectrumPeak>();
            foreach (var pair in focusedMs1Spectrum) {
                var checker = false;
                foreach (var ePair in excludedMassList) {
                    if (Math.Abs(pair.Mass - (double)ePair.Mass) < ePair.MassTolerance) {
                        checker = true;
                        break;
                    }
                }
                if (checker) continue;
                cMasses.Add(pair);
            }
            return cMasses;
        }

        #endregion


        #region MS/MS deconvolution in retention time or ion mobility axis
        //public static MSDecResult GetMSDecResult(List<List<ChromatogramPeak>> peaklistList, ParameterBase param, int targetScanNumber, 
        //    ChromXType chromType = ChromXType.RT, ChromXUnit chromUnit = ChromXUnit.Min) {
        //    if (peaklistList == null || peaklistList.Count == 0) return null;
        //    //Peak detections in MS/MS chromatogras
        //    var peakSpots = getPeakSpots(peaklistList, param, chromType, chromUnit); if (peakSpots.Count == 0) return null;

        //    //Maps the values of peak shape, symmetry, and qualiy of detected peaks into the array 
        //    //where the length is equal to the scan number
        //    var ms2decBinArray = getMsDecBinArray(peakSpots, peaklistList[0]);

        //    //apply matched filter to extract 'metabolite components' 
        //    //where peaks having slightly (1-2 scan point diff) different retention times are merged. 
        //    var matchedFilterArray = getMatchedFileterArray(ms2decBinArray, param.SigmaWindowValue);

        //    //making model chromatograms by considering their peak qualities
        //    var modelChromatograms = getModelChromatograms(peaklistList, peakSpots, ms2decBinArray, matchedFilterArray, param);

        //    //What we have to do in DIA-MS data is first to link the peak tops of MS1 chromatogram and of MS2 chromatogram.
        //    //in the current program, if the peak top defference between MS1 and MS2 chromatograms is less than 1 scan point, they are recognized as the same metabolite.
        //    var minimumID = 0;
        //    var minimumDiff = double.MaxValue;
        //    for (int i = 0; i < modelChromatograms.Count; i++) {
        //        var modelChrom = modelChromatograms[i];
        //        if (Math.Abs(targetScanNumber - modelChrom.ChromScanOfPeakTop) < minimumDiff) {
        //            minimumDiff = Math.Abs(targetScanNumber - modelChrom.ChromScanOfPeakTop);
        //            minimumID = i;
        //        }
        //    }

        //    //Run deconvolution program if the target feature related to MS1 precursor peak can be found in MS/MS chromatograms
        //    if (minimumDiff <= 2) {
        //        //considering adjacent model chromatograms to be considered as 'co-eluting' metabolites
        //        var modelChromVector = getModelChromatogramVector(minimumID, modelChromatograms, ms2decBinArray);

        //        //triming MS/MS chromatograms where the retention time range is equal to the range of model chromatogram vector
        //        var chromatograms = getMs2Chromatograms(modelChromVector, peaklistList, param);
        //        if (chromatograms.Count == 0) return null;
        //        var result = MSDecProcess.GetMsDecResult(modelChromVector, chromatograms);
        //        result.Spectrum = getRefinedMsDecSpectrum(result.Spectrum, param);
        //        result.Splash = calculateSplash(result.Spectrum);
        //        return result;
        //    }
        //    else {
        //        return null;
        //    }
        //}


        public static MSDecResult GetMSDecResult(List<ExtractedIonChromatogram> chromatogramList, ParameterBase param, int targetScanNumber,
            ChromXType chromType = ChromXType.RT, ChromXUnit chromUnit = ChromXUnit.Min) {
            if (chromatogramList == null || chromatogramList.Count == 0) return null;
            //Peak detections in MS/MS chromatogras
            var peakSpots = getPeakSpots(chromatogramList, param, chromType, chromUnit); if (peakSpots.Count == 0) return null;

            //Maps the values of peak shape, symmetry, and qualiy of detected peaks into the array 
            //where the length is equal to the scan number
            var ms2decBinArray = getMsDecBinArray(peakSpots, chromatogramList[0]);

            //apply matched filter to extract 'metabolite components' 
            //where peaks having slightly (1-2 scan point diff) different retention times are merged. 
            var matchedFilterArray = getMatchedFileterArray(ms2decBinArray, param.SigmaWindowValue);

            //making model chromatograms by considering their peak qualities
            var modelChromatograms = getModelChromatograms(chromatogramList, peakSpots, ms2decBinArray, matchedFilterArray, param);

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
                var chromatograms = getMs2Chromatograms(modelChromVector, chromatogramList, param);
                if (chromatograms.Count == 0) return null;
                var result = MSDecProcess.GetMsDecResult(modelChromVector, chromatograms);
                if (result == null) return null;
                result.Spectrum = getRefinedMsDecSpectrum(result.Spectrum, param);
                result.Splash = calculateSplash(result.Spectrum);
                return result;
            }
            else {
                return null;
            }
        }


        /// <summary>
        /// Get the result of peak spotting
        /// </summary>
        private static List<ChromatogramPeakFeature> getPeakSpots(List<List<ChromatogramPeak>> peaklistList, ParameterBase param, ChromXType type, ChromXUnit unit) {
            var peakSpots = new List<ChromatogramPeakFeature>();

            foreach (var (peaks, index) in peaklistList.WithIndex()) {
                var minDatapoints = param.MinimumDatapoints;
                var minAmps = param.MinimumAmplitude;
                var results = PeakDetection.PeakDetectionVS1(peaks, minDatapoints, minAmps);
                if (results == null || results.Count == 0) continue;
                //save as peakarea bean
                foreach (var result in results) {

                    var mass = (float)peaks[result.ScanNumAtPeakTop].Mass;
                    var chromPeakFeature = DataAccess.GetChromatogramPeakFeature(result, type, unit, mass, param.ProjectParam.IonMode);
                    chromPeakFeature.PeakID = index; // this information is needed in the later process.
                    peakSpots.Add(chromPeakFeature);
                }
            }
            return peakSpots;
        }

        private static List<ChromatogramPeakFeature> getPeakSpots(List<ExtractedIonChromatogram> chromatogramList, ParameterBase param, ChromXType type, ChromXUnit unit) {
            var peakSpots = new List<ChromatogramPeakFeature>();

            var detector = new PeakDetection(param.MinimumDatapoints, param.MinimumAmplitude);
            foreach (var (chromatogram, index) in chromatogramList.WithIndex()) {
                var results = detector.PeakDetectionVS1(chromatogram);
                if (results == null || results.Count == 0) continue;
                //save as peakarea bean
                foreach (var result in results) {
                    var mass = chromatogram.Mz(result.ScanNumAtPeakTop);
                    var chromPeakFeature = DataAccess.GetChromatogramPeakFeature(result, type, unit, mass, param.ProjectParam.IonMode);
                    chromPeakFeature.PeakID = index; // this information is needed in the later process.
                    peakSpots.Add(chromPeakFeature);
                }
            }
            return peakSpots;
        }

        private static MsDecBin[] getMsDecBinArray(List<ChromatogramPeakFeature> peakSpots, List<ChromatogramPeak> peaklist) {

            var ms2DecBins = new MsDecBin[peaklist.Count];

            for (int i = 0; i < ms2DecBins.Length; i++) {
                ms2DecBins[i] = new MsDecBin();
                ms2DecBins[i].RetentionTime = (float)peaklist[i].ChromXs.Value;
            }

            for (int i = 0; i < peakSpots.Count; i++) {
                var spot = peakSpots[i];
                var scan = peakSpots[i].ChromScanIdTop;
                var model = new PeakSpot() { PeakSpotID = i };

                if (spot.PeakShape.IdealSlopeValue > 0.999) model.Quality = ModelQuality.High;
                else if (spot.PeakShape.IdealSlopeValue > 0.9) model.Quality = ModelQuality.Middle;
                else model.Quality = ModelQuality.Low;

                if (model.Quality == ModelQuality.High || model.Quality == ModelQuality.Middle) {
                    if (ms2DecBins[scan].TotalSharpnessValue < spot.PeakShape.ShapenessValue) {
                        ms2DecBins[scan].TotalSharpnessValue = spot.PeakShape.ShapenessValue;
                    }
                    //ms2DecBins[scan].TotalSharpnessValue += spot.ShapenessValue;
                }

                ms2DecBins[scan].PeakSpots.Add(model);
            }

            return ms2DecBins;
        }


        private static MsDecBin[] getMsDecBinArray(List<ChromatogramPeakFeature> peakSpots, ExtractedIonChromatogram chromatogram) {

            var ms2DecBins = new MsDecBin[chromatogram.Length];

            for (int i = 0; i < ms2DecBins.Length; i++) {
                ms2DecBins[i] = new MsDecBin();
                ms2DecBins[i].RetentionTime = (float)chromatogram.Time(i);
            }

            for (int i = 0; i < peakSpots.Count; i++) {
                var spot = peakSpots[i];
                var scan = peakSpots[i].ChromScanIdTop;
                var model = new PeakSpot() { PeakSpotID = i };

                if (spot.PeakShape.IdealSlopeValue > 0.999) model.Quality = ModelQuality.High;
                else if (spot.PeakShape.IdealSlopeValue > 0.9) model.Quality = ModelQuality.Middle;
                else model.Quality = ModelQuality.Low;

                if (model.Quality == ModelQuality.High || model.Quality == ModelQuality.Middle) {
                    if (ms2DecBins[scan].TotalSharpnessValue < spot.PeakShape.ShapenessValue) {
                        ms2DecBins[scan].TotalSharpnessValue = spot.PeakShape.ShapenessValue;
                    }
                    //ms2DecBins[scan].TotalSharpnessValue += spot.ShapenessValue;
                }

                ms2DecBins[scan].PeakSpots.Add(model);
            }

            return ms2DecBins;
        }

        private static List<ModelChromatogram> getModelChromatograms(List<List<ChromatogramPeak>> peaklistList, List<ChromatogramPeakFeature> peakSpots, 
            MsDecBin[] msdecBins, double[] matchedFilterArray, ParameterBase param) {
            var regionMarkers = getRegionMarkers(matchedFilterArray);
            var modelChromatograms = new List<ModelChromatogram>();
            //   Debug.WriteLine("Regions count: {regionMarkers.Count}");
            foreach (var region in regionMarkers) {
                var peakAreas = new List<ChromatogramPeakFeature>();
                for (int i = region.ScanBegin; i <= region.ScanEnd; i++) {
                    foreach (var peakSpot in msdecBins[i].PeakSpots.Where(n => n.Quality == ModelQuality.High))
                        peakAreas.Add(peakSpots[peakSpot.PeakSpotID]);
                }
                if (peakAreas.Count == 0) {
                    for (int i = region.ScanBegin; i <= region.ScanEnd; i++) {
                        foreach (var peakSpot in msdecBins[i].PeakSpots.Where(n => n.Quality == ModelQuality.Middle))
                            peakAreas.Add(peakSpots[peakSpot.PeakSpotID]);
                    }
                }
                if (peakAreas.Count == 0) {
                    for (int i = region.ScanBegin; i <= region.ScanEnd; i++) {
                        foreach (var peakSpot in msdecBins[i].PeakSpots.Where(n => n.Quality == ModelQuality.Low))
                            peakAreas.Add(peakSpots[peakSpot.PeakSpotID]);
                    }
                }
                var modelChrom = getModelChromatogram(peaklistList, peakAreas, msdecBins, param);
                if (modelChrom != null) modelChromatograms.Add(modelChrom);
            }

            return modelChromatograms;
        }

        private static ModelChromatogram getModelChromatogram(List<List<ChromatogramPeak>> peaklistList, List<ChromatogramPeakFeature> peakAreas, 
            MsDecBin[] msdecBins, ParameterBase param) {
            if (peakAreas == null || peakAreas.Count == 0) return null;
            var maxSharpnessValue = peakAreas.Max(n => n.PeakShape.ShapenessValue);
            var maxIdealSlopeValue = peakAreas.Max(n => n.PeakShape.IdealSlopeValue);
            var modelChrom = new ModelChromatogram() { SharpnessValue = maxSharpnessValue, IdealSlopeValue = maxIdealSlopeValue };
            var firstFlg = false;

            var peaklist = new List<ChromatogramPeak>();
            var peaklists = new List<List<ChromatogramPeak>>();
            var baselineCorrectedPeaklist = new List<ChromatogramPeak>();

            foreach (var peak in peakAreas.Where(n => n.PeakShape.ShapenessValue >= maxSharpnessValue * 0.9).OrderByDescending(n => n.PeakShape.ShapenessValue)) {
                if (firstFlg == false) {
                    modelChrom.ChromScanOfPeakTop = peak.ChromScanIdTop;
                    modelChrom.ChromScanOfPeakLeft = peak.ChromScanIdLeft;
                    modelChrom.ChromScanOfPeakRight = peak.ChromScanIdRight;
                    modelChrom.ModelMzList.Add((float)peak.Mass);

                    peaklist = getTrimedAndSmoothedPeaklist(peaklistList[peak.PeakID], modelChrom.ChromScanOfPeakLeft, modelChrom.ChromScanOfPeakRight);
                    baselineCorrectedPeaklist = getBaselineCorrectedPeaklist(peaklist, modelChrom.ChromScanOfPeakTop - modelChrom.ChromScanOfPeakLeft);
                    peaklists.Add(baselineCorrectedPeaklist);
                    firstFlg = true;
                }
                else {
                    modelChrom.ModelMzList.Add((float)peak.Mass);
                    peaklist = getTrimedAndSmoothedPeaklist(peaklistList[peak.PeakID], modelChrom.ChromScanOfPeakLeft, modelChrom.ChromScanOfPeakRight);
                    baselineCorrectedPeaklist = getBaselineCorrectedPeaklist(peaklist, modelChrom.ChromScanOfPeakTop - modelChrom.ChromScanOfPeakLeft);
                    peaklists.Add(baselineCorrectedPeaklist);
                }
            }

            double mzCount = modelChrom.ModelMzList.Count;
            foreach (var peak in peaklists[0]) {
                modelChrom.Peaks.Add(new ChromatogramPeak(peak.ID, peak.Mass, peak.Intensity /= mzCount, peak.ChromXs));
            }
            if (peaklists.Count > 1) {
                for (int i = 1; i < peaklists.Count; i++) {
                    for (int j = 0; j < peaklists[i].Count; j++) {
                        modelChrom.Peaks[j].Intensity += peaklists[i][j].Intensity /= mzCount;
                    }
                }
            }
            modelChrom = getRefinedModelChromatogram(modelChrom, msdecBins, param);

            return modelChrom;
        }

        private static List<ChromatogramPeak> getTrimedAndSmoothedPeaklist(IReadOnlyList<ChromatogramPeak> peaklist, int chromScanOfPeakLeft, int chromScanOfPeakRight) {
            var mPeaklist = new List<ChromatogramPeak>(chromScanOfPeakRight - chromScanOfPeakLeft + 1);

            for (int i = chromScanOfPeakLeft; i <= chromScanOfPeakRight; i++) {
                var p = peaklist[i];
                var peak = new ChromatogramPeak(p.ID, p.Mass, p.Intensity, p.ChromXs);
                mPeaklist.Add(peak);
            }

            return mPeaklist;
        }

        private static List<ModelChromatogram_temp> getModelChromatograms(List<ExtractedIonChromatogram> chromatogramList, List<ChromatogramPeakFeature> peakSpots, MsDecBin[] msdecBins, double[] matchedFilterArray, ParameterBase param) {
            var regionMarkers = getRegionMarkers(matchedFilterArray);
            var modelChromatograms = new List<ModelChromatogram_temp>();
            //   Debug.WriteLine("Regions count: {regionMarkers.Count}");
            foreach (var region in regionMarkers) {
                var peakAreas = new List<ChromatogramPeakFeature>();
                for (int i = region.ScanBegin; i <= region.ScanEnd; i++) {
                    foreach (var peakSpot in msdecBins[i].PeakSpots.Where(n => n.Quality == ModelQuality.High))
                        peakAreas.Add(peakSpots[peakSpot.PeakSpotID]);
                }
                if (peakAreas.Count == 0) {
                    for (int i = region.ScanBegin; i <= region.ScanEnd; i++) {
                        foreach (var peakSpot in msdecBins[i].PeakSpots.Where(n => n.Quality == ModelQuality.Middle))
                            peakAreas.Add(peakSpots[peakSpot.PeakSpotID]);
                    }
                }
                if (peakAreas.Count == 0) {
                    for (int i = region.ScanBegin; i <= region.ScanEnd; i++) {
                        foreach (var peakSpot in msdecBins[i].PeakSpots.Where(n => n.Quality == ModelQuality.Low))
                            peakAreas.Add(peakSpots[peakSpot.PeakSpotID]);
                    }
                }
                var modelChrom = getModelChromatogram(chromatogramList, peakAreas, msdecBins, param);
                if (modelChrom != null) modelChromatograms.Add(modelChrom);
            }

            return modelChromatograms;
        }

        private static ModelChromatogram_temp getModelChromatogram(List<ExtractedIonChromatogram> chromatogramList, List<ChromatogramPeakFeature> peakAreas,
            MsDecBin[] msdecBins, ParameterBase param) {
            if (peakAreas == null || peakAreas.Count == 0) return null;
            var maxSharpnessValue = peakAreas.Max(n => n.PeakShape.ShapenessValue);
            var maxIdealSlopeValue = peakAreas.Max(n => n.PeakShape.IdealSlopeValue);
            var modelChrom = new ModelChromatogram_temp() { SharpnessValue = maxSharpnessValue, IdealSlopeValue = maxIdealSlopeValue };
            var firstFlg = false;

            ValuePeak[] peaklist = null;
            ValuePeak[] baselineCorrectedPeaklist = null;
            var peaklists = new List<ValuePeak[]>();

            foreach (var peak in peakAreas.Where(n => n.PeakShape.ShapenessValue >= maxSharpnessValue * 0.9).OrderByDescending(n => n.PeakShape.ShapenessValue)) {
                if (firstFlg == false) {
                    modelChrom.ChromScanOfPeakTop = peak.ChromScanIdTop;
                    modelChrom.ChromScanOfPeakLeft = peak.ChromScanIdLeft;
                    modelChrom.ChromScanOfPeakRight = peak.ChromScanIdRight;
                    modelChrom.ModelMzList.Add((float)peak.Mass);

                    peaklist = chromatogramList[peak.PeakID].TrimPeaks(modelChrom.ChromScanOfPeakLeft, modelChrom.ChromScanOfPeakRight);
                    baselineCorrectedPeaklist = getBaselineCorrectedPeaklist(peaklist, modelChrom.ChromScanOfPeakTop - modelChrom.ChromScanOfPeakLeft);
                    peaklists.Add(baselineCorrectedPeaklist);
                    firstFlg = true;
                }
                else {
                    modelChrom.ModelMzList.Add((float)peak.Mass);
                    peaklist = chromatogramList[peak.PeakID].TrimPeaks(modelChrom.ChromScanOfPeakLeft, modelChrom.ChromScanOfPeakRight);
                    baselineCorrectedPeaklist = getBaselineCorrectedPeaklist(peaklist, modelChrom.ChromScanOfPeakTop - modelChrom.ChromScanOfPeakLeft);
                    peaklists.Add(baselineCorrectedPeaklist);
                }
            }

            double mzCount = modelChrom.ModelMzList.Count;
            modelChrom.Peaks = new ValuePeak[peaklists[0].Length];
            for (int i = 0; i < peaklists[0].Length; i++) {
                var intensity = 0.0;
                var peak = peaklists[0][i];
                for (int j = 0; j < peaklists.Count; j++) {
                    intensity += peaklists[j][i].Intensity;
                }
                modelChrom.Peaks[i] = new ValuePeak(peak.Id, peak.Time, peak.Mz, intensity / mzCount);
            }
            modelChrom = getRefinedModelChromatogram(modelChrom, msdecBins, param);

            return modelChrom;
        }

        private static List<List<ChromatogramPeak>> getMs2Chromatograms(ModelChromVector modelChromVector, List<List<ChromatogramPeak>> peaklistList, ParameterBase param) {
            var peaksList = new List<List<ChromatogramPeak>>();
            foreach (var peaklist in peaklistList) {
                var peaks = getTrimedAndSmoothedPeaklist(peaklist, modelChromVector.ChromScanList[0], modelChromVector.ChromScanList[modelChromVector.ChromScanList.Count - 1]);
                var baselineCorrectedPeaks = getBaselineCorrectedPeaklist(peaks, modelChromVector.TargetScanTopInModelChromVector);
                var peaktopInt = baselineCorrectedPeaks[modelChromVector.TargetScanTopInModelChromVector].Intensity;

                if (peaktopInt <= param.AmplitudeCutoff) continue;

                peaksList.Add(baselineCorrectedPeaks);
            }

            return peaksList;
        }

        private static List<ValuePeak[]> getMs2Chromatograms(ModelChromVector modelChromVector, List<ExtractedIonChromatogram> chromatogramList, ParameterBase param) {
            var peaksList = new List<ValuePeak[]>();
            foreach (var chromatogram in chromatogramList) {
                var peaks = chromatogram.TrimPeaks(modelChromVector.ChromScanList[0], modelChromVector.ChromScanList[modelChromVector.ChromScanList.Count - 1]);
                var baselineCorrectedPeaks = getBaselineCorrectedPeaklist(peaks, modelChromVector.TargetScanTopInModelChromVector);
                var peaktopInt = baselineCorrectedPeaks[modelChromVector.TargetScanTopInModelChromVector].Intensity;

                if (peaktopInt <= param.AmplitudeCutoff) continue;

                peaksList.Add(baselineCorrectedPeaks);
            }

            return peaksList;
        }
        #endregion

        private static string calculateSplash(List<SpectrumPeak> peaks) {
            var ions = new List<Ion>();
            peaks.ForEach(it => ions.Add(new Ion(it.Mass, it.Intensity)));
            string splash = new Splash().splashIt(new MSSpectrum(ions));
            return splash;
        }

        private static List<ModelChromatogram> getRefinedModelChromatograms(List<ModelChromatogram> modelChromatograms) {
            modelChromatograms = modelChromatograms.OrderBy(n => n.ChromScanOfPeakTop).ToList();

            var chromatograms = new List<ModelChromatogram>() { modelChromatograms[0] };
            for (int i = 1; i < modelChromatograms.Count; i++) {
                if (chromatograms[chromatograms.Count - 1].ChromScanOfPeakTop == modelChromatograms[i].ChromScanOfPeakTop) {
                    if (chromatograms[chromatograms.Count - 1].MaximumPeakTopValue < modelChromatograms[i].MaximumPeakTopValue) {
                        chromatograms.RemoveAt(chromatograms.Count - 1);
                        chromatograms.Add(modelChromatograms[i]);
                    }
                }
                else {
                    chromatograms.Add(modelChromatograms[i]);
                }
            }

            return chromatograms;
        }

       
       
        private static ModelChromVector getModelChromatogramVector(int modelID, List<ModelChromatogram> modelChromatograms, MsDecBin[] msdecBins) {
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
            for (int i = modelChromLeft; i <= modelChromRight; i++) {
                modelVector.ChromScanList.Add(i);
                modelVector.RdamScanList.Add(msdecBins[i].RdamScanNumber);
                modelVector.RtArray.Add(msdecBins[i].RetentionTime);

                if (modelChromatograms[modelID].ChromScanOfPeakLeft > i || modelChromatograms[modelID].ChromScanOfPeakRight < i)
                    modelVector.TargetIntensityArray.Add(0);
                else
                    modelVector.TargetIntensityArray.Add((float)modelChromatograms[modelID].Peaks[i - modelChromatograms[modelID].ChromScanOfPeakLeft].Intensity);

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
            modelVector.Ms1DecPattern = getMsDecPattern(isOneLeftModel, isTwoLeftModel, isOneRightModel, isTwoRightModel);
            modelVector.TargetScanLeftInModelChromVector = modelChromatograms[modelID].ChromScanOfPeakLeft - modelChromLeft;
            modelVector.TargetScanTopInModelChromVector = modelVector.TargetScanLeftInModelChromVector + modelChromatograms[modelID].ChromScanOfPeakTop - modelChromatograms[modelID].ChromScanOfPeakLeft;
            modelVector.TargetScanRightInModelChromVector = modelVector.TargetScanLeftInModelChromVector + modelChromatograms[modelID].ChromScanOfPeakRight - modelChromatograms[modelID].ChromScanOfPeakLeft;
            modelVector.EstimatedNoise = estimatedNoise;

            return modelVector;
        }

        private static ModelChromVector getModelChromatogramVector(int modelID, List<ModelChromatogram_temp> modelChromatograms, MsDecBin[] msdecBins) {
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
            for (int i = modelChromLeft; i <= modelChromRight; i++) {
                modelVector.ChromScanList.Add(i);
                modelVector.RdamScanList.Add(msdecBins[i].RdamScanNumber);
                modelVector.RtArray.Add(msdecBins[i].RetentionTime);

                if (modelChromatograms[modelID].ChromScanOfPeakLeft > i || modelChromatograms[modelID].ChromScanOfPeakRight < i)
                    modelVector.TargetIntensityArray.Add(0);
                else
                    modelVector.TargetIntensityArray.Add((float)modelChromatograms[modelID].Peaks[i - modelChromatograms[modelID].ChromScanOfPeakLeft].Intensity);

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
            modelVector.Ms1DecPattern = getMsDecPattern(isOneLeftModel, isTwoLeftModel, isOneRightModel, isTwoRightModel);
            modelVector.TargetScanLeftInModelChromVector = modelChromatograms[modelID].ChromScanOfPeakLeft - modelChromLeft;
            modelVector.TargetScanTopInModelChromVector = modelVector.TargetScanLeftInModelChromVector + modelChromatograms[modelID].ChromScanOfPeakTop - modelChromatograms[modelID].ChromScanOfPeakLeft;
            modelVector.TargetScanRightInModelChromVector = modelVector.TargetScanLeftInModelChromVector + modelChromatograms[modelID].ChromScanOfPeakRight - modelChromatograms[modelID].ChromScanOfPeakLeft;
            modelVector.EstimatedNoise = estimatedNoise;

            return modelVector;
        }


        private static MsDecPattern getMsDecPattern(bool isOneLeftModel, bool isTwoLeftModel, bool isOneRightModel, bool isTwoRightModel) {
            if (!isOneLeftModel && !isTwoLeftModel && !isOneRightModel && !isTwoRightModel) return MsDecPattern.C;
            if (isOneLeftModel && !isTwoLeftModel && !isOneRightModel && !isTwoRightModel) return MsDecPattern.BC;
            if (!isOneLeftModel && !isTwoLeftModel && isOneRightModel && !isTwoRightModel) return MsDecPattern.CD;
            if (isOneLeftModel && !isTwoLeftModel && isOneRightModel && !isTwoRightModel) return MsDecPattern.BCD;
            if (isOneLeftModel && isTwoLeftModel && !isOneRightModel && !isTwoRightModel) return MsDecPattern.ABC;
            if (!isOneLeftModel && !isTwoLeftModel && isOneRightModel && isTwoRightModel) return MsDecPattern.CDE;
            if (isOneLeftModel && isTwoLeftModel && isOneRightModel && !isTwoRightModel) return MsDecPattern.ABCD;
            if (isOneLeftModel && !isTwoLeftModel && isOneRightModel && isTwoRightModel) return MsDecPattern.BCDE;
            if (isOneLeftModel && isTwoLeftModel && isOneRightModel && isTwoRightModel) return MsDecPattern.ABCDE;
            return MsDecPattern.C;
        }

        

        private static ModelChromatogram getRefinedModelChromatogram(ModelChromatogram modelChrom, MsDecBin[] msdecBins, ParameterBase param) {
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
            for (int i = peakTopID; i > 0; i--) {
                if (peakTopID - i < param.AveragePeakWidth * 0.5) continue;
                if (modelChrom.Peaks[i - 1].Intensity >= modelChrom.Peaks[i].Intensity) { peakLeftID = i; break; }
            }
            if (peakLeftID < 0) peakLeftID = 0;

            //right spike check
            for (int i = peakTopID; i < modelChrom.Peaks.Count - 1; i++) {
                if (i - peakTopID < param.AveragePeakWidth * 0.5) continue;
                if (modelChrom.Peaks[i].Intensity <= modelChrom.Peaks[i + 1].Intensity) { peakRightID = i; break; }
            }
            if (peakRightID < 0) peakRightID = modelChrom.Peaks.Count - 1;

            modelChrom.ChromScanOfPeakTop = peakTopID + modelChrom.ChromScanOfPeakLeft;
            modelChrom.ChromScanOfPeakRight = peakRightID + modelChrom.ChromScanOfPeakLeft;
            modelChrom.ChromScanOfPeakLeft = peakLeftID + modelChrom.ChromScanOfPeakLeft;
            modelChrom.RdamScanOfPeakTop = msdecBins[modelChrom.ChromScanOfPeakTop].RdamScanNumber;

            var peaks = new List<ChromatogramPeak>();
            for (int i = peakLeftID; i <= peakRightID; i++)
                peaks.Add(modelChrom.Peaks[i]);

            //final curation
            if (peakTopID - peakLeftID < 3) return null;
            if (peakRightID - peakTopID < 3) return null;

            //var peaktopRT = modelChrom.Peaks[peakTopID].ChromXs;
            //var peaktopMz = modelChrom.ModelMzList[0];

            modelChrom.Peaks = peaks;

            return modelChrom;
        }

        private static ModelChromatogram_temp getRefinedModelChromatogram(ModelChromatogram_temp modelChrom, MsDecBin[] msdecBins, ParameterBase param) {
            double maxIntensity = double.MinValue;
            int peakTopID = -1, peakLeftID = -1, peakRightID = -1;

            for (int i = 0; i < modelChrom.Peaks.Length; i++) {
                if (modelChrom.Peaks[i].Intensity > maxIntensity) {
                    maxIntensity = modelChrom.Peaks[i].Intensity;
                    peakTopID = i;
                }
            }

            modelChrom.MaximumPeakTopValue = maxIntensity;
            //left spike check
            for (int i = peakTopID; i > 0; i--) {
                if (peakTopID - i < param.AveragePeakWidth * 0.5) continue;
                if (modelChrom.Peaks[i - 1].Intensity >= modelChrom.Peaks[i].Intensity) { peakLeftID = i; break; }
            }
            if (peakLeftID < 0) peakLeftID = 0;

            //right spike check
            for (int i = peakTopID; i < modelChrom.Peaks.Length - 1; i++) {
                if (i - peakTopID < param.AveragePeakWidth * 0.5) continue;
                if (modelChrom.Peaks[i].Intensity <= modelChrom.Peaks[i + 1].Intensity) { peakRightID = i; break; }
            }
            if (peakRightID < 0) peakRightID = modelChrom.Peaks.Length - 1;

            modelChrom.ChromScanOfPeakTop = peakTopID + modelChrom.ChromScanOfPeakLeft;
            modelChrom.ChromScanOfPeakRight = peakRightID + modelChrom.ChromScanOfPeakLeft;
            modelChrom.ChromScanOfPeakLeft = peakLeftID + modelChrom.ChromScanOfPeakLeft;
            modelChrom.RdamScanOfPeakTop = msdecBins[modelChrom.ChromScanOfPeakTop].RdamScanNumber;

            var peaks = new ValuePeak[peakRightID - peakLeftID + 1];
            for (int i = peakLeftID; i <= peakRightID; i++)
                peaks[i - peakLeftID] = modelChrom.Peaks[i];

            //final curation
            if (peakTopID - peakLeftID < 3) return null;
            if (peakRightID - peakTopID < 3) return null;

            //var peaktopRT = modelChrom.Peaks[peakTopID].ChromXs;
            //var peaktopMz = modelChrom.ModelMzList[0];

            modelChrom.Peaks = peaks;

            return modelChrom;
        }

        private static List<ChromatogramPeak> getTrimedAndSmoothedPeaklist(IReadOnlyList<RawSpectrum> spectrumList,
            int chromScanOfPeakLeft, int chromScanOfPeakRight, int smoothedMargin, MsDecBin[] msdecBins, float focusedMass, ParameterBase param) {

            int leftRemainder = 0, rightRemainder = 0;
            double sum = 0, maxIntensityMz, maxMass;
            float massTol = param.CentroidMs1Tolerance;

            var chromLeft = chromScanOfPeakLeft - smoothedMargin;
            var chromRight = chromScanOfPeakRight + smoothedMargin;

            if (chromLeft < 0) { leftRemainder = smoothedMargin - chromScanOfPeakLeft; chromLeft = 0; }
            if (chromRight > msdecBins.Length - 1) { rightRemainder = chromScanOfPeakRight + smoothedMargin - (msdecBins.Length - 1); chromRight = msdecBins.Length - 1; }

            var peaklist = new ValuePeak[chromRight - chromLeft + 1];
            for (int i = chromLeft; i <= chromRight; i++) {
                var rdamScan = msdecBins[i].RdamScanNumber;
                var spectrum = spectrumList[rdamScan];
                var massSpectra = spectrum.Spectrum;

                sum = 0;
                maxIntensityMz = double.MinValue;
                maxMass = focusedMass;

                //var startIndex = DataAccess.GetMs1StartIndex(focusedMass, massTol, massSpectra);
                var startIndex = SearchCollection.LowerBound(massSpectra, new RawPeakElement() { Mz = focusedMass - massTol }, (a, b) => a.Mz.CompareTo(b.Mz));
                for (int j = startIndex; j < massSpectra.Length; j++) {
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
                peaklist[i - chromLeft] = new ValuePeak(spectrum.Index, spectrum.ScanStartTime, maxMass, sum);
            }

            using var smoothedChromatogram = new Chromatogram(peaklist, ChromXType.RT, ChromXUnit.Min).ChromatogramSmoothing(param.SmoothingMethod, param.SmoothingLevel);
            var smoothedPeaklist = smoothedChromatogram.AsPeakArray();
            for (int i = 0; i < smoothedMargin - leftRemainder; i++) smoothedPeaklist.RemoveAt(0);
            for (int i = 0; i < smoothedMargin - rightRemainder; i++) smoothedPeaklist.RemoveAt(smoothedPeaklist.Count - 1);

            return smoothedPeaklist;
        }

        private static List<ChromatogramPeak> getBaselineCorrectedPeaklist(List<ChromatogramPeak> peaklist, int peakTop) {
            var baselineCorrectedPeaklist = new List<ChromatogramPeak>();

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

            double coeff = (peaklist[minimumRightID].Intensity - peaklist[minimumLeftID].Intensity) / (peaklist[minimumRightID].ChromXs.Value - peaklist[minimumLeftID].ChromXs.Value);
            double intercept = (peaklist[minimumRightID].ChromXs.Value * peaklist[minimumLeftID].Intensity - peaklist[minimumLeftID].ChromXs.Value * peaklist[minimumRightID].Intensity) / (peaklist[minimumRightID].ChromXs.Value - peaklist[minimumLeftID].ChromXs.Value);
            double correctedIntensity = 0;
            for (int i = 0; i < peaklist.Count; i++) {
                correctedIntensity = peaklist[i].Intensity - (int)(coeff * peaklist[i].ChromXs.Value + intercept);
                if (correctedIntensity >= 0)
                    baselineCorrectedPeaklist.Add(new ChromatogramPeak(peaklist[i].ID, peaklist[i].Mass, correctedIntensity, peaklist[i].ChromXs));
                else
                    baselineCorrectedPeaklist.Add(new ChromatogramPeak(peaklist[i].ID, peaklist[i].Mass, 0, peaklist[i].ChromXs));
            }

            return baselineCorrectedPeaklist;
        }

        private static ValuePeak[] getBaselineCorrectedPeaklist(ValuePeak[] peaklist, int peakTop) {
            var baselineCorrectedPeaklist = new ValuePeak[peaklist.Length];

            //find local minimum of left and right edge
            var minimumLeftID = 0;
            var minimumValue = double.MaxValue;
            for (int i = peakTop; i >= 0; i--)
                if (peaklist[i].Intensity < minimumValue) {
                    minimumValue = peaklist[i].Intensity;
                    minimumLeftID = i;
                }

            var minimumRightID = peaklist.Length - 1;
            minimumValue = double.MaxValue;
            for (int i = peakTop; i < peaklist.Length; i++)
                if (peaklist[i].Intensity < minimumValue) {
                    minimumValue = peaklist[i].Intensity;
                    minimumRightID = i;
                }

            double coeff = (peaklist[minimumRightID].Intensity - peaklist[minimumLeftID].Intensity) / (peaklist[minimumRightID].Time - peaklist[minimumLeftID].Time);
            double intercept = (peaklist[minimumRightID].Time * peaklist[minimumLeftID].Intensity - peaklist[minimumLeftID].Time * peaklist[minimumRightID].Intensity) / (peaklist[minimumRightID].Time - peaklist[minimumLeftID].Time);
            double correctedIntensity = 0;
            for (int i = 0; i < peaklist.Length; i++) {
                correctedIntensity = peaklist[i].Intensity - (int)(coeff * peaklist[i].Time + intercept);
                if (correctedIntensity >= 0)
                    baselineCorrectedPeaklist[i] = new ValuePeak(peaklist[i].Id, peaklist[i].Time, peaklist[i].Mz, correctedIntensity);
                else
                    baselineCorrectedPeaklist[i] = new ValuePeak(peaklist[i].Id, peaklist[i].Time, peaklist[i].Mz, 0);
            }

            return baselineCorrectedPeaklist;
        }

        private static List<RegionMarker> getRegionMarkers(double[] matchedFilterArray) {
            var regionMarkers = new List<RegionMarker>();
            var scanBegin = 0;
            var scanBeginFlg = false;
            var margin = 5;

            for (int i = margin; i < matchedFilterArray.Length - margin; i++) {
                //Debug.WriteLine("MatchedFilter\tID\t{0}\tValue\t{1}", i, matchedFilterArray[i]);

                if (matchedFilterArray[i] > 0 && matchedFilterArray[i - 1] < matchedFilterArray[i] && scanBeginFlg == false) {
                    scanBegin = i; scanBeginFlg = true;
                }
                else if (scanBeginFlg == true) {
                    if (matchedFilterArray[i] <= 0) {
                        regionMarkers.Add(new RegionMarker() { ID = regionMarkers.Count, ScanBegin = scanBegin, ScanEnd = i - 1 });

                        scanBeginFlg = false;
                        continue;
                    }
                    else if (matchedFilterArray[i - 1] > matchedFilterArray[i] && matchedFilterArray[i] < matchedFilterArray[i + 1] && matchedFilterArray[i] >= 0) {
                        regionMarkers.Add(new RegionMarker() { ID = regionMarkers.Count, ScanBegin = scanBegin, ScanEnd = i });
                        scanBegin = i + 1;
                        scanBeginFlg = true;
                        i++;
                    }
                }
            }

            return regionMarkers;
        }

        private static Dictionary<int, int> getRdamAndMs1chromatogramScanDictionary(IReadOnlyList<RawSpectrum> spectrumList, IonMode ionmode) {
            var rdamToChromDictionary = new Dictionary<int, int>();
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            var counter = 0;
            for (int i = 0; i < spectrumList.Count; i++) {
                if (spectrumList[i].MsLevel > 1) continue;
                if (spectrumList[i].ScanPolarity != scanPolarity) continue;
                rdamToChromDictionary[spectrumList[i].ScanNumber] = counter;

                counter++;
            }

            return rdamToChromDictionary;
        }

        private static double[] getMatchedFileterArray(MsDecBin[] msdecBinArray, double sigma) {
            var halfPoint = 10.0; // currently this value should be enough for GC
            var matchedFilterArray = new double[msdecBinArray.Length];
            var matchedFilterCoefficient = new double[2 * (int)halfPoint + 1];

            for (int i = 0; i < matchedFilterCoefficient.Length; i++) {
                matchedFilterCoefficient[i] = (1 - Math.Pow((-halfPoint + i) / sigma, 2)) * Math.Exp(-0.5 * Math.Pow((-halfPoint + i) / sigma, 2));
            }

            for (int i = 0; i < msdecBinArray.Length; i++) {
                var sum = 0.0;
                for (int j = -1 * (int)halfPoint; j <= (int)halfPoint; j++) {
                    if (i + j < 0) sum += 0;
                    else if (i + j > msdecBinArray.Length - 1) sum += 0;
                    else sum += msdecBinArray[i + j].TotalSharpnessValue * matchedFilterCoefficient[(int)(j + halfPoint)];
                }
                matchedFilterArray[i] = sum;
            }

            return matchedFilterArray;
        }

        
        private static List<RawSpectrum> getMs1SpectrumList(IReadOnlyList<RawSpectrum> spectrumList, IonMode ionMode) {
            var ms1SpectrumList = new List<RawSpectrum>();
            var scanPolarity = ionMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            for (int i = 0; i < spectrumList.Count; i++) {
                var spectrum = spectrumList[i];

                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanPolarity != scanPolarity) continue;
                ms1SpectrumList.Add(spectrum);
            }
            return ms1SpectrumList;
        }
    }
}
