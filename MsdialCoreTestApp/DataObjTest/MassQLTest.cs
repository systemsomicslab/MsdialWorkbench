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

namespace CompMs.App.MsdialConsole.DataObjTest {
    public class MassQLTest {

        public MassQLTest() { }
        public async void Run() {

            var testfile = @"E:\0_SourceCode\MsdialWorkbenchDemo\massql_demofiles\2022_05_02_05_47_01.mdproject";
            var exporter = new ExporterTest();
            var storage = await exporter.LoadProjectFromPathAsync(testfile);

            var analysisFile = storage.AnalysisFiles[0];
            var alignmentFile = storage.AlignmentFiles.Last();

            var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20MS2PROD=91.05%20AND%20MS2PROD=105.07:TOLERANCEPPM=5";
            var req = WebRequest.Create(query);
            var res = req.GetResponse();
            var resStream = res.GetResponseStream();

            MassQL result = null;
            using (var sr = new StreamReader(resStream)) {
                result = JsonConvert.DeserializeObject<MassQL>(sr.ReadToEnd());
            }
            var param = storage.Parameter;

            var massQLParams = new List<PeakFeatureSearchValue>();

            // test for single analysis file
            var chromPeakFeatures = MessagePackHandler.LoadFromFile<List<ChromatogramPeakFeature>>(analysisFile.PeakAreaBeanInformationFilePath);
            var chromDecResults = MsdecResultsReader.ReadMSDecResults(analysisFile.DeconvolutionFilePath, out _, out _);

            foreach (var peak in chromPeakFeatures) {
                var msdecID = peak.MSDecResultIdUsed;
            }

            // test for alignment result file
            var alignContainer = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(alignmentFile.FilePath);
            var alignSpotFeatures = alignContainer.AlignmentSpotProperties;
            var alignDecResults = MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _);






           
            Console.WriteLine(result.ToString());
            Console.WriteLine();
        
        }


        [DataContract]
        public class MassQL {
            [DataMember(Name = "querytype")]
            public QueryType QueryType { get; set; }
            [DataMember(Name = "conditions")]
            public List<Condition> Conditions { get; set; }
            [DataMember(Name = "query")]
            public string query { get; set; }
        }

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
            public List<double> hoge { get; set; }
            [DataMember(Name = "conditiontype")]
            public string conditiontype { get; set; }
            [DataMember(Name = "qualifiers")]
            public Qualifiers qualifiers { get; set; }
        }
        public class Qualifiers {
            [DataMember(Name = "type")]
            public string type { get; set; }
            [DataMember(Name = "qualifierppmtolerance")]
            public QualifierPpmTolerance qualifierPpmTolerance { get; set; }
        }

        public class QualifierPpmTolerance {
            [DataMember(Name = "name")]
            public string name { get; set; }
            [DataMember(Name = "unit")]
            public string unit { get; set; }
            [DataMember(Name = "value")]
            public double value { get; set; }
        }
    }
}
