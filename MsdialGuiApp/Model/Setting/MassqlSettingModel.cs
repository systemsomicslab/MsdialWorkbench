using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Setting {
    public class MassqlSettingModel : BindableBase {
        //private readonly ParameterBase _parameter;
        //private readonly bool _isAlignmentResultTargeted;
        //private readonly List<ChromatogramPeakFeature> _chromatogramPeakFeatures;
        //private readonly List<AlignmentSpotProperty> _alignmentSpotProperties;
        //private readonly MSDecLoader _mSDecLoader;

        private string massql;

        public MassqlSettingModel(ParameterBase param) {
            if (param.FragmentSearchSettingValues is null) {
                param.FragmentSearchSettingValues = new List<PeakFeatureSearchValue>();
            }
        }

        public string Massql {
            get => massql;
            set => SetProperty(ref massql, value);
        }

        public List<PeakFeatureSearchValue> SendMassql() {
            var massql = "https://msql.ucsd.edu/parse?query=" + Massql;
            var req = WebRequest.Create(massql);
            var res = req.GetResponse();
            var resStream = res.GetResponseStream();
            //var isAlignmentResultTargeted = true;

            MassQL result = null;
            using (var sr = new StreamReader(resStream)) {
                result = JsonConvert.DeserializeObject<MassQL>(sr.ReadToEnd());
            }

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
            return massQLParams;        
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
