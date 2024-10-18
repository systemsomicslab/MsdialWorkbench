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
            var testfile = @"Z:\Workspaces\Nishida\massqltest\2022_05_11_06_06_41.mdproject";
            var exporter = new ExporterTest();
            var storage = await exporter.LoadProjectFromPathAsync(testfile);
            var param = storage.Parameter;

            var analysisFile = storage.AnalysisFiles[0];
            var alignmentFile = storage.AlignmentFiles.Last();

            // test for single analysis file
            var chromPeakFeatures = MessagePackHandler.LoadFromFile<List<ChromatogramPeakFeature>>(analysisFile.PeakAreaBeanInformationFilePath);

            // test for alignment result file
            var alignContainer = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(alignmentFile.FilePath);
            var alignSpotFeatures = alignContainer.AlignmentSpotProperties;

            //var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20MS2PROD=226.18:TOLERANCEPPM=5";
            //var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20MS2PROD=(100%20OR%20104):TOLERANCEPPM=5";
            //var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20MS2PROD=226.18%20AND%20MS2PROD=240.18:TOLERANCEPPM=5";
            var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20MS2PROD=660.2:TOLERANCEMZ=0.1%20AND%20MS2PROD=468.2:TOLERANCEMZ=0.1";
            //var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20MS2PROD=660.2:TOLERANCEMZ=0.1";
            //var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20RTMIN=10%20AND%20MS2PROD=660.2:TOLERANCEMZ=0.1";
            //var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20MS2NL=163";
            //var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20MS2PROD=660.2:TOLERANCEMZ=0.1%20AND%20MS2PROD=468.2:TOLERANCEMZ=0.1";

            var req = WebRequest.Create(query);
            var res = req.GetResponse();
            var resStream = res.GetResponseStream();
            var isAlignmentResultTargeted = true;

            MassQL result = null;
            using (var sr = new StreamReader(resStream)) {
                result = JsonConvert.DeserializeObject<MassQL>(sr.ReadToEnd());
            }
            //Console.WriteLine(result.Conditions[0].qualifiers.type);
            //Console.WriteLine();
      

            var massQLParams = new List<PeakFeatureSearchValue>();
            if (result.querytype.function == "functionscaninfo") {
                var searchLevel = PeakFeatureQueryLevel.MS2;
                if (result.querytype.datatype == "datams1data") {
                    searchLevel = PeakFeatureQueryLevel.MS1;
                }
                foreach (var condition in result.conditions) {                    
                    foreach (var mass in condition.value) {
                        var searchValue = new PeakFeatureSearchValue() { PeakFeatureQueryLevel = searchLevel };
                        searchValue.Mass = mass;
                        if (condition.qualifiers != null) {
                            if (condition.qualifiers.qualifierppmtolerance != null) {
                                searchValue.MassTolerance = MolecularFormulaUtility.ConvertPpmToMassAccuracy(condition.value[0], condition.qualifiers.qualifierppmtolerance.value);
                            }
                            if (condition.qualifiers.qualifiermztolerance != null) {
                                searchValue.MassTolerance = condition.qualifiers.qualifiermztolerance.value;
                            }
                        }
                        if (condition.type == "ms2neutrallosscondition") {
                            searchValue.PeakFeatureSearchType = PeakFeatureSearchType.NeutralLoss;
                        }
                        massQLParams.Add(searchValue);
                    }                    
                }
            }

            param.FragmentSearchSettingValues = massQLParams;

            if (massQLParams.Count > 1) {
                param.AndOrAtFragmentSearch = Common.Enum.AndOr.AND;
            }
            if (isAlignmentResultTargeted) {
                FragmentSearcher.Search(alignSpotFeatures.ToList(), new MsdialCore.MSDec.MSDecLoader(alignmentFile.SpectraFilePath, new List<string>(0)), param);
            }
            else {
                FragmentSearcher.Search(chromPeakFeatures, new MsdialCore.MSDec.MSDecLoader(analysisFile.DeconvolutionFilePath, new List<string>(0)), param);
            }

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
