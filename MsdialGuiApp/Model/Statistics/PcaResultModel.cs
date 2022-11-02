using CompMs.App.Msdial.Model.DataObj;
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
using Microsoft.Windows.Themes;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class ComponentLoadingModel : BindableBase {
        public ComponentLoadingModel(double[] loading, string label, AlignmentSpotPropertyModel spot) {
            Loading = loading ?? throw new ArgumentNullException(nameof(loading));
            Label = label;
            Spot = spot;
        }

        public double[] Loading { get; }
        public string Label { get; }
        public AlignmentSpotPropertyModel Spot { get; }
    }
    internal sealed class ComponentScoreModel : BindableBase {
        public ComponentScoreModel(double[] score, string label, AnalysisFileBean bean)
        {
            Score = score ?? throw new ArgumentNullException(nameof(score));
            Label = label;
            Bean = bean;
        }

        public double[] Score { get; }
        public string Label { get; }
        public AnalysisFileBean Bean { get; }
    }

    internal sealed class PcaResultModel : BindableBase {
        private readonly MultivariateAnalysisResult _multivariateAnalysisResult;
        private readonly ParameterBase _parameter;
        private readonly ObservableCollection<AlignmentSpotPropertyModel> _spotprops;
        private readonly List<AnalysisFileBean> _analysisfiles;
        private readonly IObservable<KeyBrushMapper<string>> _classBrush;

        public PcaResultModel(
            MultivariateAnalysisResult multivariateAnalysisResult,
            ParameterBase parameter,
            ObservableCollection<AlignmentSpotPropertyModel> spotprops,
            List<AnalysisFileBean> analysisfiles,
            IObservable<KeyBrushMapper<string>> brushmaps
            ) {

            _multivariateAnalysisResult = multivariateAnalysisResult ?? throw new ArgumentNullException(nameof(multivariateAnalysisResult));

            var statisticsObject = multivariateAnalysisResult.StatisticsObject;
            Loadings = new ObservableCollection<ComponentLoadingModel>(
                statisticsObject.XLabels.Select((label, i) =>
                    new ComponentLoadingModel(multivariateAnalysisResult.PPreds.Select(preds => preds[i]).ToArray(), label, spotprops[i])));
            LoadingAxises = multivariateAnalysisResult.PPreds
                .Select(pc_loadings => new Lazy<IAxisManager<double>>(() => new ContinuousAxisManager<double>(pc_loadings, new ConstantMargin(20))))
                .ToList().AsReadOnly();

            LoadingAbsoluteAxises = multivariateAnalysisResult.PPreds
                .Select(pc_loadings => new Lazy<IAxisManager<double>>(() => new AbsoluteAxisManager(new Range(0d, pc_loadings.DefaultIfEmpty().Max(Math.Abs)), new ConstantMargin(0, 10))))
                .ToList().AsReadOnly();

            Scores = new ObservableCollection<ComponentScoreModel>(
                statisticsObject.YLabels.Select((label, i) =>
                    new ComponentScoreModel(multivariateAnalysisResult.TPreds.Select(preds => preds[i]).ToArray(), label, analysisfiles[i])));
            ScoreAxises = multivariateAnalysisResult.TPreds
                .Select(pc_loadings => new Lazy<IAxisManager<double>>(() => new ContinuousAxisManager<double>(pc_loadings, new ConstantMargin(20))))
                .ToList().AsReadOnly();

            var pcAxises = new ObservableCollection<IAxisManager<string>>();
            for (int i = 0; i < NumberOfComponents; i++)
            {
                pcAxises.Add(new CategoryAxisManager<string>(Loadings.OrderByDescending(loading => Math.Abs(loading.Loading[i])).Select(loading => loading.Label).ToArray()));
            }
            PCAxises = pcAxises;
                
            //multivariateAnalysisResult.Contributions.OrderByDescending(d => d).Select(d => new ComponentContributionModel()));

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

            if (parameter.TargetOmics == TargetOmics.Lipidomics)
            {
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
        public ReadOnlyCollection<Lazy<IAxisManager<double>>> LoadingAxises { get; }
        public ReadOnlyCollection<Lazy<IAxisManager<double>>> LoadingAbsoluteAxises { get; }
        public ReadOnlyCollection<Lazy<IAxisManager<double>>> ScoreAxises { get; }
        public List<BrushMapData<ComponentLoadingViewModel>> Brushes { get; }

        public BrushMapData<ComponentLoadingViewModel> SelectedBrush
        {
            get => _selectedBrush;
            set => SetProperty(ref _selectedBrush, value);
        }
        private BrushMapData<ComponentLoadingViewModel> _selectedBrush;

        public IObservable<IBrushMapper<ComponentScoreViewModel>> PointBrush
        {
            get => _pointBrush;
            set => SetProperty(ref _pointBrush, value);
        }
        private IObservable<IBrushMapper<ComponentScoreViewModel>> _pointBrush;

        public IBrushMapper<ComponentLoadingViewModel> PosnegBrush
        {
            get => _posnegBrush;
            set => SetProperty(ref _posnegBrush, value);
        }
        private IBrushMapper<ComponentLoadingViewModel> _posnegBrush;

        public int NumberOfComponents => _multivariateAnalysisResult.PPreds.Count;
    }
}
