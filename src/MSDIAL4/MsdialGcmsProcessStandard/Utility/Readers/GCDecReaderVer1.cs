using Msdial.Gcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace edu.ucdavis.fiehnlab.msdial.Readers {
	public class GCDecReaderVer1 {
		public static List<MS1DecResult> ReadAll(FileStream fs) {
			var seekpointList = new List<long>();
			var buffer = new byte[4];

			fs.Read(buffer, 0, 4);

			var totalPeakNumber = BitConverter.ToInt32(buffer, 0);
			buffer = new byte[8 * totalPeakNumber];
			fs.Read(buffer, 0, buffer.Length);
			for (int i = 0; i < totalPeakNumber; i++)
				seekpointList.Add(BitConverter.ToInt64(buffer, 8 * i));

			var ms1DecResults = new List<MS1DecResult>();
			for (int i = 0; i < seekpointList.Count; i++) {
				buffer = new byte[100];
				fs.Read(buffer, 0, buffer.Length);

				var ms1DecResult = new MS1DecResult();

				//Scan
				ms1DecResult.SeekPoint = BitConverter.ToInt64(buffer, 0);
				ms1DecResult.ScanNumber = BitConverter.ToInt32(buffer, 8);
				ms1DecResult.Ms1DecID = BitConverter.ToInt32(buffer, 12);

				//Quant info
				ms1DecResult.BasepeakMz = BitConverter.ToSingle(buffer, 16);
                ms1DecResult.BasepeakArea = BitConverter.ToSingle(buffer, 20);
                ms1DecResult.BasepeakHeight = BitConverter.ToSingle(buffer, 24);
				ms1DecResult.RetentionTime = BitConverter.ToSingle(buffer, 28);
				ms1DecResult.RetentionIndex = BitConverter.ToSingle(buffer, 32);
				ms1DecResult.IntegratedHeight = BitConverter.ToSingle(buffer, 36);
				ms1DecResult.IntegratedArea = BitConverter.ToSingle(buffer, 40);

				//Identification
				ms1DecResult.MspDbID = BitConverter.ToInt32(buffer, 44);
				ms1DecResult.RetentionTimeSimilarity = BitConverter.ToSingle(buffer, 48);
				ms1DecResult.RetentionIndexSimilarity = BitConverter.ToSingle(buffer, 52);
				ms1DecResult.EiSpectrumSimilarity = BitConverter.ToSingle(buffer, 56);
				ms1DecResult.DotProduct = BitConverter.ToSingle(buffer, 60);
				ms1DecResult.ReverseDotProduct = BitConverter.ToSingle(buffer, 64);
				ms1DecResult.PresencePersentage = BitConverter.ToSingle(buffer, 68);
				ms1DecResult.TotalSimilarity = BitConverter.ToSingle(buffer, 72);

				//Score
				ms1DecResult.AmplitudeScore = BitConverter.ToSingle(buffer, 76);
				ms1DecResult.ModelPeakPurity = BitConverter.ToSingle(buffer, 80);
				ms1DecResult.ModelPeakQuality = BitConverter.ToSingle(buffer, 84);

				//Spectrum num
				int spectraNumber = BitConverter.ToInt32(buffer, 88);
				int datapointNumber = BitConverter.ToInt32(buffer, 92);
				int modelMasses = BitConverter.ToInt32(buffer, 96);

				double maxIntensity = double.MinValue;

				// reading spectra data
				buffer = new byte[spectraNumber * 12];
				fs.Read(buffer, 0, buffer.Length);
				for (int j = 0; j < spectraNumber; j++) {
					ms1DecResult.Spectrum.Add(new Peak {
						Mz = BitConverter.ToSingle(buffer, 12 * j),
						Intensity = BitConverter.ToSingle(buffer, 12 * j + 4),
						PeakQuality = (PeakQuality)BitConverter.ToInt32(buffer, 12 * j + 8)
					});
					if (maxIntensity < ms1DecResult.Spectrum[j].Intensity) maxIntensity = ms1DecResult.Spectrum[j].Intensity;
				}

				//reading base peaks
				var basePeaklist = new List<Peak>();
				buffer = new byte[datapointNumber * 16];
				fs.Read(buffer, 0, buffer.Length);

				for (int j = 0; j < datapointNumber; j++) {
					ms1DecResult.BasepeakChromatogram.Add(new Peak() {
						ScanNumber = BitConverter.ToInt32(buffer, 16 * j),
						RetentionTime = BitConverter.ToSingle(buffer, 16 * j + 4),
						Mz = BitConverter.ToSingle(buffer, 16 * j + 8),
						Intensity = BitConverter.ToSingle(buffer, 16 * j + 12)
					});
				}

				// reading model masses
				buffer = new byte[modelMasses * 4];
				fs.Read(buffer, 0, buffer.Length);

				for (int j = 0; j < modelMasses; j++) {
					ms1DecResult.ModelMasses.Add(BitConverter.ToSingle(buffer, 4 * j));
				}

				ms1DecResults.Add(ms1DecResult);
			}

			return ms1DecResults;
		}

		public static MS1DecResult Read(FileStream fs, long seekPoint) {
			var ms1DecResult = new MS1DecResult();

			fs.Seek(seekPoint, SeekOrigin.Begin);
			var buffer = new byte[100];
			fs.Read(buffer, 0, buffer.Length);

			//Scan
			ms1DecResult.SeekPoint = BitConverter.ToInt64(buffer, 0);
			ms1DecResult.ScanNumber = BitConverter.ToInt32(buffer, 8);
			ms1DecResult.Ms1DecID = BitConverter.ToInt32(buffer, 12);

			//Quant info
			ms1DecResult.BasepeakMz = BitConverter.ToSingle(buffer, 16);
            ms1DecResult.BasepeakArea = BitConverter.ToSingle(buffer, 20);
            ms1DecResult.BasepeakHeight = BitConverter.ToSingle(buffer, 24);
			ms1DecResult.RetentionTime = BitConverter.ToSingle(buffer, 28);
			ms1DecResult.RetentionIndex = BitConverter.ToSingle(buffer, 32);
			ms1DecResult.IntegratedHeight = BitConverter.ToSingle(buffer, 36);
			ms1DecResult.IntegratedArea = BitConverter.ToSingle(buffer, 40);

			//Identification
			ms1DecResult.MspDbID = BitConverter.ToInt32(buffer, 44);
			ms1DecResult.RetentionTimeSimilarity = BitConverter.ToSingle(buffer, 48);
			ms1DecResult.RetentionIndexSimilarity = BitConverter.ToSingle(buffer, 52);
			ms1DecResult.EiSpectrumSimilarity = BitConverter.ToSingle(buffer, 56);
			ms1DecResult.DotProduct = BitConverter.ToSingle(buffer, 60);
			ms1DecResult.ReverseDotProduct = BitConverter.ToSingle(buffer, 64);
			ms1DecResult.PresencePersentage = BitConverter.ToSingle(buffer, 68);
			ms1DecResult.TotalSimilarity = BitConverter.ToSingle(buffer, 72);

			//Score
			ms1DecResult.AmplitudeScore = BitConverter.ToSingle(buffer, 76);
			ms1DecResult.ModelPeakPurity = BitConverter.ToSingle(buffer, 80);
			ms1DecResult.ModelPeakQuality = BitConverter.ToSingle(buffer, 84);

			//Spectrum num
			int spectraNumber = BitConverter.ToInt32(buffer, 88);
			int datapointNumber = BitConverter.ToInt32(buffer, 92);
			int modelMasses = BitConverter.ToInt32(buffer, 96);

			double maxIntensity = double.MinValue;

			buffer = new byte[spectraNumber * 12];
			fs.Read(buffer, 0, buffer.Length);
			for (int j = 0; j < spectraNumber; j++) {
				ms1DecResult.Spectrum.Add(new Peak {
					Mz = BitConverter.ToSingle(buffer, 12 * j),
					Intensity = BitConverter.ToSingle(buffer, 12 * j + 4),
					PeakQuality = (PeakQuality)BitConverter.ToInt32(buffer, 12 * j + 8)
				});

				if (maxIntensity < ms1DecResult.Spectrum[j].Intensity) maxIntensity = ms1DecResult.Spectrum[j].Intensity;
			}

			var basePeaklist = new List<Peak>();
			buffer = new byte[datapointNumber * 16];
			fs.Read(buffer, 0, buffer.Length);

			for (int j = 0; j < datapointNumber; j++) {
				ms1DecResult.BasepeakChromatogram.Add(new Peak() {
					ScanNumber = BitConverter.ToInt32(buffer, 16 * j),
					RetentionTime = BitConverter.ToSingle(buffer, 16 * j + 4),
					Mz = BitConverter.ToSingle(buffer, 16 * j + 8),
					Intensity = BitConverter.ToSingle(buffer, 16 * j + 12)
				});
			}

			// reading model masses
			buffer = new byte[modelMasses * 4];
			fs.Read(buffer, 0, buffer.Length);

			for (int j = 0; j < modelMasses; j++) {
				ms1DecResult.ModelMasses.Add(BitConverter.ToSingle(buffer, 4 * j));
			}

			return ms1DecResult;
		}
	}

    public class GCDecReaderVer2 {
        public static List<MS1DecResult> ReadAll(FileStream fs) {
            var seekpointList = new List<long>();
            var buffer = new byte[4];

            fs.Read(buffer, 0, 4);

            var totalPeakNumber = BitConverter.ToInt32(buffer, 0);
            buffer = new byte[8 * totalPeakNumber];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < totalPeakNumber; i++)
                seekpointList.Add(BitConverter.ToInt64(buffer, 8 * i));

            var ms1DecResults = new List<MS1DecResult>();
            for (int i = 0; i < seekpointList.Count; i++) {
                buffer = new byte[108];
                fs.Read(buffer, 0, buffer.Length);

                var ms1DecResult = new MS1DecResult();

                //Scan
                ms1DecResult.SeekPoint = BitConverter.ToInt64(buffer, 0);
                ms1DecResult.ScanNumber = BitConverter.ToInt32(buffer, 8);
                ms1DecResult.Ms1DecID = BitConverter.ToInt32(buffer, 12);

                //Quant info
                ms1DecResult.BasepeakMz = BitConverter.ToSingle(buffer, 16);
                ms1DecResult.BasepeakArea = BitConverter.ToSingle(buffer, 20);
                ms1DecResult.BasepeakHeight = BitConverter.ToSingle(buffer, 24);
                ms1DecResult.RetentionTime = BitConverter.ToSingle(buffer, 28);
                ms1DecResult.RetentionIndex = BitConverter.ToSingle(buffer, 32);
                ms1DecResult.IntegratedHeight = BitConverter.ToSingle(buffer, 36);
                ms1DecResult.IntegratedArea = BitConverter.ToSingle(buffer, 40);
                ms1DecResult.EstimatedNoise = BitConverter.ToSingle(buffer, 44);
                ms1DecResult.SignalNoiseRatio = BitConverter.ToSingle(buffer, 48);

                //Identification
                ms1DecResult.MspDbID = BitConverter.ToInt32(buffer, 52);
                ms1DecResult.RetentionTimeSimilarity = BitConverter.ToSingle(buffer, 56);
                ms1DecResult.RetentionIndexSimilarity = BitConverter.ToSingle(buffer, 60);
                ms1DecResult.EiSpectrumSimilarity = BitConverter.ToSingle(buffer, 64);
                ms1DecResult.DotProduct = BitConverter.ToSingle(buffer, 68);
                ms1DecResult.ReverseDotProduct = BitConverter.ToSingle(buffer, 72);
                ms1DecResult.PresencePersentage = BitConverter.ToSingle(buffer, 76);
                ms1DecResult.TotalSimilarity = BitConverter.ToSingle(buffer, 80);

                //Score
                ms1DecResult.AmplitudeScore = BitConverter.ToSingle(buffer, 84);
                ms1DecResult.ModelPeakPurity = BitConverter.ToSingle(buffer, 88);
                ms1DecResult.ModelPeakQuality = BitConverter.ToSingle(buffer, 92);

                //Spectrum num
                int spectraNumber = BitConverter.ToInt32(buffer, 96);
                int datapointNumber = BitConverter.ToInt32(buffer, 100);
                int modelMasses = BitConverter.ToInt32(buffer, 104);

                double maxIntensity = double.MinValue;

                // reading spectra data
                buffer = new byte[spectraNumber * 12];
                fs.Read(buffer, 0, buffer.Length);
                for (int j = 0; j < spectraNumber; j++) {
                    ms1DecResult.Spectrum.Add(new Peak {
                        Mz = BitConverter.ToSingle(buffer, 12 * j),
                        Intensity = BitConverter.ToSingle(buffer, 12 * j + 4),
                        PeakQuality = (PeakQuality)BitConverter.ToInt32(buffer, 12 * j + 8)
                    });
                    if (maxIntensity < ms1DecResult.Spectrum[j].Intensity) maxIntensity = ms1DecResult.Spectrum[j].Intensity;
                }

                //reading base peaks
                var basePeaklist = new List<Peak>();
                buffer = new byte[datapointNumber * 16];
                fs.Read(buffer, 0, buffer.Length);

                for (int j = 0; j < datapointNumber; j++) {
                    ms1DecResult.BasepeakChromatogram.Add(new Peak() {
                        ScanNumber = BitConverter.ToInt32(buffer, 16 * j),
                        RetentionTime = BitConverter.ToSingle(buffer, 16 * j + 4),
                        Mz = BitConverter.ToSingle(buffer, 16 * j + 8),
                        Intensity = BitConverter.ToSingle(buffer, 16 * j + 12)
                    });
                }

                // reading model masses
                buffer = new byte[modelMasses * 4];
                fs.Read(buffer, 0, buffer.Length);

                for (int j = 0; j < modelMasses; j++) {
                    ms1DecResult.ModelMasses.Add(BitConverter.ToSingle(buffer, 4 * j));
                }

                ms1DecResults.Add(ms1DecResult);
            }

            return ms1DecResults;
        }

        public static MS1DecResult Read(FileStream fs, long seekPoint) {
            var ms1DecResult = new MS1DecResult();

            fs.Seek(seekPoint, SeekOrigin.Begin);
            var buffer = new byte[108];
            fs.Read(buffer, 0, buffer.Length);

            //Scan
            ms1DecResult.SeekPoint = BitConverter.ToInt64(buffer, 0);
            ms1DecResult.ScanNumber = BitConverter.ToInt32(buffer, 8);
            ms1DecResult.Ms1DecID = BitConverter.ToInt32(buffer, 12);

            //Quant info
            ms1DecResult.BasepeakMz = BitConverter.ToSingle(buffer, 16);
            ms1DecResult.BasepeakArea = BitConverter.ToSingle(buffer, 20);
            ms1DecResult.BasepeakHeight = BitConverter.ToSingle(buffer, 24);
            ms1DecResult.RetentionTime = BitConverter.ToSingle(buffer, 28);
            ms1DecResult.RetentionIndex = BitConverter.ToSingle(buffer, 32);
            ms1DecResult.IntegratedHeight = BitConverter.ToSingle(buffer, 36);
            ms1DecResult.IntegratedArea = BitConverter.ToSingle(buffer, 40);
            ms1DecResult.EstimatedNoise = BitConverter.ToSingle(buffer, 44);
            ms1DecResult.SignalNoiseRatio = BitConverter.ToSingle(buffer, 48);
         
            //Identification
            ms1DecResult.MspDbID = BitConverter.ToInt32(buffer, 52);
            ms1DecResult.RetentionTimeSimilarity = BitConverter.ToSingle(buffer, 56);
            ms1DecResult.RetentionIndexSimilarity = BitConverter.ToSingle(buffer, 60);
            ms1DecResult.EiSpectrumSimilarity = BitConverter.ToSingle(buffer, 64);
            ms1DecResult.DotProduct = BitConverter.ToSingle(buffer, 68);
            ms1DecResult.ReverseDotProduct = BitConverter.ToSingle(buffer, 72);
            ms1DecResult.PresencePersentage = BitConverter.ToSingle(buffer, 76);
            ms1DecResult.TotalSimilarity = BitConverter.ToSingle(buffer, 80);

            //Score
            ms1DecResult.AmplitudeScore = BitConverter.ToSingle(buffer, 84);
            ms1DecResult.ModelPeakPurity = BitConverter.ToSingle(buffer, 88);
            ms1DecResult.ModelPeakQuality = BitConverter.ToSingle(buffer, 92);

            //Spectrum num
            int spectraNumber = BitConverter.ToInt32(buffer, 96);
            int datapointNumber = BitConverter.ToInt32(buffer, 100);
            int modelMasses = BitConverter.ToInt32(buffer, 104);
            double maxIntensity = double.MinValue;

            buffer = new byte[spectraNumber * 12];
            fs.Read(buffer, 0, buffer.Length);
            for (int j = 0; j < spectraNumber; j++) {
                ms1DecResult.Spectrum.Add(new Peak {
                    Mz = BitConverter.ToSingle(buffer, 12 * j),
                    Intensity = BitConverter.ToSingle(buffer, 12 * j + 4),
                    PeakQuality = (PeakQuality)BitConverter.ToInt32(buffer, 12 * j + 8)
                });

                if (maxIntensity < ms1DecResult.Spectrum[j].Intensity) maxIntensity = ms1DecResult.Spectrum[j].Intensity;
            }

            var basePeaklist = new List<Peak>();
            buffer = new byte[datapointNumber * 16];
            fs.Read(buffer, 0, buffer.Length);

            for (int j = 0; j < datapointNumber; j++) {
                ms1DecResult.BasepeakChromatogram.Add(new Peak() {
                    ScanNumber = BitConverter.ToInt32(buffer, 16 * j),
                    RetentionTime = BitConverter.ToSingle(buffer, 16 * j + 4),
                    Mz = BitConverter.ToSingle(buffer, 16 * j + 8),
                    Intensity = BitConverter.ToSingle(buffer, 16 * j + 12)
                });
            }

            // reading model masses
            buffer = new byte[modelMasses * 4];
            fs.Read(buffer, 0, buffer.Length);

            for (int j = 0; j < modelMasses; j++) {
                ms1DecResult.ModelMasses.Add(BitConverter.ToSingle(buffer, 4 * j));
            }

            return ms1DecResult;
        }
    }
}
