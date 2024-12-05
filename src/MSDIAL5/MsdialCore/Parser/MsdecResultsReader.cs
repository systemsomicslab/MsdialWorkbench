using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialCore.Parser {
    public sealed class MsdecResultsReader {
		private static bool SHOW_WARNING = true;
		public static List<MSDecResult> ReadMSDecResults(string file, out int DCL_VERSION, out List<long> seekPoints) {
			using (var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                return ReadMSDecResults(fs, out DCL_VERSION, out seekPoints);
			}
		}

		public static List<MSDecResult> ReadMSDecResults(Stream stream, out int DCL_VERSION, out List<long> seekPoints) {
            var fs = stream;
            var buffer = new byte[7];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 7);

            string name = Encoding.ASCII.GetString(buffer, 0, 2);
            DCL_VERSION = BitConverter.ToInt32(buffer, 2);
            var isAnnotationInfoIncluded = BitConverter.ToBoolean(buffer, 6);

#if DEBUG
            Console.WriteLine("name: " + name);
            Console.WriteLine("version: " + DCL_VERSION);
            Console.WriteLine("annotation info: " + isAnnotationInfoIncluded);
#endif
            if (name.Equals("DC") && DCL_VERSION == 1) {
                //Console.WriteLine("Reading deconvolution file " + file + " (V." + DCL_VERSION + ")", "INFO");
                return MSDecReaderVer1(fs, isAnnotationInfoIncluded, out seekPoints);
            }
            else {
                seekPoints = null;
                Console.WriteLine("The deconvolution file/s is/are outdated. Please reprocess your data.");
                return null;
            }
		}

		public static void GetSeekPointers(string file, out int DCL_VERSION, out List<long> seekPoints, out bool isAnnotationInfoIncluded) {
			using (var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				var buffer = new byte[7];
				fs.Seek(0, SeekOrigin.Begin);
				fs.Read(buffer, 0, 7);

				string name = Encoding.ASCII.GetString(buffer, 0, 2);
				DCL_VERSION = BitConverter.ToInt32(buffer, 2);
                isAnnotationInfoIncluded = BitConverter.ToBoolean(buffer, 6);
#if DEBUG
                Console.WriteLine("name: " + name);
				Console.WriteLine("version: " + DCL_VERSION);
                Console.WriteLine("annotation info: " + isAnnotationInfoIncluded);
#endif
                seekPoints = new List<long>();
				buffer = new byte[4];
				fs.Read(buffer, 0, 4);

				var totalPeakNumber = BitConverter.ToInt32(buffer, 0);
				buffer = new byte[8 * totalPeakNumber];
				fs.Read(buffer, 0, buffer.Length);
				for (int i = 0; i < totalPeakNumber; i++)
					seekPoints.Add(BitConverter.ToInt64(buffer, 8 * i));
			}
		}

        public static void GetSeekPointers(Stream fs, out int DCL_VERSION, out List<long> seekPoints, out bool isAnnotationInfoIncluded) {
            var buffer = new byte[7];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 7);

            string name = Encoding.ASCII.GetString(buffer, 0, 2);
            DCL_VERSION = BitConverter.ToInt32(buffer, 2);
            isAnnotationInfoIncluded = BitConverter.ToBoolean(buffer, 6);

#if DEBUG
            Console.WriteLine("name: " + name);
            Console.WriteLine("version: " + DCL_VERSION);
            Console.WriteLine("annotation info: " + isAnnotationInfoIncluded);
#endif
            seekPoints = new List<long>();
            buffer = new byte[4];
            fs.Read(buffer, 0, 4);

            var totalPeakNumber = BitConverter.ToInt32(buffer, 0);
            buffer = new byte[8 * totalPeakNumber];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < totalPeakNumber; i++)
                seekPoints.Add(BitConverter.ToInt64(buffer, 8 * i));
        }

        public static MSDecResult ReadMSDecResult(Stream fs, long seekPoint, int version, bool isAnnotationInfoIncluded) {
            if (version == 1) {
                lock (fs) {
                    fs.Seek(seekPoint, SeekOrigin.Begin);
                    return ReadMSDecResultVer1(fs, isAnnotationInfoIncluded);
                }
            }
            else {
                return null;
            }
        }

        public static MSDecResult ReadMSDecResult(string file, long seekPoint) {
            using (var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                var buffer = new byte[7];
                fs.Seek(0, SeekOrigin.Begin);
                fs.Read(buffer, 0, 7);

                var name = Encoding.ASCII.GetString(buffer, 0, 2);
                var DCL_VERSION = BitConverter.ToInt32(buffer, 2);
                var isAnnotationInfoIncluded = BitConverter.ToBoolean(buffer, 6);

                if (DCL_VERSION == 1) {
                    fs.Seek(seekPoint, SeekOrigin.Begin);
                    return ReadMSDecResultVer1(fs, isAnnotationInfoIncluded);
                }
                else {
                    return null;
                }
            }
        }


        public static MSDecResult ReadMSDecResult(string file, long seekPoint, int version, bool isAnnotationInfoIncluded) {
            using (var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                if (version == 1) {
                    fs.Seek(seekPoint, SeekOrigin.Begin);
                    return ReadMSDecResultVer1(fs, isAnnotationInfoIncluded);
                }
                else {
                    return null;
                }
            }
        }

        // 7 bytes buffered already
        public static List<MSDecResult> MSDecReaderVer1(Stream fs, bool isAnnotationInfoIncluded, out List<long> seekPoints) {
            
            seekPoints = new List<long>();
            var buffer = new byte[4];

            lock (fs) {
                fs.Read(buffer, 0, 4);

                var totalPeakNumber = BitConverter.ToInt32(buffer, 0);
                buffer = new byte[8 * totalPeakNumber];
                fs.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < totalPeakNumber; i++)
                    seekPoints.Add(BitConverter.ToInt64(buffer, 8 * i));

                var results = new List<MSDecResult>();
                foreach (var item in seekPoints) {
                    var result = ReadMSDecResultVer1(fs, isAnnotationInfoIncluded);
                    results.Add(result);
                }
                return results;
            }
        }

        public static MSDecResult ReadMSDecResultVer1(Stream fs, bool isAnnotationInfoIncluded) {
            var result = new MSDecResult();
            //Scan
            var buffer = new byte[MsdecResultsWriter.GetSavedScanDataBytes(result)];
            fs.Read(buffer, 0, buffer.Length);

            result.SeekPoint = BitConverter.ToInt64(buffer, 0);
            result.ScanID = BitConverter.ToInt32(buffer, 8);
            result.RawSpectrumID = BitConverter.ToInt32(buffer, 12);
            result.PrecursorMz = BitConverter.ToDouble(buffer, 16);
            result.IonMode = (IonMode)BitConverter.ToInt32(buffer, 24);
            result.ChromXs = new ChromXs();
            result.ChromXs.RT = new RetentionTime(BitConverter.ToDouble(buffer, 28));
            result.ChromXs.RI = new RetentionIndex(BitConverter.ToDouble(buffer, 36));
            result.ChromXs.Drift = new DriftTime(BitConverter.ToDouble(buffer, 44));
            result.ChromXs.Mz = new MzValue(BitConverter.ToDouble(buffer, 52));


            //Quant info
            buffer = new byte[MsdecResultsWriter.GetSavedQuantDataBytes(result)];
            fs.Read(buffer, 0, buffer.Length);

            result.ModelPeakMz = BitConverter.ToDouble(buffer, 0);
            result.ModelPeakHeight = BitConverter.ToDouble(buffer, 8);
            result.ModelPeakArea = BitConverter.ToDouble(buffer, 16);
            result.IntegratedHeight = BitConverter.ToDouble(buffer, 24);
            result.IntegratedArea = BitConverter.ToDouble(buffer, 32);

           
            //Score
            buffer = new byte[MsdecResultsWriter.GetSavedScroingDataBytes(result)];
            fs.Read(buffer, 0, buffer.Length);

            result.AmplitudeScore = BitConverter.ToSingle(buffer, 0);
            result.ModelPeakPurity = BitConverter.ToSingle(buffer, 4);
            result.ModelPeakQuality = BitConverter.ToSingle(buffer, 8);
            result.SignalNoiseRatio = BitConverter.ToSingle(buffer, 12);
            result.EstimatedNoise = BitConverter.ToSingle(buffer, 16);


            //Spectrum num
            buffer = new byte[MsdecResultsWriter.GetSavedCountersBytes()];
            fs.Read(buffer, 0, buffer.Length);
            
            var spectraNumber = BitConverter.ToInt32(buffer, 0);
            var datapointNumber = BitConverter.ToInt32(buffer, 4);
            var modelMasses = BitConverter.ToInt32(buffer, 8);


            // reading spectra data
            var specStepSize = MsdecResultsWriter.GetSpectrumDataBytes();
            buffer = new byte[spectraNumber * specStepSize];
            fs.Read(buffer, 0, buffer.Length);
            for (int j = 0; j < spectraNumber; j++) {
                result.Spectrum.Add(new SpectrumPeak {
                    Mass = BitConverter.ToDouble(buffer, specStepSize * j),
                    Intensity = BitConverter.ToDouble(buffer, specStepSize * j + 8),
                    PeakQuality = (PeakQuality)BitConverter.ToInt32(buffer, specStepSize * j + 16)
                });
            }

            //reading base peaks
            var basePeaklist = new List<ChromatogramPeak>();
            var peakStepSize = MsdecResultsWriter.GetBasePeakDataBytes();
            buffer = new byte[datapointNumber * peakStepSize];
            fs.Read(buffer, 0, buffer.Length);

            for (int j = 0; j < datapointNumber; j++) {
                var id = BitConverter.ToInt32(buffer, peakStepSize * j);
                var chromXs = new ChromXs(BitConverter.ToDouble(buffer, peakStepSize * j + 4));
                var mass = BitConverter.ToDouble(buffer, peakStepSize * j + 12);
                var intensity = BitConverter.ToDouble(buffer, peakStepSize * j + 20);
                result.ModelPeakChromatogram.Add(new ChromatogramPeak(id, mass, intensity, chromXs));
            }

            // reading model masses
            var modelMassSize = MsdecResultsWriter.GetModelMassSize();
            buffer = new byte[modelMasses * modelMassSize];
            fs.Read(buffer, 0, buffer.Length);

            for (int j = 0; j < modelMasses; j++) {
                result.ModelMasses.Add(BitConverter.ToDouble(buffer, modelMassSize * j));
            }


            if (isAnnotationInfoIncluded) { // annotation
                var size = MsdecResultsWriter.GetSavedAnnotationDataBytes(result);
                buffer = new byte[size];
                fs.Read(buffer, 0, buffer.Length);

                result.MspID = BitConverter.ToInt32(buffer, 0);
                result.MspIDWhenOrdered = BitConverter.ToInt32(buffer, 4);

                result.MspBasedMatchResult = new MsScanMatchResult();
                var mResult = result.MspBasedMatchResult;

                mResult.TotalScore = BitConverter.ToSingle(buffer, 8);
                mResult.WeightedDotProduct = BitConverter.ToSingle(buffer, 12);
                mResult.SimpleDotProduct = BitConverter.ToSingle(buffer, 16);
                mResult.ReverseDotProduct = BitConverter.ToSingle(buffer, 20);
                mResult.MatchedPeaksCount = BitConverter.ToSingle(buffer, 24);
                mResult.MatchedPeaksPercentage = BitConverter.ToSingle(buffer, 28);
                mResult.EssentialFragmentMatchedScore = BitConverter.ToSingle(buffer, 32);
                mResult.RtSimilarity = BitConverter.ToSingle(buffer, 36);
                mResult.RiSimilarity = BitConverter.ToSingle(buffer, 40);
                mResult.CcsSimilarity = BitConverter.ToSingle(buffer, 44);
                mResult.IsotopeSimilarity = BitConverter.ToSingle(buffer, 48);
                mResult.AcurateMassSimilarity = BitConverter.ToSingle(buffer, 52);

                mResult.LibraryID = BitConverter.ToInt32(buffer, 56);
                mResult.LibraryIDWhenOrdered = BitConverter.ToInt32(buffer, 60);

                mResult.IsPrecursorMzMatch = BitConverter.ToBoolean(buffer, 64);
                mResult.IsSpectrumMatch = BitConverter.ToBoolean(buffer, 65);
                mResult.IsRtMatch = BitConverter.ToBoolean(buffer, 66);
                mResult.IsRiMatch = BitConverter.ToBoolean(buffer, 67);
                mResult.IsCcsMatch = BitConverter.ToBoolean(buffer, 68);
                mResult.IsLipidClassMatch = BitConverter.ToBoolean(buffer, 69);
                mResult.IsLipidChainsMatch = BitConverter.ToBoolean(buffer, 70);
                mResult.IsLipidPositionMatch = BitConverter.ToBoolean(buffer, 71);
                mResult.IsOtherLipidMatch = BitConverter.ToBoolean(buffer, 72);

                buffer = new byte[4];
                fs.Read(buffer, 0, buffer.Length);
                var idCount = BitConverter.ToInt32(buffer, 0);
                buffer = new byte[idCount * 4];
                fs.Read(buffer, 0, buffer.Length);
                for (int j = 0; j < idCount; j++) {
                    result.MspIDs.Add(BitConverter.ToInt32(buffer, 4 * j));
                }
            }

            return result;
        }
    }
}
