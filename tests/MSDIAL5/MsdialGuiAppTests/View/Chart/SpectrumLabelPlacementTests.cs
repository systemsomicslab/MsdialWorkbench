using CompMs.Common.Components;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Behavior;
using CompMs.Graphics.Chart;
using CompMs.Graphics.Core.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.View.Chart.Tests;

[TestClass]
public class SpectrumLabelPlacementTests
{
    [STATestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SpectrumLabels_RevealClosePeaksWhenZoomedAndThinThemAgainWhenZoomedOut(bool flippedY) {
        var chart = CreateChart(new[] { Peak(481d, 1000d), Peak(483d, 990d) });
        chart.FlippedY = flippedY;
        chart.ContentAlignment = flippedY ? System.Drawing.ContentAlignment.TopCenter : System.Drawing.ContentAlignment.BottomCenter;
        AxisRangeRecalculationBehavior.SetIsEnabled(chart, false);
        var axis = new ContinuousAxisManager<double>(new AxisRange(0d, 620d));
        chart.HorizontalAxis = axis;

        CollectionAssert.AreEqual(new[] { "481.0000" }, RenderLabels(chart));
        axis.Focus(new AxisRange(478d, 486d));
        CollectionAssert.AreEquivalent(new[] { "481.0000", "483.0000" }, RenderLabels(chart));
        axis.Focus(new AxisRange(482d, 486d));
        CollectionAssert.AreEqual(new[] { "483.0000" }, RenderLabels(chart));
        axis.Focus(new AxisRange(0d, 620d));
        CollectionAssert.AreEqual(new[] { "481.0000" }, RenderLabels(chart));
    }

    [STATestMethod]
    [DataRow(84.05d, 86.05d)]
    [DataRow(267d, 269d)]
    [DataRow(135.07d, 136.05d)]
    public void SpectrumLabels_ThinClosePeaksByScreenDistanceRatherThanMassDifference(double mass, double neighbour) {
        var peaks = new[] { Peak(neighbour, 700d), Peak(mass, 1000d) };
        var chart = CreateChart(peaks);
        AxisRangeRecalculationBehavior.SetIsEnabled(chart, false);
        var axis = new ContinuousAxisManager<double>(new AxisRange(0d, 320d));
        chart.HorizontalAxis = axis;
        var expected = new[] { mass.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) };

        CollectionAssert.AreEqual(expected, RenderLabels(chart));
        axis.Focus(new AxisRange(mass - 10d, neighbour + 10d));
        Assert.AreEqual(2, RenderLabels(chart).Length);
        axis.Focus(new AxisRange(0d, 320d));
        CollectionAssert.AreEqual(expected, RenderLabels(chart));
        Assert.AreSame(peaks, chart.ItemsSource);
        CollectionAssert.AreEqual(new[] { 700d, 1000d }, peaks.Select(peak => peak.Intensity).ToArray());
    }

    [STATestMethod]
    [DataRow(true, false)]
    [DataRow(false, false)]
    [DataRow(true, true)]
    [DataRow(false, true)]
    public void SpectrumLabels_MoveAwayFromPeakLinesEvenWhenThosePeaksHaveNoLabels(bool flippedY, bool flippedX) {
        var chart = CreateChart(new[] { Peak(90d, 1000d), Peak(92d, 900d), Peak(100d, 500d) });
        chart.FlippedY = flippedY;
        chart.FlippedX = flippedX;
        chart.ContentAlignment = flippedY ? System.Drawing.ContentAlignment.TopCenter : System.Drawing.ContentAlignment.BottomCenter;

        CollectionAssert.AreEquivalent(new[] { "90.0000", "100.0000" }, RenderLabels(chart));
        var label = GlyphRuns(VisualTreeHelper.GetDrawing(chart))
            .Single(glyph => new string(glyph.GlyphRun.Characters.ToArray()) == "100.0000");
        var center = label.Bounds.Left + label.Bounds.Width / 2d;
        Assert.IsTrue(flippedX ? center < 199d : center > 201d, $"Label center: {center}");
        Assert.IsTrue(Math.Abs(center - 200d) <= label.Bounds.Width / 2d + 1d);
        var leader = GeometryDrawings(VisualTreeHelper.GetDrawing(chart)).Single();
        Assert.IsTrue(leader.Pen.Thickness <= 1d);
        Assert.IsTrue(leader.Pen.DashStyle.Dashes.Count > 0);
        var line = (LineGeometry)leader.Geometry;
        Assert.AreEqual(200d, line.StartPoint.X, 1d);
        var peakY = chart.VerticalAxis.TranslateToRenderPoint(chart.VerticalAxis.TranslateToAxisValue(500d), flippedY, chart.ActualHeight);
        Assert.AreEqual(peakY, line.StartPoint.Y, 1d);
        Assert.IsTrue(flippedY ? line.EndPoint.Y < line.StartPoint.Y : line.EndPoint.Y > line.StartPoint.Y);
    }

