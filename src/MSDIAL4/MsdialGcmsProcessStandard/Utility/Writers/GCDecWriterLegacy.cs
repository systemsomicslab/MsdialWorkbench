using Msdial.Gcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using System;
using System.IO;

namespace edu.ucdavis.fiehnlab.msdial.Writers {
	public class GCDecWriterLegacy {

		public static void Write(FileStream fs, MS1DecResult ms1DecResult, int ms1DecID, AlignmentPropertyBean alignedProperty = null) {
			//Scan
			SaveScanData(fs, ms1DecResult);

			//Quant info
			SaveQuantData(fs, ms1DecResult, alignedProperty);

			//Information
			SaveInfoData(fs, ms1DecResult, alignedProperty);

			//Score
			SaveScoringData(fs, ms1DecResult);

			//Counters for variable lists of data
			SaveCounters(fs, ms1DecResult);

			//Spectral Data
			SaveSpectra(fs, ms1DecResult);

			//Base Peak Data
			SaveBasePeaks(fs, ms1DecResult);
		}

		protected static void SaveScanData(FileStream fs, MS1DecResult ms1DecResult) {
			fs.Write(BitConverter.GetBytes(ms1DecResult.SeekPoint), 0, 8);
			fs.Write(BitConverter.GetBytes(ms1DecResult.ScanNumber), 0, 4);
			fs.Write(BitConverter.GetBytes(ms1DecResult.Ms1DecID), 0, 4);
		}

		protected static void SaveQuantData(FileStream fs, MS1DecResult ms1DecResult, AlignmentPropertyBean alignedProperty) {
			var quantMass = alignedProperty != null ? alignedProperty.QuantMass : ms1DecResult.BasepeakMz;
			var basePeakArea = alignedProperty != null ? alignedProperty.AverageValiable : ms1DecResult.BasepeakArea;
			var basePeakHeight = alignedProperty != null ? alignedProperty.AverageValiable : ms1DecResult.BasepeakHeight;
			var retentionTime = alignedProperty != null ? alignedProperty.CentralRetentionTime : ms1DecResult.RetentionTime;
			var retentionIndex = alignedProperty != null ? alignedProperty.CentralRetentionIndex : ms1DecResult.RetentionIndex;
			var integratedHeight = alignedProperty != null ? alignedProperty.AverageValiable : ms1DecResult.IntegratedHeight;
			var integratedArea = alignedProperty != null ? alignedProperty.AverageValiable : ms1DecResult.IntegratedArea;
            var estimatedNoise = alignedProperty != null ? alignedProperty.EstimatedNoiseAve : ms1DecResult.EstimatedNoise;
            var signalToNoise = alignedProperty != null ? alignedProperty.SignalToNoiseAve : ms1DecResult.SignalNoiseRatio;
            fs.Write(BitConverter.GetBytes(quantMass), 0, 4);
			fs.Write(BitConverter.GetBytes(basePeakArea), 0, 4);
			fs.Write(BitConverter.GetBytes(basePeakHeight), 0, 4);
			fs.Write(BitConverter.GetBytes(retentionTime), 0, 4);
			fs.Write(BitConverter.GetBytes(retentionIndex), 0, 4);
			fs.Write(BitConverter.GetBytes(integratedHeight), 0, 4);
            fs.Write(BitConverter.GetBytes(integratedArea), 0, 4);
            fs.Write(BitConverter.GetBytes(estimatedNoise), 0, 4); // added in v.2.0
            fs.Write(BitConverter.GetBytes(signalToNoise), 0, 4); // added in v.2.0
        }

        protected static void SaveInfoData(FileStream fs, MS1DecResult ms1DecResult, AlignmentPropertyBean alignedProperty) {
			var mspDbId = alignedProperty != null ? alignedProperty.LibraryID : ms1DecResult.MspDbID;
			var retentionTimeSimilarity = alignedProperty != null ? alignedProperty.RetentionTimeSimilarity : ms1DecResult.RetentionTimeSimilarity;
			var retentionIndexSimilarity = alignedProperty != null ? alignedProperty.RetentionIndexSimilarity : ms1DecResult.RetentionIndexSimilarity;
			var eiSpectrumSimilarity = alignedProperty != null ? alignedProperty.EiSpectrumSimilarity : ms1DecResult.EiSpectrumSimilarity;
			var dotProduct = alignedProperty != null ? alignedProperty.MassSpectraSimilarity : ms1DecResult.DotProduct;
			var reverseDotProduct = alignedProperty != null ? alignedProperty.ReverseSimilarity : ms1DecResult.ReverseDotProduct;
			var presencePersentage = alignedProperty != null ? alignedProperty.FragmentPresencePercentage : ms1DecResult.PresencePersentage;
			var totalSimilarity = alignedProperty != null ? alignedProperty.TotalSimilairty : ms1DecResult.TotalSimilarity;
			fs.Write(BitConverter.GetBytes(mspDbId), 0, 4);
			fs.Write(BitConverter.GetBytes(retentionTimeSimilarity), 0, 4);
			fs.Write(BitConverter.GetBytes(retentionIndexSimilarity), 0, 4);
			fs.Write(BitConverter.GetBytes(eiSpectrumSimilarity), 0, 4);
			fs.Write(BitConverter.GetBytes(dotProduct), 0, 4);
			fs.Write(BitConverter.GetBytes(reverseDotProduct), 0, 4);
			fs.Write(BitConverter.GetBytes(presencePersentage), 0, 4);
			fs.Write(BitConverter.GetBytes(totalSimilarity), 0, 4);
		}

		protected static void SaveScoringData(FileStream fs, MS1DecResult ms1DecResult) {
			fs.Write(BitConverter.GetBytes(ms1DecResult.AmplitudeScore), 0, 4);
		}

		protected static void SaveCounters(FileStream fs, MS1DecResult ms1DecResult) {
			//Spectrum
			fs.Write(BitConverter.GetBytes(ms1DecResult.Spectrum.Count), 0, 4);
			fs.Write(BitConverter.GetBytes(ms1DecResult.BasepeakChromatogram.Count), 0, 4);
		}

		protected static void SaveSpectra(FileStream fs, MS1DecResult ms1DecResult) {
			for (int i = 0; i < ms1DecResult.Spectrum.Count; i++) {
				var peak = ms1DecResult.Spectrum[i];
				fs.Write(BitConverter.GetBytes((float)peak.Mz), 0, 4);
				fs.Write(BitConverter.GetBytes((float)peak.Intensity), 0, 4);
				fs.Write(BitConverter.GetBytes((int)peak.PeakQuality), 0, 4);
			}
		}

		protected static void SaveBasePeaks(FileStream fs, MS1DecResult ms1DecResult) {
			for (int i = 0; i < ms1DecResult.BasepeakChromatogram.Count; i++) {
				var peak = ms1DecResult.BasepeakChromatogram[i];
				fs.Write(BitConverter.GetBytes(peak.ScanNumber), 0, 4);
				fs.Write(BitConverter.GetBytes((float)peak.RetentionTime), 0, 4);
				fs.Write(BitConverter.GetBytes((float)peak.Mz), 0, 4);
				fs.Write(BitConverter.GetBytes((float)peak.Intensity), 0, 4);
			}
		}
	}
}
