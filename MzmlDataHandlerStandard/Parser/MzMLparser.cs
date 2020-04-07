using Riken.Metabolomics.MzmlHandler.Parser;
using Riken.Metabolomics.RawData;
using Riken.Metabolomics.RawDataHandlerCommon.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Riken.Metabolomics.MzmlHandler.Parser
{
    public partial class MzmlReader
    {
        public string MzMLfilePath { get; private set; }
        public int SpectraCount { get; private set; }
        public MzMlDataFileContent FileContent { get; private set; }
        public List<RAW_SourceFileInfo> SourceFiles { get; private set; }
        public List<RAW_Sample> Samples { get; private set; }
        public DateTime StartTimeStamp { get; private set; }
        public List<RAW_Spectrum> SpectraList { get; private set; }
        public List<RAW_Spectrum> AccumulatedMs1SpectrumList { get; private set; }
        public List<RAW_Chromatogram> ChromatogramsList { get; private set; }
        public bool IsIonMobilityData { get; set; }
        public BackgroundWorker bgWorker { get; set; }
        public bool IsGuiProcess { get; set; }
        private XmlReader xmlRdr = null;
        //private BackgroundWorker bgWorker;

        #region
        public RAW_Measurement ReadMzml(string inputMzMLfilePath, int fileID, bool isGuiProcess, BackgroundWorker bgWorker = null)
        {
            this.SourceFiles = new List<RAW_SourceFileInfo>();
            this.Samples = new List<RAW_Sample>();
            this.StartTimeStamp = new DateTime();
            this.SpectraList = null;
            this.ChromatogramsList = null;
            this.IsIonMobilityData = false;
            this.bgWorker = bgWorker;
            this.IsGuiProcess = isGuiProcess;

            using (var fs = new FileStream(inputMzMLfilePath, FileMode.Open, FileAccess.Read))
            {
                using (var xmlRdr = new XmlTextReader(fs))
                {
                    this.xmlRdr = xmlRdr;
                    while (xmlRdr.Read())
                    {
                        if (xmlRdr.NodeType == XmlNodeType.Element)
                        {
                            switch (xmlRdr.Name)
                            {
                                case "cvList": break; // mandatory
                                case "fileDescription": this.parseFileDescription(); break; // mandatory
                                case "referenceableParamGroupList": break; // optional
                                case "sampleList": this.parseSampleList(); break; // optional
                                case "softwareList": break; // mandatory
                                case "scanSettingsList": break; // optional
                                case "instrumentConfigurationList": break; // mandatory
                                case "dataProcessingList": break; // mandatory
                                case "run": this.parseRun(); break; // mandatory
                            }
                        }
                    }
                }
            }
            if (this.IsIonMobilityData) {
                this.createAccumulatedSpectralData();
            }
            if (this.SourceFiles.Count == 0) {
                this.SourceFiles.Add(new RAW_SourceFileInfo() { Id = fileID.ToString(), Location = inputMzMLfilePath, Name = System.IO.Path.GetFileNameWithoutExtension(inputMzMLfilePath) });
            }
            if (this.Samples.Count == 0) {
                this.Samples.Add(new RAW_Sample() { Id = fileID.ToString(), Name = System.IO.Path.GetFileNameWithoutExtension(inputMzMLfilePath) });
            }

            return new RAW_Measurement() {
                SourceFileInfo = this.SourceFiles[0],
                Sample = this.Samples[0],
                SpectrumList = this.SpectraList,
                ChromatogramList = this.ChromatogramsList,
                AccumulatedSpectrumList = this.AccumulatedMs1SpectrumList,
            };
        }

        public RAW_Measurement ReadMzmlVer2(string inputMzMLfilePath, int fileID) {
            this.SourceFiles = new List<RAW_SourceFileInfo>();
            this.Samples = new List<RAW_Sample>();
            this.StartTimeStamp = new DateTime();
            this.SpectraList = null;
            this.ChromatogramsList = null;

            var settings = new XmlReaderSettings() { IgnoreWhitespace = true, IgnoreComments = true };

            using (var fs = new FileStream(inputMzMLfilePath, FileMode.Open, FileAccess.Read)) {
                using (var xmlRdr = XmlReader.Create(fs, settings)) {
                    this.xmlRdr = xmlRdr;
                    while (xmlRdr.Read()) {
                        if (xmlRdr.NodeType == XmlNodeType.Element) {
                            switch (xmlRdr.Name) {
                                case "cvList": break; // mandatory
                                case "fileDescription": this.parseFileDescription(); break; // mandatory
                                case "referenceableParamGroupList": break; // optional
                                case "sampleList": this.parseSampleList(); break; // optional
                                case "softwareList": break; // mandatory
                                case "scanSettingsList": break; // optional
                                case "instrumentConfigurationList": break; // mandatory
                                case "dataProcessingList": break; // mandatory
                                case "run": this.parseRun(); break; // mandatory
                            }
                        }
                    }
                }
            }
       
            if (this.SourceFiles.Count == 0) {
                this.SourceFiles.Add(new RAW_SourceFileInfo() { Id = fileID.ToString(), Location = inputMzMLfilePath, Name = System.IO.Path.GetFileNameWithoutExtension(inputMzMLfilePath) });
            }
            if (this.Samples.Count == 0) {
                this.Samples.Add(new RAW_Sample() { Id = fileID.ToString(), Name = System.IO.Path.GetFileNameWithoutExtension(inputMzMLfilePath) });
            }

            return new RAW_Measurement() {
                SourceFileInfo = this.SourceFiles[0],
                Sample = this.Samples[0],
                SpectrumList = this.SpectraList,
                AccumulatedSpectrumList = this.AccumulatedMs1SpectrumList,
                ChromatogramList = this.ChromatogramsList
            };
        }

        private void createAccumulatedSpectralData() {
            if (this.SpectraList == null || this.SpectraList.Count == 0) return;
            this.AccumulatedMs1SpectrumList = new List<RAW_Spectrum>();
            var lastSpec = this.SpectraList[0];
            var counter = 0;

            if (this.SpectraList != null && this.SpectraList.Count > 0) {
                this.SpectraList = this.SpectraList.OrderBy(n => n.ScanStartTime).ThenBy(n => n.DriftTime).ToList();

                lastSpec = this.SpectraList[0];
                counter = 0;

                var scanNumber = 0;
                var driftNumber = 0;

                foreach (var spec in this.SpectraList) {
                    spec.Index = counter;
                    spec.OriginalIndex = counter;
                    counter++;

                    if (lastSpec.ScanStartTime == spec.ScanStartTime) {
                        if (spec.Index != 0)
                            driftNumber++;
                        spec.ScanNumber = scanNumber;
                        spec.DriftScanNumber = driftNumber;
                    }
                    else {
                        driftNumber = 0;
                        scanNumber++;
                        lastSpec = spec;

                        spec.ScanNumber = scanNumber;
                        spec.DriftScanNumber = driftNumber;
                    }

                    //Console.WriteLine("scan num {0}, drift num {1}, spec {2}", spec.ScanNumber, spec.DriftScanNumber, spec.Spectrum.Length);
                }
            }

            //var aSpectrum = new double[3000000];
            var accumulatedMassBin = new Dictionary<int, double[]>();
            counter = 0;
            lastSpec = this.SpectraList[0];

            foreach (var spec in this.SpectraList.Where(n => n.MsLevel == 1)) {
                if (spec.ScanStartTime == lastSpec.ScanStartTime) {
                    var peaks = spec.Spectrum;
                    for (int i = 0; i < peaks.Length; i++) {
                        SpectrumParser.AddToMassBinDictionary(accumulatedMassBin, peaks[i].Mz, peaks[i].Intensity);
                        //aSpectrum[(int)(peaks[i].Mz * 1000)] += peaks[i].Intensity;
                    }
                }
                else {
                    var rawSpec = getAccumulatedSpecObject(lastSpec, accumulatedMassBin);
                    this.AccumulatedMs1SpectrumList.Add(rawSpec);
                    //aSpectrum = new double[3000000];
                    accumulatedMassBin = new Dictionary<int, double[]>();
                    counter++;
                    lastSpec = spec;
                }
            }
            // reminder
            var finalSpec = getAccumulatedSpecObject(lastSpec, accumulatedMassBin);
            this.AccumulatedMs1SpectrumList.Add(finalSpec);

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

                counter = 0;
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

        }

        private RAW_Spectrum getAccumulatedSpecObject(RAW_Spectrum spec, Dictionary<int, double[]> aSpectrum) {
            var rawSpec = new RAW_Spectrum() {
                ScanNumber = spec.ScanNumber,
                ScanStartTime = spec.ScanStartTime,
                ScanStartTimeUnit = spec.ScanStartTimeUnit,
                MsLevel = 1,
                ScanPolarity = spec.ScanPolarity,
                Precursor = null,
            };

            SpectrumParser.setSpectrumProperties(spec, aSpectrum);

            return spec;
        }

        private void parseRun()
        {
            this.parserCommonMethod(
                "run",
                new Dictionary<string, Action<string>>()
                {
                    { "startTimeStamp", (v) => {
                        DateTime dt;
                        if (DateTime.TryParse(v, out dt)) this.StartTimeStamp = dt;
                    }}
                },
                new Dictionary<string, Action>()
                {
                    { "spectrumList", () => this.parseSpectrumList() },
                    { "chromatogramList", () => this.parseChromatogramList() },
                });
        }

        private void parseSpectrumList()
        {
            this.SpectraList = new List<RAW_Spectrum>();
            this.parserCommonMethod(
                "spectrumList",
                new Dictionary<string, Action<string>>()
                {
                    { "count", (v) => {
                        int n;
                        if (int.TryParse(v, out n)) this.SpectraCount = n;
                    }}
                },
                new Dictionary<string, Action>() { { "spectrum", () => this.parseSpectrum() } });
        }

        private void parseChromatogramList()
        {
            this.ChromatogramsList = new List<RAW_Chromatogram>();
            this.parserCommonMethod("chromatogramList", null, new Dictionary<string, Action>() { { "chromatogram", () => this.parseChromatogram() } });
        }

        private void parseSampleList()
        {
            while (this.xmlRdr.Read())
            {
                if (this.xmlRdr.NodeType == XmlNodeType.Element)
                {
                    if (this.xmlRdr.Name == "sample")
                    {
                        var sample = new RAW_Sample();
                        while (this.xmlRdr.MoveToNextAttribute())
                        {
                            switch (this.xmlRdr.Name)
                            {
                                case "id": sample.Id = this.xmlRdr.Value; break;
                                case "name": sample.Name = this.xmlRdr.Value; break;
                            }
                        }
                        this.Samples.Add(sample);
                    }
                }
                else if (this.xmlRdr.NodeType == XmlNodeType.EndElement)
                {
                    if (this.xmlRdr.Name == "sampleList") return;
                }
            }
        }

        private void parseFileDescription()
        {
            var isSourceFileList = false;
            while (this.xmlRdr.Read())
            {
                if (this.xmlRdr.NodeType == XmlNodeType.Element)
                {
                    switch (this.xmlRdr.Name)
                    {
                        case "fileContent": this.parseFileContent(); break;
                        case "sourceFileList": isSourceFileList = true; break;
                        case "sourceFile":
                            if (!isSourceFileList) break;
                            var sourceFileInfo = new RAW_SourceFileInfo();
                            while (this.xmlRdr.MoveToNextAttribute())
                            {
                                switch (this.xmlRdr.Name)
                                {
                                    case "id": sourceFileInfo.Id = this.xmlRdr.Value; break;
                                    case "name": sourceFileInfo.Name = this.xmlRdr.Value; break;
                                    case "location": sourceFileInfo.Location = this.xmlRdr.Value; break;
                                }
                            }
                            this.SourceFiles.Add(sourceFileInfo);
                            break;
                        case "contact": break;
                    }
                }
                else if (this.xmlRdr.NodeType == XmlNodeType.EndElement)
                {
                    switch (this.xmlRdr.Name)
                    {
                        case "sourceFileList": isSourceFileList = false; break;
                        case "fileDescription": return;
                    }
                }
            }
        }

        private void parseFileContent()
        {
            var elemActions = new Dictionary<string, Action>()
            {
                { 
                    "cvParam", () => 
                    {
                        var pv = this.parseCvParam();
                        if (pv.paramType == CvParamTypes.DataFileContent) 
                            this.FileContent = (MzMlDataFileContent)pv.value;
                    }
                },
            };
            this.parserCommonMethod("fileContent", null, elemActions);
        }

        #endregion

        private void parserCommonMethod(string returnElementName, Dictionary<string, Action<string>> attributeActions, Dictionary<string, Action> elementActions)
        {
            if (elementActions == null) throw new ArgumentNullException();
            if (attributeActions != null)
            {
                while (this.xmlRdr.MoveToNextAttribute())
                {
                    if (attributeActions.ContainsKey(this.xmlRdr.Name)) attributeActions[this.xmlRdr.Name](this.xmlRdr.Value);
                }
            }
            while (this.xmlRdr.Read())
            {
                if (this.xmlRdr.NodeType == XmlNodeType.Element)
                {
                    if (elementActions.ContainsKey(this.xmlRdr.Name))
                        elementActions[this.xmlRdr.Name]();
                }
                else if (this.xmlRdr.NodeType == XmlNodeType.EndElement)
                {
                    if (this.xmlRdr.Name == returnElementName)
                        return;
                }
            }
        }
    }
}
