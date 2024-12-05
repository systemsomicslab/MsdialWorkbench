using CompMs.Common.DataObj.Result;
using CompMs.Common.Utility;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialCore.Parser {
    public sealed class MsdecResultsWriter {

        public const int DCL_VERSION = 1;

        #region Writer
        public static void Write(string file, IReadOnlyList<MSDecResult> results, bool isAnnotationInfoIncluded = false) {
            using (var fs = File.Open(file, FileMode.Create, FileAccess.ReadWrite)) {
                var totalPeakNumber = results.Count;
                var seekPointer = new List<long>();

                WriteHeaders(fs, seekPointer, totalPeakNumber, isAnnotationInfoIncluded);
                for (int i = 0; i < results.Count; i++) {
                    var seekpoint = fs.Position;
                    seekPointer.Add(seekpoint);

                    results[i].SeekPoint = seekpoint;
                    MSDecWriterVer1(fs, results[i], isAnnotationInfoIncluded);
                }
                WriteSeekpointer(fs, seekPointer);
            }
        }

        public static void MSDecWriterVer1(Stream fs, MSDecResult msdecResult, bool isAnnotationInfoIncluded = false) {
            //Scan
            SaveScanData(fs, msdecResult);

            //Quant info
            SaveQuantData(fs, msdecResult);

            //Score
            SaveScoringData(fs, msdecResult);

            //Counters for variable lists of data
            SaveCounters(fs, msdecResult);

            //Spectral Data
            SaveSpectra(fs, msdecResult);

            //Base Peak Data
            SaveBasePeaks(fs, msdecResult);

            //Model Masses
            SaveModelMasses(fs, msdecResult);			//added in v.1

            //Information
            if (isAnnotationInfoIncluded)
                SaveAnnotationInfo(fs, msdecResult); // for gcms only
        }

        private static void SaveModelMasses(Stream fs, MSDecResult msdecResult) {
            msdecResult.ModelMasses.ForEach(mz => {
                fs.Write(BitConverter.GetBytes(mz), 0, ByteConvertion.ToByteCount(mz));
            });
        }

        public static int GetModelMassSize() {
            return 8;
        }

        private static void SaveBasePeaks(Stream fs, MSDecResult msdecResult) {
            for (int i = 0; i < msdecResult.ModelPeakChromatogram.Count; i++) {
                var peak = msdecResult.ModelPeakChromatogram[i];
                fs.Write(BitConverter.GetBytes(peak.ID), 0, ByteConvertion.ToByteCount(peak.ID));
                fs.Write(BitConverter.GetBytes(peak.ChromXs.Value), 0, ByteConvertion.ToByteCount(peak.ChromXs.Value));
                fs.Write(BitConverter.GetBytes(peak.Mass), 0, ByteConvertion.ToByteCount(peak.Mass));
                fs.Write(BitConverter.GetBytes(peak.Intensity), 0, ByteConvertion.ToByteCount(peak.Intensity));
            }
        }

        public static int GetBasePeakDataBytes() {
            return 28;
        }


        private static void SaveSpectra(Stream fs, MSDecResult msdecResult) {
            for (int i = 0; i < msdecResult.Spectrum.Count; i++) {
                var peak = msdecResult.Spectrum[i];
                fs.Write(BitConverter.GetBytes(peak.Mass), 0, ByteConvertion.ToByteCount(peak.Mass));
                fs.Write(BitConverter.GetBytes(peak.Intensity), 0, ByteConvertion.ToByteCount(peak.Intensity));
                fs.Write(BitConverter.GetBytes((int)peak.PeakQuality), 0, 4);
            }
        }

        public static int GetSpectrumDataBytes() {
            // return 12; // 4(float SpectrumPeak.Mass) + 4(float SpectrumPeak.Intensity) + 4(int SpectrumPeak.PeakQuality)
            return 20; // 8(double SpectrumPeak.Mass) + 8(double SpectrumPeak.Intensity) + 4(int SpectrumPeak.PeakQuality)
        }

        private static void SaveCounters(Stream fs, MSDecResult msdecResult) {
            //Spectrum
            fs.Write(BitConverter.GetBytes(msdecResult.Spectrum.Count), 0, 4);
            fs.Write(BitConverter.GetBytes(msdecResult.ModelPeakChromatogram.Count), 0, 4);
            fs.Write(BitConverter.GetBytes(msdecResult.ModelMasses.Count), 0, 4);
        }

        public static int GetSavedCountersBytes() {
            return 12;
        }

        private static void SaveScoringData(Stream fs, MSDecResult msdecResult) {
            fs.Write(BitConverter.GetBytes(msdecResult.AmplitudeScore), 0, ByteConvertion.ToByteCount(msdecResult.AmplitudeScore));
            fs.Write(BitConverter.GetBytes(msdecResult.ModelPeakPurity), 0, ByteConvertion.ToByteCount(msdecResult.ModelPeakPurity));
            fs.Write(BitConverter.GetBytes(msdecResult.ModelPeakQuality), 0, ByteConvertion.ToByteCount(msdecResult.ModelPeakQuality));
            fs.Write(BitConverter.GetBytes(msdecResult.SignalNoiseRatio), 0, ByteConvertion.ToByteCount(msdecResult.SignalNoiseRatio));
            fs.Write(BitConverter.GetBytes(msdecResult.EstimatedNoise), 0, ByteConvertion.ToByteCount(msdecResult.EstimatedNoise));
        }

        public static int GetSavedScroingDataBytes(MSDecResult obj) {
            var byteCount = ByteConvertion.ToByteCount(obj.AmplitudeScore)
                + ByteConvertion.ToByteCount(obj.ModelPeakPurity)
                + ByteConvertion.ToByteCount(obj.ModelPeakQuality)
                + ByteConvertion.ToByteCount(obj.SignalNoiseRatio)
                + ByteConvertion.ToByteCount(obj.EstimatedNoise);
            return byteCount;
        }

        private static void SaveScanData(Stream fs, MSDecResult msdecResult) {
            
            fs.Write(BitConverter.GetBytes(msdecResult.SeekPoint), 0, ByteConvertion.ToByteCount(msdecResult.SeekPoint));
            fs.Write(BitConverter.GetBytes(msdecResult.ScanID), 0, ByteConvertion.ToByteCount(msdecResult.ScanID));
            fs.Write(BitConverter.GetBytes(msdecResult.RawSpectrumID), 0, ByteConvertion.ToByteCount(msdecResult.RawSpectrumID));
            fs.Write(BitConverter.GetBytes(msdecResult.PrecursorMz), 0, ByteConvertion.ToByteCount(msdecResult.PrecursorMz));
            fs.Write(BitConverter.GetBytes((int)msdecResult.IonMode), 0, 4);

            // chromXs
            var chromXs = msdecResult.ChromXs;
            fs.Write(BitConverter.GetBytes(chromXs.RT.Value), 0, ByteConvertion.ToByteCount(chromXs.RT.Value)); // min
            fs.Write(BitConverter.GetBytes(chromXs.RI.Value), 0, ByteConvertion.ToByteCount(chromXs.RI.Value)); // no unit
            fs.Write(BitConverter.GetBytes(chromXs.Drift.Value), 0, ByteConvertion.ToByteCount(chromXs.Drift.Value)); // msec
            fs.Write(BitConverter.GetBytes(chromXs.Mz.Value), 0, ByteConvertion.ToByteCount(chromXs.Mz.Value)); // Da
        }

        public static int GetSavedScanDataBytes(MSDecResult obj) {
            var byteCount = ByteConvertion.ToByteCount(obj.SeekPoint)
                + ByteConvertion.ToByteCount(obj.ScanID)
                + ByteConvertion.ToByteCount(obj.RawSpectrumID)
                + ByteConvertion.ToByteCount(obj.PrecursorMz)
                + ByteConvertion.ToByteCount((int)obj.IonMode)
                + ByteConvertion.ToByteCount(obj.ChromXs.RT.Value)
                + ByteConvertion.ToByteCount(obj.ChromXs.RI.Value)
                + ByteConvertion.ToByteCount(obj.ChromXs.Drift.Value)
                + ByteConvertion.ToByteCount(obj.ChromXs.Mz.Value);
            return byteCount;
        }

        private static void SaveQuantData(Stream fs, MSDecResult msdecResult) {
            fs.Write(BitConverter.GetBytes(msdecResult.ModelPeakMz), 0, ByteConvertion.ToByteCount(msdecResult.ModelPeakMz));
            fs.Write(BitConverter.GetBytes(msdecResult.ModelPeakHeight), 0, ByteConvertion.ToByteCount(msdecResult.ModelPeakHeight));
            fs.Write(BitConverter.GetBytes(msdecResult.ModelPeakArea), 0, ByteConvertion.ToByteCount(msdecResult.ModelPeakArea));
            fs.Write(BitConverter.GetBytes(msdecResult.IntegratedHeight), 0, ByteConvertion.ToByteCount(msdecResult.IntegratedHeight));
            fs.Write(BitConverter.GetBytes(msdecResult.IntegratedArea), 0, ByteConvertion.ToByteCount(msdecResult.IntegratedArea));
        }

        public static int GetSavedQuantDataBytes(MSDecResult obj) {
            var byteCount = ByteConvertion.ToByteCount(obj.ModelPeakMz)
                + ByteConvertion.ToByteCount(obj.ModelPeakHeight)
                + ByteConvertion.ToByteCount(obj.ModelPeakArea)
                + ByteConvertion.ToByteCount(obj.IntegratedHeight)
                + ByteConvertion.ToByteCount(obj.IntegratedArea);
            return byteCount;
        }

        // for gcms project only
        private static void SaveAnnotationInfo(Stream fs, MSDecResult msdecResult) {
            fs.Write(BitConverter.GetBytes(msdecResult.MspID), 0, ByteConvertion.ToByteCount(msdecResult.MspID));
            fs.Write(BitConverter.GetBytes(msdecResult.MspIDWhenOrdered), 0, ByteConvertion.ToByteCount(msdecResult.MspIDWhenOrdered));

            var result = msdecResult.MspBasedMatchResult;

            fs.Write(BitConverter.GetBytes(result.TotalScore), 0, ByteConvertion.ToByteCount(result.TotalScore));
            fs.Write(BitConverter.GetBytes(result.WeightedDotProduct), 0, ByteConvertion.ToByteCount(result.WeightedDotProduct));
            fs.Write(BitConverter.GetBytes(result.SimpleDotProduct), 0, ByteConvertion.ToByteCount(result.SimpleDotProduct));
            fs.Write(BitConverter.GetBytes(result.ReverseDotProduct), 0, ByteConvertion.ToByteCount(result.ReverseDotProduct));
            fs.Write(BitConverter.GetBytes(result.MatchedPeaksCount), 0, ByteConvertion.ToByteCount(result.MatchedPeaksCount));
            fs.Write(BitConverter.GetBytes(result.MatchedPeaksPercentage), 0, ByteConvertion.ToByteCount(result.MatchedPeaksPercentage));
            fs.Write(BitConverter.GetBytes(result.EssentialFragmentMatchedScore), 0, ByteConvertion.ToByteCount(result.EssentialFragmentMatchedScore));
            fs.Write(BitConverter.GetBytes(result.RtSimilarity), 0, ByteConvertion.ToByteCount(result.RtSimilarity));
            fs.Write(BitConverter.GetBytes(result.RiSimilarity), 0, ByteConvertion.ToByteCount(result.RiSimilarity));
            fs.Write(BitConverter.GetBytes(result.CcsSimilarity), 0, ByteConvertion.ToByteCount(result.CcsSimilarity));
            fs.Write(BitConverter.GetBytes(result.IsotopeSimilarity), 0, ByteConvertion.ToByteCount(result.IsotopeSimilarity));
            fs.Write(BitConverter.GetBytes(result.AcurateMassSimilarity), 0, ByteConvertion.ToByteCount(result.AcurateMassSimilarity));
           
            fs.Write(BitConverter.GetBytes(result.LibraryID), 0, ByteConvertion.ToByteCount(result.LibraryID));
            fs.Write(BitConverter.GetBytes(result.LibraryIDWhenOrdered), 0, ByteConvertion.ToByteCount(result.LibraryIDWhenOrdered));
         
            fs.Write(BitConverter.GetBytes(result.IsPrecursorMzMatch), 0, ByteConvertion.ToByteCount(result.IsPrecursorMzMatch));
            fs.Write(BitConverter.GetBytes(result.IsSpectrumMatch), 0, ByteConvertion.ToByteCount(result.IsSpectrumMatch));
            fs.Write(BitConverter.GetBytes(result.IsRtMatch), 0, ByteConvertion.ToByteCount(result.IsRtMatch));
            fs.Write(BitConverter.GetBytes(result.IsRiMatch), 0, ByteConvertion.ToByteCount(result.IsRiMatch));
            fs.Write(BitConverter.GetBytes(result.IsCcsMatch), 0, ByteConvertion.ToByteCount(result.IsCcsMatch));
            fs.Write(BitConverter.GetBytes(result.IsLipidClassMatch), 0, ByteConvertion.ToByteCount(result.IsLipidClassMatch));
            fs.Write(BitConverter.GetBytes(result.IsLipidChainsMatch), 0, ByteConvertion.ToByteCount(result.IsLipidChainsMatch));
            fs.Write(BitConverter.GetBytes(result.IsLipidPositionMatch), 0, ByteConvertion.ToByteCount(result.IsLipidPositionMatch));
            fs.Write(BitConverter.GetBytes(result.IsOtherLipidMatch), 0, ByteConvertion.ToByteCount(result.IsOtherLipidMatch));

            fs.Write(BitConverter.GetBytes(msdecResult.MspIDs.Count), 0, 4);
            for (int i = 0; i < msdecResult.MspIDs.Count; i++) {
                fs.Write(BitConverter.GetBytes(msdecResult.MspIDs[i]), 0, ByteConvertion.ToByteCount(msdecResult.MspIDs[i]));
            }
        }

        public static int GetSavedAnnotationDataBytes(MSDecResult obj) {
            var mObj = new MsScanMatchResult();
            var byteCount = ByteConvertion.ToByteCount(obj.MspID)
                + ByteConvertion.ToByteCount(obj.MspIDWhenOrdered)
                + ByteConvertion.ToByteCount(mObj.TotalScore)
                + ByteConvertion.ToByteCount(mObj.WeightedDotProduct)
                + ByteConvertion.ToByteCount(mObj.SimpleDotProduct)
                + ByteConvertion.ToByteCount(mObj.ReverseDotProduct)
                + ByteConvertion.ToByteCount(mObj.MatchedPeaksCount)
                + ByteConvertion.ToByteCount(mObj.MatchedPeaksPercentage)
                + ByteConvertion.ToByteCount(mObj.EssentialFragmentMatchedScore)
                + ByteConvertion.ToByteCount(mObj.RtSimilarity)
                + ByteConvertion.ToByteCount(mObj.RiSimilarity)
                + ByteConvertion.ToByteCount(mObj.CcsSimilarity)
                + ByteConvertion.ToByteCount(mObj.IsotopeSimilarity)
                + ByteConvertion.ToByteCount(mObj.AcurateMassSimilarity)
                + ByteConvertion.ToByteCount(mObj.LibraryID)
                + ByteConvertion.ToByteCount(mObj.LibraryIDWhenOrdered)
                + ByteConvertion.ToByteCount(mObj.IsPrecursorMzMatch)
                + ByteConvertion.ToByteCount(mObj.IsSpectrumMatch)
                + ByteConvertion.ToByteCount(mObj.IsRtMatch)
                + ByteConvertion.ToByteCount(mObj.IsRiMatch)
                + ByteConvertion.ToByteCount(mObj.IsCcsMatch)
                + ByteConvertion.ToByteCount(mObj.IsLipidClassMatch)
                + ByteConvertion.ToByteCount(mObj.IsLipidChainsMatch)
                + ByteConvertion.ToByteCount(mObj.IsLipidPositionMatch)
                + ByteConvertion.ToByteCount(mObj.IsOtherLipidMatch);
            return byteCount;
        }


        public static void WriteHeaders(FileStream fs, List<long> seekPointer, int totalPeakNumber, bool isAnnotationInfoIncluded = false) {
#if DEBUG
            Console.WriteLine("Writing deconvolution file: " + fs.Name + " (Ver: DC" + DCL_VERSION, "INFO");
#endif
            //first header
            seekPointer.Add(fs.Position);
            fs.Write(Encoding.ASCII.GetBytes("DC"), 0, 2);
            fs.Write(BitConverter.GetBytes(DCL_VERSION), 0, 4);
            fs.Write(BitConverter.GetBytes(isAnnotationInfoIncluded), 0, 1);

            //second header
            seekPointer.Add(fs.Position);
            fs.Write(BitConverter.GetBytes(totalPeakNumber), 0, 4);

            //third header
            var buffer = new byte[totalPeakNumber * 8];
            seekPointer.Add(fs.Position);
            fs.Write(buffer, 0, buffer.Length);
        }

        public static void WriteSeekpointer(FileStream fs, List<long> seekPointer) {
            //Finalize
            fs.Seek(seekPointer[2], SeekOrigin.Begin);
            for (int i = 3; i < seekPointer.Count; i++)
                fs.Write(BitConverter.GetBytes(seekPointer[i]), 0, 8);
        }
#endregion
    }
}
