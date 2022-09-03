using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Mathematics.Statistics;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class PcaSettingModel : BindableBase
    {
        private readonly ParameterBase _parameter;
        private readonly ObservableCollection<AlignmentSpotPropertyModel> _spotprops;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;

        public PcaSettingModel(ParameterBase parameter, ObservableCollection<AlignmentSpotPropertyModel> spotprops, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            _parameter = parameter ?? throw new System.ArgumentNullException(nameof(parameter));
            _spotprops = spotprops ?? throw new System.ArgumentNullException(nameof(spotprops));
            _evaluator = evaluator ?? throw new System.ArgumentNullException(nameof(evaluator));
        }

        public int MaxPcNumber {
            get => maxPcNumber;
            set => SetProperty(ref maxPcNumber, value);
        }
        private int maxPcNumber;
        public bool IsIdentifiedImportedInStatistics {
            get => isIdentifiedImportedInStatistics;
            set => SetProperty(ref isIdentifiedImportedInStatistics, value);
        }
        private bool isIdentifiedImportedInStatistics;

        public bool IsAnnotatedImportedInStatistics {
            get => isAnnotatedImportedInStatistics;
            set => SetProperty(ref isAnnotatedImportedInStatistics, value);
        }
        private bool isAnnotatedImportedInStatistics;

        public bool IsUnknownImportedInStatistics {
            get => isUnknownImportedInStatistics;
            set => SetProperty(ref isUnknownImportedInStatistics, value);
        }
        private bool isUnknownImportedInStatistics;

        public PcaResultModel PcaResultModel {
            get => _pcaResultModel;
            set => SetProperty(ref _pcaResultModel, value);
        }
        private PcaResultModel _pcaResultModel;

        public void RunPca() {

            var statObj = new StatisticsObject()
            {
                //XDataMatrix = new double[_spotprops.Count, _parameter.FileID_AnalysisFileType.Keys.Count],
                XScaled = new double[_parameter.FileID_AnalysisFileType.Keys.Count, _spotprops.Count],
                XLabels = new ObservableCollection<string>(_spotprops.Select(prop => $@"ID: {prop.MasterAlignmentID}_{(string.IsNullOrEmpty(prop.Name) ? "Unknown" : prop.Name)}"))
            };

            //private Dictionary<int, string> ColumnIndex_MetaboliteName { get; set; } = null;

            int counterSample = 0;
            int counterMetabolite = 0;

            var metaboliteIDs = new ObservableCollection<int>();

            foreach (var spot in _spotprops) {
                if (isIdentifiedImportedInStatistics && _evaluator.IsReferenceMatched(spot.ScanMatchResult)) {
                    metaboliteIDs.Add(spot.MasterAlignmentID);
                }
                if (isAnnotatedImportedInStatistics && _evaluator.IsAnnotationSuggested(spot.ScanMatchResult)) {
                    metaboliteIDs.Add(spot.MasterAlignmentID);
                }
                if (isUnknownImportedInStatistics && !_evaluator.IsReferenceMatched(spot.ScanMatchResult) && !_evaluator.IsAnnotationSuggested(spot.ScanMatchResult)) {
                    metaboliteIDs.Add(spot.MasterAlignmentID);
                }
            }

            for (int i = 0; i < _parameter.FileID_AnalysisFileType.Keys.Count; i++) {
                counterMetabolite = 0;
                for (int j = 0; j < _spotprops.Count; j++) {
                    if (!metaboliteIDs.Contains(_spotprops[j].MasterAlignmentID)) continue;
                    var alignProp = _spotprops[j].AlignedPeakProperties;
                    statObj.XScaled[counterSample, counterMetabolite] = alignProp[i].NormalizedPeakHeight;
                    counterMetabolite++;
                }
                counterSample++;
            }

            //for (int i = 0; i < _spotprops.Count; i++) {
            //    var alignProp = _spotprops[i].AlignedPeakProperties;
            //    counterMetabolite = 0;
            //    for (int j = 0; j < _parameter.FileID_AnalysisFileType.Keys.Count; j++) {
            //        //counterMetabolite = 0;
            //        //Console.WriteLine("CounterSample" + counterSample);
            //        //Console.WriteLine("CounterMetabolite" + counterMetabolite);
            //        statObj.XDataMatrix[counterSample, counterMetabolite] = alignProp[j].NormalizedPeakHeight;
            //        //counterSample++;
            //    }                
            //    counterSample++;
            //}

            //foreach (var spot in _spotprops) {
            //    foreach(var peak in spot.AlignedPeakProperties) {
            //        statObj.XDataMatrix[peak.FileID, spot.MasterAlignmentID] = peak.NormalizedPeakHeight;
            //    }
            //}

            var pcaResult = StatisticsMathematics.PrincipalComponentAnalysis(statObj, MultivariateAnalysisOption.Pca, MaxPcNumber);
            PcaResultModel = new PcaResultModel(pcaResult);
        }
    }
}
