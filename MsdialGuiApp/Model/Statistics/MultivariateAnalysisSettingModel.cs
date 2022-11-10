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
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Statistics {
    internal class MultivariateAnalysisSettingModel : BindableBase {
        private readonly ParameterBase _parameter;
        private readonly ObservableCollection<AlignmentSpotPropertyModel> _spotprops;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly List<AnalysisFileBean> _analysisfiles;
        private readonly IObservable<KeyBrushMapper<string>> _brushmaps;

        public MultivariateAnalysisSettingModel(ParameterBase parameter,
           ObservableCollection<AlignmentSpotPropertyModel> spotprops,
           IMatchResultEvaluator<MsScanMatchResult> evaluator,
           List<AnalysisFileBean> analysisfiles,
           IObservable<KeyBrushMapper<string>> brushmaps
           ) {
            _parameter = parameter ?? throw new System.ArgumentNullException(nameof(parameter));
            _spotprops = spotprops ?? throw new System.ArgumentNullException(nameof(spotprops));
            _analysisfiles = analysisfiles ?? throw new System.ArgumentNullException(nameof(analysisfiles));
            _evaluator = evaluator ?? throw new System.ArgumentNullException(nameof(evaluator));
            _brushmaps = brushmaps ?? throw new System.ArgumentNullException(nameof(brushmaps));
            maxPcNumber = 5;
        }

        public int MaxPcNumber {
            get => maxPcNumber;
            set => SetProperty(ref maxPcNumber, value);
        }
        private int maxPcNumber;

        public bool IsAutoFit {
            get => isAutoFit;
            set => SetProperty(ref isAutoFit, value);
        }
        private bool isAutoFit;

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

        public MultivariateAnalysisOption MultivariateAnalysisOption {
            get => multivariateAnalysisOption;
            set => SetProperty(ref multivariateAnalysisOption, value);
        }
        private MultivariateAnalysisOption multivariateAnalysisOption;

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

        public PCAPLSResultModel PCAPLSResultModel {
            get => _pcaplsResultModel;
            set => SetProperty(ref _pcaplsResultModel, value);
        }
        private PCAPLSResultModel _pcaplsResultModel;

        public MultivariateAnalysisResult HCAResult { get; set; }

        public void ExecutePCA() {
            if (!variableChecker()) return;
            if (MaxPcNumber <= 0) {
                MessageBox.Show("Component number should be a positive integer value.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            setParameters();
            var observableSpots = new ObservableCollection<AlignmentSpotPropertyModel>();
            var result = StatisticsObjectConverter.PrincipalComponentAnalysis(_analysisfiles, _spotprops, _parameter, _evaluator, ref observableSpots);
            if (result == null) return;
            PCAPLSResultModel = new PCAPLSResultModel(result, _parameter, observableSpots, _analysisfiles, _brushmaps);
        }

        private bool variableChecker() {
            if (isIdentifiedImportedInStatistics == false &&
                isAnnotatedImportedInStatistics == false &&
                isUnknownImportedInStatistics == false) {
                MessageBox.Show("Please select at least one metabolite selection option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else {
                return true;
            }
        }

        public void ExecuteHCA() {
            if (!variableChecker()) return;

            setParameters();
            var observableSpots = new ObservableCollection<AlignmentSpotPropertyModel>();
            var result = StatisticsObjectConverter.HierarchicalClusteringAnalysis(_analysisfiles, _spotprops, _parameter, _evaluator, ref observableSpots);
            if (result == null) return;

            HCAResult = result;
        }

        public void ExecutePLS() {
            if (!variableChecker()) return;
            if (IsAutoFit == false && MaxPcNumber <= 0) {
                MessageBox.Show("For user-defined component calculations, a positive integer value should be added.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var yValues = _analysisfiles.
                Where(n => n.AnalysisFileIncluded == true).
                Select(n => n.ResponseVariable).ToList();

            // all same value check
            var isAllSame = true;
            for (int i = 1; i < yValues.Count; i++) {
                if (yValues[0] != yValues[i]) {
                    isAllSame = false; break;
                }
            }
            if (isAllSame) {
                MessageBox.Show("All of Y values is same. Please set Y values at file property option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var zeroValueCount = 0;
            if (MultivariateAnalysisOption == MultivariateAnalysisOption.Plsda || MultivariateAnalysisOption == MultivariateAnalysisOption.Oplsda) {
                for (int i = 0; i < yValues.Count; i++) {
                    if (yValues[i] == 0.0) {
                        zeroValueCount++;
                    }
                }
                if (zeroValueCount == 0 || zeroValueCount == yValues.Count) {
                    MessageBox.Show("Set zero values correctly for (O)PLS-DA.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            setParameters();
            var observableSpots = new ObservableCollection<AlignmentSpotPropertyModel>();
            var result = StatisticsObjectConverter.PartialLeastSquares(_analysisfiles, _spotprops, _parameter, _evaluator, ref observableSpots);
            if (result == null) return;
            PCAPLSResultModel = new PCAPLSResultModel(result, _parameter, observableSpots, _analysisfiles, _brushmaps);
        }

        private void setParameters() {
            var statsParam = this._parameter.StatisticsBaseParam;
            statsParam.MultivariateAnalysisOption = MultivariateAnalysisOption;
            statsParam.MaxComponent = MaxPcNumber;
            statsParam.Scale = ScaleMethod;
            statsParam.Transform = TransformMethod;
            statsParam.IsIdentifiedImportedInStatistics = IsIdentifiedImportedInStatistics;
            statsParam.IsAnnotatedImportedInStatistics = IsAnnotatedImportedInStatistics;
            statsParam.IsUnknownImportedInStatistics = IsUnknownImportedInStatistics;
        }
    }
}
