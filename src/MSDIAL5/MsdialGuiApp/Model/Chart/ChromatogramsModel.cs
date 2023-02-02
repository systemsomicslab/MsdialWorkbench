using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class ChromatogramsModel : DisposableModelBase {
        public ChromatogramsModel(
            string name,
            List<DisplayChromatogram> chromatograms,
            string graphTitle, string horizontalTitle, string verticalTitle) {

            DisplayChromatograms = chromatograms ?? throw new ArgumentNullException(nameof(chromatograms));

            Name = name;
            GraphTitle = graphTitle;
            HorizontalTitle = horizontalTitle;
            VerticalTitle = verticalTitle;

            HorizontalProperty = nameof(PeakItem.Time);
            VerticalProperty = nameof(PeakItem.Intensity);

            AbundanceAxis = new ContinuousAxisManager<double>(0d, chromatograms.DefaultIfEmpty().Max(chromatogram => chromatogram?.MaxIntensity) ?? 1d, new ConstantMargin(0, 10d), new Range(0d, 0d))
            {
                LabelType = LabelType.Order,
            }.AddTo(Disposables);
            ChromAxis = new ContinuousAxisManager<double>(chromatograms.Aggregate<DisplayChromatogram, Range>(null, (acc, chromatogram) => chromatogram.ChromXRange.Union(acc)) ?? new Range(0d, 1d)).AddTo(Disposables);
        }

        public ChromatogramsModel(string name, DisplayChromatogram chromatogram, string graphTitle, string horizontalTitle, string verticalTitle)
           : this(name, new List<DisplayChromatogram>() { chromatogram }, graphTitle, horizontalTitle, verticalTitle) {

        }

        public ChromatogramsModel(string name, DisplayChromatogram chromatogram)
           : this(name, new List<DisplayChromatogram>() { chromatogram }, string.Empty, string.Empty, string.Empty) {

        }


        public List<DisplayChromatogram> DisplayChromatograms { get; }

        public IAxisManager<double> AbundanceAxis { get; }
        public IAxisManager<double> ChromAxis { get; }

        public string Name { get; }
        public string HorizontalTitle { get; }
        public string VerticalTitle { get; }
        public string GraphTitle { get; }
        public string HorizontalProperty { get; }
        public string VerticalProperty { get; }

        public ChromatogramsModel Merge(ChromatogramsModel other) {
            return new ChromatogramsModel($"{Name} and {other.Name}", DisplayChromatograms.Concat(other.DisplayChromatograms).ToList(), $"{GraphTitle} and {other.GraphTitle}", HorizontalTitle, VerticalTitle);
        }
    }
}
