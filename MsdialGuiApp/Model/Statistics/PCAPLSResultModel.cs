using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.View.Statistics;
using CompMs.App.Msdial.ViewModel.Statistics;
using CompMs.Common.Enum;
using CompMs.Common.Mathematics.Statistics;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class ComponentLoadingModel : BindableBase {
        public ComponentLoadingModel(double[] loading, double[] oLoading, string label, AlignmentSpotPropertyModel spot) {
            Loading = loading ?? throw new ArgumentNullException(nameof(loading));
            OLoading = oLoading;
            Label = label;
            Spot = spot;
        }

        public double[] Loading { get; }
        public double[] OLoading { get; }
        public string Label { get; }
        public AlignmentSpotPropertyModel Spot { get; }
    }
    internal sealed class ComponentScoreModel : BindableBase {
        public ComponentScoreModel(double[] score, double[] oScore, string label, AnalysisFileBean bean)
        {
            Score = score ?? throw new ArgumentNullException(nameof(score));
            OScore = oScore;
            Label = label;
            Bean = bean;
        }

        public double[] Score { get; }
        public double[] OScore { get; }
        public string Label { get; }
        public AnalysisFileBean Bean { get; }
    }

    internal sealed class PCAPLSResultModel : BindableBase {
        private readonly MultivariateAnalysisResult _result;
        private readonly ParameterBase _parameter;
        private readonly ObservableCollection<AlignmentSpotPropertyModel> _spotprops;
        private readonly List<AnalysisFileBean> _analysisfiles;
        private readonly IObservable<KeyBrushMapper<string>> _classBrush;

        public PCAPLSResultModel(
            MultivariateAnalysisResult result,
            ParameterBase parameter,
            IReadOnlyList<AlignmentSpotPropertyModel> spotprops,
            IReadOnlyList<AnalysisFileBean> analysisfiles,
            IObservable<KeyBrushMapper<string>> brushmaps
            ) {

            _result = result ?? throw new ArgumentNullException(nameof(result));

            var statisticsObject = result.StatisticsObject;
            var option = result.MultivariateAnalysisOption;

            if (option == MultivariateAnalysisOption.Oplsr || option == MultivariateAnalysisOption.Oplsda) {
                Scores = new ObservableCollection<ComponentScoreModel>(
                statisticsObject.YLabels.Select((label, i) =>
                    new ComponentScoreModel(result.TPreds.Select(preds => preds[i]).ToArray(), result.ToPreds.Select(preds => preds[i]).ToArray(), label, analysisfiles[i])));

                Loadings = new ObservableCollection<ComponentLoadingModel>(
                statisticsObject.XLabels.Select((label, i) =>
                    new ComponentLoadingModel(result.PPreds.Select(preds => preds[i]).ToArray(), result.PoPreds.Select(preds => preds[i]).ToArray(), label, spotprops[i])));
            }
            else {
                Scores = new ObservableCollection<ComponentScoreModel>(
                statisticsObject.YLabels.Select((label, i) =>
                    new ComponentScoreModel(result.TPreds.Select(preds => preds[i]).ToArray(), null, label, analysisfiles[i])));

                Loadings = new ObservableCollection<ComponentLoadingModel>(
                statisticsObject.XLabels.Select((label, i) =>
                    new ComponentLoadingModel(result.PPreds.Select(preds => preds[i]).ToArray(), null, label, spotprops[i])));
            }
            
            LoadingAxises = result.PPreds
                .Select(pc_loadings => new Lazy<IAxisManager<double>>(() => new ContinuousAxisManager<double>(pc_loadings, new ConstantMargin(20))))
                .ToList().AsReadOnly();

            LoadingAbsoluteAxises = result.PPreds
                .Select(pc_loadings => new Lazy<IAxisManager<double>>(() => new AbsoluteAxisManager(new Range(0d, pc_loadings.DefaultIfEmpty().Max(Math.Abs)), new ConstantMargin(0, 10))))
                .ToList().AsReadOnly();

            ScoreAxises = result.TPreds
                .Select(pc_loadings => new Lazy<IAxisManager<double>>(() => new ContinuousAxisManager<double>(pc_loadings, new ConstantMargin(20))))
                .ToList().AsReadOnly();

            var pcAxises = new ObservableCollection<IAxisManager<string>>();
            for (int i = 0; i < NumberOfComponents; i++) {
                pcAxises.Add(new CategoryAxisManager<string>(Loadings.OrderByDescending(loading => Math.Abs(loading.Loading[i])).Select(loading => loading.Label).ToArray()));
            }
            PCAxises = pcAxises;

            if (option == MultivariateAnalysisOption.Oplsr || option == MultivariateAnalysisOption.Oplsda) {
                OLoadingAxises = result.PoPreds
                .Select(pc_loadings => new Lazy<IAxisManager<double>>(() => new ContinuousAxisManager<double>(pc_loadings, new ConstantMargin(20))))
                .ToList().AsReadOnly();

                OLoadingAbsoluteAxises = result.PoPreds
                    .Select(pc_loadings => new Lazy<IAxisManager<double>>(() => new AbsoluteAxisManager(new Range(0d, pc_loadings.DefaultIfEmpty().Max(Math.Abs)), new ConstantMargin(0, 10))))
                    .ToList().AsReadOnly();

                OScoreAxises = result.ToPreds
                    .Select(pc_loadings => new Lazy<IAxisManager<double>>(() => new ContinuousAxisManager<double>(pc_loadings, new ConstantMargin(20))))
                    .ToList().AsReadOnly();

                var opcAxises = new ObservableCollection<IAxisManager<string>>();
                for (int i = 0; i < NumberOfOComponents; i++) {
                    opcAxises.Add(new CategoryAxisManager<string>(Loadings.OrderByDescending(loading => Math.Abs(loading.OLoading[i])).Select(loading => loading.Label).ToArray()));
                }
                PCOAxises = opcAxises;
            }

            PointBrush = brushmaps.Select(bm => bm.Contramap((ComponentScoreViewModel csvm) => csvm.Model.Bean.AnalysisFileClass)).ToReactiveProperty();

            var ontology = new BrushMapData<ComponentLoadingViewModel>(
                new KeyBrushMapper<ComponentLoadingViewModel, string>(
                    ChemOntologyColor.Ontology2RgbaBrush,
                    loading => loading?.Model.Spot.Ontology ?? string.Empty,
                    Color.FromArgb(180, 181, 181, 181)),
                "Ontology");
            var amplitude = new BrushMapData<ComponentLoadingViewModel>(
                new DelegateBrushMapper<ComponentLoadingViewModel>(
                    loading => Color.FromArgb(
                        180,
                        (byte)(255 * loading.Model.Spot.innerModel.RelativeAmplitudeValue),
                        (byte)(255 * (1 - Math.Abs(loading.Model.Spot.innerModel.RelativeAmplitudeValue - 0.5))),
                        (byte)(255 - 255 * loading.Model.Spot.innerModel.RelativeAmplitudeValue)),
                    enableCache: true),
                "Amplitude");

            Brushes = new List<BrushMapData<ComponentLoadingViewModel>>
            {
                amplitude, ontology,
            };

            if (parameter.TargetOmics == TargetOmics.Lipidomics) {
                SelectedBrush = ontology;
            }
            else if (parameter.TargetOmics == TargetOmics.Proteomics || parameter.TargetOmics == TargetOmics.Metabolomics) {
                SelectedBrush = amplitude;
            }

            PosnegBrush = new DelegateBrushMapper<ComponentLoadingViewModel>(
                    loading => loading.ComponentX > 0 ? Colors.Red : Colors.Blue);

        }

        public ObservableCollection<ComponentLoadingModel> Loadings { get; }
        public ObservableCollection<ComponentScoreModel> Scores { get; }
        public ObservableCollection<IAxisManager<string>> PCAxises { get; }
        public ObservableCollection<IAxisManager<string>> PCOAxises { get; }
        public ReadOnlyCollection<Lazy<IAxisManager<double>>> LoadingAxises { get; }
        public ReadOnlyCollection<Lazy<IAxisManager<double>>> OLoadingAxises { get; }
        public ReadOnlyCollection<Lazy<IAxisManager<double>>> LoadingAbsoluteAxises { get; }
        public ReadOnlyCollection<Lazy<IAxisManager<double>>> OLoadingAbsoluteAxises { get; }
        public ReadOnlyCollection<Lazy<IAxisManager<double>>> ScoreAxises { get; }
        public ReadOnlyCollection<Lazy<IAxisManager<double>>> OScoreAxises { get; }
        public List<BrushMapData<ComponentLoadingViewModel>> Brushes { get; }

        public BrushMapData<ComponentLoadingViewModel> SelectedBrush {
            get => _selectedBrush;
            set => SetProperty(ref _selectedBrush, value);
        }
        private BrushMapData<ComponentLoadingViewModel> _selectedBrush;

        public IObservable<IBrushMapper<ComponentScoreViewModel>> PointBrush {
            get => _pointBrush;
            set => SetProperty(ref _pointBrush, value);
        }
        private IObservable<IBrushMapper<ComponentScoreViewModel>> _pointBrush;

        public IBrushMapper<ComponentLoadingViewModel> PosnegBrush {
            get => _posnegBrush;
            set => SetProperty(ref _posnegBrush, value);
        }
        private IBrushMapper<ComponentLoadingViewModel> _posnegBrush;

        public int NumberOfComponents => _result.PPreds.Count;
        public int NumberOfOComponents => _result.PoPreds.Count;

        public MultivariateAnalysisOption MultivariateAnalysisOption => _result.MultivariateAnalysisOption;

        public string ScorePlotTitle {
            get => scorePlotTitle;
            set => SetProperty(ref scorePlotTitle, value);
        }
        private string scorePlotTitle;
        public string LoadingPlotTitle {
            get => loadingPlotTitle;
            set => SetProperty(ref loadingPlotTitle, value);
        }
        private string loadingPlotTitle;

        public void ShowContributionPlot(Window owner) {
            if (_result.MultivariateAnalysisOption == MultivariateAnalysisOption.Pca) {
                var contributions = _result.Contributions;
                var items = new ObservableCollection<SimpleBarItem>();
                for (int i = 0; i < contributions.Count; i++) {
                    items.Add(new SimpleBarItem(i, "Component " + (i + 1), contributions[i], 0));
                }
                ShowBarChartUI(owner, items, "Component", "Contribution", "Contribution plot");
            }
            else if (_result.MultivariateAnalysisOption == MultivariateAnalysisOption.Oplsda || 
                _result.MultivariateAnalysisOption == MultivariateAnalysisOption.Oplsr) {
                var items = new ObservableCollection<SimpleBarItem>();
                var q2values = _result.Q2Cums;
                for (int i = 0; i < q2values.Count; i++) {
                    items.Add(new SimpleBarItem(i, "To " + (i), q2values[i], 0));
                }
                ShowBarChartUI(owner, items, "Latent variables", "Contribution", "Q2 plot");
            }
            else {
                var items = new ObservableCollection<SimpleBarItem>();
                var q2values = _result.Q2Cums;
                for (int i = 0; i < q2values.Count; i++) {
                    items.Add(new SimpleBarItem(i, "LV " + (i + 1), q2values[i], 0));
                }
                ShowBarChartUI(owner, items, "Latent variables", "Contribution", "Q2 plot");
            }
        }

        public void ShowVIPs(Window owner) {
            if (_result.MultivariateAnalysisOption == MultivariateAnalysisOption.Pca || _result.MultivariateAnalysisOption == MultivariateAnalysisOption.Hca) {
                return;
            }
            else {
                var xAxisTitle = "Metabolite name";
                var yAxisTitle = "Value";
                var graphTitle = "Variable importance for prediction (VIP)";

                var xAxisValues = _result.StatisticsObject.XLabels;
                var yAxisValues = _result.Vips;

                var brushes = convertRgbaToBrush(_result.StatisticsObject.YColors);
                var idValues = _result.StatisticsObject.XIndexes;
                var labels = _result.StatisticsObject.XLabels;

                var items = new ObservableCollection<SimpleBarItem>();
                for (int i = 0; i < labels.Count; i++) {
                    items.Add(new SimpleBarItem(idValues[i], labels[i], yAxisValues[i], 0));
                }
                ShowBarChartUI(owner, items, xAxisTitle, yAxisTitle, graphTitle);
            }
        }

        public void ShowPredVsExp(Window owner) {
            if (_result.MultivariateAnalysisOption == MultivariateAnalysisOption.Pca || _result.MultivariateAnalysisOption == MultivariateAnalysisOption.Hca) {
                return;
            }
            else {
                var xAxisTitle = "Experiment values";
                var yAxisTitle = "Predicted values";
                var graphTitle = "Predicted vs Experiment plot";

                var xAxisValues = _result.StatisticsObject.YVariables;
                var yAxisValues = _result.PredictedYs;

                var brushes = convertRgbaToBrush(_result.StatisticsObject.YColors);
                var idValues = _result.StatisticsObject.YIndexes;
                var labels = _result.StatisticsObject.YLabels;

                var items = new ObservableCollection<SimplePlotItem>();
                for (int i = 0; i < idValues.Count; i++) {
                    items.Add(new SimplePlotItem(idValues[i], labels[i], xAxisValues[i], yAxisValues[i], brushes[i]));
                }
                ShowScatterPlotChartUI(owner, items, xAxisTitle, yAxisTitle, graphTitle);
            }
        }

        public void ShowCoefficients(Window owner) {
            if (_result.MultivariateAnalysisOption == MultivariateAnalysisOption.Pca || _result.MultivariateAnalysisOption == MultivariateAnalysisOption.Hca) {
                return;
            }
            else {
                var xAxisTitle = "Metabolite name";
                var yAxisTitle = "Value";
                var graphTitle = "Coefficients";

                var yAxisValues = _result.Coefficients;

                var brushes = convertRgbaToBrush(_result.StatisticsObject.YColors);
                var idValues = _result.StatisticsObject.XIndexes;
                var labels = _result.StatisticsObject.XLabels;

                var items = new ObservableCollection<SimpleBarItem>();
                for (int i = 0; i < labels.Count; i++) {
                    items.Add(new SimpleBarItem(idValues[i], labels[i], yAxisValues[i], 0));
                }
                ShowBarChartUI(owner, items, xAxisTitle, yAxisTitle, graphTitle);
            }
        }

        public void SaveResult(string output) {
            _result.WriteResult(output);
        }

        public void ShowSPlot(Window owner) {
            if (_result.MultivariateAnalysisOption == MultivariateAnalysisOption.Pca || _result.MultivariateAnalysisOption == MultivariateAnalysisOption.Hca) {
                return;
            }
            else {
                var xAxisTitle = "P loading";
                var yAxisTitle = "P correlation";
                var graphTitle = "S-plot";

                var xAxisValues = _result.PPreds[0];
                var yAxisValues = _result.PPredCoeffs[0];

                var brushes = convertRgbaToBrush(_result.StatisticsObject.XColors);
                var idValues = _result.StatisticsObject.XIndexes;
                var labels = _result.StatisticsObject.XLabels;

                var items = new ObservableCollection<SimplePlotItem>();
                for (int i = 0; i < idValues.Count; i++) {
                    items.Add(new SimplePlotItem(idValues[i], labels[i], xAxisValues[i], yAxisValues[i], brushes[i]));
                }
                ShowScatterPlotChartUI(owner, items, xAxisTitle, yAxisTitle, graphTitle);
            }
        }

        private ObservableCollection<SolidColorBrush> convertRgbaToBrush(ObservableCollection<byte[]> bytes) {
            if (bytes == null) return null;
            var brushes = new ObservableCollection<SolidColorBrush>();
            foreach (var colorBytes in bytes) {
                var colorprop = new Color() { R = colorBytes[0], G = colorBytes[1], B = colorBytes[2], A = colorBytes[3] };
                var brush = new SolidColorBrush(colorprop);
                brushes.Add(brush);
            }
            return brushes;
        }


        public void ShowBarChartUI(Window owner, ObservableCollection<SimpleBarItem> items, string xAxisTitle, string yAxisTitle, string graphTitle) {
            var model = new SimpleBarChartModel(items, xAxisTitle, yAxisTitle, graphTitle);
            var vm = new SimpleBarChartViewModel(model);
            var view = new BarChartView {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            view.Show();
        }

        public void ShowScatterPlotChartUI(Window owner, ObservableCollection<SimplePlotItem> items, string xAxisTitle, string yAxisTitle, string graphTitle) {
            var model = new SimpleScatterPlotModel(items, xAxisTitle, yAxisTitle, graphTitle);
            var vm = new SimpleScatterPlotViewModel(model);
            var view = new ScatterPlotView {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            view.Show();
        }
    }
}
