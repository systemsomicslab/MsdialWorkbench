using CompMs.Common.Components;
#if NETSTANDARD || NETFRAMEWORK
using CompMs.Common.Extension;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CompMs.Common.Algorithm.ChromSmoothing.Tests;

[TestClass()]
public class SmoothingTests
{
    static List<ChromatogramPeak> Original_SimpleMovingAverage(List<ChromatogramPeak> peaklist, int smoothingLevel) {
        var smoothedPeaklist = new List<ChromatogramPeak>();
        double sum;
        int normalizationValue = 2 * smoothingLevel + 1;

        for (int i = 0; i < peaklist.Count; i++) {
            var smoothedPeakIntensity = 0.0;
            sum = 0;

            for (int j = -smoothingLevel; j <= smoothingLevel; j++) {
                if (i + j < 0 || i + j > peaklist.Count - 1) sum += peaklist[i].Intensity;
                else sum += peaklist[i + j].Intensity;
            }
            smoothedPeakIntensity = (double)(sum / normalizationValue);
            var smoothedPeak = new ChromatogramPeak(peaklist[i].ID, peaklist[i].Mass, smoothedPeakIntensity, peaklist[i].ChromXs);
            smoothedPeaklist.Add(smoothedPeak);
        }
        return smoothedPeaklist;
    }

    static List<ChromatogramPeak> Original_LinearWeightedMovingAverage(List<ChromatogramPeak> peaklist, int smoothingLevel)
    {
        var smoothedPeaklist = new List<ChromatogramPeak>();
        double sum;
        int lwmaNormalizationValue = smoothingLevel + 1;

        for (int i = 1; i <= smoothingLevel; i++)
            lwmaNormalizationValue += i * 2;

        for (int i = 0; i < peaklist.Count; i++)
        {
            var smoothedPeakIntensity = 0.0;
            sum = 0;

            for (int j = -smoothingLevel; j <= smoothingLevel; j++)
            {
                if (i + j < 0 || i + j > peaklist.Count - 1) sum += peaklist[i].Intensity * (smoothingLevel - Math.Abs(j) + 1);
                else sum += peaklist[i + j].Intensity * (smoothingLevel - Math.Abs(j) + 1);
            }
            smoothedPeakIntensity = (double)(sum / lwmaNormalizationValue);
            var smoothedPeak = new ChromatogramPeak(peaklist[i].ID, peaklist[i].Mass, smoothedPeakIntensity, peaklist[i].ChromXs);
            
            smoothedPeaklist.Add(smoothedPeak);
        }
        return smoothedPeaklist;
    }

    [TestMethod()]
    public void SimpleMovingAverageTest() {
        var rand = new Random(4869);
        var timer = new Stopwatch();
        var chrom = new List<ChromatogramPeak>(1000000);

        for (int i = 0; i < 1000000; i++) {
            chrom.Add(new ChromatogramPeak(i, rand.NextDouble(), rand.NextDouble() * 10000000, new ChromXs(i / 100d)));
        }

        timer.Start();
        var expected = Original_SimpleMovingAverage(chrom, 30);
        timer.Stop();
        Console.WriteLine($"original method: {timer.ElapsedMilliseconds}");
        timer.Reset();

        timer.Start();
        var actual = Smoothing.SimpleMovingAverage(chrom, 30);
        timer.Stop();
        Console.WriteLine($"new method: {timer.ElapsedMilliseconds}");
        timer.Reset();

        foreach ((var peak1, var peak2) in expected.ZipInternal(actual)) {
            Assert.AreEqual(peak1.ID, peak2.ID);
            Assert.AreEqual(peak1.ChromXs.Value, peak2.ChromXs.Value);
            Assert.AreEqual(peak1.Mass, peak2.Mass);
            Assert.AreEqual(peak1.Intensity, peak2.Intensity, 0.00001);
        }
    }

    [TestMethod()]
    public void SimpleMovingAverageShortListTest() {
        var chrom = new List<ChromatogramPeak>
        {
            new ChromatogramPeak(0, 200, 1000000, new ChromXs(100)),
            new ChromatogramPeak(1, 200, 5000000, new ChromXs(200)),
            new ChromatogramPeak(2, 200, 3000000, new ChromXs(300)),
        };

        var expected = Original_SimpleMovingAverage(chrom, 5);
        var actual = Smoothing.SimpleMovingAverage(chrom, 5);

        foreach ((var peak1, var peak2) in expected.ZipInternal(actual)) {
            Assert.AreEqual(peak1.ID, peak2.ID);
            Assert.AreEqual(peak1.ChromXs.Value, peak2.ChromXs.Value);
            Assert.AreEqual(peak1.Mass, peak2.Mass);
            Assert.AreEqual(peak1.Intensity, peak2.Intensity, 0.00001);
        }
    }

