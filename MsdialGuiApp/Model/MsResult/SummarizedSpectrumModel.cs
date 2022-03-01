using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.MsResult
{
    public class SummarizedSpectrumModel : BindableBase
    {
        public SummarizedSpectrumModel(List<SpectrumPeak> spectrums, int experimentId) {
            Spectrums = spectrums;
            ExperimentId = experimentId;
            var horizontalAxis = ContinuousAxisManager<double>.Build(Spectrums, s => s.Mass);
            horizontalAxis.ChartMargin = new RelativeMargin(0.05);
            var verticalAxis = ContinuousAxisManager<double>.Build(Spectrums, s => s.Intensity, 0d, 0d);
            verticalAxis.ChartMargin = new ConstantMargin(0, 30);
            var horizontalProperty = "Mass";
            var verticalProperty = "Intensity";
            SpectrumModel = new SpectrumModel(spectrums, horizontalAxis, verticalAxis, horizontalProperty, verticalProperty, $"Experiment={experimentId}");
        }

        public SpectrumModel SpectrumModel { get; }
        public List<SpectrumPeak> Spectrums { get; }
        public int ExperimentId { get; }
    }
}
