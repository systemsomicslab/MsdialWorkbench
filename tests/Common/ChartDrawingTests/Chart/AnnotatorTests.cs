using CompMs.Common.Components;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Behavior;
using CompMs.Graphics.Core.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompMs.Graphics.Chart.Tests;

[TestClass]
public class AnnotatorTests
{
    [STATestMethod]
    public void LabelPlacement_UsesTheStrategyAndRestoresOverlapBehaviourWhenCleared() {
        var chart = CreateChart(new[] { Peak(100d, 1000d), Peak(160d, 500d) });
        CollectionAssert.AreEquivalent(new[] { "100.0000", "160.0000" }, RenderLabels(chart));

        var placement = new FirstLabelPlacement();
        chart.LabelPlacement = placement;
        chart.MinimumRelativeOrder = .75d;
        CollectionAssert.AreEqual(new[] { "100.0000" }, RenderLabels(chart));
        Assert.AreEqual(2, placement.AnchorCount);
        chart.LabelPlacement = null;
        chart.MinimumRelativeOrder = 0d;
        CollectionAssert.AreEquivalent(new[] { "100.0000", "160.0000" }, RenderLabels(chart));
    }

    [STATestMethod]
    public void ExistingCharts_KeepCloseLabelsAtDifferentHeights() {
        var chart = CreateChart(new[] { Peak(100d, 1000d), Peak(102d, 500d) });

        CollectionAssert.AreEquivalent(new[] { "100.0000", "102.0000" }, RenderLabels(chart));
    }

    [STATestMethod]
    public void RelativeThreshold_HidesWeakLabelsWithoutChangingSourcePeaks() {
        var peaks = new[] { Peak(30d, 1000d), Peak(90d, 6d), Peak(150d, 2d) };
        var chart = CreateChart(peaks);
        chart.MinimumRelativeOrder = .01d;

        CollectionAssert.AreEqual(new[] { "30.0000" }, RenderLabels(chart));
        Assert.AreSame(peaks, chart.ItemsSource);
        CollectionAssert.AreEqual(new[] { 1000d, 6d, 2d }, peaks.Select(peak => peak.Intensity).ToArray());
    }

    [STATestMethod]
    public void RelativeThreshold_UsesTheFullSourceWhenTheBasePeakIsOutsideTheView() {
        var chart = CreateChart(new[] { Peak(30d, 1000d), Peak(90d, 6d) });
        chart.MinimumRelativeOrder = .01d;
        AxisRangeRecalculationBehavior.SetIsEnabled(chart, false);
        var axis = new ContinuousAxisManager<double>(new AxisRange(0d, 200d), new AxisRange(80d, 100d));
        chart.HorizontalAxis = axis;
        axis.Focus(new AxisRange(80d, 100d));
        Assert.IsFalse(axis.Range.Contains(axis.TranslateToAxisValue(30d)));

        var labels = RenderLabels(chart);
        Assert.AreEqual(0, labels.Length, $"Visible labels: {string.Join(", ", labels)}; range: {axis.Range.Minimum.Value}..{axis.Range.Maximum.Value}");
    }

    [STATestMethod]
    public void RelativeThreshold_UsesRawIntensityOnLogarithmicAxes() {
        var chart = CreateChart(new[] { Peak(30d, 1000d), Peak(90d, 6d) });
        chart.VerticalAxis = new LogScaleAxisManager<double>(1d, 1200d);

        CollectionAssert.AreEquivalent(new[] { "30.0000", "90.0000" }, RenderLabels(chart));
        chart.MinimumRelativeOrder = .01d;

        CollectionAssert.AreEqual(new[] { "30.0000" }, RenderLabels(chart));
    }

    [STATestMethod]
    public void MinimumOrder_IncludesTheBoundaryAndFiltersLowerValues() {
        var chart = CreateChart(new[] { Peak(90d, 6d) });
        CollectionAssert.AreEqual(new[] { "90.0000" }, RenderLabels(chart));

        chart.MinimumOrder = 10d;
        Assert.AreEqual(0, RenderLabels(chart).Length);

        chart.ItemsSource = new[] { Peak(90d, 10d) };
        CollectionAssert.AreEqual(new[] { "90.0000" }, RenderLabels(chart));
    }

    [STATestMethod]
    public void FragmentationScoreLabels_KeepLowScoresWhenTheIntensityThresholdIsDisabled() {
        var chart = CreateChart(new[] {
            new SpectrumPeak(30d, 1000d) { FragmentationScore = 100d },
            new SpectrumPeak(90d, 1000d) { FragmentationScore = .5d },
        });
        chart.VerticalPropertyName = nameof(SpectrumPeak.FragmentationScore);
        chart.OrderingPropertyName = nameof(SpectrumPeak.FragmentationScore);
        chart.MinimumOrder = double.NegativeInfinity;

        CollectionAssert.AreEquivalent(new[] { "30.0000", "90.0000" }, RenderLabels(chart));
    }

