using Riken.Metabolomics.RawData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.RawDataHandlerCommon.Writer {
    public class RawDataWriter {

        public void IabfWriter(RAW_Measurement measurment, string filePath, int versionNum,
            out string errorMessage, BackgroundWorker bgWorker = null) {
            errorMessage = string.Empty;
            if (measurment == null || measurment.AccumulatedSpectrumList == null || measurment.SpectrumList == null) {
                errorMessage = "Error in reading raw data file.";
                return;
            }

            var aMs1Spectra = measurment.AccumulatedSpectrumList;
            var allSpectra = measurment.SpectrumList;
            var seekPointer = new List<long>();

            using (var fs = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite)) {
                //first header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes('V'), 0, 2);
                fs.Write(BitConverter.GetBytes(versionNum), 0, 4);

                //second header: for accumulated MS1 Spectra
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes(aMs1Spectra.Count), 0, 4);

                //third header: for all spectra
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes(allSpectra.Count), 0, 4);

                //forth header
                var buffer = new byte[aMs1Spectra.Count * 8];
                seekPointer.Add(fs.Position);
                fs.Write(buffer, 0, buffer.Length);

                //fifth header
                buffer = new byte[allSpectra.Count * 8];
                seekPointer.Add(fs.Position);
                fs.Write(buffer, 0, buffer.Length);

                //sixth header: ion mobility metadata
                seekPointer.Add(fs.Position);
                writeIabfMetaData(fs, measurment);

                writeIabfData(fs, seekPointer, aMs1Spectra, allSpectra, bgWorker);

                //Finalize
                fs.Seek(seekPointer[3], SeekOrigin.Begin);
                for (int i = 6; i < seekPointer.Count; i++)
                    fs.Write(BitConverter.GetBytes(seekPointer[i]), 0, 8);
            }
        }

        private void writeIabfMetaData(FileStream fs, RAW_Measurement measurment) {

            var method = measurment.Method;
            var cal = measurment.CalibrantInfo;

            // scan meta data
            fs.Write(BitConverter.GetBytes((int)method), 0, 4);
            Console.WriteLine(BitConverter.GetBytes((int)method)[0] + "\t" + BitConverter.GetBytes((int)method)[1] + "\t" + BitConverter.GetBytes((int)method)[2] + "\t" + BitConverter.GetBytes((int)method)[3]);

            fs.Write(BitConverter.GetBytes((Boolean)cal.IsAgilentIM), 0, 1);
            Console.WriteLine(BitConverter.GetBytes((Boolean)cal.IsAgilentIM).Length);

            fs.Write(BitConverter.GetBytes((Boolean)cal.IsWatersIM), 0, 1);
            Console.WriteLine(BitConverter.GetBytes((Boolean)cal.IsWatersIM).Length);

            fs.Write(BitConverter.GetBytes((Boolean)cal.IsBrukerIM), 0, 1);
            fs.Write(BitConverter.GetBytes((float)cal.AgilentBeta), 0, 4);
            fs.Write(BitConverter.GetBytes((float)cal.AgilentTFix), 0, 4);
            fs.Write(BitConverter.GetBytes((float)cal.WatersCoefficient), 0, 4);
            fs.Write(BitConverter.GetBytes((float)cal.WatersExponent), 0, 4);
            fs.Write(BitConverter.GetBytes((float)cal.WatersT0), 0, 4);

        }

        private void writeIabfData(FileStream fs, List<long> seekPointer, List<RAW_Spectrum> aMs1Spectra, List<RAW_Spectrum> allSpectra, BackgroundWorker bgWorker) {
            var totalCount = aMs1Spectra.Count + allSpectra.Count;
            var counter = 0;

            foreach (var spec in aMs1Spectra) {
                seekPointer.Add(fs.Position);
                saveSpectrum(fs, spec);

                //Console.WriteLine("scan id {0}, rt {1}, spec {2}", spec.ScanNumber, spec.ScanStartTime, spec.Spectrum.Length);
                if (bgWorker != null) {
                    //progressReports(counter, totalCount, bgWorker);
                }
                else {
                    if (!Console.IsOutputRedirected) {
                        Console.Write("{0} / {1}", counter, totalCount);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("{0} / {1}", counter, totalCount);
                    }
                }
                counter++;
            }

            foreach (var spec in allSpectra) {
                seekPointer.Add(fs.Position);
                saveSpectrum(fs, spec);

                if (bgWorker != null) {
                    //progressReports(counter, totalCount, bgWorker);
                }
                else {
                    if (!Console.IsOutputRedirected) {
                        Console.Write("{0} / {1}", counter, totalCount);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("{0} / {1}", counter, totalCount);
                    }
                }
                counter++;
            }
        }

        private void saveSpectrum(FileStream fs, RAW_Spectrum spec) {
            // scan meta data
            fs.Write(BitConverter.GetBytes((int)spec.ScanNumber), 0, 4);
            fs.Write(BitConverter.GetBytes((int)spec.Index), 0, 4);
            fs.Write(BitConverter.GetBytes((int)spec.OriginalIndex), 0, 4);
            fs.Write(BitConverter.GetBytes((float)spec.ScanStartTime), 0, 4);
            fs.Write(BitConverter.GetBytes((int)spec.ScanStartTimeUnit), 0, 4); // [0] Undefined, [1] Second, [2] Minute, [3] Mz, [4] NumberOfCounts, [5] ElectronVolt, [6] Milliseconds, [7] Oneoverk0

            fs.Write(BitConverter.GetBytes((int)spec.DriftScanNumber), 0, 4);
            fs.Write(BitConverter.GetBytes((float)spec.DriftTime), 0, 4);
            fs.Write(BitConverter.GetBytes((int)spec.DriftTimeUnit), 0, 4); // [0] Undefined, [1] Second, [2] Minute, [3] Mz, [4] NumberOfCounts, [5] ElectronVolt, [6] Milliseconds, [7] Oneoverk0

            fs.Write(BitConverter.GetBytes((int)spec.ScanPolarity), 0, 4); // [0] Undefined, [1] Positive, [2] Negative, [3] Alternating

            // spectrum basic info
            fs.Write(BitConverter.GetBytes((float)spec.BasePeakIntensity), 0, 4);
            fs.Write(BitConverter.GetBytes((float)spec.BasePeakMz), 0, 4);
            fs.Write(BitConverter.GetBytes((float)spec.TotalIonCurrent), 0, 4);
            fs.Write(BitConverter.GetBytes((float)spec.LowestObservedMz), 0, 4);
            fs.Write(BitConverter.GetBytes((float)spec.HighestObservedMz), 0, 4);
            fs.Write(BitConverter.GetBytes((float)spec.MinIntensity), 0, 4);

            // spectrum
            fs.Write(BitConverter.GetBytes((int)spec.DefaultArrayLength), 0, 4);
            foreach (var peak in spec.Spectrum) {
                fs.Write(BitConverter.GetBytes((float)peak.Mz), 0, 4);
                fs.Write(BitConverter.GetBytes((float)peak.Intensity), 0, 4);
            }

            // register ms level and precursor ion info if precursor ion information is included
            fs.Write(BitConverter.GetBytes((int)spec.MsLevel), 0, 4);
            if (spec.MsLevel == 2) {
                var precursor = spec.Precursor;
                fs.Write(BitConverter.GetBytes((float)precursor.CollisionEnergy), 0, 4);
                fs.Write(BitConverter.GetBytes((int)precursor.CollisionEnergyUnit), 0, 4); // [0] Undefined, [1] Second, [2] Minute, [3] Mz, [4] NumberOfCounts, [5] ElectronVolt, [6] Milliseconds, [7] Oneoverk0
                fs.Write(BitConverter.GetBytes((int)precursor.Dissociationmethod), 0, 4); //[0] Undefined,  [1] CID,  [2] PD, PSD, SID, BIRD, ECD, IRMPD, SORI, HCD, LowEnergyCID, MPD, ETD, PQD, InSourceCID, LIFT,
                fs.Write(BitConverter.GetBytes((float)precursor.IsolationTargetMz), 0, 4);
                fs.Write(BitConverter.GetBytes((float)precursor.IsolationWindowLowerOffset), 0, 4);
                fs.Write(BitConverter.GetBytes((float)precursor.IsolationWindowUpperOffset), 0, 4);
                fs.Write(BitConverter.GetBytes((float)precursor.SelectedIonMz), 0, 4);
            }
        }

        private void progressReports(int currentProgress, int maxProgress, BackgroundWorker bgWorker) {
            var progress = (double)currentProgress / (double)maxProgress * 100.0;
            if (bgWorker != null)
                bgWorker.ReportProgress((int)progress);
        }
    }
}