    [TestMethod()]
    public void LinearWeightedMovingAverageTest() {
        var rand = new Random(4869);
        var timer = new Stopwatch();
        var chrom = new List<ChromatogramPeak>(1000000);

        for (int i = 0; i < 1000000; i++) {
            chrom.Add(new ChromatogramPeak(i, rand.NextDouble(), rand.NextDouble() * 10000000, new ChromXs(i / 100d)));
        }

        timer.Start();
        var expected = Original_LinearWeightedMovingAverage(chrom, 3);
        timer.Stop();
        Console.WriteLine($"original method: {timer.ElapsedMilliseconds}");
        timer.Reset();

        timer.Start();
        var actual = Smoothing.LinearWeightedMovingAverage(chrom, 3);
        timer.Stop();
        Console.WriteLine($"new method: {timer.ElapsedMilliseconds}");
        timer.Reset();

        foreach ((var peak1, var peak2) in expected.ZipInternal(actual)) {
            Assert.AreEqual(peak1.ID, peak2.ID);
            Assert.AreEqual(peak1.ChromXs.Value, peak2.ChromXs.Value);
            Assert.AreEqual(peak1.Mass, peak2.Mass);
            Assert.AreEqual(peak1.Intensity, peak2.Intensity, 0.2);
            // Assert.AreEqual(peak1.Intensity, peak2.Intensity, 0.01);
        }
    }

    [TestMethod()]
    public void LinearWeightedMovingAverageShortListTest() {
        var chrom = new List<ChromatogramPeak>
        {
            new ChromatogramPeak(0, 200, 1000000, new ChromXs(100)),
            new ChromatogramPeak(1, 200, 5000000, new ChromXs(200)),
            new ChromatogramPeak(2, 200, 3000000, new ChromXs(300)),
        };

        var expected = Original_LinearWeightedMovingAverage(chrom, 5);
        var actual = Smoothing.LinearWeightedMovingAverage(chrom, 5);

        foreach ((var peak1, var peak2) in expected.ZipInternal(actual)) {
            Assert.AreEqual(peak1.ID, peak2.ID);
            Assert.AreEqual(peak1.ChromXs.Value, peak2.ChromXs.Value);
            Assert.AreEqual(peak1.Mass, peak2.Mass);
            Assert.AreEqual(peak1.Intensity, peak2.Intensity, 0.00001);
        }
    }

    [TestMethod()]
    public void ApplySimpleTwiceEqualsToLinearWeightedTest() {
        var rand = new Random(4869);
        var timer = new Stopwatch();
        var chrom = new List<ChromatogramPeak>(1000000);
        var width = 3;

        for (int i = 0; i < 1000000; i++) {
            chrom.Add(new ChromatogramPeak(i, rand.NextDouble(), rand.NextDouble() * 10000000, new ChromXs(i / 100d)));
        }

        timer.Start();
        var expected = Smoothing.SimpleMovingAverage(Smoothing.SimpleMovingAverage(chrom, width), width);
        timer.Stop();
        Console.WriteLine($"original method: {timer.ElapsedMilliseconds}");
        timer.Reset();

        timer.Start();
        var actual = Smoothing.LinearWeightedMovingAverage(chrom, width * 2);
        timer.Stop();
        Console.WriteLine($"new method: {timer.ElapsedMilliseconds}");
        timer.Reset();

        for (int i = 0; i < 10; i++) {
            Console.WriteLine("{0} {1}", expected[i].Intensity, actual[i].Intensity);
        }

        // Simple width ∘ Simple width equals to LinearWeighted (width * 2) except at the begins and ends width * 2.
        foreach ((var peak1, var peak2) in expected.ZipInternal(actual).Skip(width * 2).Take(actual.Count - width * 4)) {
            Assert.AreEqual(peak1.ID, peak2.ID);
            Assert.AreEqual(peak1.ChromXs.Value, peak2.ChromXs.Value);
            Assert.AreEqual(peak1.Mass, peak2.Mass);
            Assert.AreEqual(peak1.Intensity, peak2.Intensity, 0.2);
            // Assert.AreEqual(peak1.Intensity, peak2.Intensity, 0.01);
        }
    }

    /// <summary>
    /// Tests the TimeBasedLinearWeightedMovingAverage method with valid input data.
    /// Verifies that the smoothing process produces correct results.
    /// </summary>
    [TestMethod]
    public void Test_TimeBasedLinearWeightedMovingAverage_WithValidInput()
    {
        // Arrange
        var peaklist = new List<ValuePeak>
        {
            new(1, 0.0, 100.0, 10.0),
            new(2, 1.0, 100.0, 20.0),
            new(3, 2.0, 100.0, 30.0),
            new(4, 3.0, 100.0, 40.0),
        };
        int smoothingLevel = 1;

        // Act
        var result = new Smoothing().TimeBasedLinearWeightedMovingAverage(peaklist, smoothingLevel);

        // Assert
        Assert.AreEqual(4, result.Length, "The number of elements in the result does not match the input.");

        // Check that smoothing results match expectations (adjust based on your expected output)
        Assert.IsTrue(result[0].Intensity > peaklist[0].Intensity, "The smoothed intensity is incorrect.");
        Assert.IsTrue(result[3].Intensity < peaklist[3].Intensity, "The smoothed intensity is incorrect.");
    }

