using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Newtonsoft.Json;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;

namespace CompMs.App.Msdial.Model.Setting {
    internal sealed class MassqlSettingModel : BindableBase {
        private readonly IResultModel _model;
        private readonly AdvancedProcessOptionBaseParameter _parameter;

        public MassqlSettingModel(IResultModel model, AdvancedProcessOptionBaseParameter parameter) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            parameter.FragmentSearchSettingValues ??= new List<PeakFeatureSearchValue>();
        }

        public string Massql {
            get => _massql;
            set => SetProperty(ref _massql, value);
        }
        private string _massql = string.Empty;

        public void SendMassql() {
            var massql = "https://massql.gnps2.org/parse?query=" + Massql;
            var req = WebRequest.Create(massql);

            try {
                var res = req.GetResponse();
                var resStream = res.GetResponseStream();
                List<MassQL>? results = null;
                using (var sr = new StreamReader(resStream)) {
                    var json = sr.ReadToEnd();
                    try {
                        if (json.TrimStart().StartsWith("[")) {
                            results = JsonConvert.DeserializeObject<List<MassQL>>(json);
                        }
                        else {
                            MassQL? result_t = JsonConvert.DeserializeObject<MassQL>(json);
                            results = result_t is not null ? new List<MassQL> { result_t } : null;
                        }
                    }
                    catch (JsonReaderException ex) {
                        System.Diagnostics.Debug.WriteLine("MassQL JSON parse error: " + ex.Message);
                        results = null;
                    }
                    catch (JsonSerializationException ex) {
                        System.Diagnostics.Debug.WriteLine("MassQL JSON parse error: " + ex.Message);
                        results = null;
                    }
                }

                if (results is null || results.Count == 0) {
                    return;
                }
                var result = results[0];
                var massQLParams = new List<PeakFeatureSearchValue>();
                if (result?.querytype?.function == "functionscaninfo") {
                    var searchLevel = PeakFeatureQueryLevel.MS2;
                    if (result.querytype.datatype == "datams1data") {
                        searchLevel = PeakFeatureQueryLevel.MS1;
                    }
                    foreach (var condition in result.conditions.OrEmptyIfNull()) {
                        foreach (var mass in condition.value.OrEmptyIfNull()) {
                            var searchValue = new PeakFeatureSearchValue() {
                                PeakFeatureQueryLevel = searchLevel,
                                Mass = mass,
                                MassTolerance = 0.05,
                                PeakFeatureSearchType = condition.type?.ToLowerInvariant().Contains("neutralloss") == true ? PeakFeatureSearchType.NeutralLoss : PeakFeatureSearchType.ProductIon,
                                ConditionType = condition.conditiontype ?? string.Empty
                            };

                            if (condition.qualifiers != null) {
                                if (condition.qualifiers.qualifierppmtolerance != null) {
                                    searchValue.MassTolerance = MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, condition.qualifiers.qualifierppmtolerance.value);
                                }
                                if (condition.qualifiers.qualifiermztolerance != null) {
                                    searchValue.MassTolerance = condition.qualifiers.qualifiermztolerance.value;
                                }
                                if (condition.qualifiers.qualifierintensity != null) {
                                    searchValue.AbsoluteIntensityCutoff = condition.qualifiers.qualifierintensity.value;
                                }
                                if (condition.qualifiers.qualifierintensitypercent != null) {
                                    searchValue.RelativeIntensityCutoff = condition.qualifiers.qualifierintensitypercent.value;
                                }
                                if (condition.qualifiers.qualifiertimemin != null) {
                                    searchValue.TimeMin = condition.qualifiers.qualifiertimemin.value;
                                }
                                if (condition.qualifiers.qualifiertimemax != null) {
                                    searchValue.TimeMax = condition.qualifiers.qualifiertimemax.value;
                                }
                                if (condition.qualifiers.qualifierdrifttime != null) {
                                    var center = condition.qualifiers.qualifierdrifttime.value;
                                    var window = condition.qualifiers.qualifierdrifttime.window;
                                    searchValue.MobilityMin = center - window;
                                    searchValue.MobilityMax = center + window;
                                }
                                if (condition.qualifiers.qualifiercharge != null) {
                                    searchValue.Charge = (int)condition.qualifiers.qualifiercharge.value;
                                }
                            }

                            massQLParams.Add(searchValue);
                        }
                    }
                }

                _parameter.FragmentSearchSettingValues = massQLParams;
                if (_parameter.FragmentSearchSettingValues.Count > 1) {
                    _parameter.AndOrAtFragmentSearch = AndOr.AND;
                }
                _model.SearchFragment();
            }
            catch (WebException) {
                var request = new ErrorMessageBoxRequest {
                    Caption = "MassQL query error",
                    Content = "Your MassQL query was not processed well. The query itself was invalid or we could not connect to the MassQL query parser API. MS-DIAL's MASSQL searcher requires a network connection to connect to MassQL's parser Web API.",
                    ButtonType = System.Windows.MessageBoxButton.OK,
                };
                MessageBroker.Default.Publish(request);
            }
        }
    }

    [DataContract]
    public class MassQL {
        [DataMember(Name = "querytype")]
        public QueryType? querytype { get; set; }
        [DataMember(Name = "conditions")]
        public List<Condition>? conditions { get; set; }
        [DataMember(Name = "query")]
        public string? query { get; set; }
    }

    [DataContract]
    public class QueryType {
        [DataMember(Name = "function")]
        public string? function { get; set; }
        [DataMember(Name = "datatype")]
        public string? datatype { get; set; }
    }

    [DataContract]
    public class Condition {
        [DataMember(Name = "type")]
        public string? type { get; set; }
        [DataMember(Name = "value")]
        public List<double>? value { get; set; }
        [DataMember(Name = "conditiontype")]
        public string? conditiontype { get; set; }
        [DataMember(Name = "qualifiers")]
        public Qualifiers? qualifiers { get; set; }
    }

    [DataContract]
    public class Qualifiers {
        [DataMember(Name = "type")]
        public string? type { get; set; }
        [DataMember(Name = "qualifierppmtolerance")]
        public QualifierValue? qualifierppmtolerance { get; set; }
        [DataMember(Name = "qualifiermztolerance")]
        public QualifierValue? qualifiermztolerance { get; set; }
        [DataMember(Name = "qualifierintensity")]
        public QualifierValue? qualifierintensity { get; set; }
        [DataMember(Name = "qualifierintensitypercent")]
        public QualifierValue? qualifierintensitypercent { get; set; }
        [DataMember(Name = "qualifiertimemin")]
        public QualifierValue? qualifiertimemin { get; set; }
        [DataMember(Name = "qualifiertimemax")]
        public QualifierValue? qualifiertimemax { get; set; }
        [DataMember(Name = "qualifierdrifttime")]
        public QualifierDriftTime? qualifierdrifttime { get; set; }
        [DataMember(Name = "qualifiercharge")]
        public QualifierValue? qualifiercharge { get; set; }
    }

    [DataContract]
    public class QualifierValue {
        [DataMember(Name = "name")]
        public string? name { get; set; }
        [DataMember(Name = "unit")]
        public string? unit { get; set; }
        [DataMember(Name = "value")]
        public double value { get; set; }
    }

    [DataContract]
    public class QualifierDriftTime {
        [DataMember(Name = "value")]
        public double value { get; set; }
        [DataMember(Name = "window")]
        public double window { get; set; }
    }
}