    [STATestMethod]
    public void RelativeThreshold_IncludesTheBoundaryAndCanBeDisabled() {
        var chart = CreateChart(new[] { Peak(30d, 1000d), Peak(90d, 10d), Peak(150d, 2d) });
        chart.MinimumRelativeOrder = .01d;
        CollectionAssert.AreEquivalent(new[] { "30.0000", "90.0000" }, RenderLabels(chart));

        chart.MinimumRelativeOrder = 0d;
        CollectionAssert.AreEquivalent(new[] { "30.0000", "90.0000", "150.0000" }, RenderLabels(chart));
    }

    [STATestMethod]
    public void ExistingCharts_KeepTheirUnfilteredHorizontalOverlapBehaviour() {
        var chart = CreateChart(new[] { Peak(100d, 1000d), Peak(120d, 200d), Peak(160d, 2d) });
        CollectionAssert.AreEquivalent(new[] { "100.0000", "120.0000", "160.0000" }, RenderLabels(chart));
        chart.Overlap = (IOverlapMethod)new OverlapMethodTypeConverter().ConvertFromInvariantString("Horizontal,Direct");

        Assert.AreEqual(0d, chart.MinimumRelativeOrder);
        CollectionAssert.AreEquivalent(new[] { "100.0000", "160.0000" }, RenderLabels(chart));
    }

    [STATestMethod]
    public void Format_RefreshesExistingLabelsWhenChangedToFourDecimalPlaces() {
        var peak = Peak(100.123456d, 1000d);
        var chart = CreateChart(new[] { peak });
        chart.Format = "F5";
        CollectionAssert.AreEqual(new[] { "100.12346" }, RenderLabels(chart));

        chart.Format = "F4";
        CollectionAssert.AreEqual(new[] { "100.1235" }, RenderLabels(chart));
        Assert.AreEqual(100.123456d, peak.Mass);
    }

    [STATestMethod]
    public void RelativeThreshold_HandlesEmptyAndZeroIntensitySpectra() {
        var chart = CreateChart(Array.Empty<SpectrumPeak>());
        chart.MinimumRelativeOrder = .01d;
        Assert.AreEqual(0, RenderLabels(chart).Length);

        chart.ItemsSource = new[] { Peak(100d, 0d) };
        Assert.AreEqual(0, RenderLabels(chart).Length);
    }

    [STATestMethod]
    public void RelativeThreshold_RejectsInvalidFractions() {
        var chart = CreateChart(Array.Empty<SpectrumPeak>());
        foreach (var value in new[] { -1d, 1.1d, double.NaN, double.PositiveInfinity }) {
            Assert.ThrowsExactly<ArgumentException>(() => chart.MinimumRelativeOrder = value);
        }
    }

    private static SpectrumPeak Peak(double mass, double intensity) => new SpectrumPeak(mass, intensity);

    private sealed class FirstLabelPlacement : ILabelPlacement {
        public Pen? LeaderLinePen => null;

        public int AnchorCount { get; private set; }

        public bool TryPlaceLabel(LabelLayout candidate, IReadOnlyList<LabelLayout> accepted, IReadOnlyList<Point> anchors, Size plotSize, bool flippedY, out Point origin) {
            AnchorCount = anchors.Count;
            origin = candidate.Origin;
            return accepted.Count == 0;
        }
    }

    private static Annotator CreateChart(SpectrumPeak[] peaks) => new Annotator {
        HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 200d)),
        VerticalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 1200d)),
        HorizontalPropertyName = nameof(SpectrumPeak.Mass),
        VerticalPropertyName = nameof(SpectrumPeak.Intensity),
        OrderingPropertyName = nameof(SpectrumPeak.Intensity),
        LabelPropertyName = nameof(SpectrumPeak.Mass),
        Format = "F4",
        Overlap = (IOverlapMethod)new OverlapMethodTypeConverter().ConvertFromInvariantString("Direct"),
        ItemsSource = peaks,
    };

    private static string[] RenderLabels(Annotator chart, double width = 400d, double height = 240d) {
        chart.Measure(new Size(width, height));
        chart.Arrange(new Rect(0d, 0d, width, height));
        chart.UpdateLayout();
        new RenderTargetBitmap((int)Math.Ceiling(width), (int)Math.Ceiling(height), 96d, 96d, PixelFormats.Pbgra32).Render(chart);
        var drawing = VisualTreeHelper.GetDrawing(chart);
        return drawing is null ? Array.Empty<string>() : GlyphRuns(drawing)
            .Select(glyph => new string(glyph.GlyphRun.Characters.ToArray())).ToArray();
    }

    private static IEnumerable<GlyphRunDrawing> GlyphRuns(Drawing drawing) {
        if (drawing is GlyphRunDrawing glyph) {
            yield return glyph;
        }
        else if (drawing is DrawingGroup group) {
            foreach (var child in group.Children.SelectMany(GlyphRuns)) {
                yield return child;
            }
        }
    }
}
