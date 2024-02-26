using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;

namespace CompMs.App.RawDataViewer.Model
{
    public class MsSpectrumSummary : DisposableModelBase
    {
        public MsSpectrumSummary(DataPoint[] intensityHistogram, string title) {
            IntensityHistogram = intensityHistogram ?? throw new ArgumentNullException(nameof(intensityHistogram));
            AccumulateIntensityHistogram = Accumulate(intensityHistogram);
            HorizontalAxis = new LogScaleAxisManager<double>(AccumulateIntensityHistogram.Select(bin => bin.X).ToArray(), new ConstantMargin(40), base_: 2).AddTo(Disposables);
            VerticalAxis = new ContinuousAxisManager<double>(AccumulateIntensityHistogram.Select(bin => bin.Y).ToArray(), new ConstantMargin(0, 30), new AxisRange(0d, 0d)).AddTo(Disposables);

            Title = title;
        }

        public DataPoint[] IntensityHistogram { get; }
        public DataPoint[] AccumulateIntensityHistogram { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public string Title { get; }

        private static DataPoint[] Accumulate(DataPoint[] points) {
            var result = new DataPoint[points.Length];
            for (int i = 0; i < points.Length; i++) {
                result[i] = new DataPoint { X = points[i].X, Y = points[i].Y, Type = points[i].Type, };
            }
            for(int i = 1; i < points.Length; i++) {
                result[i].Y += result[i - 1].Y;
            }
            return result;
        }
    }
}