    [STATestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SpectrumLabels_KeepVisibleModeratePeaksWhenTheirLabelsNeedShortLeaders(bool flippedY) {
        var chart = CreateChart(new[] { Peak(162d, 1000d), Peak(177d, 300d), Peak(220d, 150d) });
        chart.HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(-22d, 333d));
        chart.FlippedY = flippedY;
        chart.ContentAlignment = flippedY ? System.Drawing.ContentAlignment.TopCenter : System.Drawing.ContentAlignment.BottomCenter;

        CollectionAssert.AreEquivalent(new[] { "162.0000", "177.0000", "220.0000" }, RenderLabels(chart, 357.5d, 164.865d));
        var peakX = chart.HorizontalAxis.TranslateToRenderPoint(chart.HorizontalAxis.TranslateToAxisValue(220d), chart.FlippedX, chart.ActualWidth);
        Assert.IsTrue(GeometryDrawings(VisualTreeHelper.GetDrawing(chart))
            .Any(drawing => Math.Abs(((LineGeometry)drawing.Geometry).StartPoint.X - peakX) < 1d));
    }

    [STATestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SpectrumLabels_OmitObstructedWeakLabelsInsteadOfMovingThem(bool flippedY) {
        var chart = CreateChart(new[] { Peak(90d, 1000d), Peak(92d, 900d), Peak(100d, 50d), Peak(160d, 100d) });
        chart.FlippedY = flippedY;
        chart.ContentAlignment = flippedY ? System.Drawing.ContentAlignment.TopCenter : System.Drawing.ContentAlignment.BottomCenter;

        CollectionAssert.AreEquivalent(new[] { "90.0000", "160.0000" }, RenderLabels(chart));
        Assert.AreEqual(0, GeometryDrawings(VisualTreeHelper.GetDrawing(chart)).Count());
        chart.Format = "F0";
        CollectionAssert.Contains(RenderLabels(chart), "100");
        Assert.AreEqual(0, GeometryDrawings(VisualTreeHelper.GetDrawing(chart)).Count());
    }

    [STATestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SpectrumLabels_OmitHeavilyObstructedLabelsAndRestoreThemWithShorterText(bool flippedY) {
        var chart = CreateChart(new[] {
            Peak(88d, 1000d), Peak(90d, 990d), Peak(110d, 980d), Peak(112d, 970d), Peak(100d, 500d),
        });
        chart.FlippedY = flippedY;
        chart.ContentAlignment = flippedY ? System.Drawing.ContentAlignment.TopCenter : System.Drawing.ContentAlignment.BottomCenter;

        var labels = RenderLabels(chart);
        CollectionAssert.Contains(labels, "88.0000");
        CollectionAssert.DoesNotContain(labels, "100.0000");
        chart.Format = "F0";
        CollectionAssert.Contains(RenderLabels(chart), "100");
    }

    [STATestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SpectrumLabels_ThinStaggeredLabelsUntilThePlotOrTextHasMoreSpace(bool flippedY) {
        var chart = CreateChart(new[] { Peak(91.04d, 760d), Peak(105.06d, 650d), Peak(161.12d, 1000d) });
        chart.HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 300d));
        chart.FlippedY = flippedY;
        chart.ContentAlignment = flippedY ? System.Drawing.ContentAlignment.TopCenter : System.Drawing.ContentAlignment.BottomCenter;

        CollectionAssert.AreEquivalent(new[] { "91.0400", "161.1200" }, RenderLabels(chart, 300d));
        CollectionAssert.AreEquivalent(new[] { "91.0400", "105.0600", "161.1200" }, RenderLabels(chart, 900d));
        CollectionAssert.AreEquivalent(new[] { "91.0400", "161.1200" }, RenderLabels(chart, 300d));
        chart.Format = "F0";
        CollectionAssert.AreEquivalent(new[] { "91", "105", "161" }, RenderLabels(chart, 300d));
    }

    [STATestMethod]
    public void SpectrumLabels_UseActualTextWidthForPeakSpacingAndRefreshWhenTheFontChanges() {
        var chart = CreateChart(new[] { Peak(100d, 1000d), Peak(105.5d, 400d) });
        AxisRangeRecalculationBehavior.SetIsEnabled(chart, false);
        chart.HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 500d));

