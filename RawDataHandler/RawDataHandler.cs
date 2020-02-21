using Riken.Metabolomics.AbfDataHandler;
using Riken.Metabolomics.BrukerDataHandler.Parser;
using Riken.Metabolomics.MzmlHandler.Parser;
using Riken.Metabolomics.Netcdf;
using Riken.Metabolomics.RawData;
using Riken.Metabolomics.RawDataHandlerCommon.Reader;
using Riken.Metabolomics.RawDataHandlerCommon.Writer;
using Riken.Metabolomics.WatersDataHandler.Parser;
using Riken.Metabolomics.AgilentDataHandler.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Riken.Metabolomics.RawDataHandler
{
    public enum RawDataExtension { abf, mzml, cdf, raw, d, ibf }

    public class RawDataAccess : IDisposable
    {
        private bool disposed = false;
        private bool isDataSupported = true;

        private string filepath;
        private RAW_Measurement measurement;
        private RawDataExtension extension;
        private string extensionString;
        private int fileID;
        private bool isGuiProcess;
        private List<double> correctedRtList;
        private BackgroundWorker bgWorker;
        public double PeakCutOff { get; set; } = 1.0;

        public RawDataAccess(string filepath, int fileID, bool isGuiProcess, List<double> correctedRts = null, BackgroundWorker bgWorker = null) 
        {
            this.filepath = filepath;
            this.fileID = fileID;
            this.isGuiProcess = isGuiProcess;
            this.correctedRtList = correctedRts;
            this.bgWorker = bgWorker;
            this.extensionString = System.IO.Path.GetExtension(this.filepath).ToLower();
            switch (this.extensionString) {
                case ".abf": this.extension = RawDataExtension.abf; break;
                case ".mzml": this.extension = RawDataExtension.mzml; break;
                case ".cdf": this.extension = RawDataExtension.cdf; break;
                case ".raw": this.extension = RawDataExtension.raw; break;
                case ".d": this.extension = RawDataExtension.d; break;
                case ".iabf": this.extension = RawDataExtension.ibf; break;
                case ".ibf": this.extension = RawDataExtension.ibf; break;
                default: this.isDataSupported = false; break;
            }
        }

        public RawDataAccess() { }

        public RAW_Measurement GetMeasurement()
        {
            if (this.isDataSupported == false) {
                MessageBox.Show("Raw data format is not supported: " + this.extensionString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            else {
                try {
                    var errorMessage = string.Empty;
                    switch (this.extension) {
                        case RawDataExtension.abf   : return RAW_Measurement_Wrapper_RtCorrection(new ObjectConverter().ReadAbf(this.filepath, this.fileID));
                        case RawDataExtension.cdf   : return RAW_Measurement_Wrapper_RtCorrection(new NetCdfReader().ReadNetCdf(this.filepath, this.fileID));
                        case RawDataExtension.mzml  : return RAW_Measurement_Wrapper_RtCorrection(new MzmlReader().ReadMzml(this.filepath, this.fileID, this.isGuiProcess, this.bgWorker));
                        case RawDataExtension.raw   : return RAW_Measurement_Wrapper_RtCorrection(new MasslynxDataReader().ReadWartersRaw(this.filepath, this.fileID, PeakCutOff, out errorMessage, this.bgWorker));
                        case RawDataExtension.d:
                            var acqDataFolder = Path.Combine(filepath, "AcqData");
                            var isAcqDataExist = Directory.Exists(acqDataFolder); // if true, it's agilent data.
                            if (isAcqDataExist)
                                return RAW_Measurement_Wrapper_RtCorrection(new AgilentMidacDataReader().ReadAgilentMidacDotD(this.filepath, this.fileID, PeakCutOff, out errorMessage, this.bgWorker));
                            else
                                return RAW_Measurement_Wrapper_RtCorrection(new TimsTofDataReader().ReadBrukerDotD(this.filepath, this.fileID, PeakCutOff, out errorMessage, this.bgWorker));
                        case RawDataExtension.ibf: return RAW_Measurement_Wrapper_RtCorrection(new RawDataReader().ReadIABF(this.filepath, this.fileID, out errorMessage));
                    }
                }
                catch (Exception ex) {
                    var errorString = ex.Message + "\r\n";
                    errorString += "If your data is netCDF format, confirm whether netCDF 3 or later is installed or not on your PC.\r\n";
                    errorString += "See: " + @"http://www.unidata.ucar.edu/software/netcdf/docs/winbin.html";
                    errorString += "In other cases, please email to me: hiroshi.tsugawa@riken.jp with one data file that you want to analyze.";
                    //                    MessageBox.Show(, MessageBoxButton.OK);
                    Console.WriteLine("Error: " + errorString);
                    return null;
                }
            }
            return null;
        }

        public Raw_CalibrationInfo ReadIonmobilityCalibrationInfo() {
            if (this.extensionString != ".ibf") return null;
            var errorString = string.Empty;
            return new RawDataReader().ReadCalibrationInformation(this.filepath, 0, out errorString);
        }

        // now Bruker data converter is tested
        public void ConvertToIABF() {
            var version = 2;
            var errorString = string.Empty;
            if (this.isDataSupported == false) {
                MessageBox.Show("Raw data format is not supported: " + this.extensionString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else {
                try {
                    var directory = System.IO.Path.GetDirectoryName(this.filepath);
                    var filename = System.IO.Path.GetFileNameWithoutExtension(this.filepath);
                    var filepathHeader = directory + "\\" + filename;
                    var iabf_Path = filepathHeader + ".ibf"; // store versions and pointers (accumulated ms1 and all spectra)
                    var measurement = GetMeasurement();
                    if (measurement == null) {
                        MessageBox.Show("Raw data was not read correctly: " + filename, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var writer = new RawDataWriter();
                    var error = string.Empty;
                    writer.IabfWriter(measurement, iabf_Path, version, out error, this.bgWorker);
                }
                catch (Exception ex) {
                    errorString +=  " " + ex.Message + "\r\n";
                    errorString += "In other cases, please email to me: hiroshi.tsugawa@riken.jp with one data file that you want to analyze.";
                    Console.WriteLine("Error: " + errorString);
                    return;
                }
            }
        }

        public void IbfDataDump(string filepath) {
            var error = string.Empty;
            var fileobj = new RawDataReader().ReadIABF(filepath, 0, out error);
            Console.WriteLine(fileobj.Method);

            var calinfo = fileobj.CalibrantInfo;
            Console.WriteLine("Waters {0}, Agilent {1}, Bruker {2}", calinfo.IsWatersIM, calinfo.IsAgilentIM, calinfo.IsBrukerIM);
            Console.WriteLine("Waters coeff {0}, Waters exponent {1}, Waters T0 {2}, Agilent beta {3}, Agilent tfix {4}",
                calinfo.WatersCoefficient, calinfo.WatersExponent, calinfo.WatersT0, calinfo.AgilentBeta, calinfo.AgilentTFix);

            var speclist = fileobj.SpectrumList;
            foreach (var peak in speclist) {
                Console.WriteLine("ID {0}, RT {1}, DT {2}, MS level {3}, Spec count {4}",
                    peak.OriginalIndex, peak.ScanStartTime, peak.DriftTime, peak.MsLevel, peak.Spectrum.Length);
            }
        }

        // now Bruker data converter is tested
        public void ConvertToIABF(string filepath) {
            using (var access = new RawDataAccess(filepath, 0, false)) {
                access.ConvertToIABF();
            }
        }

        private RAW_Measurement RAW_Measurement_Wrapper_RtCorrection(RAW_Measurement measurement) {
            if (this.correctedRtList == null || measurement == null) return measurement;
            if (this.correctedRtList.Count != measurement.SpectrumList.Count) {
                System.Diagnostics.Debug.WriteLine("Error, different data points: " + correctedRtList.Count + "\t" + measurement.SpectrumList.Count);
                return measurement;
            }
            for (var i = 0; i < correctedRtList.Count; i++) {
                measurement.SpectrumList[i].ScanStartTime = correctedRtList[i];
            }
            return measurement;
        }

        #region prop
        public RAW_Measurement Measurement
        {
            get { return measurement; }
            set { measurement = value; }
        }

        public RawDataExtension Extension
        {
            get { return extension; }
            set { extension = value; }
        }

        public string Filepath
        {
            get { return filepath; }
            set { filepath = value; }
        }

        public bool IsDataSupported
        {
            get { return isDataSupported; }
            set { isDataSupported = value; }
        }

        public int FileID
        {
            get { return fileID; }
            set { fileID = value; }
        }

        #endregion

        ~RawDataAccess()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!this.disposed) {
                this.disposed = true;
                if (this.disposed) {
                    GC.SuppressFinalize(this);
                }
            }
        }
    }
}
