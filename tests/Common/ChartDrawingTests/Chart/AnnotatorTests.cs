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
    public void SpectrumLabels_KeepOnlyTheStrongerLabelWithinFiveMassUnits() {
        foreach (var pair in new[] { (84.05d, 86.05d), (267d, 269d), (135.07d, 136.05d), (100d, 105d) }) {
            var peaks = new[] { Peak(pair.Item2, 700d), Peak(pair.Item1, 1000d) };
            var chart = CreateChart(peaks);
            chart.HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 320d));
            var expected = new[] { pair.Item1.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) };

            CollectionAssert.AreEqual(expected, RenderLabels(chart));
            CollectionAssert.AreEqual(expected, RenderLabels(chart, 1600d));
            chart.FlippedY = false;
            chart.ContentAlignment = System.Drawing.ContentAlignment.BottomCenter;
            CollectionAssert.AreEqual(expected, RenderLabels(chart));
            Assert.AreSame(peaks, chart.ItemsSource);
            CollectionAssert.AreEqual(new[] { 700d, 1000d }, peaks.Select(peak => peak.Intensity).ToArray());
        }
    }

    [STATestMethod]
    public void SpectrumLabels_CompareUnroundedMassesInsteadOfAxisCoordinates() {
        var chart = CreateChart(new[] { Peak(84d, 1000d), Peak(86d, 700d), Peak(135d, 400d) });
        chart.HorizontalAxis = new LogScaleAxisManager<double>(1d, 200d);
        CollectionAssert.AreEquivalent(new[] { "84.0000", "135.0000" }, RenderLabels(chart));

        chart = CreateChart(new[] { Peak(100d, 1000d), Peak(105.00001d, 700d) });
        CollectionAssert.AreEquivalent(new[] { "100.0000", "105.0000" }, RenderLabels(chart));
    }

    [STATestMethod]
    public void SpectrumLabels_OnlyAcceptedPeaksSuppressTheirMassNeighbours() {
        var chart = CreateChart(new[] { Peak(100d, 900d), Peak(104d, 1000d), Peak(108d, 800d), Peak(112d, 500d) });

        CollectionAssert.AreEquivalent(new[] { "104.0000", "112.0000" }, RenderLabels(chart));
    }

    [STATestMethod]
    public void ExistingCharts_KeepCloseMassLabelsOutsideSpectrumMode() {
        var chart = CreateChart(new[] { Peak(100d, 1000d), Peak(102d, 500d) });
        chart.Overlap = (IOverlapMethod)new OverlapMethodTypeConverter().ConvertFromInvariantString("Direct");

        CollectionAssert.AreEquivalent(new[] { "100.0000", "102.0000" }, RenderLabels(chart));
    }

    [STATestMethod]
    public void SpectrumLabels_KeepAProminentPeakInTheGapInsteadOfItsCrowdedNeighbour() {
        var chart = CreateChart(new[] {
            Peak(74d, 1000d), Peak(87d, 700d), Peak(147d, 600d), Peak(55d, 580d),
            Peak(43d, 450d), Peak(111d, 430d), Peak(143d, 400d),
        });
        chart.HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 250d));

        var labels = RenderLabels(chart, 227.5d, 113.5d);
        foreach (var label in new[] { "74.0000", "87.0000", "147.0000", "111.0000" }) {
            CollectionAssert.Contains(labels, label, $"Missing {label}; visible: {string.Join(", ", labels)}");
        }
        CollectionAssert.DoesNotContain(labels, "143.0000");

        chart.FlippedY = false;
        chart.ContentAlignment = System.Drawing.ContentAlignment.BottomCenter;
        var mirrored = RenderLabels(chart, 227.5d, 113.5d);
        CollectionAssert.Contains(mirrored, "111.0000");
        CollectionAssert.DoesNotContain(mirrored, "143.0000");
    }

    [STATestMethod]
    public void SpectrumLabels_SpaceNearbyWeakPeaksWithoutHidingIsolatedWeakPeaks() {
        var chart = CreateChart(new[] {
            Peak(177d, 1000d), Peak(220d, 460d), Peak(135d, 380d),
            Peak(67d, 220d), Peak(41d, 215d), Peak(91d, 180d), Peak(121d, 170d),
            Peak(66d, 20d), Peak(39.9d, 15d), Peak(310d, 20d),
        });
        chart.HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 350d));
        chart.MinimumRelativeOrder = .01d;

        var labels = RenderLabels(chart, 227.5d, 113.5d);
        foreach (var label in new[] { "177.0000", "220.0000", "135.0000", "67.0000", "310.0000" }) {
            CollectionAssert.Contains(labels, label, $"Missing {label}; visible: {string.Join(", ", labels)}");
        }
        foreach (var label in new[] { "41.0000", "91.0000", "66.0000", "39.9000" }) {
            CollectionAssert.DoesNotContain(labels, label);
        }
    }

    [STATestMethod]
    public void SpectrumLabels_LimitDenseClustersAndFillUnlabelledRegions() {
        var chart = CreateChart(new[] {
            Peak(172.08d, 1000d), Peak(157.08d, 480d), Peak(158.04d, 350d),
            Peak(130.05d, 260d), Peak(173.01d, 180d), Peak(126.99d, 150d),
            Peak(167.99d, 100d), Peak(63d, 80d), Peak(70d, 70d), Peak(250d, 50d),
        });
        chart.HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 300d));

        var expected = new[] { "172.0800", "157.0800", "63.0000", "250.0000" };
        CollectionAssert.AreEquivalent(expected, RenderLabels(chart, 177.5d, 113.5d));
        chart.FlippedY = false;
        chart.ContentAlignment = System.Drawing.ContentAlignment.BottomCenter;
        CollectionAssert.AreEquivalent(expected, RenderLabels(chart, 177.5d, 113.5d));

        chart.HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(-7d, 293d));
        chart.FlippedX = true;
        CollectionAssert.AreEquivalent(expected, RenderLabels(chart, 177.5d, 113.5d));
    }

    [STATestMethod]
    public void SpectrumLabels_AllowMoreLabelsWhenThePlotOrFormattedTextHasMoreSpace() {
        var chart = CreateChart(new[] { Peak(100d, 1000d), Peak(120d, 990d) });
        chart.HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 300d));

        CollectionAssert.AreEquivalent(new[] { "100.0000" }, RenderLabels(chart, 300d, 180d));
        CollectionAssert.AreEquivalent(new[] { "100.0000", "120.0000" }, RenderLabels(chart, 600d, 180d));

        chart.Format = "F1";
        CollectionAssert.AreEquivalent(new[] { "100.0", "120.0" }, RenderLabels(chart, 300d, 180d));
        chart.Format = "F0";
        CollectionAssert.AreEquivalent(new[] { "100", "120" }, RenderLabels(chart, 300d, 180d));

        chart.Format = "F4";
        chart.FontSize = 24d;
        chart.InvalidateVisual();
        CollectionAssert.AreEquivalent(new[] { "100.0000" }, RenderLabels(chart, 600d, 180d));
    }

    [STATestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SpectrumLabels_UseSeparatedHeightsWhenThePlotHasSpace(bool flippedY) {
        var peaks = new[] { Peak(100d, 1000d), Peak(106d, 500d), Peak(120d, 200d) };
        var chart = CreateChart(peaks);
        chart.FlippedY = flippedY;
        chart.ContentAlignment = flippedY ? System.Drawing.ContentAlignment.TopCenter : System.Drawing.ContentAlignment.BottomCenter;

        CollectionAssert.AreEquivalent(new[] { "100.0000", "106.0000" }, RenderLabels(chart, 177.5d, 113.5d));
        CollectionAssert.AreEquivalent(new[] { "100.0000", "106.0000", "120.0000" }, RenderLabels(chart));
    }

    [STATestMethod]
    public void SpectrumLabels_PreferTheStrongestPeakInEachLocalRegion() {
        var peaks = new[] { Peak(100d, 480d), Peak(110d, 500d), Peak(120d, 490d), Peak(160d, 200d) };
        var chart = CreateChart(peaks);

        CollectionAssert.AreEquivalent(new[] { "110.0000", "160.0000" }, RenderLabels(chart));
        CollectionAssert.AreEquivalent(new[] { "100.0000", "110.0000", "120.0000", "160.0000" }, RenderLabels(chart, 1600));
        Assert.AreSame(peaks, chart.ItemsSource);
    }

    [STATestMethod]
    public void SpectrumLabels_KeepNearbyPeaksWhenTheirHeightsAreClearlySeparated() {
        var chart = CreateChart(new[] { Peak(162.07d, 1000d), Peak(177.09d, 250d) });
        chart.HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 300d));

        CollectionAssert.AreEquivalent(new[] { "162.0700", "177.0900" }, RenderLabels(chart, 177.5d, 113.5d));
        chart.FlippedY = false;
        chart.ContentAlignment = System.Drawing.ContentAlignment.BottomCenter;
        CollectionAssert.AreEquivalent(new[] { "162.0700", "177.0900" }, RenderLabels(chart, 177.5d, 113.5d));
    }

    [STATestMethod]
    public void SpectrumLabels_KeepProminentPeaksAtTheOriginalSmallPlotSize() {
        var chart = CreateChart(new[] { Peak(220d, 1000d), Peak(70d, 870d), Peak(145d, 770d), Peak(145.9d, 20d) });
        chart.HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(-20d, 320d));

        var labels = RenderLabels(chart, 177.5d, 113.5d);
        foreach (var label in new[] { "220.0000", "70.0000", "145.0000" }) {
            CollectionAssert.Contains(labels, label, $"Missing {label}; visible: {string.Join(", ", labels)}");
        }
        var shifted = GlyphRuns(VisualTreeHelper.GetDrawing(chart))
            .Single(glyph => new string(glyph.GlyphRun.Characters.ToArray()) == "145.0000");
        var peakX = chart.HorizontalAxis.TranslateToRenderPoint(chart.HorizontalAxis.TranslateToAxisValue(145d), chart.FlippedX, chart.ActualWidth);
        Assert.IsTrue(shifted.Bounds.Left + shifted.Bounds.Width / 2d > peakX + 1d);
        Assert.IsTrue(shifted.Bounds.Right <= chart.ActualWidth);

        chart.FontSize = 17d;
        chart.InvalidateVisual();
        var largerLabels = RenderLabels(chart, 177.5d, 113.5d);
        foreach (var label in new[] { "220.0000", "70.0000", "145.0000" }) {
            CollectionAssert.Contains(largerLabels, label);
        }
    }

    [STATestMethod]
    public void SpectrumLabels_DoNotShiftLabelsBeyondTheRightPlotEdge() {
        var chart = CreateChart(new[] { Peak(138d, 1000d), Peak(174d, 960d) });

        CollectionAssert.AreEquivalent(new[] { "138.0000" }, RenderLabels(chart, 200d));
        CollectionAssert.AreEquivalent(new[] { "138.0000", "174.0000" }, RenderLabels(chart, 400d));
    }

    [STATestMethod]
    public void SpectrumLabels_RecheckLaterNeighboursAfterShifting() {
        var chart = CreateChart(new[] { Peak(110d, 960d), Peak(130d, 980d), Peak(80d, 1000d) });

        CollectionAssert.AreEquivalent(new[] { "80.0000", "130.0000" }, RenderLabels(chart, 200d));
        CollectionAssert.AreEquivalent(new[] { "80.0000", "130.0000", "110.0000" }, RenderLabels(chart, 600d));
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

    private static Annotator CreateChart(SpectrumPeak[] peaks) => new Annotator {
        HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 200d)),
        VerticalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 1200d)),
        HorizontalPropertyName = nameof(SpectrumPeak.Mass),
        VerticalPropertyName = nameof(SpectrumPeak.Intensity),
        OrderingPropertyName = nameof(SpectrumPeak.Intensity),
        LabelPropertyName = nameof(SpectrumPeak.Mass),
        Format = "F4",
        Overlap = (IOverlapMethod)new OverlapMethodTypeConverter().ConvertFromInvariantString("Spectrum"),
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
