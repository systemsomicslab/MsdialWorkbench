using Microsoft.Data.Sqlite;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.RawData;
using Riken.Metabolomics.RawDataHandlerCommon.Parser;
using Riken.Metabolomics.RawDataHandlerCommon.Writer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Riken.Metabolomics.BrukerDataHandler.Parser
{
    public class TimsDataArray {
        public double[] IndexArray { get; set; }
        public double[] IntensityArray { get; set; }
    }

    public class Frame {
        public long Id { get; set; }
        public double Time { get; set; }
        public IonMode Polarity { get; set; }
        public int MsMsType { get; set; }
        public long TimsId { get; set; }
        public long NumScans { get; set; }
        public long NumPeaks { get; set; }
    }

    public class PasefFrameMsMsInfo {
        public long Frame { get; set; }
        public long ScanNumBegin { get; set; }
        public long ScanNumEnd { get; set; }
        public double IsolationMz { get; set; }
        public double IsolationWidth { get; set; }
        public double CollisionEnergy { get; set; }
        public long Precursor { get; set; }
    }

    public class Precursors {
        public long Id { get; set; }
        public double LargestPeakMz { get; set; }
        public int Charge { get; set; }
        public long ScanNumber { get; set; }
    }

    public class TimsTofDataReader
    {
        public string RawfilePath { get; private set; }
        public int SpectraCount { get; private set; }
        public List<RAW_SourceFileInfo> SourceFiles { get; private set; }
        public List<RAW_Sample> Samples { get; private set; }
        public DateTime StartTimeStamp { get; private set; }
        public List<RAW_Spectrum> SpectraList { get; private set; }
        public List<RAW_Spectrum> AccumulatedMs1SpectrumList { get; private set; }
        public List<RAW_Chromatogram> ChromatogramsList { get; private set; }
        public int Counter = 0;

        //public void ConvertToIABF(string filePath, int versionNum, 
        //    out string errorMessage, BackgroundWorker bgWorker = null) {
        //    var stopwatch = new Stopwatch();
        //    stopwatch.Start();
        //    errorMessage = string.Empty;

        //    var directory = System.IO.Path.GetDirectoryName(filePath);
        //    var filename = System.IO.Path.GetFileNameWithoutExtension(filePath);
        //    var filepathHeader = directory + "\\" + filename;
        //    var iabf_Path = filepathHeader + ".iabf"; // store versions and pointers (accumulated ms1 and all spectra)
        //    var measurment = ReadBrukerDotD(filePath, 0, bgWorker);
        //    var writer = new RawDataWriter();
        //    var error = string.Empty;
        //    writer.IabfWriter(measurment, iabf_Path, versionNum, out error, bgWorker);
        //    Console.WriteLine("Converting speed (sec): " + stopwatch.Elapsed.Seconds);
        //}

        public RAW_Measurement ReadBrukerDotD(string filePath, int fileID, double peakCutOff, out string errorMessage, BackgroundWorker bgWorker = null) {

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            this.SourceFiles = new List<RAW_SourceFileInfo>();
            this.Samples = new List<RAW_Sample>();
            this.StartTimeStamp = new DateTime();
            this.SpectraList = new List<RAW_Spectrum>();
            this.AccumulatedMs1SpectrumList = new List<RAW_Spectrum>();
            this.ChromatogramsList = null;

            errorMessage = string.Empty;

            var tdfPath = String.Join("\\", new string[] { filePath, "analysis.tdf" });
            uint use_recalibrated_state = 0; // use the most recent recalibrated state of the analysis, if there is one;

            try {
                var td1 = TimsData.tims_open(filePath, use_recalibrated_state);
                var td2 = TimsData.tims_open(filePath, use_recalibrated_state);
                var td3 = TimsData.tims_open(filePath, use_recalibrated_state);
                var td4 = TimsData.tims_open(filePath, use_recalibrated_state);
                //var td5 = TimsData.tims_open(filePath, use_recalibrated_state);
                //var td6 = TimsData.tims_open(filePath, use_recalibrated_state);
                var sqlConn = new SqliteConnectionStringBuilder { DataSource = tdfPath };

                using (var cn = new SqliteConnection(sqlConn.ToString())) {
                    cn.Open();
                    using (var cmd = cn.CreateCommand()) {

                        List<object[]> queries = null;
                        cmd.CommandText = String.Format("SELECT Id,Time,Polarity,MsMsType,TimsId,NumScans,NumPeaks FROM Frames");
                        queries = SQLiteExtension.GetQueries(cmd);
                        var frameQueries = getFrameQueries(queries);
                        if (frameQueries == null || frameQueries.Count == 0) {
                            TimsData.tims_close(td1);
                            return null;
                        }

                        cmd.CommandText = String.Format("SELECT Frame,ScanNumBegin,ScanNumEnd,IsolationMz,IsolationWidth,CollisionEnergy,Precursor FROM PasefFrameMsMsInfo");
                        queries = SQLiteExtension.GetQueries(cmd);
                        var pasefQueries = getPasefQueries(queries);
                        var frameToPasefs = getFrameToPrecursorsDictionary(pasefQueries);

                        cmd.CommandText = String.Format("SELECT Id,LargestPeakMz,Charge,ScanNumber FROM Precursors");
                        queries = SQLiteExtension.GetQueries(cmd);
                        var precursorQueries = getPrecursorQueries(queries);
                        var precursoridToPrecursors = getPrecursoridToPrecursors(precursorQueries);

                        //var num_scans = frameQueries[0].NumScans;
                        //var scanArray = getScanArray(num_scans);
                        //var driftArray = TimsData.ScanToOneOverK0(td1, frameQueries[0].Id, scanArray);

                        var syncObj = new object();
                        //ReadFrames(syncObj, td1, frameQueries, 0, frameQueries.Count - 1, frameToPasefs, precursoridToPrecursors, bgWorker);
                        var seg = 4;
                        var segment = (int)(frameQueries.Count / seg);
                        var td1StartID = 0;
                        var td2StartID = segment * 1;
                        var td3StartID = segment * 2;
                        var td4StartID = segment * 3;
                        var td5StartID = segment * 4;
                        var td6StartID = segment * 5;



                        Parallel.Invoke(
                            () => { ReadFrames(syncObj, td1, frameQueries, td1StartID, td2StartID - 1, frameToPasefs, precursoridToPrecursors, peakCutOff, bgWorker); },
                            () => { ReadFrames(syncObj, td2, frameQueries, td2StartID, td3StartID - 1, frameToPasefs, precursoridToPrecursors, peakCutOff, bgWorker); },
                            () => { ReadFrames(syncObj, td3, frameQueries, td3StartID, td4StartID - 1, frameToPasefs, precursoridToPrecursors, peakCutOff, bgWorker); },
                            () => { ReadFrames(syncObj, td4, frameQueries, td4StartID, frameQueries.Count - 1, frameToPasefs, precursoridToPrecursors, peakCutOff, bgWorker); }
                            //() => { ReadFrames(syncObj, td5, frameQueries, td5StartID, td6StartID - 1, frameToPasefs, precursoridToPrecursors, bgWorker); },
                            //() => { ReadFrames(syncObj, td6, frameQueries, td6StartID, frameQueries.Count - 1, frameToPasefs, precursoridToPrecursors, bgWorker); }
                            );

                    }
                }

                TimsData.tims_close(td1);
                TimsData.tims_close(td2);
                TimsData.tims_close(td3);
                TimsData.tims_close(td4);
                //TimsData.tims_close(td5);
                //TimsData.tims_close(td6);
            }
            catch (Exception e) {
                errorMessage = e.Message;
                Console.WriteLine(errorMessage);
            }

            if (this.SourceFiles.Count == 0) {
                this.SourceFiles.Add(new RAW_SourceFileInfo() { Id = fileID.ToString(), Location = filePath, Name = System.IO.Path.GetFileNameWithoutExtension(filePath) });
            }
            if (this.Samples.Count == 0) {
                this.Samples.Add(new RAW_Sample() { Id = fileID.ToString(), Name = System.IO.Path.GetFileNameWithoutExtension(filePath) });
            }

            if (this.SpectraList != null && this.SpectraList.Count > 0) {
                this.SpectraList = this.SpectraList.OrderBy(n => n.ScanNumber).ThenBy(n => n.DriftScanNumber).ToList();
                var counter = 0;
                foreach (var spec in this.SpectraList) {
                    spec.Index = counter;
                    spec.OriginalIndex = counter;
                    counter++;
                }
            }

            // finalize for storing accumulated spectra
            if (this.AccumulatedMs1SpectrumList != null && this.AccumulatedMs1SpectrumList.Count > 0) {
                this.AccumulatedMs1SpectrumList = this.AccumulatedMs1SpectrumList.OrderBy(n => n.ScanNumber).ToList();
                var tempSpeclist = new List<RAW_Spectrum>();
                var lastFrameID = -1;
                foreach (var spec in this.SpectraList.Where(n => n.MsLevel == 1)) {
                    if (spec.ScanNumber == lastFrameID) {

                    }
                    else {
                        tempSpeclist.Add(spec);
                        lastFrameID = spec.ScanNumber;
                    }
                }

                var counter = 0;
                foreach (var spec in this.AccumulatedMs1SpectrumList) {
                    spec.Index = counter;
                    counter++;
                    foreach (var tSpec in tempSpeclist) {
                        if (spec.ScanNumber == tSpec.ScanNumber) {
                            spec.OriginalIndex = tSpec.Index;
                            break;
                        }
                    }
                }
            }

            Debug.WriteLine("Retrieving speed (sec): " + stopwatch.Elapsed.Seconds);

            return new RAW_Measurement() {
                SourceFileInfo = this.SourceFiles[0],
                Sample = this.Samples[0],
                Method = MeasurmentMethod.IONMOBILITY,
                SpectrumList = this.SpectraList,
                AccumulatedSpectrumList = this.AccumulatedMs1SpectrumList,
                ChromatogramList = this.ChromatogramsList
            };
        }

        public void ReadFrames(object syncObj, ulong td, List<Frame> frameQueries,
           int frameBegin, int frameEnd, 
           Dictionary<long, List<PasefFrameMsMsInfo>> frameToPasefs,
           Dictionary<long, Precursors> precursoridToPrecursors, double peakCutOff, BackgroundWorker bgWorker = null)
        {

            for (int i = frameBegin; i <= frameEnd; i++) {

                var frame = frameQueries[i];
                List<TimsDataArray> timsDataArrays = null;
                var localRawSpectra = new List<RAW_Spectrum>();
                var num_scans = frameQueries[i].NumScans;
                var scanArray = getScanArray(num_scans);
                var driftArray = TimsData.ScanToOneOverK0(td, frameQueries[i].Id, scanArray);

                var accumulatedMassBin = new Dictionary<int, double[]>();
                //var accumulatedMassIntensityArray = new double[300000000];
                if (frame.MsMsType == 0) {
                    timsDataArrays = ReadScan(td, frame.Id, 0, (uint)num_scans);
                    if (timsDataArrays == null) continue;
                    for (int j = 0; j < timsDataArrays.Count; j++) {
                        var timsarray = timsDataArrays[j];
                        if (timsarray.IndexArray == null || timsarray.IndexArray.Length == 0) continue;

                        var indexes = timsarray.IndexArray;
                        var intValues = timsarray.IntensityArray;
                        var mzValues = TimsData.IndexToMz(td, frame.Id, indexes);

                        var spectrum = new RAW_Spectrum() {
                            ScanNumber = (int)frame.Id,
                            ScanStartTime = frame.Time / 60.0,
                            ScanStartTimeUnit = Units.Minute,
                            MsLevel = 1,
                            ScanPolarity = frame.Polarity == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative,
                            Precursor = null,
                            DriftScanNumber = j,
                            DriftTime = driftArray[j],
                            DriftTimeUnit = Units.Oneoverk0
                        };
                        //SpectrumParser.setSpectrumProperties(spectrum, mzValues, intValues, peakCutOff, ref accumulatedMassIntensityArray);
                        SpectrumParser.setSpectrumProperties(spectrum, mzValues, intValues, peakCutOff, accumulatedMassBin);
                        if (spectrum.BasePeakIntensity > 0 && spectrum.Spectrum.Length > 0) {
                            localRawSpectra.Add(spectrum);
                        }
                    }
                }
                else {
                    var pasefs = frameToPasefs[frame.Id];
                    foreach (var pasef in pasefs) {
                        var preID = pasef.Precursor;
                        var precursor = precursoridToPrecursors[preID];

                        var precursorMz = precursor.LargestPeakMz;
                        var charge = precursor.Charge;
                        var scanNumber = precursor.ScanNumber;
                        var scanBegin = pasef.ScanNumBegin;
                        var scanEnd = pasef.ScanNumEnd;

                        //timsDataArrays = ReadScan(td, frame.Id, (uint)scanNumber, (uint)(scanNumber + 1));
                        timsDataArrays = ReadScan(td, frame.Id, (uint)scanBegin, (uint)(scanEnd + 1));
                        if (timsDataArrays == null) continue;
                        //var aSpectrum = new double[300000000];
                        var aSpectrumMassBin = new Dictionary<int, double[]>();
                        for (int j = 0; j < timsDataArrays.Count; j++) {

                            var timsarray = timsDataArrays[j];
                            if (timsarray.IndexArray == null || timsarray.IndexArray.Length == 0) continue;
                            var indexes = timsarray.IndexArray;
                            var intValues = timsarray.IntensityArray;
                            var mzValues = TimsData.IndexToMz(td, frame.Id, indexes);
                            for (int k = 0; k < mzValues.Length; k++) {
                                SpectrumParser.AddToMassBinDictionary(aSpectrumMassBin, mzValues[k], intValues[k]);
                                //aSpectrum[(int)(mzValues[k] * 100000)] += intValues[k];
                            }

                            //var spectrum = new RAW_Spectrum() {
                            //    ScanNumber = (int)frame.Id,
                            //    ScanStartTime = frame.Time / 60.0,
                            //    ScanStartTimeUnit = Units.Minute,
                            //    MsLevel = 2,
                            //    ScanPolarity = frame.Polarity == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative,
                            //    Precursor = new RAW_PrecursorIon() {
                            //         CollisionEnergy = pasef.CollisionEnergy,
                            //          CollisionEnergyUnit = Units.ElectronVolt,
                            //           Dissociationmethod = DissociationMethods.CID, 
                            //            IsolationTargetMz = precursor.LargestPeakMz,
                            //             IsolationWindowLowerOffset = pasef.IsolationMz - pasef.IsolationWidth * 0.5,
                            //              IsolationWindowUpperOffset = pasef.IsolationMz + pasef.IsolationWidth * 0.5,
                            //               SelectedIonMz = precursor.LargestPeakMz
                            //    },
                            //    DriftScanNumber = (int)scanNumber,
                            //    DriftTime = driftArray[scanNumber],
                            //    DriftTimeUnit = Units.Oneoverk0
                            //};
                            //setSpectrumProperties(spectrum, mzValues, intValues, ref accumulatedMassIntensityArray);
                            //localRawSpectra.Add(spectrum);
                        }

                        var spectrum = new RAW_Spectrum() {
                            ScanNumber = (int)frame.Id,
                            ScanStartTime = frame.Time / 60.0,
                            ScanStartTimeUnit = Units.Minute,
                            MsLevel = 2,
                            ScanPolarity = frame.Polarity == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative,
                            Precursor = new RAW_PrecursorIon() {
                                CollisionEnergy = pasef.CollisionEnergy,
                                CollisionEnergyUnit = Units.ElectronVolt,
                                Dissociationmethod = DissociationMethods.CID,
                                IsolationTargetMz = precursor.LargestPeakMz,
                                IsolationWindowLowerOffset = pasef.IsolationMz - pasef.IsolationWidth * 0.5,
                                IsolationWindowUpperOffset = pasef.IsolationMz + pasef.IsolationWidth * 0.5,
                                SelectedIonMz = precursor.LargestPeakMz
                            },
                            DriftScanNumber = (int)scanNumber,
                            DriftTime = driftArray[scanNumber],
                            DriftTimeUnit = Units.Oneoverk0
                        };

                        //SpectrumParser.setSpectrumProperties(spectrum, aSpectrum);
                        SpectrumParser.setSpectrumProperties(spectrum, aSpectrumMassBin);
                        localRawSpectra.Add(spectrum);
                    }
                }
             
                lock (syncObj) {
                    foreach (var spec in localRawSpectra) {
                        this.SpectraList.Add(spec);
                    }

                    if (frame.MsMsType == 0) {

                        var spec = new RAW_Spectrum() {
                            ScanNumber = (int)frame.Id,
                            ScanStartTime = frame.Time / 60.0,
                            ScanStartTimeUnit = Units.Minute,
                            MsLevel = 1,
                            ScanPolarity = frame.Polarity == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative,
                            Precursor = null,
                            DriftScanNumber = 0,
                            DriftTime = driftArray[0],
                            DriftTimeUnit = Units.Oneoverk0
                        };

                        //SpectrumParser.setSpectrumProperties(spec, accumulatedMassIntensityArray);
                        SpectrumParser.setSpectrumProperties(spec, accumulatedMassBin);
                        this.AccumulatedMs1SpectrumList.Add(spec);
                    }
                    this.Counter++;
                    
                    if (bgWorker != null)
                        progressReports(this.Counter, frameQueries.Count, bgWorker);
                    else {
                        if (!Console.IsOutputRedirected) {
                            Console.Write("{0} / {1}", this.Counter, frameQueries.Count);
                            Console.SetCursorPosition(0, Console.CursorTop);
                        }
                        else {
                            Console.WriteLine("{0} / {1}", this.Counter, frameQueries.Count);
                        }
                    }
                }
            }
        }

        private void progressReports(int currentProgress, int maxProgress, BackgroundWorker bgWorker) {
            var progress = (double)currentProgress / (double)maxProgress * 100.0;
            if (bgWorker != null)
                bgWorker.ReportProgress((int)progress);
        }

        private Dictionary<long, Precursors> getPrecursoridToPrecursors(List<Precursors> precursorQueries) {
            if (precursorQueries == null || precursorQueries.Count == 0) return new Dictionary<long, Precursors>();

            var idToPrecursors = new Dictionary<long, Precursors>();
            foreach (var query in precursorQueries) {
                idToPrecursors[query.Id] = query;
            }

            return idToPrecursors;
        }

        //[0]Id[1]LargestPeakMz[2]Charge[3]ScanNumber
        private List<Precursors> getPrecursorQueries(List<object[]> queries) {
            var pQueries = new List<Precursors>();
            if (queries == null || queries.Count == 0) return pQueries;

            long longValue;
            double doubleValue;
            int intValue;

            foreach (var query in queries) {

                if (query.Length != 4) continue;

                var precursor = new Precursors();
                if (long.TryParse(query[0].ToString(), out longValue)) {
                    precursor.Id = longValue;
                }
                if (double.TryParse(query[1].ToString(), out doubleValue)) {
                    precursor.LargestPeakMz = doubleValue;
                }

                if (int.TryParse(query[2].ToString(), out intValue)) {
                    precursor.Charge = intValue;
                }
                if (double.TryParse(query[3].ToString(), out doubleValue)) {
                    precursor.ScanNumber = (long)doubleValue;
                }

                pQueries.Add(precursor);
            }

            return pQueries;
        }

        //[0]Frame[1]ScanNumBegin[2]ScanNumEnd[3]IsolationMz[4]IsolationWidth[5]CollisionEnergy[6]Precursor
        private List<PasefFrameMsMsInfo> getPasefQueries(List<object[]> queries) {
            var pasefs = new List<PasefFrameMsMsInfo>();
            if (queries == null || queries.Count == 0) return pasefs;

            long longValue;
            double doubleValue;

            foreach (var query in queries) {

                if (query.Length != 7) continue;

                var pasef = new PasefFrameMsMsInfo();
                if (long.TryParse(query[0].ToString(), out longValue)) {
                    pasef.Frame = longValue;
                }

                if (long.TryParse(query[1].ToString(), out longValue)) {
                    pasef.ScanNumBegin = longValue;
                }

                if (long.TryParse(query[2].ToString(), out longValue)) {
                    pasef.ScanNumEnd = longValue;
                }

                if (double.TryParse(query[3].ToString(), out doubleValue)) {
                    pasef.IsolationMz = doubleValue;
                }

                if (double.TryParse(query[4].ToString(), out doubleValue)) {
                    pasef.IsolationWidth = doubleValue;
                }

                if (double.TryParse(query[5].ToString(), out doubleValue)) {
                    pasef.CollisionEnergy = doubleValue;
                }

                if (long.TryParse(query[6].ToString(), out longValue)) {
                    pasef.Precursor = longValue;
                }

                pasefs.Add(pasef);
            }

            return pasefs;
        }

        // [0]Id[1]Time[2]Polarity[3]MsMsType[4]TimsId[5]NumScans[6]NumPeaks
        private List<Frame> getFrameQueries(List<object[]> queries) {
            var frames = new List<Frame>();
            if (queries == null || queries.Count == 0) return frames;
            long longValue;
            double doubleValue;
            int intValue;

            foreach (var query in queries) {

                if (query.Length != 7) continue;
                
                var frame = new Frame();
                if (long.TryParse(query[0].ToString(), out longValue)) {
                    frame.Id = longValue;
                }
                if (double.TryParse(query[1].ToString(), out doubleValue)) {
                    frame.Time = doubleValue;
                }

                frame.Polarity = query[2].ToString() == "+" ? IonMode.Positive : IonMode.Negative;

                if (int.TryParse(query[3].ToString(), out intValue)) {
                    frame.MsMsType = intValue;
                }
                if (long.TryParse(query[4].ToString(), out longValue)) {
                    frame.TimsId = longValue;
                }
                if (long.TryParse(query[5].ToString(), out longValue)) {
                    frame.NumScans = longValue;
                }
                if (long.TryParse(query[5].ToString(), out longValue)) {
                    frame.NumPeaks = longValue;
                }

                frames.Add(frame);
            }

            return frames;
        }

        private Dictionary<long, List<PasefFrameMsMsInfo>> getFrameToPrecursorsDictionary(List<PasefFrameMsMsInfo> pasefQueries) {
            if (pasefQueries == null || pasefQueries.Count == 0) return new Dictionary<long, List<PasefFrameMsMsInfo>>();

            var frameToPasefs = new Dictionary<long, List<PasefFrameMsMsInfo>>();
            foreach (var query in pasefQueries) {
                if (frameToPasefs.ContainsKey(query.Frame))
                    frameToPasefs[query.Frame].Add(query);
                else {
                    frameToPasefs[query.Frame] = new List<PasefFrameMsMsInfo>() {
                        query
                    };
                }
            }
            return frameToPasefs;
        }

        private double[] getScanArray(long num_scans) {
            var scanArray = new double[num_scans];
            for (int i = 0; i < num_scans; i++) {
                scanArray[i] = i;
            }
            return scanArray;
        }

        public List<TimsDataArray> ReadScan(ulong handle, long frameID, uint scanBegin, uint scanEnd) {
            uint initial_frame_buffer_size = 128;
            uint cnt = initial_frame_buffer_size; 
            uint required_len = 128;
            uint len = 128;

            int size = Marshal.SizeOf(typeof(UInt32)) * (int)cnt;
            var buffer = new uint[initial_frame_buffer_size];

            while (true) {
                cnt = initial_frame_buffer_size; 
                buffer = new uint[initial_frame_buffer_size];
                len = (uint)(4 * cnt);
                //Console.WriteLine(cnt + "\t" + len);
                required_len = TimsData.tims_read_scans_v2(handle, frameID,
                    scanBegin, scanEnd, buffer, len);
                if (required_len == 0) {
                    Console.WriteLine("required_len == 0");
                }
                if (required_len > len) {
                    if (required_len > 16777216) {
                        //arbitrary limit for now...
                        Console.WriteLine("required_len > 16777216");
                    }
                    else {
                        initial_frame_buffer_size = required_len / 4 + 1; // # grow buffer
                    }
                }
                else {
                    break;
                }
            }
            if (required_len == 0) {
                return null;
            }

            var timsDataArrays = new List<TimsDataArray>();
            var scan_begin = (int)scanBegin;
            var scan_end = (int)scanEnd;
            var d = scan_end - scan_begin;
            for (int i = scan_begin; i < scan_end; i++) {

                var timsDataArray = new TimsDataArray();
                var npeaks = (int)buffer[i - scan_begin];
                //Console.WriteLine(npeaks);
                var indices = new double[npeaks];
                for (int j = d; j < d + npeaks; j++)
                    indices[j - d] = buffer[j];
                d += npeaks;

                var intensities = new double[npeaks];
                for (int j = d; j < d + npeaks; j++)
                    intensities[j - d] = buffer[j];

                d += npeaks;

                timsDataArray.IndexArray = indices;
                timsDataArray.IntensityArray = intensities;
                timsDataArrays.Add(timsDataArray);
            }
            return timsDataArrays;
        }

        T[] IntPtrToArray<T>(IntPtr ptr, T[] array) {
            for (int i = 0; i < array.Length; i++) {
                array[i] = (T)Marshal.PtrToStructure(ptr, typeof(T));
                ptr = (IntPtr)((int)ptr + Marshal.SizeOf(typeof(T)));
            }
            return array;
        }
    }

    public static class SQLiteExtension {
        public static int InsertDenco(this SqliteCommand command, int no, string name, string type, string attr,
            int maxap, int maxhp, string skill = null) {
            var skillstr = skill == null ? "null" : $"'{skill}'";
            command.CommandText = "INSERT INTO denco(no, name, type, attribute, maxap, maxhp, skill) VALUES(" +
                $"{no}, '{name}', '{type}', '{attr}', {maxap}, {maxhp}, {skillstr})";
            return command.ExecuteNonQuery();
        }

        public static void DumpQuery(this SqliteDataReader reader) {
            var i = 0;
            var sb = new StringBuilder();
            while (reader.Read()) {
                if (i == 0) {
                    object[] objectarray = null;
                    var returnValue = reader.GetValues(objectarray);
                    Console.WriteLine(string.Join("\t", objectarray.ToString()));
                    Console.WriteLine(new string('=', 8 * reader.FieldCount));
                }

                Console.WriteLine(string.Join("\t", Enumerable.Range(0, reader.FieldCount).Select(x => reader.GetValue(x))));
                i++;
            }
            // return sb.ToString();
        }

        public static List<object[]> GetQuery(this SqliteDataReader reader) {
            var queries = new List<object[]>();
            while (reader.Read()) {
                var objects = new object[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++) {
                    objects[i] = reader.GetValue(i);
                }
                queries.Add(objects);
            }
            return queries;
        }

        //cmd.text should be set.
        public static List<object[]> GetQueries(SqliteCommand cmd) {
            var queries = new List<object[]>();
            //var exists = cmd.ExecuteScalar();
            //Console.WriteLine(exists);
            try {
                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        var objects = new object[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++) {
                            objects[i] = reader.GetValue(i);
                        }
                        queries.Add(objects);
                    }
                }
            } catch (Microsoft.Data.Sqlite.SqliteException ex) {
                Console.WriteLine(ex.Message);
            }
            
            return queries;
        }
    }
}
