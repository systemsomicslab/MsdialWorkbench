using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Statistics;
using CompMs.Common.Enum;
using CompMs.Common.Mathematics.Statistics;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

        public PcaResultModel(
            MultivariateAnalysisResult multivariateAnalysisResult,
            ParameterBase parameter,
            ObservableCollection<AlignmentSpotPropertyModel> spotprops
            ) {

            _multivariateAnalysisResult = multivariateAnalysisResult ?? throw new ArgumentNullException(nameof(multivariateAnalysisResult));

            var statisticsObject = multivariateAnalysisResult.StatisticsObject;
            Loadings = new ObservableCollection<ComponentLoadingModel>(
                statisticsObject.XLabels.Select((label, i) =>
                    new ComponentLoadingModel(multivariateAnalysisResult.PPreds.Select(preds => preds[i]).ToArray(), label, spotprops[i])));
            LoadingAxises = multivariateAnalysisResult.PPreds
                .Select(pc_loadings => new Lazy<IAxisManager<double>>(() => new ContinuousAxisManager<double>(pc_loadings, new ConstantMargin(10))))
                .ToList().AsReadOnly();

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

            //Brushes = new List<BrushMapData<AlignmentSpotPropertyModel>>
            //{
            //    new BrushMapData<AlignmentSpotPropertyModel>(
            //        new KeyBrushMapper<AlignmentSpotPropertyModel, string>(
            //            ChemOntologyColor.Ontology2RgbaBrush,
            //            spot => spot?.Ontology ?? string.Empty,
            //            Color.FromArgb(180, 181, 181, 181)),
            //        "Ontology"),
            //    new BrushMapData<AlignmentSpotPropertyModel>(
            //        new DelegateBrushMapper<AlignmentSpotPropertyModel>(
            //            spot => Color.FromArgb(
            //                180,
            //                (byte)(255 * spot.innerModel.RelativeAmplitudeValue),
            //                (byte)(255 * (1 - Math.Abs(spot.innerModel.RelativeAmplitudeValue - 0.5))),
            //                (byte)(255 - 255 * spot.innerModel.RelativeAmplitudeValue)),
            //            enableCache: true),
            //        "Amplitude"),
            //};
            //switch (parameter.TargetOmics) {
            //    case TargetOmics.Lipidomics:
            //        SelectedBrush = Brushes[0];
            //        break;
            //    case TargetOmics.Metabolomics:
            //    case TargetOmics.Proteomics:
            //        SelectedBrush = Brushes[1];
            //        break;
            //}
        }

        public ObservableCollection<ComponentLoadingModel> Loadings { get; }
        public ReadOnlyCollection<Lazy<IAxisManager<double>>> LoadingAxises { get; }
        public List<BrushMapData<ComponentLoadingViewModel>> Brushes { get; }

        public BrushMapData<ComponentLoadingViewModel> SelectedBrush
        {
            get => _selectedBrush;
            set => SetProperty(ref _selectedBrush, value);
        }
        private BrushMapData<ComponentLoadingViewModel> _selectedBrush;

        public int NumberOfComponents => _multivariateAnalysisResult.PPreds.Count;
    }
}
