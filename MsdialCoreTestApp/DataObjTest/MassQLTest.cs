using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using CompMs.App.MsdialConsole.Export;
using System.Linq;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Algorithm;
using CompMs.Common.FormulaGenerator.Function;

namespace CompMs.App.MsdialConsole.DataObjTest {
    public class MassQLTest {

        public MassQLTest() { }
        public async void Run() {

            //var testfile = @"E:\0_SourceCode\MsdialWorkbenchDemo\massql_demofiles\2022_05_02_05_47_01.mdproject";
            var testfile = @"Z:\Workspaces\Nishida\massql\2022_05_06_01_30_07.mdproject";
            var exporter = new ExporterTest();
            var storage = await exporter.LoadProjectFromPathAsync(testfile);

            var analysisFile = storage.AnalysisFiles[0];
            //var alignmentFile = storage.AlignmentFiles.Last();

            // test for single analysis file
            var chromPeakFeatures = MessagePackHandler.LoadFromFile<List<ChromatogramPeakFeature>>(analysisFile.PeakAreaBeanInformationFilePath);
            var chromDecResults = MsdecResultsReader.ReadMSDecResults(analysisFile.DeconvolutionFilePath, out _, out _);


            //var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20MS2PROD=226.18:TOLERANCEPPM=5";
            //var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20MS2PROD=660.2:TOLERANCEMZ=0.1";
            var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20MS2NL=163";
            var req = WebRequest.Create(query);
            var res = req.GetResponse();
            var resStream = res.GetResponseStream();

            MassQL result = null;
            using (var sr = new StreamReader(resStream)) {
                result = JsonConvert.DeserializeObject<MassQL>(sr.ReadToEnd());
            }
            //Console.WriteLine(result.Conditions[0].qualifiers.type);
            //Console.WriteLine();
            var param = storage.Parameter;
            //param.FragmentSearchSettingValues = result.Conditions[0].value;

            var features = new List<ChromatogramPeakFeature>();
            var massQLParams = new List<PeakFeatureSearchValue>();
            if (result.querytype.function == "functionscaninfo") {
                if (result.querytype.datatype == "datams2data") {
                    foreach (var feature in chromPeakFeatures) {
                        if (feature.PrecursorMz != null) {
                            features.Add(feature);
                        }
                    }
                }
                if (result.querytype.datatype == "datams1data") {
                    foreach (var feature in chromPeakFeatures) {
                        if (feature.PrecursorMz == null) {
                            features.Add(feature);
                        }
                    }
                }
                foreach (var condition in result.conditions) {
                    var searchValue = new PeakFeatureSearchValue();
                    searchValue.Mass = condition.value[0];
                    if (condition.qualifiers != null) {
                        if (condition.qualifiers.qualifierppmtolerance != null)
                        {
                            searchValue.MassTolerance = MolecularFormulaUtility.ConvertPpmToMassAccuracy(condition.value[0], condition.qualifiers.qualifierppmtolerance.value);
                        }
                        if (condition.qualifiers.qualifiermztolerance != null)
                        {
                            searchValue.TimeTolerance = condition.qualifiers.qualifiermztolerance.value;
                        }
                    }
                    if (condition.type == "ms2neutrallosscondition") {
                        searchValue.PeakFeatureSearchType = PeakFeatureSearchType.NeutralLoss;
                    }
                    massQLParams.Add(searchValue);
                }
            }

            param.FragmentSearchSettingValues = massQLParams;

            //FragmentSearcher.Search(chromPeakFeatures, new MsdialCore.MSDec.MSDecLoader(analysisFile.DeconvolutionFilePath), param);
            FragmentSearcher.Search(features, new MsdialCore.MSDec.MSDecLoader(analysisFile.DeconvolutionFilePath), param);

            //foreach (var peak in chromPeakFeatures) {
            //    var msdecID = peak.MSDecResultIdUsed6;
            //}

            // test for alignment result file
            //var alignContainer = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(alignmentFile.FilePath);
            //var alignSpotFeatures = alignContainer.AlignmentSpotProperties;
            //var alignDecResults = MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _);

        }

        [DataContract]
        public class MassQL {
            [DataMember(Name = "querytype")]
            public QueryType querytype { get; set; }
            [DataMember(Name = "conditions")]
            public List<Condition> conditions { get; set; }
            [DataMember(Name = "query")]
            public string query { get; set; }
        }

        [DataContract]
        public class QueryType {
            [DataMember(Name = "function")]
            public string function { get; set; }
            [DataMember(Name = "datatype")]
            public string datatype { get; set; }
        }

        [DataContract]
        public class Condition {
            [DataMember(Name = "type")]
            public string type { get; set; }
            [DataMember(Name = "value")]
            public List<double> value { get; set; }
            [DataMember(Name = "conditiontype")]
            public string conditiontype { get; set; }
            [DataMember(Name = "qualifiers")]
            public Qualifiers qualifiers { get; set; }
        }

        [DataContract]
        public class Qualifiers {
            [DataMember(Name = "type")]
            public string type { get; set; }
            [DataMember(Name = "qualifierppmtolerance")]
            public QualifierPpmTolerance qualifierppmtolerance { get; set; }
            [DataMember(Name = "qualifiermztolerance")]
            public QualifierMzTolerance qualifiermztolerance { get; set; }
        }

        [DataContract]
        public class QualifierPpmTolerance {
            [DataMember(Name = "name")]
            public string name { get; set; }
            [DataMember(Name = "unit")]
            public string unit { get; set; }
            [DataMember(Name = "value")]
            public double value { get; set; }
        }

        [DataContract]
        public class QualifierMzTolerance {
            [DataMember(Name = "name")]
            public string name { get; set; }
            [DataMember(Name = "unit")]
            public string unit { get; set; }
            [DataMember(Name = "value")]
            public double value { get; set; }
        }

    }
}
