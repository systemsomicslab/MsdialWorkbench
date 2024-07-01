using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Mathematics.Statistics;
using CompMs.CommonMVVM;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.Model.Statistics {
    internal sealed class MultivariateAnalysisSettingModel : DisposableModelBase {
        private readonly ParameterBase _parameter;
        private readonly ReadOnlyObservableCollection<AlignmentSpotPropertyModel> _spotprops;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly List<AnalysisFileBean> _analysisfiles;
        private readonly IObservable<KeyBrushMapper<string>> _brushmaps;

        public MultivariateAnalysisSettingModel(ParameterBase parameter,
           ReadOnlyObservableCollection<AlignmentSpotPropertyModel> spotprops,
           IMatchResultEvaluator<MsScanMatchResult> evaluator,
           List<AnalysisFileBean> analysisfiles,
           IObservable<KeyBrushMapper<string>> brushmaps
           ) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _spotprops = spotprops ?? throw new ArgumentNullException(nameof(spotprops));
            _analysisfiles = analysisfiles ?? throw new ArgumentNullException(nameof(analysisfiles));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _brushmaps = brushmaps ?? throw new ArgumentNullException(nameof(brushmaps));
            _maxPcNumber = 5;
        }

        public int MaxPcNumber {
            get => _maxPcNumber;
            set => SetProperty(ref _maxPcNumber, value);
        }
        private int _maxPcNumber;

        public bool IsAutoFit {
            get => _isAutoFit;
            set => SetProperty(ref _isAutoFit, value);
        }
        private bool _isAutoFit;

        public ScaleMethod ScaleMethod {
            get => _scaleMethod;
            set => SetProperty(ref _scaleMethod, value);
        }
        private ScaleMethod _scaleMethod;

        public TransformMethod TransformMethod {
            get => _transformMethod;
            set => SetProperty(ref _transformMethod, value);
        }
        private TransformMethod _transformMethod;

        public MultivariateAnalysisOption MultivariateAnalysisOption {
            get => _multivariateAnalysisOption;
            set => SetProperty(ref _multivariateAnalysisOption, value);
        }
        private MultivariateAnalysisOption _multivariateAnalysisOption;

        public bool IsIdentifiedImportedInStatistics {
            get => _isIdentifiedImportedInStatistics;
            set => SetProperty(ref _isIdentifiedImportedInStatistics, value);
        }
        private bool _isIdentifiedImportedInStatistics;

        public bool IsAnnotatedImportedInStatistics {
            get => _isAnnotatedImportedInStatistics;
            set => SetProperty(ref _isAnnotatedImportedInStatistics, value);
        }
        private bool _isAnnotatedImportedInStatistics;

        public bool IsUnknownImportedInStatistics {
            get => _isUnknownImportedInStatistics;
            set => SetProperty(ref _isUnknownImportedInStatistics, value);
        }
        private bool _isUnknownImportedInStatistics;

        public PCAPLSResultModel? PCAPLSResultModel {
            get => _pcaplsResultModel;
            set => SetProperty(ref _pcaplsResultModel, value);
        }
        private PCAPLSResultModel? _pcaplsResultModel;

        public MultivariateAnalysisResult? HCAResult {
            get => _hCAResult;
            set => SetProperty(ref _hCAResult, value);
        }
        private MultivariateAnalysisResult? _hCAResult;

        public AnalysisFileBean[] IncludedFiles => _analysisfiles.Where(file => file.AnalysisFileIncluded).ToArray();

        public void ExecutePCA() {
            if (!SelectedSomeMetaboliteSelectionOptions()) {
                MessageBox.Show("Please select at least one metabolite selection option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (MaxPcNumber <= 0) {
                MessageBox.Show("Component number should be a positive integer value.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            SetParameters();
            var observableSpots = new ObservableCollection<AlignmentSpotPropertyModel>();
            var analysisFiles = IncludedFiles;
            var result = StatisticsObjectConverter.PrincipalComponentAnalysis(_analysisfiles, _spotprops, _parameter, _evaluator, ref observableSpots);
            if (result == null) {
                return;
            }

            if (PCAPLSResultModel is PCAPLSResultModel resultModel) {
                Disposables.Remove(resultModel);
                resultModel.Dispose();
            }
            PCAPLSResultModel = new PCAPLSResultModel(result, _parameter, observableSpots, analysisFiles, _brushmaps).AddTo(Disposables);
        }

        private bool SelectedSomeMetaboliteSelectionOptions() {
            return _isIdentifiedImportedInStatistics || _isAnnotatedImportedInStatistics || _isUnknownImportedInStatistics;
        }

        public void ExecuteHCA() {
            if (!SelectedSomeMetaboliteSelectionOptions()) {
                MessageBox.Show("Please select at least one metabolite selection option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetParameters();
            var observableSpots = new ObservableCollection<AlignmentSpotPropertyModel>();
            var result = StatisticsObjectConverter.HierarchicalClusteringAnalysis(_analysisfiles, _spotprops, _parameter, _evaluator, ref observableSpots);
            if (result is null) return;

            HCAResult = result;
        }

        public void ExecutePLS() {
            if (!SelectedSomeMetaboliteSelectionOptions()) {
                MessageBox.Show("Please select at least one metabolite selection option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (IsAutoFit == false && MaxPcNumber <= 0) {
                MessageBox.Show("For user-defined component calculations, a positive integer value should be added.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var analysisFiles = IncludedFiles;
            var yValues = analysisFiles.Select(file => file.ResponseVariable).ToList();

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

            SetParameters();
            var observableSpots = new ObservableCollection<AlignmentSpotPropertyModel>();
            var result = StatisticsObjectConverter.PartialLeastSquares(_analysisfiles, _spotprops, _parameter, _evaluator, ref observableSpots);
            if (result == null) return;
            if (PCAPLSResultModel is PCAPLSResultModel resultModel) {
                Disposables.Remove(resultModel);
                resultModel.Dispose();
            }
            PCAPLSResultModel = new PCAPLSResultModel(result, _parameter, observableSpots, analysisFiles, _brushmaps).AddTo(Disposables);
        }

        private void SetParameters() {
            var statsParam = _parameter.StatisticsBaseParam;
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
