using Riken.Metabolomics.RawData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassLynxRawSDK;
using System.Diagnostics;
using System.ComponentModel;
using Riken.Metabolomics.RawDataHandlerCommon.Parser;

namespace Riken.Metabolomics.WatersDataHandler.Parser
{
    public class MasslynxDataReader
    {
        public string RawfilePath { get; private set; }
        public int SpectraCount { get; private set; }
        public int TotalSpectraCount { get; private set; }
        public List<RAW_SourceFileInfo> SourceFiles { get; private set; }
        public List<RAW_Sample> Samples { get; private set; }
        public DateTime StartTimeStamp { get; private set; }
        public List<RAW_Spectrum> SpectraList { get; private set; }
        public List<RAW_Spectrum> AccumulatedMs1SpectrumList { get; private set; }
        public List<RAW_Chromatogram> ChromatogramsList { get; private set; }
        public Raw_CalibrationInfo CalibrantInfo { get; private set; }
        public bool IsMs1Profile { get; private set; }
        public bool IsMs2Profile { get; private set; }
        public bool IsMSE { get; private set; }

        public RAW_Measurement ReadWartersRaw(string filePath, int fileID, double peakCutOff, out string errorMessage, BackgroundWorker bgWorker = null) {
            this.SourceFiles = new List<RAW_SourceFileInfo>();
            this.Samples = new List<RAW_Sample>();
            this.StartTimeStamp = new DateTime();
            this.SpectraList = new List<RAW_Spectrum>();
            this.AccumulatedMs1SpectrumList = new List<RAW_Spectrum>();
            this.ChromatogramsList = null;
            this.IsMs1Profile = true;
            this.IsMs2Profile = true;
            this.CalibrantInfo = new Raw_CalibrationInfo();
            this.SpectraCount = 0;

            errorMessage = string.Empty;
            var errorCode = 0;
            var isMobilityDataIncluded = false;
            try {
                var watersInfInfo = new ReadExternInf(filePath); //need to be modified
                var molcabInfo = new ReadMobCal(filePath);
                var mlInfoReader = new MassLynxRawInfoReader(filePath);
                var nFunctions = mlInfoReader.GetFunctionCount();

                if (molcabInfo.doesMobCalFileExist) { // if there is mobcal info, set the values
                    this.CalibrantInfo.IsWatersIM = true;
                    this.CalibrantInfo.WatersCoefficient = molcabInfo.Coefficient;
                    this.CalibrantInfo.WatersExponent = molcabInfo.Exponent;
                    this.CalibrantInfo.WatersT0 = molcabInfo.T0;
                }

                // as the raw data is already open in the inforeader
                // create scan reader from inforeader
                var mlScanReader = new MassLynxRawScanReader(mlInfoReader);
                this.TotalSpectraCount = getTotalScanCount(mlInfoReader, mlScanReader, watersInfInfo);

                for (int i = 0; i < nFunctions; i++) {
                    //how many scans in the function
                    var nWhichFunction = i;
                    var functionType = mlInfoReader.GetFunctionType(nWhichFunction);
                    //var isIonMobility = watersInfInfo.functionToIsIonMobility[i + 1] ? "TRUE" : "FALSE";

                    if (!watersInfInfo.functionToString.ContainsKey(i + 1)) continue;
                    if (watersInfInfo.functionIsReference.ContainsKey(i + 1)) continue;
                    // if (i != 0) continue;
                    // Debug.WriteLine("function id: {0}, function type: {1}, scan count: {2}, drift scan count: {3}", i, functionType.ToString(), nScans, nDriftScans);

                    var isMobilityData = watersInfInfo.functionToString[i + 1].ToLower().Contains("mobility") ? true : false;
                    if (watersInfInfo.functionToIsIonMobility.ContainsKey(i + 1) && watersInfInfo.functionToIsIonMobility[i + 1] == true) {
                        isMobilityData = true;
                    }
                    if (isMobilityData) isMobilityDataIncluded = true;

                    var msLevel = watersInfInfo.functionToString[i + 1].ToLower().Contains("msms") ? 2 : 1;
                    parseFunctionSpectra(watersInfInfo, mlInfoReader, mlScanReader, nWhichFunction, msLevel, isMobilityData, peakCutOff, bgWorker);
                }

                this.SpectraList = this.SpectraList.OrderBy(n => n.ScanNumber).ThenBy(n => n.DriftScanNumber).ToList();
                for (int i = 0; i < this.SpectraList.Count; i++) {
                    this.SpectraList[i].Index = i;
                    this.SpectraList[i].OriginalIndex = i;
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
                      //  Console.WriteLine("Scan num {0}, RT {1}, Spec num {2}, ms level {3}", spec.ScanNumber,
                      //  spec.ScanStartTime, spec.MsLevel, spec.Spectrum.Length);
                        spec.Index = counter;
                        counter++;
                        foreach (var tSpec in tempSpeclist) {
                            if (spec.ScanNumber == tSpec.ScanNumber) {
                                spec.OriginalIndex = tSpec.Index;
                                //Console.WriteLine("Spec index {0}, spec original index {1}", spec.Index, spec.OriginalIndex);
                                break;
                            }
                        }
                    }
                }
            }
            catch (MassLynxException e) {
                errorMessage = e.Message;
                errorCode = e.code;
            }

