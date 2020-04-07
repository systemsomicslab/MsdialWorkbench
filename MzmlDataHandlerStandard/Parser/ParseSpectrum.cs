using Riken.Metabolomics.MzmlHandler.Converter;
using Riken.Metabolomics.RawData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.MzmlHandler.Parser
{
    public partial class MzmlReader
    {
        private RAW_Spectrum currentSpectrum;
        private RAW_PrecursorIon currentPrecursor;
        private RAW_ProductIon currentProduct;

        private const double initialProgress = 0.0;
        private const double progressMax = 100.0;

        #region /// parseSpectrum and related methods;
        private void parseSpectrum()
        {
            var spectrum = new RAW_Spectrum();
            this.currentSpectrum = spectrum;

            this.parserCommonMethod("spectrum",
                new Dictionary<string, Action<string>>()
                {
                   { "defaultArrayLength", (v) => { int n; if (Int32.TryParse(v, out n)) spectrum.DefaultArrayLength = n; } },
                   { "id", (v) => { spectrum.Id = v; this.parseSpectrumId(); } },
                   { "index", (v) => { int n; if (Int32.TryParse(v, out n)) spectrum.ScanNumber = n; } },
                },
                new Dictionary<string, Action>()
                {
                    { "cvParam", () => {
                        var cvParamVal = this.parseCvParam();
                        switch (cvParamVal.paramType) {
                            case CvParamTypes.MsLevel: spectrum.MsLevel = (int)cvParamVal.value; break;
                            case CvParamTypes.ScanPolarity: spectrum.ScanPolarity = convertCvParamValToScanPolarity(cvParamVal.value); break;
                            case CvParamTypes.SpectrumRepresentation: spectrum.SpectrumRepresentation = convertCvParamValToSpectrumPresentation(cvParamVal.value); break;
                            case CvParamTypes.BasePeakMz: spectrum.BasePeakMz = (double)cvParamVal.value; break;
                            case CvParamTypes.BasePeakIntensity: spectrum.BasePeakIntensity = (double)cvParamVal.value; break;
                            case CvParamTypes.TotalIonCurrent: spectrum.TotalIonCurrent = (double)cvParamVal.value; break;
                            case CvParamTypes.HighestObservedMz: spectrum.HighestObservedMz = (double)cvParamVal.value; break;
                            case CvParamTypes.LowestObservedMz: spectrum.LowestObservedMz = (double)cvParamVal.value; break;
                        }
                    }},
                    { "scanList", () => this.parseScanList() },
                    { "precursorList", () => this.parsePrecursorList() },
                    { "productList", () => this.parseProductList() },
                    { "binaryDataArrayList", () => this.parseBinaryDataArrayListForSpectrum() },
                });

            this.SpectraList.Add(spectrum);
            //Console.WriteLine("{0} / {1}", this.SpectraList.Count, this.SpectraCount);
            //if (this.SpectraCount - this.SpectraList.Count == 1) {
            //    Console.WriteLine();
            //}
            if (this.bgWorker != null)
                progressReports(this.SpectraList.Count, this.SpectraCount, bgWorker);
            else if (this.IsGuiProcess) {

            }
            else {
                if (!Console.IsOutputRedirected) {
                    Console.Write("{0} / {1}", this.SpectraList.Count, this.SpectraCount);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }
                else {
                    Console.WriteLine("{0} / {1}", this.SpectraList.Count, this.SpectraCount);
                }
            }
            this.currentSpectrum = null;
        }

        private void progressReports(int current, int total, System.ComponentModel.BackgroundWorker bgWorker)
        {
            var progress = (double)current / (double)total * 100.0;
            this.bgWorker.ReportProgress((int)progress);
        }

        private SpectrumRepresentation convertCvParamValToSpectrumPresentation(object presentation)
        {
            var mzMlPresentation = (MzMlSpectrumRepresentation)presentation;
            switch (mzMlPresentation) {
                case MzMlSpectrumRepresentation.Undefined: return SpectrumRepresentation.Undefined;
                case MzMlSpectrumRepresentation.Centroid: return SpectrumRepresentation.Centroid;
                case MzMlSpectrumRepresentation.Profile: return SpectrumRepresentation.Profile;
                default: return SpectrumRepresentation.Undefined;
            }
        }

        private ScanPolarity convertCvParamValToScanPolarity(object polarity)
        {
            var mzMlPolarity = (MzMlScanPolarity)polarity;
            switch (mzMlPolarity) {
                case MzMlScanPolarity.Undefined: return ScanPolarity.Undefined;
                case MzMlScanPolarity.Positive: return ScanPolarity.Positive;
                case MzMlScanPolarity.Negative: return ScanPolarity.Negative;
                case MzMlScanPolarity.Alternating: return ScanPolarity.Alternating;
                default: return ScanPolarity.Undefined;
            }
        }

        private Units convertCvParamValToUnits(MzMlUnits mzMlUnits)
        {
            switch (mzMlUnits) {
                case MzMlUnits.Undefined: return Units.Undefined;
                case MzMlUnits.Second: return Units.Second;
                case MzMlUnits.Minute: return Units.Minute;
                case MzMlUnits.Millisecond: return Units.Milliseconds;
                case MzMlUnits.Mz: return Units.Mz;
                case MzMlUnits.NumberOfCounts: return Units.NumberOfCounts;
                case MzMlUnits.ElectronVolt: return Units.ElectronVolt;
                default: return Units.Undefined;
            }
        }

        private DissociationMethods convertCvParamValToDissociationMethods(object disMethod)
        {
            var mzmlDisMethod = (MzMlDissociationMethods)disMethod;

            switch (mzmlDisMethod) {
                case MzMlDissociationMethods.Undefined: return DissociationMethods.Undefined;
                case MzMlDissociationMethods.CID: return DissociationMethods.CID;
                case MzMlDissociationMethods.BIRD: return DissociationMethods.BIRD;
                case MzMlDissociationMethods.PD: return DissociationMethods.PD;
                case MzMlDissociationMethods.PSD: return DissociationMethods.PSD;
                case MzMlDissociationMethods.SID: return DissociationMethods.SID;
                case MzMlDissociationMethods.ECD: return DissociationMethods.ECD;
                case MzMlDissociationMethods.IRMPD: return DissociationMethods.IRMPD;
                case MzMlDissociationMethods.SORI: return DissociationMethods.SORI;
                case MzMlDissociationMethods.HCD: return DissociationMethods.HCD;
                case MzMlDissociationMethods.LowEnergyCID: return DissociationMethods.LowEnergyCID;
                case MzMlDissociationMethods.MPD: return DissociationMethods.MPD;
                case MzMlDissociationMethods.ETD: return DissociationMethods.ETD;
                case MzMlDissociationMethods.PQD: return DissociationMethods.PQD;
                case MzMlDissociationMethods.InSourceCID: return DissociationMethods.InSourceCID;
                case MzMlDissociationMethods.LIFT: return DissociationMethods.LIFT;
                default: return DissociationMethods.CID;
            }
        }

        private void parseSpectrumId()
        {
            int n;
            foreach (string keyValStr in this.currentSpectrum.Id.Split(' '))
            {
                string[] kv = keyValStr.Split('=');
                if (kv[0] == "scan")
                {
                    if (Int32.TryParse(kv[1], out n)) {
                        if (this.currentSpectrum.ScanNumber == 0)
                            this.currentSpectrum.ScanNumber = n;
                    }
                }
            }
        } 
        #endregion

        #region /// parsing scanlist and belonging elements

        private void parseScanList()
        {
            this.parserCommonMethod("scanList", null,
                new Dictionary<string, Action>() { { "scan", () => this.parseScan() } });
        }

        private void parseScan()
        {
            this.parserCommonMethod("scan", null,
                new Dictionary<string, Action>()
                {
                    {"scanWindowList", () => this.parseScanWindowList() },
                    { "cvParam", () => {
                        var cv = this.parseCvParam();
                        if (cv.paramType == CvParamTypes.ScanStartTime){
                            this.currentSpectrum.ScanStartTime = (double)cv.value;
                            this.currentSpectrum.ScanStartTimeUnit =  convertCvParamValToUnits(cv.unit);
                            if (this.currentSpectrum.ScanStartTimeUnit == Units.Second) {
                                this.currentSpectrum.ScanStartTime = this.currentSpectrum.ScanStartTime / 60.0;
                                this.currentSpectrum.ScanStartTimeUnit = Units.Minute;
                            }
                        }
                        else if (cv.paramType == CvParamTypes.IonMobilityDriftTime){
                            this.currentSpectrum.DriftTime = (double)cv.value;
                            this.currentSpectrum.DriftTimeUnit =  convertCvParamValToUnits(cv.unit);
                            this.IsIonMobilityData = true;
                        }
                    }}
                });
        }

        

        private void parseScanWindowList()
        {
            this.parserCommonMethod("scanWindowList", null,
                new Dictionary<string, Action>() { { "scanWindow", () => this.parseScanWindow() } });
        }

        private void parseScanWindow()
        {
            this.parserCommonMethod("scanWindow", null,
                new Dictionary<string, Action>() {
                    { "cvParam", () => {
                        CvParamValue cv = this.parseCvParam();
                        switch (cv.paramType) {
                            case CvParamTypes.ScanWindowLowerLimit: this.currentSpectrum.ScanWindowLowerLimit = (double)cv.value; break;
                            case CvParamTypes.ScanWindowUpperLimit: this.currentSpectrum.ScanWindowUpperLimit = (double)cv.value; break;
                        }
                    } },
                });

        } 
        #endregion

        #region /// parsing precursorList and related;
        private void parsePrecursorList()
        {
            /// may contain more than one, but...
            this.parserCommonMethod("precursorList", null,
                new Dictionary<string, Action>() { { "precursor", () => this.parsePrecursor() } });
            this.currentSpectrum.Precursor = this.currentPrecursor;
            this.currentPrecursor = null;
        }

        private void parsePrecursor()
        {
            this.currentPrecursor = new RAW_PrecursorIon();
            this.parserCommonMethod("precursor", null,
                new Dictionary<string, Action>() {
                    { "isolationWindow", () => this.parseIsolatedWindowForPrecursor() },
                    { "selectedIonList", () => this.parseSelectedIonList() },
                    { "activation", () => this.parseActivation() },
                });
        }

        private void parseIsolatedWindowForPrecursor()
        {
            this.parserCommonMethod("isolationWindow", null,
                new Dictionary<string, Action>() {
                    { "cvParam", () => {
                        CvParamValue cv = this.parseCvParam();
                        switch (cv.paramType) {
                            case CvParamTypes.IsolationWindowTargetMz: this.currentPrecursor.IsolationTargetMz = (double)cv.value; break;
                            case CvParamTypes.IsolationWindowLowerOffset: this.currentPrecursor.IsolationWindowLowerOffset = (double)cv.value; break;
                            case CvParamTypes.IsolationWindowUpperOffset: this.currentPrecursor.IsolationWindowUpperOffset = (double)cv.value; break;
                        }
                    } },
                });
        }

        private void parseSelectedIonList()
        {
            /// may contain more than one, but...
            this.parserCommonMethod("selectedIonList", null,
                new Dictionary<string, Action>() { { "selectedIon", () => this.parseSelectedIon() } });
        }

        private void parseSelectedIon()
        {
            this.parserCommonMethod("selectedIon", null,
                new Dictionary<string, Action>() {
                    { "cvParam", () => {
                        CvParamValue cv = this.parseCvParam();
                        switch (cv.paramType) {
                            case CvParamTypes.SelectedIonMz: this.currentPrecursor.SelectedIonMz = (double)cv.value; break;
                        }
                    } },
                });
        }

        private void parseActivation()
        {
            this.parserCommonMethod("activation", null,
                new Dictionary<string, Action>() {
                    { "cvParam", () => {
                        CvParamValue cv = this.parseCvParam();
                        switch (cv.paramType) {
                            case CvParamTypes.DissociationMethod: this.currentPrecursor.Dissociationmethod = convertCvParamValToDissociationMethods(cv.value); break;
                            case CvParamTypes.CollisionEnergy:
                                this.currentPrecursor.CollisionEnergy = (double)cv.value;
                                this.currentPrecursor.CollisionEnergyUnit = convertCvParamValToUnits(cv.unit);
                                break;
                        }
                    } },
                });
        }

       
        #endregion

        #region /// parsing productList and related;

        private void parseProductList()
        {
            /// may contain more than one, but...
            this.parserCommonMethod("productList", null,
                new Dictionary<string, Action>() { { "product", () => this.parseProduct() } });
            this.currentSpectrum.Product = this.currentProduct;
            this.currentProduct = null;
        }

        private void parseProduct()
        {
            this.currentProduct = new RAW_ProductIon();
            this.parserCommonMethod("product", null,
                new Dictionary<string, Action>() {
                    { "isolationWindow", () => this.parseIsolatedWindowForProduct() },
                    { "selectedIonList", () => this.parseSelectedIonList() },
                    { "activation", () => this.parseActivation() },
                });
        }

        private void parseIsolatedWindowForProduct()
        {
            this.parserCommonMethod("isolationWindow", null,
                new Dictionary<string, Action>() {
                    { "cvParam", () => {
                        CvParamValue cv = this.parseCvParam();
                        switch (cv.paramType) {
                            case CvParamTypes.IsolationWindowTargetMz: this.currentProduct.IsolationTargetMz = (double)cv.value; break;
                            case CvParamTypes.IsolationWindowLowerOffset: this.currentProduct.IsolationWindowLowerOffset = (double)cv.value; break;
                            case CvParamTypes.IsolationWindowUpperOffset: this.currentProduct.IsolationWindowUpperOffset = (double)cv.value; break;
                        }
                    } },
                });
        }

        #endregion

        #region /// parsing binaryDataArrayList
        private void parseBinaryDataArrayListForSpectrum()
        {
            if (this.currentSpectrum.DefaultArrayLength == 0) {
                this.currentSpectrum.Spectrum = new RAW_PeakElement[] { };
                this.currentSpectrum.LowestObservedMz = this.currentSpectrum.ScanWindowLowerLimit;
                this.currentSpectrum.HighestObservedMz = this.currentSpectrum.ScanWindowUpperLimit;
                this.currentSpectrum.MinIntensity = 0;
            }
            else {
                BinaryDataArrayConverter mzData = null, intensityData = null;
                this.parserCommonMethod("binaryDataArrayList", null,
                    new Dictionary<string, Action>() 
                {
                    {
                        "binaryDataArray", () => {
                            var dataConverter = BinaryDataArrayConverter.Convert(this.xmlRdr);
                            switch (dataConverter.ContentType){
                                case BinaryArrayContentType.MzArray: mzData = dataConverter; break;
                                case BinaryArrayContentType.IntensityArray: intensityData = dataConverter; break;
                            }
                        }
                    }
                });

                if (mzData == null) {
                    Console.WriteLine("binaryDataArray for m/z is missing");
                    mzData = new BinaryDataArrayConverter();
                    // throw new ApplicationException("binaryDataArray for m/z is missing");
                }
                if (intensityData == null) {
                    Console.WriteLine("binaryDataArray for intensity is missing");
                    intensityData = new BinaryDataArrayConverter();
                    //throw new ApplicationException("binaryDataArray for intensity is missing");
                }
                if (mzData.ValueArray.Length != intensityData.ValueArray.Length) {
                    Console.WriteLine("Length of binaryDataArray for m/z and intensity mismatched");
                    mzData = new BinaryDataArrayConverter();
                    intensityData = new BinaryDataArrayConverter();
                    //throw new ApplicationException("Length of binaryDataArray for m/z and intensity mismatched");
                }

                if (mzData.ValueArray.Length == 0) {
                    this.currentSpectrum.DefaultArrayLength = 0;
                    this.currentSpectrum.Spectrum = new RAW_PeakElement[] { };
                    this.currentSpectrum.MinIntensity = 0;
                    this.currentSpectrum.LowestObservedMz = this.currentSpectrum.ScanWindowLowerLimit;
                    this.currentSpectrum.HighestObservedMz = this.currentSpectrum.ScanWindowUpperLimit;
                }
                else {

                    var arrayLength = mzData.ValueArray.Length;
                    var minIntensity = double.MaxValue;
                    var maxIntensity = double.MinValue;
                    var lowestMz = double.MaxValue;
                    var highestMz = double.MinValue;

                    var lastMz = mzData.ValueType == BinaryArrayValueType.Single ? (double)((float)mzData.ValueArray.GetValue(mzData.ValueArray.Length - 1)) : (double)mzData.ValueArray.GetValue(mzData.ValueArray.Length - 1);
                    if ((int)lastMz == 0) {
                        arrayLength = arrayLength - 1;
                    }

                    if (arrayLength == 0) {
                        this.currentSpectrum.Spectrum = new RAW_PeakElement[] { };
                        this.currentSpectrum.DefaultArrayLength = 0;
                        this.currentSpectrum.MinIntensity = 0;
                        this.currentSpectrum.LowestObservedMz = this.currentSpectrum.ScanWindowLowerLimit;
                        this.currentSpectrum.HighestObservedMz = this.currentSpectrum.ScanWindowUpperLimit;
                    }
                    else {
                        this.currentSpectrum.Spectrum = new RAW_PeakElement[arrayLength];
                        for (int i = 0; i < arrayLength; i++) {
                            this.currentSpectrum.Spectrum[i].Mz = mzData.ValueType == BinaryArrayValueType.Single ? (double)((float)mzData.ValueArray.GetValue(i)) : (double)mzData.ValueArray.GetValue(i);
                            this.currentSpectrum.Spectrum[i].Intensity = intensityData.ValueType == BinaryArrayValueType.Single ? (double)((float)intensityData.ValueArray.GetValue(i)) : (double)intensityData.ValueArray.GetValue(i);
                            if (this.currentSpectrum.Spectrum[i].Intensity < minIntensity) minIntensity = this.currentSpectrum.Spectrum[i].Intensity;
                            if (this.currentSpectrum.Spectrum[i].Intensity > maxIntensity) maxIntensity = this.currentSpectrum.Spectrum[i].Intensity;
                            if (this.currentSpectrum.Spectrum[i].Mz < lowestMz) lowestMz = this.currentSpectrum.Spectrum[i].Mz;
                            if (this.currentSpectrum.Spectrum[i].Mz > highestMz) highestMz = this.currentSpectrum.Spectrum[i].Mz;
                        }
                        this.currentSpectrum.DefaultArrayLength = arrayLength;
                        this.currentSpectrum.MinIntensity = minIntensity;
                        this.currentSpectrum.LowestObservedMz = lowestMz;
                        this.currentSpectrum.HighestObservedMz = highestMz;
                        this.currentSpectrum.BasePeakMz = highestMz;
                        this.currentSpectrum.BasePeakIntensity = maxIntensity;
                    }
                }
            }
        }
        #endregion
    }

}
