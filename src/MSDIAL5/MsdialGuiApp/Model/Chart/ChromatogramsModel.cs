using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class ChromatogramsModel : DisposableModelBase {
        public ChromatogramsModel(string name, IReadOnlyList<DisplayChromatogram> chromatograms, string graphTitle, string horizontalTitle, string verticalTitle) {
            DisplayChromatograms = (chromatograms as List<DisplayChromatogram>) ?? chromatograms?.ToList() ?? throw new ArgumentNullException(nameof(chromatograms));

            Name = name;
            GraphTitle = graphTitle;

            HorizontalProperty = nameof(PeakItem.Time);
            VerticalProperty = nameof(PeakItem.Intensity);

            var abundanceAxis = new ContinuousAxisManager<double>(0d, chromatograms.DefaultIfEmpty().Max(chromatogram => chromatogram?.MaxIntensity) ?? 1d, new ConstantMargin(0, 10d), new Range(0d, 0d))
            {
                LabelType = LabelType.Order,
            }.AddTo(Disposables);
            var chromAxis = new ContinuousAxisManager<double>(chromatograms.Aggregate<DisplayChromatogram, Range>(null, (acc, chromatogram) => chromatogram.ChromXRange.Union(acc)) ?? new Range(0d, 1d)).AddTo(Disposables);

            AbundanceAxisItemSelector = new AxisItemSelector<double>(new AxisItemModel<double>(verticalTitle, abundanceAxis, verticalTitle)).AddTo(Disposables);
            ChromAxisItemSelector = new AxisItemSelector<double>(new AxisItemModel<double>(horizontalTitle, chromAxis, horizontalTitle)).AddTo(Disposables);
        }

        public List<DisplayChromatogram> DisplayChromatograms { get; }

        public AxisItemSelector<double> AbundanceAxisItemSelector { get; }
        public AxisItemSelector<double> ChromAxisItemSelector { get; }

        public string Name { get; }
        public string GraphTitle { get; }
        public string HorizontalProperty { get; }
        public string VerticalProperty { get; }

        public ChromatogramsModel Merge(ChromatogramsModel other) {
            return new ChromatogramsModel($"{Name} and {other.Name}", DisplayChromatograms.Concat(other.DisplayChromatograms).ToList(), $"{GraphTitle} and {other.GraphTitle}", ChromAxisItemSelector.SelectedAxisItem.GraphLabel, AbundanceAxisItemSelector.SelectedAxisItem.GraphLabel);
        }

        public async Task ExportAsync(Stream stream, string separator) {
            using (var writer = new StreamWriter(stream, encoding: Encoding.UTF8, bufferSize: 1024, leaveOpen: true)) {
                await writer.WriteLineAsync(string.Format("Chromatogram{0}Time{0}Intensity", separator)).ConfigureAwait(false);
                for (int i = 0; i < DisplayChromatograms.Count; i++) {
                    var chromatogram = DisplayChromatograms[i];
                    var builder = new StringBuilder();
                    foreach (var peak in chromatogram.ChromatogramPeaks) {
                        builder.AppendLine(string.Format("{1}{0}{2}{0}{3}", separator, chromatogram.Name, peak.Time, peak.Intensity));
                    }
                    await writer.WriteAsync(builder.ToString()).ConfigureAwait(false);
                }
            }
        }
    }
}