            if (this.SourceFiles.Count == 0) {
                this.SourceFiles.Add(new RAW_SourceFileInfo() { Id = fileID.ToString(), Location = filePath, Name = System.IO.Path.GetFileNameWithoutExtension(filePath) });
            }
            if (this.Samples.Count == 0) {
                this.Samples.Add(new RAW_Sample() { Id = fileID.ToString(), Name = System.IO.Path.GetFileNameWithoutExtension(filePath) });
            }

            var method = MeasurmentMethod.DDA;
            if (isMobilityDataIncluded) {
                if (IsMSE) {
                    method = MeasurmentMethod.IM_ALLIONS;
                }
                else {
                    method = MeasurmentMethod.IM_DDA;
                }
            }
            else {
                if (IsMSE) {
                    method = MeasurmentMethod.ALLIONS;
                }
                else {
                    method = MeasurmentMethod.DDA;
                }
            }

            return new RAW_Measurement() {
                SourceFileInfo = this.SourceFiles[0],
                Sample = this.Samples[0],
                SpectrumList = this.SpectraList,
                AccumulatedSpectrumList = this.AccumulatedMs1SpectrumList,
                ChromatogramList = this.ChromatogramsList,
                CalibrantInfo = this.CalibrantInfo,
                Method = method
            };
        }

        private int getTotalScanCount(MassLynxRawInfoReader mlInfoReader, MassLynxRawScanReader mlScanReader, ReadExternInf watersInfInfo) {
            var nFunctions = mlInfoReader.GetFunctionCount();
            var totalScan = 0;
            for (int i = 0; i < nFunctions; i++) {
                //how many scans in the function
                var nWhichFunction = i;
                var functionType = mlInfoReader.GetFunctionType(nWhichFunction);
                //var isIonMobility = watersInfInfo.functionToIsIonMobility[i + 1] ? "TRUE" : "FALSE";

                if (!watersInfInfo.functionToString.ContainsKey(i + 1)) continue;
                if (watersInfInfo.functionIsReference.ContainsKey(i + 1)) continue;
                var nScans = mlInfoReader.GetScansInFunction(nWhichFunction);
                totalScan += nScans;
            }
            return totalScan;
        }