    /// <summary>
    /// Tests that the TimeBasedLinearWeightedMovingAverage method handles an empty input list correctly.
    /// Ensures that an empty result is returned for an empty input.
    /// </summary>
    [TestMethod]
    public void Test_TimeBasedLinearWeightedMovingAverage_WithEmptyInput()
    {
        // Arrange
        var peaklist = new List<ValuePeak>();
        int smoothingLevel = 2;

        // Act
        var result = new Smoothing().TimeBasedLinearWeightedMovingAverage(peaklist, smoothingLevel);

        // Assert
        Assert.AreEqual(0, result.Length, "The result should be empty for an empty input list.");
    }

    /// <summary>
    /// Tests that the TimeBasedLinearWeightedMovingAverage method returns the same peak
    /// when only a single element is provided in the input.
    /// Ensures that the smoothing operation has no effect in this case.
    /// </summary>
    [TestMethod]
    public void Test_TimeBasedLinearWeightedMovingAverage_WithSingleElement()
    {
        // Arrange
        var peaklist = new List<ValuePeak>
        {
            new(1, 0.0, 100.0, 10.0)
        };
        int smoothingLevel = 2;

        // Act
        var result = new Smoothing().TimeBasedLinearWeightedMovingAverage(peaklist, smoothingLevel);

        // Assert
        Assert.AreEqual(1, result.Length, "The result length should match the input when there is only one element.");
        Assert.AreEqual(peaklist[0].Intensity, result[0].Intensity, "The smoothed result should match the input for a single element.");
    }

    /// <summary>
    /// Tests the TimeBasedLinearWeightedMovingAverage method with different smoothing levels.
    /// Verifies that higher smoothing levels result in more smoothing effects, while lower levels keep more of the original values.
    /// </summary>
    [TestMethod]
    public void Test_TimeBasedLinearWeightedMovingAverage_VariousSmoothingLevels()
    {
        // Arrange
        var peaklist = new List<ValuePeak>
        {
            new(1, 0.0, 100.0, 10.0),
            new(2, 1.0, 100.0, 20.0),
            new(3, 2.0, 100.0, 30.0),
            new(4, 3.0, 100.0, 40.0)
        };

        // Act
        var resultLowSmoothing = new Smoothing().TimeBasedLinearWeightedMovingAverage(peaklist, 1);
        var resultHighSmoothing = new Smoothing().TimeBasedLinearWeightedMovingAverage(peaklist, 5);

        // Assert
        Assert.AreEqual(4, resultLowSmoothing.Length, "The result length with smoothingLevel=1 is incorrect.");
        Assert.AreEqual(4, resultHighSmoothing.Length, "The result length with smoothingLevel=5 is incorrect.");

        // Verify that higher smoothing levels result in more smoothing
        Assert.IsTrue(resultHighSmoothing[3].Intensity < resultLowSmoothing[3].Intensity, "High smoothing level should result in a more smoothed value.");
    }

    /// <summary>
    /// Tests the TimeBasedLinearWeightedMovingAverage method with non-uniform time intervals.
    /// Verifies that the smoothing function can handle cases where the time intervals between peaks are not evenly spaced.
    /// </summary>
    [TestMethod]
    public void Test_TimeBasedLinearWeightedMovingAverage_WithNonUniformTimeIntervals()
    {
        // Arrange
        var peaklist = new List<ValuePeak>
        {
            new(1, 0.0, 100.0, 10.0),
            new(2, 1.5, 100.0, 20.0),  // Non-uniform time interval
            new(3, 3.0, 100.0, 30.0),
            new(4, 4.0, 100.0, 40.0),
            new(5, 10.0, 100.0, 50.0)  // Large time gap
        };
        int smoothingLevel = 2;

        // Act
        var result = new Smoothing().TimeBasedLinearWeightedMovingAverage(peaklist, smoothingLevel);

        // Assert
        Assert.AreEqual(5, result.Length, "The result length does not match the input for non-uniform time intervals.");

        // Check if smoothing is correctly applied to non-uniform time data (customize expected results)
        Assert.AreEqual((10.0 * 3 + 20.0 * 2.4 + 30.0 * 1.8 + 40.0 * 1.4) / (3 + 2.4 + 1.8 + 1.4), result[0].Intensity, "Smoothing not applied correctly for non-uniform intervals.");
        Assert.AreEqual((50.0 * 3 + 40.0 * .6 + 30.0 * .2) / (3 + .6 + .2), result[4].Intensity, "Smoothing not applied correctly for large time gap.");
    }
}