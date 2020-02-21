using Riken.Metabolomics.RawData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.RawDataHandlerCommon.Reader {
    public class RawDataReader {

        public string RawfilePath { get; private set; }
        public int SpectraCount { get; private set; }
        public List<RAW_SourceFileInfo> SourceFiles { get; private set; }
        public List<RAW_Sample> Samples { get; private set; }
        public DateTime StartTimeStamp { get; private set; }
        public List<RAW_Spectrum> SpectraList { get; private set; }
        public List<RAW_Spectrum> AccumulatedMs1SpectrumList { get; private set; }
        public List<RAW_Chromatogram> ChromatogramsList { get; private set; }
        public int Counter = 0;

        public RAW_Measurement ReadIABF(string filePath, int fileID, out string errorMessage, BackgroundWorker bgWorker = null) {

            this.RawfilePath = filePath;
            this.SourceFiles = new List<RAW_SourceFileInfo>() {
                new RAW_SourceFileInfo() { Id = fileID.ToString(), Location = filePath, Name = System.IO.Path.GetFileNameWithoutExtension(filePath) }
            };
            this.Samples = new List<RAW_Sample>() {
                new RAW_Sample() { Id = fileID.ToString(), Name = System.IO.Path.GetFileNameWithoutExtension(filePath) }
            };
            this.StartTimeStamp = new DateTime();

            errorMessage = string.Empty;
            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite)) {
                var seekpointList = ReadSeekPoints(fs);
                var buffer = new byte[6];
                fs.Seek(0, SeekOrigin.Begin);
                fs.Read(buffer, 0, 6);

                var version = BitConverter.ToInt32(buffer, 2);
                if (version == 1) {
                    return ReadIABF(fs, seekpointList);
                }
                else if (version == 2) {
                    return ReadIabfVS2(fs, seekpointList);
                }
                else {
                    errorMessage = "Data reader error in IBF data";
                    return null;
                }
            }
        }

        public Raw_CalibrationInfo ReadCalibrationInformation(string filePath, int fileID, out string errorMessage) {
            errorMessage = string.Empty;
            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite)) {
                var seekpointList = ReadSeekPoints(fs);
                var buffer = new byte[6];
                fs.Seek(0, SeekOrigin.Begin);
                fs.Read(buffer, 0, 6);

                var version = BitConverter.ToInt32(buffer, 2);
                if (version == 2) {
                    return ReadCalibrationInformationVs2(fs, seekpointList);
                }
                else {
                    errorMessage = "Data reader error in IBF data";
                    return null;
                }
            }
        }

        private Raw_CalibrationInfo ReadCalibrationInformationVs2(FileStream fs, List<long> seekpointList) {
            
            var buffer = new byte[4];
            fs.Seek(6, SeekOrigin.Begin);
            fs.Read(buffer, 0, 4);
            var accumulatedMs1DatapointCount = BitConverter.ToInt32(buffer, 0);

            fs.Read(buffer, 0, 4);
            var allSpectraCount = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[accumulatedMs1DatapointCount * 8];
            fs.Read(buffer, 0, buffer.Length);

            buffer = new byte[allSpectraCount * 8];
            fs.Read(buffer, 0, buffer.Length);

            MeasurmentMethod methodinfo = MeasurmentMethod.DDA;
            Raw_CalibrationInfo calinfo = new Raw_CalibrationInfo();
            readIbfMetaData(fs, out methodinfo, out calinfo);

            return calinfo;
        }

        private RAW_Measurement ReadIabfVS2(FileStream fs, List<long> seekpointList) {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var buffer = new byte[4];
            fs.Seek(6, SeekOrigin.Begin);
            fs.Read(buffer, 0, 4);
            var accumulatedMs1DatapointCount = BitConverter.ToInt32(buffer, 0);

            fs.Read(buffer, 0, 4);
            var allSpectraCount = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[accumulatedMs1DatapointCount * 8];
            fs.Read(buffer, 0, buffer.Length);

            buffer = new byte[allSpectraCount * 8];
            fs.Read(buffer, 0, buffer.Length);

            MeasurmentMethod methodinfo =  MeasurmentMethod.DDA;
            Raw_CalibrationInfo calinfo = new Raw_CalibrationInfo();
            readIbfMetaData(fs, out methodinfo, out calinfo);

            fs.Seek(seekpointList[0], SeekOrigin.Begin); // to the start point of spectrum object
            var accumulatedMs1Spectra = new List<RAW_Spectrum>();
            for (int i = 0; i < accumulatedMs1DatapointCount; i++) {
                var spec = readSpectrumVer1(fs);
                spec.ScanNumber = i;
                spec.Index = i;

                //if (i > 100 && spec.ScanStartTime <= 0.1) break;
                //Console.WriteLine("scan id {0}, rt {1}, spec {2}", spec.ScanNumber, spec.ScanStartTime, spec.Spectrum.Length);
                accumulatedMs1Spectra.Add(spec);
            }

            var allSpectra = new List<RAW_Spectrum>();
            for (int i = 0; i < allSpectraCount; i++) {
                var spec = readSpectrumVer1(fs);
                //spec.ScanNumber = i;
                //Console.WriteLine("scan id {0}, drift id {1}, spec {2}", spec.ScanNumber, spec.DriftScanNumber, spec.Spectrum.Length);
                spec.Index = i;

                // if (i > 100 && spec.ScanStartTime <= 0.1) break;
                allSpectra.Add(spec);
            }

            Console.WriteLine("Reading speed (sec): " + stopwatch.Elapsed.Seconds);

            return new RAW_Measurement() {
                SourceFileInfo = this.SourceFiles[0],
                Sample = this.Samples[0],
                Method = methodinfo,
                CalibrantInfo = calinfo,
                SpectrumList = allSpectra,
                AccumulatedSpectrumList = accumulatedMs1Spectra,
                ChromatogramList = null,
            };
        }

        private void readIbfMetaData(FileStream fs, out MeasurmentMethod methodinfo, out Raw_CalibrationInfo calinfo) {

            methodinfo = MeasurmentMethod.DDA;
            calinfo = new Raw_CalibrationInfo();

            var buffer = new byte[4];
            fs.Read(buffer, 0, 4);
            Console.WriteLine(BitConverter.ToInt32(buffer, 0));
            methodinfo = (MeasurmentMethod)Enum.ToObject(typeof(MeasurmentMethod), BitConverter.ToInt32(buffer, 0));

            buffer = new byte[3];
            fs.Read(buffer, 0, 3);
            calinfo.IsAgilentIM = BitConverter.ToBoolean(buffer, 0);
            calinfo.IsWatersIM = BitConverter.ToBoolean(buffer, 1);
            calinfo.IsBrukerIM = BitConverter.ToBoolean(buffer, 2);

            buffer = new byte[20];
            fs.Read(buffer, 0, 20);
            calinfo.AgilentBeta = BitConverter.ToSingle(buffer, 0);
            calinfo.AgilentTFix = BitConverter.ToSingle(buffer, 4);
            calinfo.WatersCoefficient = BitConverter.ToSingle(buffer, 8);
            calinfo.WatersExponent = BitConverter.ToSingle(buffer, 12);
            calinfo.WatersT0 = BitConverter.ToSingle(buffer, 16);

        }

        private RAW_Measurement ReadIABF(FileStream fs, List<long> seekpointList) {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var buffer = new byte[4];
            fs.Seek(6, SeekOrigin.Begin);
            fs.Read(buffer, 0, 4);
            var accumulatedMs1DatapointCount = BitConverter.ToInt32(buffer, 0);
            fs.Read(buffer, 0, 4);
            var allSpectraCount = BitConverter.ToInt32(buffer, 0);

            fs.Seek(seekpointList[1], SeekOrigin.Begin); // to the start point of spectrum object
            var accumulatedMs1Spectra = new List<RAW_Spectrum>();
            for (int i = 0; i < accumulatedMs1DatapointCount; i++) {
                var spec = readSpectrumVer1(fs);
                spec.ScanNumber = i;
                spec.Index = i;

                //if (i > 100 && spec.ScanStartTime <= 0.1) break;
                //Console.WriteLine("scan id {0}, rt {1}, spec {2}", spec.ScanNumber, spec.ScanStartTime, spec.Spectrum.Length);
                accumulatedMs1Spectra.Add(spec);
            }

            var allSpectra = new List<RAW_Spectrum>();
            for (int i = 0; i < allSpectraCount; i++) {
                var spec = readSpectrumVer1(fs);
                //spec.ScanNumber = i;
                //Console.WriteLine("scan id {0}, drift id {1}, spec {2}", spec.ScanNumber, spec.DriftScanNumber, spec.Spectrum.Length);
                spec.Index = i;

               // if (i > 100 && spec.ScanStartTime <= 0.1) break;
                allSpectra.Add(spec);
            }

            Console.WriteLine("Reading speed (sec): " + stopwatch.Elapsed.Seconds);

            return new RAW_Measurement() {
                SourceFileInfo = this.SourceFiles[0],
                Sample = this.Samples[0],
                Method = MeasurmentMethod.IONMOBILITY,
                SpectrumList = allSpectra,
                AccumulatedSpectrumList = accumulatedMs1Spectra,
                ChromatogramList = null,
            };
        }

        private RAW_Spectrum readSpectrumVer1(FileStream fs) {
            var spectrum = new RAW_Spectrum();
            var buffer = new byte[64];
            fs.Read(buffer, 0, buffer.Length); // store the meta field buffer

            // read scan meta data
            spectrum.ScanNumber = BitConverter.ToInt32(buffer, 0);
            spectrum.Index = BitConverter.ToInt32(buffer, 4);
            spectrum.OriginalIndex = BitConverter.ToInt32(buffer, 8);
            spectrum.ScanStartTime = BitConverter.ToSingle(buffer, 12);
            spectrum.ScanStartTimeUnit = (Units)BitConverter.ToInt32(buffer, 16);

            spectrum.DriftScanNumber = BitConverter.ToInt32(buffer, 20);
            spectrum.DriftTime = BitConverter.ToSingle(buffer, 24);
            spectrum.DriftTimeUnit = (Units)BitConverter.ToInt32(buffer, 28);

            spectrum.ScanPolarity = (ScanPolarity)BitConverter.ToInt32(buffer, 32);

            // read spectrum basic info
            spectrum.BasePeakIntensity = BitConverter.ToSingle(buffer, 36);
            spectrum.BasePeakMz = BitConverter.ToSingle(buffer, 40);
            spectrum.TotalIonCurrent = BitConverter.ToSingle(buffer, 44);
            spectrum.LowestObservedMz = BitConverter.ToSingle(buffer, 48);
            spectrum.HighestObservedMz = BitConverter.ToSingle(buffer, 52);
            spectrum.MinIntensity = BitConverter.ToSingle(buffer, 56);

            spectrum.DefaultArrayLength = BitConverter.ToInt32(buffer, 60);
            var peakelements = new RAW_PeakElement[spectrum.DefaultArrayLength];
            if (spectrum.DefaultArrayLength != 0) {
                buffer = new byte[spectrum.DefaultArrayLength * 8];
                fs.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < spectrum.DefaultArrayLength; i++) {
                    var elem = new RAW_PeakElement() { Mz = BitConverter.ToSingle(buffer, 8 * i), Intensity = BitConverter.ToSingle(buffer, 8 * i + 4) };
                    peakelements[i] = elem;
                }
            }
            spectrum.Spectrum = peakelements;

            buffer = new byte[4];
            fs.Read(buffer, 0, buffer.Length);
            spectrum.MsLevel = BitConverter.ToInt32(buffer, 0);
            if (spectrum.MsLevel == 2) {
                buffer = new byte[28];
                fs.Read(buffer, 0, buffer.Length);
                spectrum.Precursor = new RAW_PrecursorIon();
                spectrum.Precursor.CollisionEnergy = BitConverter.ToSingle(buffer, 0);
                spectrum.Precursor.CollisionEnergyUnit = (Units)BitConverter.ToInt32(buffer, 4);
                spectrum.Precursor.Dissociationmethod = (DissociationMethods)BitConverter.ToInt32(buffer, 8);
                spectrum.Precursor.IsolationTargetMz = BitConverter.ToSingle(buffer, 12);
                spectrum.Precursor.IsolationWindowLowerOffset = BitConverter.ToSingle(buffer, 16);
                spectrum.Precursor.IsolationWindowUpperOffset = BitConverter.ToSingle(buffer, 20);
                spectrum.Precursor.SelectedIonMz = BitConverter.ToSingle(buffer, 24);
            }


            return spectrum;
        }

        public static List<long> ReadSeekPoints(FileStream fs) {
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);

            if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 1) {
                return getSeekpointListVer1(fs);
            } else if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 2) {
                return getSeekpointListVer1(fs);
            }
            return null;
        }

        private static List<long> getSeekpointListVer1(FileStream fs) {
            var seekpointList = new List<long>();
            byte[] buffer = new byte[4];

            fs.Seek(6, SeekOrigin.Begin);
            fs.Read(buffer, 0, 4);
            var accumulatedMs1DatapointCount = BitConverter.ToInt32(buffer, 0);

            fs.Read(buffer, 0, 4);
            var allSpectraCount = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[8 * accumulatedMs1DatapointCount];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < accumulatedMs1DatapointCount; i++)
                seekpointList.Add(BitConverter.ToInt64(buffer, 8 * i));

            buffer = new byte[8 * allSpectraCount];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < allSpectraCount; i++)
                seekpointList.Add(BitConverter.ToInt64(buffer, 8 * i));

            return seekpointList;
        }

    }
}
