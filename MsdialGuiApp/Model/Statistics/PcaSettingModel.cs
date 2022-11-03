using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Mathematics.Statistics;
using CompMs.CommonMVVM;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class PcaSettingModel : BindableBase
    {
        private readonly ParameterBase _parameter;
        private readonly IReadOnlyList<AlignmentSpotPropertyModel> _spotprops;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly IReadOnlyList<AnalysisFileBean> _analysisfiles;
        private readonly IObservable<KeyBrushMapper<string>> _brushmaps;

        public PcaSettingModel(ParameterBase parameter,
            IReadOnlyList<AlignmentSpotPropertyModel> spotprops,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            IReadOnlyList<AnalysisFileBean> analysisfiles,
            IObservable<KeyBrushMapper<string>> brushmaps
            ) {
            _parameter = parameter ?? throw new System.ArgumentNullException(nameof(parameter));
            _spotprops = spotprops ?? throw new System.ArgumentNullException(nameof(spotprops));
            _analysisfiles = analysisfiles ?? throw new System.ArgumentNullException(nameof(analysisfiles));
            _evaluator = evaluator ?? throw new System.ArgumentNullException(nameof(evaluator));
            _brushmaps = brushmaps ?? throw new System.ArgumentNullException(nameof(brushmaps));
            maxPcNumber = 5;
            if (_parameter.StatisticsBaseParam == null) {
                _parameter.StatisticsBaseParam = new StatisticsBaseParameter();
            }
        }

        public int MaxPcNumber {
            get => maxPcNumber;
            set => SetProperty(ref maxPcNumber, value);
        }
        private int maxPcNumber;
        public ScaleMethod ScaleMethod {
            get => scaleMethod;
            set => SetProperty(ref scaleMethod, value);
        }
        private ScaleMethod scaleMethod;

        public TransformMethod TransformMethod {
            get => transformMethod;
            set => SetProperty(ref transformMethod, value);
        }
        private TransformMethod transformMethod;

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

            var statsParam = this._parameter.StatisticsBaseParam;
            statsParam.MaxComponent = MaxPcNumber;
            statsParam.Scale = ScaleMethod;
            statsParam.Transform = TransformMethod;
            statsParam.IsIdentifiedImportedInStatistics = IsIdentifiedImportedInStatistics;
            statsParam.IsAnnotatedImportedInStatistics = IsAnnotatedImportedInStatistics;
            statsParam.IsUnknownImportedInStatistics = IsUnknownImportedInStatistics;

            var result = StatisticsObjectConverter.PrincipalComponentAnalysis(_analysisfiles, _spotprops, _parameter, _evaluator);


            //int counterSample = 0;
            //int counterMetabolite = 0;

            //var metaboliteIDs = new ObservableCollection<int>();
            var metaboliteSpotProps = new ObservableCollection<AlignmentSpotPropertyModel>();

            foreach (var spot in _spotprops) {
                if (isIdentifiedImportedInStatistics && _evaluator.IsReferenceMatched(spot.ScanMatchResult)) {
                    metaboliteSpotProps.Add(spot);
                }
                if (isAnnotatedImportedInStatistics && _evaluator.IsAnnotationSuggested(spot.ScanMatchResult)) {
                    metaboliteSpotProps.Add(spot);
                }
                if (isUnknownImportedInStatistics && !_evaluator.IsReferenceMatched(spot.ScanMatchResult) && !_evaluator.IsAnnotationSuggested(spot.ScanMatchResult)) {
                    metaboliteSpotProps.Add(spot);
                }
            }

            //var statObj = new StatisticsObject()
            //{
            //    XDataMatrix = new double[_parameter.FileID_AnalysisFileType.Keys.Count, metaboliteSpotProps.Count],
            //    XLabels = new ObservableCollection<string>(metaboliteSpotProps.Select(prop => $@"ID: {prop.MasterAlignmentID}_{(string.IsNullOrEmpty(prop.Name) ? "Unknown" : prop.Name)}")),
            //    YLabels = new ObservableCollection<string>(_analysisfiles.Select(file => file.AnalysisFileName)),
            //    YVariables = new[] { 0d, 0d, },
            //    Scale = ScaleMethod,
            //    Transform = TransformMethod,
            //};

            //for (int i = 0; i < _parameter.FileID_AnalysisFileType.Keys.Count; i++) {
            //    counterMetabolite = 0;
            //    for (int j = 0; j < metaboliteSpotProps.Count; j++) {
            //        if (!metaboliteIDs.Contains(metaboliteSpotProps[j].MasterAlignmentID)) continue;
            //        var alignProp = metaboliteSpotProps[j].AlignedPeakProperties;
            //        statObj.XDataMatrix[counterSample, counterMetabolite] = alignProp[i].NormalizedPeakHeight;
            //        counterMetabolite++;
            //    }
            //    counterSample++;
            //}
            //statObj.StatInitialization();

            //var pcaResult = StatisticsMathematics.PrincipalComponentAnalysis(statObj, MultivariateAnalysisOption.Pca, MaxPcNumber);
            PcaResultModel = new PcaResultModel(result, _parameter, metaboliteSpotProps, _analysisfiles, _brushmaps);
        }
    }
}