        CollectionAssert.AreEqual(new[] { "100.0000" }, RenderLabels(chart));
        chart.Format = "F1";
        CollectionAssert.AreEquivalent(new[] { "100.0", "105.5" }, RenderLabels(chart));
        chart.Format = "F0";
        CollectionAssert.AreEquivalent(new[] { "100", "106" }, RenderLabels(chart));
        chart.Format = "F1";
        chart.FontSize = 24d;
        CollectionAssert.AreEqual(new[] { "100.0" }, RenderLabels(chart));
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

        var expected = new[] { "172.0800", "157.0800", "250.0000" };
        var labels = RenderLabels(chart, 177.5d, 113.5d);
        CollectionAssert.AreEquivalent(expected, labels, string.Join(", ", labels));
        chart.FlippedY = false;
        chart.ContentAlignment = System.Drawing.ContentAlignment.BottomCenter;
        CollectionAssert.AreEquivalent(expected, RenderLabels(chart, 177.5d, 113.5d));

        chart.HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(-7d, 293d));
        chart.FlippedX = true;
        CollectionAssert.AreEquivalent(expected, RenderLabels(chart, 177.5d, 113.5d));
        CollectionAssert.Contains(RenderLabels(chart, 355d, 113.5d), "63.0000");
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
        var labels = RenderLabels(chart);
        CollectionAssert.AreEquivalent(new[] { "100.0000", "106.0000", "120.0000" }, labels, string.Join(", ", labels));
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
        Assert.IsTrue(shifted.Bounds.Left <= peakX + 1d);
        Assert.IsTrue(shifted.Bounds.Right <= chart.ActualWidth);

        chart.FontSize = 17d;
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

    private static SpectrumPeak Peak(double mass, double intensity) => new SpectrumPeak(mass, intensity);

    private static Annotator CreateChart(SpectrumPeak[] peaks) => new Annotator {
        HorizontalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 200d)),
        VerticalAxis = new ContinuousAxisManager<double>(new AxisRange(0d, 1200d)),
        HorizontalPropertyName = nameof(SpectrumPeak.Mass),
        VerticalPropertyName = nameof(SpectrumPeak.Intensity),
        OrderingPropertyName = nameof(SpectrumPeak.Intensity),
        LabelPropertyName = nameof(SpectrumPeak.Mass),
        Format = "F4",
        LabelPlacement = new SpectrumLabelPlacement(),
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

    private static IEnumerable<GeometryDrawing> GeometryDrawings(Drawing drawing) {
        if (drawing is GeometryDrawing geometry) {
            yield return geometry;
        }
        else if (drawing is DrawingGroup group) {
            foreach (var child in group.Children.SelectMany(GeometryDrawings)) {
                yield return child;
            }
        }
    }
}