        private void parseFunctionSpectra(ReadExternInf watersInfInfo, MassLynxRawInfoReader mlInfoReader, 
            MassLynxRawScanReader mlScanReader, 
            int nWhichFunction, int msLevel, bool isMobilityData, double peakCutOff, BackgroundWorker bgWorker = null) {

            var nScans = mlInfoReader.GetScansInFunction(nWhichFunction);
            var scanCount = 0;

            //the below protocol is checking the ms level and if ion mobility data or not.
            //currently, the below code is optimized for LC-MS, LC-MS/MS, or LC-IMS
            for (int i = 0; i < nScans; i++) {

                var nWhichScan = i;
                var retentiontime = mlInfoReader.GetRetentionTime(nWhichFunction, nWhichScan);
                var ionmode = mlInfoReader.GetIonMode(nWhichFunction);
                // MSE is stored as ms1 data. Therefore, the collision energy should be parsed in this area.
                var collisionEnergy = mlInfoReader.GetScanItem(nWhichFunction, nWhichScan, MassLynxScanItem.COLLISION_ENERGY);
                double colEnv = 0.0;
                double.TryParse(collisionEnergy, out colEnv);

                RAW_PrecursorIon precurosr = null;
                if (msLevel == 2) {
                    var precursorString = mlInfoReader.GetScanItem(nWhichFunction, nWhichScan, MassLynxScanItem.SET_MASS);
                    double preMz;
                    if (double.TryParse(precursorString, out preMz)) {

                        var offset = 0.0;
                        if (preMz < 0.01) { // now meaning MSE
                            IsMSE = true;
                            preMz = (watersInfInfo.functionToMassRange[nWhichFunction][0] + watersInfInfo.functionToMassRange[nWhichFunction][1]) * 0.5;
                            offset = preMz - watersInfInfo.functionToMassRange[nWhichFunction][0];
                        }
                        precurosr = new RAW_PrecursorIon() {
                            SelectedIonMz = preMz,
                            IsolationTargetMz = preMz,
                            IsolationWindowLowerOffset = offset,
                            IsolationWindowUpperOffset = offset,
                            CollisionEnergy = colEnv,
                            CollisionEnergyUnit = Units.ElectronVolt,
                            Dissociationmethod = DissociationMethods.CID
                        };
                    }
                }

                var isStored = false;
                if (isMobilityData) {
                    var nDriftScans = mlInfoReader.GetDriftScanCount(nWhichFunction);
                    var driftCount = 0;
                    //var accumulatedMassIntensityArray = new double[300000000];
                    var accumulatedMassBin = new Dictionary<int, double[]>();
                    for (int j = 0; j < nDriftScans; j++) {

                        var nWhichDrift = j;
                        var drifttime = mlInfoReader.GetDriftTime(nWhichFunction, nWhichDrift);

                        var specturm = new RAW_Spectrum() {
                            ScanNumber = nWhichScan,
                            ScanStartTime = retentiontime,
                            ScanStartTimeUnit = Units.Minute,
                            MsLevel = msLevel,
                            ScanPolarity = ionmode.ToString().Contains("POS") ? ScanPolarity.Positive : ScanPolarity.Negative,
                            CollisionEnergy = colEnv,
                            Precursor = precurosr,
                            DriftTime = drifttime,
                            DriftTimeUnit = Units.Milliseconds
                        };

                        float[] masses = null;
                        float[] intensities = null;

                        mlScanReader.ReadScan(nWhichFunction, nWhichScan, nWhichDrift, out masses, out intensities);
                        SpectrumParser.setSpectrumProperties(specturm, masses, intensities, peakCutOff, accumulatedMassBin);

                        if (specturm.BasePeakIntensity > 0 && specturm.Spectrum.Length > 0) {
                            specturm.DriftScanNumber = j;
                            this.SpectraList.Add(specturm);
                            driftCount++;
                            isStored = true;
                        }

                        //Debug.WriteLine("Function ID {0}, Scan ID {1}, Drift ID {2}, retention time {3}, drift time {4}", nWhichFunction, nWhichScan, nWhichDrift, retentiontime, drifttime);

                        //for (int m = 0; m < masses.Length; m++) {
                        //    Debug.WriteLine("m/z {0}, intensity {1}", masses[m], intensities[m]);
                        //}
                    }
                    if (msLevel == 1) {
                        var accuSpec = new RAW_Spectrum() {
                            ScanNumber = nWhichScan,
                            ScanStartTime = retentiontime,
                            ScanStartTimeUnit = Units.Minute,
                            MsLevel = msLevel,
                            CollisionEnergy = colEnv,
                            ScanPolarity = ionmode.ToString().Contains("POS") ? ScanPolarity.Positive : ScanPolarity.Negative,
                            Precursor = precurosr
                        };
                        //Console.WriteLine("Scan num {0}, RT {1}, Spec num {2}, ms level {3}", nWhichScan,
                        //    retentiontime, msLevel, accumulatedMassIntensityArray.Length);
                        SpectrumParser.setSpectrumProperties(accuSpec, accumulatedMassBin);
                        this.AccumulatedMs1SpectrumList.Add(accuSpec);
                    }
                }
                else {
                    var spectrum = new RAW_Spectrum() {
                        ScanNumber = nWhichScan,
                        ScanStartTime = retentiontime,
                        ScanStartTimeUnit = Units.Minute,
                        MsLevel = msLevel,
                        CollisionEnergy = colEnv,
                        ScanPolarity = ionmode.ToString().Contains("POS") ? ScanPolarity.Positive : ScanPolarity.Negative,
                        Precursor = precurosr
                    };

                    float[] masses = null;
                    float[] intensities = null;

                    mlScanReader.ReadScan(nWhichFunction, nWhichScan, out masses, out intensities);
                    SpectrumParser.setSpectrumProperties(spectrum, masses, intensities, peakCutOff);
                    if (spectrum.BasePeakIntensity > 0 && spectrum.Spectrum.Length > 0) {
                        this.SpectraList.Add(spectrum);
                        isStored = true;
                    }
                }

                if (isStored) {
                    scanCount++;
                }

                this.SpectraCount++;
               
                if (bgWorker != null)
                    progressReports(this.SpectraCount, this.TotalSpectraCount, bgWorker);
                else {
                    if (!Console.IsOutputRedirected) {
                        Console.Write("{0} / {1}", this.SpectraCount, this.TotalSpectraCount);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("{0} / {1}", this.SpectraCount, this.TotalSpectraCount);
                    }
                }
            }
        }

