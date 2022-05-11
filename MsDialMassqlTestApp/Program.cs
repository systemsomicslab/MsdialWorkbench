using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CompMs.App.MsDialMassqlTestApp {

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

    class Program {
        static void Main(string[] args) {
            var query = @"https://msql.ucsd.edu/parse?query=QUERY%20scaninfo(MS2DATA)%20WHERE%20MS2PROD=226.18:TOLERANCEPPM=5";
            var req = WebRequest.Create(query);
            var res = req.GetResponse();

            var resStream = res.GetResponseStream();
            var sr = new StreamReader(resStream);
            //Console.WriteLine(sr.ReadToEnd());
            var result = JsonConvert.DeserializeObject<MassQL>(sr.ReadToEnd());
            Console.WriteLine(result.Conditions[0].hoge[0]);
            Console.ReadKey();
        }
    }
}
