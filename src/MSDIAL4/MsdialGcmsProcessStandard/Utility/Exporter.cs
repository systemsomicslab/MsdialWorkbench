using CompMs.Common.DataObj;
using Msdial.Gcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Algorithm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
//using System.Windows.Documents;

namespace Rfx.Riken.OsakaUniv {
	public class Exporter {
		static string path = @"G:\Data\Carrot\P20-lipids\results\";

		public static void export(string filename, List<double[]> data) {
			Debug.WriteLine("Writing file: " + filename);
			using (StreamWriter fs = new StreamWriter(path + filename)) {
				fs.WriteLine("Scan#\tRT(min)\tMZ\tIntensity");
				if (data == null || data.Count == 0) { return; }
				foreach (var item in data) {
					fs.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}", item[0], item[1], item[2], item[3]));
				}
			}
		}

		public static void export(string filename, ObservableCollection<PeakDetectionResult> data) {
			Debug.WriteLine("Writing file: " + filename);
			using (StreamWriter fs = new StreamWriter(path + filename)) {
				fs.WriteLine("AmplitudeOrderValue\tAmplitudeScoreValue\tAreaAboveBaseline\tAreaAboveZero\tBasePeakValue\tGaussianSimilarityValue\tIdealSlopeValue\tIntensityAtLeftPeakEdge\tIntensityAtPeakTop\tIntensityAtRightPeakEdge\tPeakID\tPeakPureValue\tRtAtLeftPeakEdge\tRtAtPeakTop\tRtAtRightPeakEdge\tScanNumAtLeftPeakEdge\tScanNumAtPeakTop\tScanNumAtRightPeakEdge\tShapnessValue\tSymmetryValue");
				if (data == null || data.Count == 0) { return; }
				foreach (var item in data) {
					fs.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\t{19}",
						item.AmplitudeOrderValue, item.AmplitudeScoreValue,
						item.AreaAboveBaseline, item.AreaAboveZero,
						item.BasePeakValue, item.GaussianSimilarityValue,
						item.IdealSlopeValue, item.IntensityAtLeftPeakEdge,
						item.IntensityAtPeakTop, item.IntensityAtRightPeakEdge,
						item.PeakID, item.PeakPureValue,
						item.RtAtLeftPeakEdge, item.RtAtPeakTop,
						item.RtAtRightPeakEdge, item.ScanNumAtLeftPeakEdge,
						item.ScanNumAtPeakTop, item.ScanNumAtRightPeakEdge,
						item.ShapnessValue, item.SymmetryValue));
				}
			}
		}

		// exports a peak area bean
		public static void export(ObservableCollection<PeakAreaBean> peakCollection, string fileName) {
			using (var fs = new StreamWriter(path + fileName)) {
				fs.WriteLine("Peak ID\tAccurateMass\tAccurateMassSimilarity\tAdductIonAccurateMass\tAdductIonChargeNumber\tAdductIonName\tAdductIonXmer\tAdductParent\tAlignedRetentionTime\tAmplitudeOrderValue\t" +
					"AmplitudeRatioSimilatiryValue\tAmplitudeScoreValue\tAreaAboveBaseline\tAreaAboveZero\tBasePeakValue\tDeconvolutionID\tGaussianSimilarityValue\tIdealSlopeValue\t" +
					"IntensityAtLeftPeakEdge\tIntensityAtPeakTop\tIntensityAtRightPeakEdge\tIsotopeParentPeakID\tIsotopeSimilarityValue\tIsotopeWeightNumber\tLibraryID\tMassSpectraSimilarityValue\tMetaboliteName\t" +
					"Ms1IsotopicIonM1PeakHeight\tMs1IsotopicIonM2PeakHeight\tMs1LevelDatapointNumber\tMs2LevelDatapointNumber\tNormalizedValue\tPeakPureValue\tPeakShapeSimilarityValue\tPeakTopDifferencialValue\t" +
					"PostIdentificationLibraryId\tPresenseSimilarityValue\tReverseSearchSimilarityValue\tRtAtLeftPeakEdge\tRtAtPeakTop\tRtAtRightPeakEdge\tRtSimilarityValue\tScanNumberAtLeftPeakEdge\t" +
					"ScanNumberAtPeakTop\tScanNumberAtRightPeakEdge\tShapenessValue\tSymmetryValue\tTotalScore");

				foreach (PeakAreaBean peak in peakCollection) {
					fs.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\t{19}\t" +
											"{20}\t{21}\t{22}\t{23}\t{24}\t{25}\t{26}\t{27}\t{28}\t{29}\t{30}\t{31}\t{32}\t{33}\t{34}\t{35}\t{36}\t{37}\t{38}\t{39}\t" +
											"{40}\t{41}\t{42}\t{43}\t{44}\t{45}\t{46}\t{47}",
						peak.PeakID, peak.AccurateMass, peak.AccurateMassSimilarity,
						peak.AdductIonAccurateMass, peak.AdductIonChargeNumber,
						peak.AdductIonName, peak.AdductIonXmer, peak.AdductParent,
						peak.AlignedRetentionTime, peak.AmplitudeOrderValue,
						peak.AmplitudeRatioSimilatiryValue, peak.AmplitudeScoreValue,
						peak.AreaAboveBaseline, peak.AreaAboveZero,
						peak.BasePeakValue, peak.DeconvolutionID,
						peak.GaussianSimilarityValue, peak.IdealSlopeValue,
						peak.IntensityAtLeftPeakEdge, peak.IntensityAtPeakTop, peak.IntensityAtRightPeakEdge,
						peak.IsotopeParentPeakID, peak.IsotopeSimilarityValue, peak.IsotopeWeightNumber,
						peak.LibraryID, peak.MassSpectraSimilarityValue, peak.MetaboliteName,
						peak.Ms1IsotopicIonM1PeakHeight, peak.Ms1IsotopicIonM2PeakHeight,
						peak.Ms1LevelDatapointNumber, peak.Ms2LevelDatapointNumber,
						peak.NormalizedValue, peak.PeakPureValue,
						peak.PeakShapeSimilarityValue, peak.PeakTopDifferencialValue,
						peak.PostIdentificationLibraryId, peak.PresenseSimilarityValue,
						peak.ReverseSearchSimilarityValue,
						peak.RtAtLeftPeakEdge, peak.RtAtPeakTop, peak.RtAtRightPeakEdge,
						peak.RtSimilarityValue,
						peak.ScanNumberAtLeftPeakEdge, peak.ScanNumberAtPeakTop, peak.ScanNumberAtRightPeakEdge,
						peak.ShapenessValue, peak.SymmetryValue, peak.TotalScore
						));
				}
			}
		}

		//export an MS1 level deconvolution result
		public static void export(List<MS1DecResult> peakList, string fileName) {
			using (var fs = new StreamWriter(path + fileName)) {
				fs.WriteLine("Scan#\tRT\tRI\tMs1DecID\tBasepeak Mz\tBasePeak Area\tBAsePeak Height\tIntegrated Area\tIntegrated Height\tModelPeakPurity\tModelPeakQuality\t" +
					"DotProduct\tAmplitudeScore\tMetaboliteName\tMspDbID\tPresencePersentage\tReverseDotProduct\tSignalNoiseRatio\tSeekPoint\tSplash\tRetentionTimeSimilarity\t" +
					"RetentionIndexSimilarity\tEiSpectrumSimilarity\tTotalSimilarity\tModelMasses\tSpectrum\tBasepeakChromatogram");

				foreach (MS1DecResult peak in peakList) {
					fs.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\t{19}\t{20}\t{21}\t{22}\t{23}\t[{24}]\t[{25}]\t[{26}]",
							peak.ScanNumber,
							peak.RetentionTime,
							peak.RetentionIndex,
							peak.Ms1DecID,
							peak.BasepeakMz,
							peak.BasepeakArea,
							peak.BasepeakHeight,
							peak.IntegratedArea,
							peak.IntegratedHeight,
							peak.ModelPeakPurity,
							peak.ModelPeakQuality,
							peak.DotProduct,
							peak.AmplitudeScore,
							peak.MetaboliteName,
							peak.MspDbID,
							peak.PresencePersentage,
							peak.ReverseDotProduct,
							peak.SignalNoiseRatio,
							peak.SeekPoint,
							peak.Splash,
							peak.RetentionTimeSimilarity,
							peak.RetentionIndexSimilarity,
							peak.EiSpectrumSimilarity,
							peak.TotalSimilarity,
							string.Join(",", peak.ModelMasses),
							string.Join("; ", peak.Spectrum.ConvertAll(s => string.Format("({0}:{1})", s.Mz, s.Intensity))),
							string.Join("; ", peak.BasepeakChromatogram.ConvertAll(s => string.Format("{0},{1},{2},{3},{4}", s.ScanNumber, s.RetentionTime, s.Resolution, s.PeakQuality, s.IsotopeFrag)))
						));
				}
			}
		}

		//export a deconvolution result
		public static void export(List<MS2DecResult> peakList, string fileName) {
			using (var fs = new StreamWriter(path + fileName)) {
				fs.WriteLine("PeakTopScan\tPeakTopRetentionTime\tUniqueMs\tMs1AccurateMass\tMs1PeakHeight\tMs1IsotopicIonM1PeakHeight\tMs1IsotopicIonM2PeakHeight\tMs2DecPeakArea\tMs2DecPeakHeight\t" +
					"ModelMasses\tMassSpectrum\tBaseChromatogram\tPeakListList");

				foreach (MS2DecResult peak in peakList) {
					fs.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t[{9}]\t[{10}]\t[{11}]\t[{12}]",
							peak.PeakTopScan,
							peak.PeakTopRetentionTime,
							peak.UniqueMs,
							peak.Ms1AccurateMass,
							peak.Ms1PeakHeight,
							peak.Ms1IsotopicIonM1PeakHeight,
							peak.Ms1IsotopicIonM2PeakHeight,
							peak.Ms2DecPeakArea,
							peak.Ms2DecPeakHeight,
							string.Join(",", peak.ModelMasses),
							string.Join(" ", peak.MassSpectra.ConvertAll(s => s[0] + ":" + s[1])),
							string.Join(" ", peak.BaseChromatogram.ConvertAll(s => s[0] + ":" + s[1])),
							string.Join("|", peak.PeaklistList.ConvertAll(pl => string.Join(" ; ", pl.ConvertAll(p => p[0] + "," + p[1] + "," + p[2] + "," + p[3]))))
						));
				}
			}
		}

		//export a Raw spectrum collection
		public static void export(ObservableCollection<RawSpectrum> spectra, string fileName) {
			using (var fs = new StreamWriter(path + fileName)) {
				fs.WriteLine("Scan#\tRT(min)\tMS Level\tNumber of Peaks\tPolarity\tPrecursor MZ\tBase Peak MZ\tBase Peak Int\tLowest MZ\tHighest MZ\tMin Intensity\tTIC\tSpectrum");

				foreach (RawSpectrum spec in spectra) {
					var specList = new List<RawPeakElement>(spec.Spectrum);
					fs.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}",
							spec.ScanNumber,
							spec.ScanStartTime,
							spec.MsLevel,
							spec.Spectrum.Length,
							spec.ScanPolarity,
							spec.Precursor,
							spec.BasePeakMz,
							spec.BasePeakIntensity,
							spec.LowestObservedMz,
							spec.HighestObservedMz,
							spec.MinIntensity,
							spec.TotalIonCurrent,
							string.Join("; ", specList.ConvertAll(peak => string.Format("({0}:{1})", peak.Mz, peak.Intensity)))
						));
				}
			}
		}

		//export a RDAM spectrum collection
		public static void export(GcmsDecBin[] data, string fileName) {
			using (var fs = new StreamWriter(path + fileName)) {
				fs.WriteLine("Scan#\tRT\ttot sharpness\tpeak spots");

				for (int i = 0; i < data.Length; i++) {
					GcmsDecBin spec = data[i];
					fs.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}",
							spec.RdamScanNumber,
							spec.RetentionTime,
							spec.TotalSharpnessValue,
							spec.PeakSpots
						));
				}
			}
		}
    }
}
