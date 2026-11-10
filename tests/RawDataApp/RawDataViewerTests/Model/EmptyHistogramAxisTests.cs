using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using static CompMs.App.RawDataViewer.Model.Tests.MsSpectrumIntensitySummarizerTests;

namespace CompMs.App.RawDataViewer.Model.Tests
{
    [TestClass]
    public class EmptyHistogramAxisTests
    {
        [TestMethod]
        public async Task AnMs1OnlyFileGetsAnMs2HistogramWithFiniteAxes() {
            var provider = new StubDataProvider(Spectrum(1, 10d, 1000d));

            var summaries = await new MsSpectrumIntensitySummarizer().SummarizeAsync(Task.FromResult<IDataProvider>(provider));

            Assert.AreEqual(0, summaries[1].IntensityHistogram.Length);
            AssertUsable(summaries[1].HorizontalAxis);
            AssertCountAxis(summaries[1].VerticalAxis);
            AssertUsable(summaries[0].HorizontalAxis);
            AssertCountAxis(summaries[0].VerticalAxis);
        }

        [TestMethod]
        public void AnEmptySpectrumSummaryHasFiniteAxes() {
            var summary = new MsSpectrumSummary(new DataPoint[0], "Histogram of ms2 spectrum intensity");

            AssertUsable(summary.HorizontalAxis);
            AssertCountAxis(summary.VerticalAxis);
        }

        [TestMethod]
        public void AnEmptyPeakSpotsSummaryHasFiniteAxes() {
            // What MsPeakSpotsDetector returns for GC-MS, and for direct infusion when no peak is found.
            var summary = new MsPeakSpotsSummary(new DataPoint[0]);

            AssertUsable(summary.HorizontalAxis);
            AssertCountAxis(summary.VerticalAxis);
        }

        [TestMethod]
        public void ASpectrumSummaryAxisSpansItsBins() {
            var summary = new MsSpectrumSummary(new[] { new DataPoint { X = Math.Pow(2, -5), Y = 1, }, new DataPoint { X = Math.Pow(2, 6), Y = 3, }, }, "title");

            // The axis is in log2 units: the bins 2^-5 and 2^6 lie on it.
            Assert.IsTrue(summary.HorizontalAxis.Contains(new AxisValue(-5d)));
            Assert.IsTrue(summary.HorizontalAxis.Contains(new AxisValue(6d)));
            AssertUsable(summary.HorizontalAxis);
            // The counts are accumulated, so the axis reaches 1 + 3.
            Assert.IsTrue(summary.VerticalAxis.Contains(new AxisValue(4d)));
            AssertCountAxis(summary.VerticalAxis);
        }

        [TestMethod]
        public void APeakSpotsSummaryCountAxisSpansItsCounts() {
            var summary = new MsPeakSpotsSummary(new[] { new DataPoint { X = Math.Pow(2, 10), Y = 2, }, new DataPoint { X = Math.Pow(2, 12), Y = 5, }, });

            Assert.IsTrue(summary.VerticalAxis.Contains(new AxisValue(7d)));
            AssertCountAxis(summary.VerticalAxis);
        }

        // A count axis starts at 0 whether or not there is a count to draw, and keeps 0 at its foot once laid out.
        private static void AssertCountAxis(IAxisManager<double> axis) {
            AssertUsable(axis);
            Assert.AreEqual(0d, axis.Range.Minimum.Value, $"count axis after layout is {axis.Range}");
        }

        // What a chart does with an axis: lay it out over its width, reset it to the whole range and draw its ticks.
        private static void AssertUsable(IAxisManager<double> axis) {
            AssertFinite(axis.Range, "range as built");
            axis.Recalculate(400d);
            axis.Reset();
            AssertFinite(axis.Range, "range after layout");
            Assert.IsTrue(axis.Range.Delta > 0d, "range after layout has a width");
            var ticks = axis.GetLabelTicks();
            Assert.IsTrue(ticks.All(t => !double.IsNaN(t.Center) && !double.IsInfinity(t.Center)));
        }

        private static void AssertFinite(AxisRange range, string what) {
            var values = new[] { range.Minimum.Value, range.Maximum.Value, };
            Assert.IsFalse(values.Any(v => double.IsNaN(v) || double.IsInfinity(v)), $"{what} is {range}");
        }
    }
}