        private void progressReports(int currentProgress, int maxProgress, BackgroundWorker bgWorker) {
            var progress = (double)currentProgress / (double)maxProgress * 100.0;
            if (bgWorker != null)
                bgWorker.ReportProgress((int)progress);
        }

        //private void setSpectrumProperties(RAW_Spectrum spectrum, double[] accumulatedMassIntensityArray) {

        //    //spectrum.DefaultArrayLength = accumulatedMassIntensityArray.Count(n => n > 1);

        //    var basepeakIntensity = 0.0;
        //    var basepeakMz = 0.0;
        //    var totalIonCurrnt = 0.0;
        //    var lowestMz = double.MaxValue;
        //    var highestMz = double.MinValue;
        //    var minIntensity = double.MaxValue;

        //    var spectra = new List<RAW_PeakElement>();

        //    //spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];
        //    for (int i = 0; i < accumulatedMassIntensityArray.Length; i++) {
        //        if (accumulatedMassIntensityArray[i] < 1) continue;
        //        var mass = (double)i * 0.001;
        //        var intensity = accumulatedMassIntensityArray[i];

        //        totalIonCurrnt += intensity;

        //        if (intensity > basepeakIntensity) {
        //            basepeakIntensity = intensity;
        //            basepeakMz = mass;
        //        }
        //        if (lowestMz > mass) lowestMz = mass;
        //        if (highestMz < mass) highestMz = mass;
        //        if (minIntensity > intensity) minIntensity = intensity;

        //        var spec = new RAW_PeakElement() {
        //            Mz = Math.Round(mass, 5),
        //            Intensity = Math.Round(intensity, 0)
        //        };
        //        spectra.Add(spec);
        //    }

