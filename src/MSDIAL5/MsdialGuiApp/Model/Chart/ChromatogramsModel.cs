using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Algorithm.PeakPick;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart;

public sealed class ChromatogramsModel : DisposableModelBase {
    private readonly ObservableCollection<DisplayChromatogram> _displayChromatograms;

    public ChromatogramsModel(string name, ObservableCollection<DisplayChromatogram> chromatograms, string graphTitle, string horizontalTitle, string verticalTitle) {
        _displayChromatograms = chromatograms ?? throw new ArgumentNullException(nameof(chromatograms));
        DisplayChromatograms = new ReadOnlyObservableCollection<DisplayChromatogram>(_displayChromatograms);

        Name = name;
        GraphTitle = graphTitle;

        HorizontalProperty = nameof(PeakItem.Time);
        VerticalProperty = nameof(PeakItem.Intensity);

        var abundanceAxis = new ContinuousAxisManager<double>(0d, chromatograms.DefaultIfEmpty().Max(chromatogram => chromatogram?.MaxIntensity) ?? 1d, new ConstantMargin(0, 10d), new AxisRange(0d, 0d))
        {
            LabelType = LabelType.Order,
        }.AddTo(Disposables);
        var chromAxis = new ContinuousAxisManager<double>(chromatograms.Aggregate<DisplayChromatogram, AxisRange?>(null, (acc, chromatogram) => chromatogram.ChromXRange.Union(acc)) ?? new AxisRange(0d, 1d)).AddTo(Disposables);

        AbundanceAxisItemSelector = new AxisItemSelector<double>(new AxisItemModel<double>(verticalTitle, abundanceAxis, verticalTitle)).AddTo(Disposables);
        ChromAxisItemSelector = new AxisItemSelector<double>(new AxisItemModel<double>(horizontalTitle, chromAxis, horizontalTitle)).AddTo(Disposables);
    }

    public ChromatogramsModel(string name, DisplayChromatogram chromatogram, string graphTitle, string horizontalTitle, string verticalTitle)
       : this(name, new ObservableCollection<DisplayChromatogram>() { chromatogram }, graphTitle, horizontalTitle, verticalTitle) {

    }

    public ChromatogramsModel(string name, DisplayChromatogram chromatogram)
       : this(name, new ObservableCollection<DisplayChromatogram>() { chromatogram }, string.Empty, string.Empty, string.Empty) {

    }

    public ChromatogramsModel(string name, List<DisplayChromatogram> chromatograms, string graphTitle, string horizontalTitle, string verticalTitle)
        : this(name, new ObservableCollection<DisplayChromatogram>(chromatograms), graphTitle, horizontalTitle, verticalTitle){

    }

    public ReadOnlyObservableCollection<DisplayChromatogram> DisplayChromatograms { get; }

    public AxisItemSelector<double> AbundanceAxisItemSelector { get; }
    public AxisItemSelector<double> ChromAxisItemSelector { get; }

    public string Name { get; }
    public string GraphTitle { get; }
    public string HorizontalProperty { get; }
    public string VerticalProperty { get; }

    public ChromatogramsModel Merge(ChromatogramsModel other) {
        return new ChromatogramsModel($"{Name} and {other.Name}", new ObservableCollection<DisplayChromatogram>(DisplayChromatograms.Concat(other.DisplayChromatograms)), $"{GraphTitle} and {other.GraphTitle}", ChromAxisItemSelector.SelectedAxisItem.GraphLabel, AbundanceAxisItemSelector.SelectedAxisItem.GraphLabel);
    }

    public async Task ExportAsync(Stream stream, string separator) {
        using var writer = new StreamWriter(stream, encoding: new UTF8Encoding(false), bufferSize: 1024, leaveOpen: true);
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

    public void DetectPeaks(PeakDetection detector) {
        foreach (var chromatogram in DisplayChromatograms) {
            chromatogram.DetectPeaks(detector);
        }
    }

    public void AddPeak(double timeLeft, double timeRight) {
        foreach (var chromatogram in DisplayChromatograms) {
            chromatogram.AsPeak(timeLeft, timeRight);
        }
    }

    public void ResetPeaks() {
        foreach (var chromatogram in DisplayChromatograms) {
            chromatogram.ResetPeaks();
        }
    }

    public void RemovePeak(DisplayPeakOfChromatogram peak) {
        foreach (var chromatogram in DisplayChromatograms) {
            chromatogram.RemovePeak(peak);
        }
    }
}
