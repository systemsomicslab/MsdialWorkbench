using Msdial.Gcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using System;
using System.IO;

namespace edu.ucdavis.fiehnlab.msdial.Writers {
	public class GCDecWriterVer1 : GCDecWriterLegacy {

		public static new void Write(FileStream fs, MS1DecResult ms1DecResult, int ms1DecID, AlignmentPropertyBean alignedProperty = null) {
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

			//Model Masses
			SaveModelMasses(fs, ms1DecResult);			//added in v.1
           
		}

        private static void SaveSignalToNoises(FileStream fs, MS1DecResult ms1DecResult) {
            //noise and S/N
            fs.Write(BitConverter.GetBytes(ms1DecResult.EstimatedNoise), 0, 4);
            fs.Write(BitConverter.GetBytes(ms1DecResult.SignalNoiseRatio), 0, 4);
        }

        protected static new void SaveCounters(FileStream fs, MS1DecResult ms1DecResult) {
			//Spectrum
			fs.Write(BitConverter.GetBytes(ms1DecResult.Spectrum.Count), 0, 4);
			fs.Write(BitConverter.GetBytes(ms1DecResult.BasepeakChromatogram.Count), 0, 4);

			//added in V.1
			fs.Write(BitConverter.GetBytes(ms1DecResult.ModelMasses.Count), 0, 4);
		}

		protected static void SaveModelMasses(FileStream fs, MS1DecResult ms1DecResult) {
			ms1DecResult.ModelMasses.ForEach(mz => {
				fs.Write(BitConverter.GetBytes(mz), 0, 4);
			});
		}

		protected static new void SaveScoringData(FileStream fs, MS1DecResult ms1DecResult) {
			fs.Write(BitConverter.GetBytes(ms1DecResult.AmplitudeScore), 0, 4);

			//added in V.1
			fs.Write(BitConverter.GetBytes(ms1DecResult.ModelPeakPurity), 0, 4);
			fs.Write(BitConverter.GetBytes(ms1DecResult.ModelPeakQuality), 0, 4);


		}

	}
}