        //    spectrum.Spectrum = spectra.ToArray();
        //    spectrum.DefaultArrayLength = spectra.Count();
        //    spectrum.BasePeakIntensity = basepeakIntensity;
        //    spectrum.BasePeakMz = basepeakMz;
        //    spectrum.TotalIonCurrent = totalIonCurrnt;
        //    spectrum.LowestObservedMz = lowestMz;
        //    spectrum.HighestObservedMz = highestMz;
        //    spectrum.MinIntensity = minIntensity;
        //}

        //private void setSpectrumProperties(RAW_Spectrum spectrum, float[] masses, float[] intensities,
        //    ref double[] accumulatedMassIntensityArray) {
        //    spectrum.DefaultArrayLength = masses.Length;

        //    var basepeakIntensity = 0.0;
        //    var basepeakMz = 0.0;
        //    var totalIonCurrnt = 0.0;
        //    var lowestMz = double.MaxValue;
        //    var highestMz = double.MinValue;
        //    var minIntensity = double.MaxValue;

        //    spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];
        //    for (int i = 0; i < spectrum.DefaultArrayLength; i++) {
        //        var mass = masses[i];
        //        var intensity = intensities[i];
        //        totalIonCurrnt += intensity;

        //        if (intensity > basepeakIntensity) {
        //            basepeakIntensity = intensity;
        //            basepeakMz = mass;
        //        }
        //        if (lowestMz > mass) lowestMz = mass;
        //        if (highestMz < mass) highestMz = mass;
        //        if (minIntensity > intensity) minIntensity = intensity;

        //        spectrum.Spectrum[i].Mz = mass;
        //        spectrum.Spectrum[i].Intensity = intensity;
        //        if (spectrum.MsLevel == 1) {
        //            accumulatedMassIntensityArray[(int)(mass * 1000)] += intensity;
        //        }
        //    }

        //    spectrum.Spectrum = spectrum.Spectrum.OrderBy(n => n.Mz).ToArray();
        //    spectrum.BasePeakIntensity = basepeakIntensity;
        //    spectrum.BasePeakMz = basepeakMz;
        //    spectrum.TotalIonCurrent = totalIonCurrnt;
        //    spectrum.LowestObservedMz = lowestMz;
        //    spectrum.HighestObservedMz = highestMz;
        //    spectrum.MinIntensity = minIntensity;
        //}

        //private void setSpectrumProperties(RAW_Spectrum spectrum, float[] masses, float[] intensities) {
        //    spectrum.DefaultArrayLength = masses.Length;

        //    var basepeakIntensity = 0.0;
        //    var basepeakMz = 0.0;
        //    var totalIonCurrnt = 0.0;
        //    var lowestMz = double.MaxValue;
        //    var highestMz = double.MinValue;
        //    var minIntensity = double.MaxValue;

        //    spectrum.Spectrum = new RAW_PeakElement[spectrum.DefaultArrayLength];
        //    for (int i = 0; i < spectrum.DefaultArrayLength; i++) {
        //        var mass = masses[i];
        //        var intensity = intensities[i];
        //        totalIonCurrnt += intensity;

        //        if (intensity > basepeakIntensity) {
        //            basepeakIntensity = intensity;
        //            basepeakMz = mass;
        //        }
        //        if (lowestMz > mass) lowestMz = mass;
        //        if (highestMz < mass) highestMz = mass;
        //        if (minIntensity > intensity) minIntensity = intensity;

        //        spectrum.Spectrum[i].Mz = mass;
        //        spectrum.Spectrum[i].Intensity = intensity;
        //    }

        //    spectrum.Spectrum = spectrum.Spectrum.OrderBy(n => n.Mz).ToArray();
        //    spectrum.BasePeakIntensity = basepeakIntensity;
        //    spectrum.BasePeakMz = basepeakMz;
        //    spectrum.TotalIonCurrent = totalIonCurrnt;
        //    spectrum.LowestObservedMz = lowestMz;
        //    spectrum.HighestObservedMz = highestMz;
        //    spectrum.MinIntensity = minIntensity;
        //}
    }
}
