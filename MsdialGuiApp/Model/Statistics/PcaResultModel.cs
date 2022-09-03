using CompMs.Common.Mathematics.Statistics;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class ComponentLoadingModel : BindableBase {
        public ComponentLoadingModel(double[] loading, string label) {
            Loading = loading ?? throw new ArgumentNullException(nameof(loading));
            Label = label;
        }

        public double[] Loading { get; }
        public string Label { get; }
    }

    internal sealed class PcaResultModel : BindableBase
    {
        private readonly MultivariateAnalysisResult _multivariateAnalysisResult;

        public PcaResultModel(MultivariateAnalysisResult multivariateAnalysisResult) {
            _multivariateAnalysisResult = multivariateAnalysisResult ?? throw new ArgumentNullException(nameof(multivariateAnalysisResult));

            var statisticsObject = multivariateAnalysisResult.StatisticsObject;
            Loadings = new ObservableCollection<ComponentLoadingModel>(statisticsObject.XLabels.Select((label, i) => new ComponentLoadingModel(multivariateAnalysisResult.PPreds.Select(preds => preds[i]).ToArray(), label)));
            LoadingAxises = multivariateAnalysisResult.PPreds
                .Select(pc_loadings => new Lazy<IAxisManager<double>>(() => new ContinuousAxisManager<double>(pc_loadings, new ConstantMargin(10))))
                .ToList().AsReadOnly();
        }

        public ObservableCollection<ComponentLoadingModel> Loadings { get; }
        public ReadOnlyCollection<Lazy<IAxisManager<double>>> LoadingAxises { get; }

        public int NumberOfComponents => _multivariateAnalysisResult.PPreds.Count;
    }
}
