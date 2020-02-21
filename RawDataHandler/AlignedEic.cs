using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.RawData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.AlignedDataHandler {
    public class AlignedPeakSpotInfo {
        public List<double[]> PeakList { get; set; }
        public float TargetRt { get; set; }
        public float TargetLeftRt { get; set; }
        public float TargetRightRt { get; set; }
        public bool GapFilled { get; set; }
        public AlignedPeakSpotInfo() {
            PeakList = new List<double[]>();
            TargetRt = -1;
            TargetLeftRt = -1;
            TargetRightRt = -1;
            GapFilled = false;
        }
    }

    public class AlignedPeakSpotContainer {
        public List<AlignedPeakSpotInfo> AlignedPeakSpotInfoList { get; set; }
        public AlignedPeakSpotContainer() {
            AlignedPeakSpotInfoList = new List<AlignedPeakSpotInfo>();
        }
    }

    public class AlignedData {
        private AlignmentPropertyBean alignmentPropertyBean;
        private AnalysisParamOfMsdialGcms param;
        private AlignedDriftSpotPropertyBean driftSpotProp;

        public float Mz { get; set; }
        public float Rt { get; set; }
        public int NumAnalysisFiles { get; set; }
        public float MinRt { get; set; }
        public float MaxRt { get; set; }
        public List<AlignedPeakSpotInfo> PeakLists { get; set; }

        public AlignedData() { }

        public AlignedData(AlignmentPropertyBean alignedSpotProp) {
            this.Mz = alignedSpotProp.CentralAccurateMass;
            this.Rt = alignedSpotProp.CentralRetentionTime;
            this.MinRt = alignedSpotProp.MinRt - alignedSpotProp.AveragePeakWidth * 1.5F;
            this.MaxRt = alignedSpotProp.MaxRt + alignedSpotProp.AveragePeakWidth * 1.5F;
            if(MaxRt - MinRt > 5) {
                MinRt = Rt - 3;
                MaxRt = Rt + 3;
            }
        }

        public AlignedData(AlignedDriftSpotPropertyBean driftSpotProp) {
            this.Mz = driftSpotProp.CentralAccurateMass;
            this.Rt = driftSpotProp.CentralDriftTime;
            this.MinRt = driftSpotProp.MinDt - driftSpotProp.AveragePeakWidth * 1.5F;
            this.MaxRt = driftSpotProp.MaxDt + driftSpotProp.AveragePeakWidth * 1.5F;
            if (MaxRt - MinRt > 3) {
                MinRt = Rt - 3;
                MaxRt = Rt + 3;
            }
        }
    }

    public class AlignedEic {
        private static int versionNum = 3;

        public static void WriteAlignedEic(List<AlignedData> alignedEics, ProjectPropertyBean projectPropertyBean, List<int> newIdList, int numAnalysisFiles, string tmpDirPath, string FilePath) {
            var seekPointer = new List<long>();
            if (System.IO.File.Exists(FilePath))
                System.IO.File.Delete(FilePath);

            using (var fs = File.Open(FilePath, FileMode.Create, FileAccess.ReadWrite)) {
                //first header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes('V'), 0, 2);
                fs.Write(BitConverter.GetBytes(versionNum), 0, 4);

                //second header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes(newIdList.Count), 0, 4);

                //third header
                var buffer = new byte[newIdList.Count * 8];
                seekPointer.Add(fs.Position);
                fs.Write(buffer, 0, buffer.Length);

                //from forth header
                WriteAlignedEicRsult(fs, seekPointer, alignedEics, newIdList, numAnalysisFiles, tmpDirPath);

                //Finalize
                fs.Seek(seekPointer[2], SeekOrigin.Begin);
                for (int i = 3; i < seekPointer.Count; i++) { //Debug.Write(seekPointer[i] + " ");
                    fs.Write(BitConverter.GetBytes(seekPointer[i]), 0, 8);
                }
            }
        }

        public static void WriteAlignedEicRsult(FileStream fs, List<long> seekPointer, List<AlignedData> alignedEics, List<int> newIdList, int numAnalysisFiles, string tmpDirPath) {
            var fslist = new List<FileStream>();
            var seekpointerList = new List<List<long>>();
            for (int i = 0; i < numAnalysisFiles; i++) {
                var peakfs = File.Open(tmpDirPath + "\\peaklist_" + i + ".pll", FileMode.Open, FileAccess.ReadWrite);
                var seekpointer = ReadSeekPointsOfPeaklistlist(peakfs);
                fslist.Add(peakfs);
                seekpointerList.Add(seekpointer);
            }

            foreach (var i in newIdList) {
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes((float)alignedEics[i].Rt), 0, 4); // RT
                fs.Write(BitConverter.GetBytes((float)alignedEics[i].Mz), 0, 4); // mz
                fs.Write(BitConverter.GetBytes((int)numAnalysisFiles), 0, 4); // num analysis files

                //   Debug.WriteLine("Mz: " + alignedSpotInfo[i][0].ToString() + ", Rt: " + alignedSpotInfo[i][1].ToString() + ", NumScan: " + alignedSpotInfo[i][2].ToString() +
                //      ", StartScan: " + alignedSpotInfo[i][3].ToString() + ", EndScan" + alignedSpotInfo[i][4].ToString());

                for (int j = 0; j < numAnalysisFiles; j++) {
                    var alignedPeakSpotinfo = ReadPeaklistlist(fslist[j], seekpointerList[j], i);
                    fs.Write(BitConverter.GetBytes((int)alignedPeakSpotinfo.PeakList.Count), 0, 4); // num scan
                    //var check = alignedPeakSpotinfo.TargetRt > -1 ? true : false;
                    //fs.Write(BitConverter.GetBytes((bool)check), 0, 1);
                    //if (check) {
                    //    fs.Write(BitConverter.GetBytes((float)alignedPeakSpotinfo.TargetRt), 0, 4); // PeakTop
                    //    fs.Write(BitConverter.GetBytes((float)alignedPeakSpotinfo.TargetLeftRt), 0, 4); // PeakLeftEdge
                    //    fs.Write(BitConverter.GetBytes((float)alignedPeakSpotinfo.TargetRightRt), 0, 4); // PeakRightEdge
                    //}

                    fs.Write(BitConverter.GetBytes((bool)alignedPeakSpotinfo.GapFilled), 0, 1);
                    fs.Write(BitConverter.GetBytes((float)alignedPeakSpotinfo.TargetRt), 0, 4); // PeakTop
                    fs.Write(BitConverter.GetBytes((float)alignedPeakSpotinfo.TargetLeftRt), 0, 4); // PeakLeftEdge
                    fs.Write(BitConverter.GetBytes((float)alignedPeakSpotinfo.TargetRightRt), 0, 4); // PeakRightEdge

                    foreach (var peak in alignedPeakSpotinfo.PeakList) {
                        fs.Write(BitConverter.GetBytes((float)peak[0]), 0, 4); // Rt time
                        fs.Write(BitConverter.GetBytes((float)peak[1]), 0, 4); // Intensity
                    }
                }
            }
            for (int i = 0; i < numAnalysisFiles; i++) {
                fslist[i].Dispose(); fslist[i].Close();
                seekpointerList[i] = null;
            }
        }

        #region // old methods
        /*
        public static void WriteAlignedEicResultRaw(List<float[]> alignedSpotInfo, List<List<float[]>> alignedEics, ProjectPropertyBean projectPropertyBean, int numAnalysisFiles) {
            var seekPointer = new List<long>();
            var totalAlignedSpotNum = alignedSpotInfo.Count;
            var filepath = projectPropertyBean.ProjectFolderPath + "\\AlignedEics" + ".txt";
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                for (var i = 0; i < alignedSpotInfo.Count; i++) {
                    sw.WriteLine("Mz: " + alignedSpotInfo[i][0].ToString() + ", Rt: " + alignedSpotInfo[i][1].ToString() + ", NumScan: " + alignedSpotInfo[i][2].ToString() +
                        ", StartScan: " + alignedSpotInfo[i][3].ToString() + ", EndScan" + alignedSpotInfo[i][4].ToString());
                    for (var j = 0; j < numAnalysisFiles; j++) {
                        var count = alignedEics[j][i];
                        for (int k = 0; k < (int)(alignedSpotInfo[i][2] - 1); k++) {
                            sw.Write(alignedEics[j][i][k] + "\t");
                        }
                        sw.WriteLine(alignedEics[j][i][(int)alignedSpotInfo[i][2] - 1] + "\r\n");
                    }
                }
            }
        }
        

        public static float[] GetScanMs1(float targetRtStart, float targetRtEnd, ObservableCollection<RAW_Spectrum> spectrumCollection, int ms1LevelId, int experimentNumber) {
            RAW_Spectrum spectrum;
            float start = 0, end = spectrumCollection.Count - 1;
            float startRt = 0, endRt = 0;
            var scan = new float[] { start, end, startRt, endRt };
            var counter = ms1LevelId;
            while (counter < spectrumCollection.Count) {
                spectrum = spectrumCollection[counter];
                if (spectrum.ScanStartTime < targetRtStart) { counter += experimentNumber; continue; }
                if (spectrum.ScanStartTime > targetRtEnd) { end = counter - experimentNumber; endRt = (float)spectrumCollection[(int)end].ScanStartTime; return new float[] { start, end, startRt, endRt }; }
                if (start == 0) { start = counter; startRt = (float)spectrum.ScanStartTime; }
                counter += experimentNumber;
            }
            return scan;
        }

        public static List<float[]> GetMs1Int(ObservableCollection<RAW_Spectrum> spectrumCollection, float focusedMass, float ms1Tolerance, int startScan, int endScan, int ms1LevelId, int experimentNumber, int numArray, int smoothingLevel) {
            RAW_Spectrum spectrum;
            RAW_PeakElement[] massSpectra;
            double sum = 0;
            var result = new List<float[]>();
            var counter = startScan;
            var startIndex = 0;
            if (endScan > spectrumCollection.Count) endScan = spectrumCollection.Count;

            while (counter < endScan) {
                spectrum = spectrumCollection[counter];
                sum = 0;
                massSpectra = spectrum.Spectrum;
                startIndex = DataAccessLcUtility.GetMs1StartIndex(focusedMass, ms1Tolerance, massSpectra);

                for (int k = startIndex; k < massSpectra.Length; k++) {
                    if (massSpectra[k].Mz < focusedMass - ms1Tolerance) continue;
                    else if (focusedMass - ms1Tolerance <= massSpectra[k].Mz && massSpectra[k].Mz <= focusedMass + ms1Tolerance)
                        sum += massSpectra[k].Intensity;
                    else if (massSpectra[k].Mz > focusedMass + ms1Tolerance) break;
                }
                //                Debug.WriteLine(" id " + ((counter - startScan) / experimentNumber) + " " + numArray + " " + spectrum.ScanStartTime);
                if ((counter - startScan) / experimentNumber < numArray)
                    result.Add(new float[] { (float)spectrum.ScanStartTime, (float)sum });
                counter += experimentNumber;
            }
            return result;
        }
        */
        #endregion


        // reader
        public static AlignedData ReadAlignedEicResult(FileStream fs, List<long> seekpointList, int peakID) {
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);
            if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 1)
                return GetAlignedEicResultVer1(fs, seekpointList, peakID);
            else if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 3) {
                return GetAlignedEicResultVer2(fs, seekpointList, peakID);
            }
            else
                return GetAlignedEicResultVer1(fs, seekpointList, peakID);
        }

        private static AlignedData GetAlignedEicResultVer1(FileStream fs, List<long> seekpointList, int peakID) {
            AlignedData result = new AlignedData();

            fs.Seek(seekpointList[peakID], SeekOrigin.Begin); // go to the target seek point

            var buffer = new byte[12];
            fs.Read(buffer, 0, buffer.Length); // store the meta field buffer

            result.Rt = BitConverter.ToSingle(buffer, 0);
            result.Mz = BitConverter.ToSingle(buffer, 4);
            result.NumAnalysisFiles = BitConverter.ToInt32(buffer, 8);
            result.PeakLists = new List<AlignedPeakSpotInfo>();
            for (int i = 0; i < result.NumAnalysisFiles; i++) {
                result.PeakLists.Add(new AlignedPeakSpotInfo());
                buffer = new byte[5];
                fs.Read(buffer, 0, buffer.Length);
                var numScan = BitConverter.ToInt32(buffer, 0);

                var check = BitConverter.ToBoolean(buffer, 4);
                if (check) {
                    buffer = new byte[12];
                    fs.Read(buffer, 0, buffer.Length); // store the meta field buffer
                    result.PeakLists[i].TargetRt = BitConverter.ToSingle(buffer, 0);
                    result.PeakLists[i].TargetLeftRt = BitConverter.ToSingle(buffer, 4);
                    result.PeakLists[i].TargetRightRt = BitConverter.ToSingle(buffer, 8);
                    //                    Debug.WriteLine(result.PeakLists[i].TargetRt + "\t" + result.PeakLists[i].TargetLeftRt + "\t" + result.PeakLists[i].TargetRightRt);
                }

                //              Debug.WriteLine(numScan + "\t");
                var peaks = new List<double[]>();
                buffer = new byte[(int)numScan * 8];
                fs.Read(buffer, 0, buffer.Length);
                for (int j = 0; j < numScan; j++) {
                    var Rt = BitConverter.ToSingle(buffer, 8 * j);
                    var Int = BitConverter.ToSingle(buffer, 8 * j + 4);
                    peaks.Add(new double[] { Rt, Int });
                }
                result.PeakLists[i].PeakList = peaks;
            }
            return result;
        }

        private static AlignedData GetAlignedEicResultVer2(FileStream fs, List<long> seekpointList, int peakID) {
            AlignedData result = new AlignedData();

            fs.Seek(seekpointList[peakID], SeekOrigin.Begin); // go to the target seek point

            var buffer = new byte[12];
            fs.Read(buffer, 0, buffer.Length); // store the meta field buffer

            result.Rt = BitConverter.ToSingle(buffer, 0);
            result.Mz = BitConverter.ToSingle(buffer, 4);
            result.NumAnalysisFiles = BitConverter.ToInt32(buffer, 8);
            result.PeakLists = new List<AlignedPeakSpotInfo>();
            for (int i = 0; i < result.NumAnalysisFiles; i++) {
                result.PeakLists.Add(new AlignedPeakSpotInfo());
                buffer = new byte[5];
                fs.Read(buffer, 0, buffer.Length);
                var numScan = BitConverter.ToInt32(buffer, 0);

                result.PeakLists[i].GapFilled = BitConverter.ToBoolean(buffer, 4);

                buffer = new byte[12];
                fs.Read(buffer, 0, buffer.Length); // store the meta field buffer
                result.PeakLists[i].TargetRt = BitConverter.ToSingle(buffer, 0);
                result.PeakLists[i].TargetLeftRt = BitConverter.ToSingle(buffer, 4);
                result.PeakLists[i].TargetRightRt = BitConverter.ToSingle(buffer, 8);
                    //                    Debug.WriteLine(result.PeakLists[i].TargetRt + "\t" + result.PeakLists[i].TargetLeftRt + "\t" + result.PeakLists[i].TargetRightRt);

                //              Debug.WriteLine(numScan + "\t");
                var peaks = new List<double[]>();
                buffer = new byte[(int)numScan * 8];
                fs.Read(buffer, 0, buffer.Length);
                for (int j = 0; j < numScan; j++) {
                    var Rt = BitConverter.ToSingle(buffer, 8 * j);
                    var Int = BitConverter.ToSingle(buffer, 8 * j + 4);
                    peaks.Add(new double[] { Rt, Int });
                }
                result.PeakLists[i].PeakList = peaks;
            }
            return result;
        }

        public static List<long> ReadSeekPointsOfAlignedEic(FileStream fs) {
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);

            if (BitConverter.ToChar(buffer, 0).Equals('V') && (BitConverter.ToInt32(buffer, 2) == 1))
                return GetSeekpointListVer1(fs);
            else
                return GetSeekpointListVer1(fs);
        }

        private static List<long> GetSeekpointListVer1(FileStream fs) {
            var seekpointList = new List<long>();
            int totalPeakNumber;
            byte[] buffer = new byte[4];

            fs.Seek(6, SeekOrigin.Begin);
            fs.Read(buffer, 0, 4);

            totalPeakNumber = BitConverter.ToInt32(buffer, 0);
            buffer = new byte[8 * totalPeakNumber];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < totalPeakNumber; i++) {
                seekpointList.Add((long)BitConverter.ToInt64(buffer, 8 * i));
                //    Debug.Write(seekpointList[i] + " ");
            }
            return seekpointList;
        }



        // writer for peak info
        public static void WritePeakList(List<AlignedPeakSpotInfo> alignedPeakSpotInfoList, ProjectPropertyBean projectPropertyBean, string filename) {
            var seekPointer = new List<long>();
            var totalAlignedSpotNum = alignedPeakSpotInfoList.Count;

            var dt = projectPropertyBean.ProjectDate;
            if (System.IO.File.Exists(filename))
                System.IO.File.Delete(filename);

            using (var fs = File.Open(filename, FileMode.Create, FileAccess.ReadWrite)) {
                //first header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes('V'), 0, 2);
                fs.Write(BitConverter.GetBytes(versionNum), 0, 4);

                //second header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes(totalAlignedSpotNum), 0, 4);

                //third header
                var buffer = new byte[totalAlignedSpotNum * 8];
                seekPointer.Add(fs.Position);
                fs.Write(buffer, 0, buffer.Length);

                //from forth header
                WritePeakTmp(fs, seekPointer, alignedPeakSpotInfoList);

                //Finalize
                fs.Seek(seekPointer[2], SeekOrigin.Begin);
                for (int i = 3; i < seekPointer.Count; i++) { // Debug.Write(seekPointer[i] + " ");
                    fs.Write(BitConverter.GetBytes(seekPointer[i]), 0, 8);
                }
            }
        }

        public static void WritePeakTmp(FileStream fs, List<long> seekPointer, List<AlignedPeakSpotInfo> alignedPeakSpotInfoList) {
            for (int i = 0; i < alignedPeakSpotInfoList.Count; i++) {
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes((int)alignedPeakSpotInfoList[i].PeakList.Count), 0, 4);
                var check = alignedPeakSpotInfoList[i].TargetRt > -1 ? true : false;
                fs.Write(BitConverter.GetBytes((bool)alignedPeakSpotInfoList[i].GapFilled), 0, 1);
                fs.Write(BitConverter.GetBytes((float)alignedPeakSpotInfoList[i].TargetRt), 0, 4); // PeakTop
                fs.Write(BitConverter.GetBytes((float)alignedPeakSpotInfoList[i].TargetLeftRt), 0, 4); // PeakLeftEdge
                fs.Write(BitConverter.GetBytes((float)alignedPeakSpotInfoList[i].TargetRightRt), 0, 4); // PeakRightEdge

                //var check = alignedPeakSpotInfoList[i].TargetRt > -1 ? true : false;
                //fs.Write(BitConverter.GetBytes((bool)check), 0, 1);
                //if (check) {
                //    fs.Write(BitConverter.GetBytes((float)alignedPeakSpotInfoList[i].TargetRt), 0, 4); // PeakTop
                //    fs.Write(BitConverter.GetBytes((float)alignedPeakSpotInfoList[i].TargetLeftRt), 0, 4); // PeakLeftEdge
                //    fs.Write(BitConverter.GetBytes((float)alignedPeakSpotInfoList[i].TargetRightRt), 0, 4); // PeakRightEdge
                //}
                for (int k = 0; k < alignedPeakSpotInfoList[i].PeakList.Count; k++) {
                    fs.Write(BitConverter.GetBytes((float)alignedPeakSpotInfoList[i].PeakList[k][1]), 0, 4); // Rt time
                    fs.Write(BitConverter.GetBytes((float)alignedPeakSpotInfoList[i].PeakList[k][3]), 0, 4); // Intensity
                }
            }
        }

        // reader
        public static AlignedPeakSpotInfo ReadPeaklistlist(FileStream fs, List<long> seekpointList, int peakID) {
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);
            if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 2)
                return GetPeaklistlistVer2(fs, seekpointList, peakID);
            else if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 3) {
                return GetPeaklistlistVer3(fs, seekpointList, peakID);
            }
            else
                return null;
            //return GetPeaklistlistVer1(fs, seekpointList, peakID);
        }

        private static AlignedPeakSpotInfo GetPeaklistlistVer2(FileStream fs, List<long> seekpointList, int peakID) {
            var alignedPeakSpotInfo = new AlignedPeakSpotInfo();
            alignedPeakSpotInfo.PeakList = new List<double[]>();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin); // go to the target seek point
            var buffer = new byte[5];
            fs.Read(buffer, 0, buffer.Length); // store the meta field buffer
            var numPeaks = BitConverter.ToInt32(buffer, 0);
            var check = BitConverter.ToBoolean(buffer, 4);
            if (check) {
                buffer = new byte[12];
                fs.Read(buffer, 0, buffer.Length); // store the meta field buffer
                alignedPeakSpotInfo.TargetRt = BitConverter.ToSingle(buffer, 0);
                alignedPeakSpotInfo.TargetLeftRt = BitConverter.ToSingle(buffer, 4);
                alignedPeakSpotInfo.TargetRightRt = BitConverter.ToSingle(buffer, 8);
            }
            buffer = new byte[(int)(numPeaks * 8)];
            fs.Read(buffer, 0, buffer.Length);
            for (int j = 0; j < numPeaks; j++) {
                var Rt = BitConverter.ToSingle(buffer, 8 * j);
                var Int = BitConverter.ToSingle(buffer, 8 * j + 4);
                alignedPeakSpotInfo.PeakList.Add(new double[] { Rt, Int });
            }
            return alignedPeakSpotInfo;
        }

        private static AlignedPeakSpotInfo GetPeaklistlistVer3(FileStream fs, List<long> seekpointList, int peakID) {
            var alignedPeakSpotInfo = new AlignedPeakSpotInfo();
            alignedPeakSpotInfo.PeakList = new List<double[]>();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin); // go to the target seek point
            var buffer = new byte[5];
            fs.Read(buffer, 0, buffer.Length); // store the meta field buffer
            var numPeaks = BitConverter.ToInt32(buffer, 0);
            alignedPeakSpotInfo.GapFilled = BitConverter.ToBoolean(buffer, 4);

            buffer = new byte[12];
            fs.Read(buffer, 0, buffer.Length); // store the meta field buffer
            alignedPeakSpotInfo.TargetRt = BitConverter.ToSingle(buffer, 0);
            alignedPeakSpotInfo.TargetLeftRt = BitConverter.ToSingle(buffer, 4);
            alignedPeakSpotInfo.TargetRightRt = BitConverter.ToSingle(buffer, 8);

            buffer = new byte[(int)(numPeaks * 8)];
            fs.Read(buffer, 0, buffer.Length);
            for (int j = 0; j < numPeaks; j++) {
                var Rt = BitConverter.ToSingle(buffer, 8 * j);
                var Int = BitConverter.ToSingle(buffer, 8 * j + 4);
                alignedPeakSpotInfo.PeakList.Add(new double[] { Rt, Int });
            }
            return alignedPeakSpotInfo;
        }


        private static List<float[]> GetPeaklistlistVer1(FileStream fs, List<long> seekpointList, int peakID) {
            var peaks = new List<float[]>();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin); // go to the target seek point
            var buffer = new byte[4];
            fs.Read(buffer, 0, buffer.Length); // store the meta field buffer
            var numPeaks = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[(int)(numPeaks * 8)];
            fs.Read(buffer, 0, buffer.Length);
            for (int j = 0; j < numPeaks; j++) {
                var Rt = BitConverter.ToSingle(buffer, 8 * j);
                var Int = BitConverter.ToSingle(buffer, 8 * j + 4);
                peaks.Add(new float[] { Rt, Int });
            }
            return peaks;
        }

        public static List<long> ReadSeekPointsOfPeaklistlist(FileStream fs) {
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);

            if (BitConverter.ToChar(buffer, 0).Equals('V') && (BitConverter.ToInt32(buffer, 2) == 1))
                return GetSeekpointListForPeaklistVer1(fs);
            else
                return GetSeekpointListForPeaklistVer1(fs);
        }

        private static List<long> GetSeekpointListForPeaklistVer1(FileStream fs) {
            var seekpointList = new List<long>();
            int totalPeakNumber;
            byte[] buffer = new byte[4];

            fs.Seek(6, SeekOrigin.Begin);
            fs.Read(buffer, 0, 4);

            totalPeakNumber = BitConverter.ToInt32(buffer, 0);
            Debug.WriteLine("seekPoint: " + totalPeakNumber);
            buffer = new byte[8 * totalPeakNumber];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < totalPeakNumber; i++) {
                seekpointList.Add((long)BitConverter.ToInt64(buffer, 8 * i));
                //Debug.Write(seekpointList[i] + " ");
            }
            return seekpointList;
        }

    }
}
