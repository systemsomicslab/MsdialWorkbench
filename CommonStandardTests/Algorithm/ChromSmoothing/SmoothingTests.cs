using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.Algorithm.ChromSmoothing;
using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.Components;
using System.Diagnostics;

namespace CompMs.Common.Algorithm.ChromSmoothing.Tests
{
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
                var smoothedPeak = new ChromatogramPeak(i, peaklist[i].Id, smoothedPeaklist.Count, peaklist[i].Mass, smoothedPeakIntensity, peaklist[i].ChromXs);
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
                var smoothedPeak = new ChromatogramPeak(i, peaklist[i].Id, smoothedPeaklist.Count, peaklist[i].Mass, smoothedPeakIntensity, peaklist[i].ChromXs);
                
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
                chrom.Add(new ChromatogramPeak(i, i, i, rand.NextDouble(), rand.NextDouble() * 10000000, new ChromXs(i / 100d)));
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

            foreach ((var peak1, var peak2) in expected.Zip(actual)) {
                Assert.AreEqual(peak1.IDOrIndex, peak2.IDOrIndex);
                Assert.AreEqual(peak1.ChromXs.Value, peak2.ChromXs.Value);
                Assert.AreEqual(peak1.Mass, peak2.Mass);
                Assert.AreEqual(peak1.Intensity, peak2.Intensity, 0.00001);
            }
        }

        [TestMethod()]
        public void SimpleMovingAverageShortListTest() {
            var chrom = new List<ChromatogramPeak>
            {
                new ChromatogramPeak(0, 0, 0, 200, 1000000, new ChromXs(100)),
                new ChromatogramPeak(1, 1, 1, 200, 5000000, new ChromXs(200)),
                new ChromatogramPeak(2, 2, 2, 200, 3000000, new ChromXs(300)),
            };

            var expected = Original_SimpleMovingAverage(chrom, 5);
            var actual = Smoothing.SimpleMovingAverage(chrom, 5);

            foreach ((var peak1, var peak2) in expected.Zip(actual)) {
                Assert.AreEqual(peak1.IDOrIndex, peak2.IDOrIndex);
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
                chrom.Add(new ChromatogramPeak(i, i, i, rand.NextDouble(), rand.NextDouble() * 10000000, new ChromXs(i / 100d)));
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

            foreach ((var peak1, var peak2) in expected.Zip(actual)) {
                Assert.AreEqual(peak1.IDOrIndex, peak2.IDOrIndex);
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
                new ChromatogramPeak(0, 0, 0, 200, 1000000, new ChromXs(100)),
                new ChromatogramPeak(1, 1, 1, 200, 5000000, new ChromXs(200)),
                new ChromatogramPeak(2, 2, 2, 200, 3000000, new ChromXs(300)),
            };

            var expected = Original_LinearWeightedMovingAverage(chrom, 5);
            var actual = Smoothing.LinearWeightedMovingAverage(chrom, 5);

            foreach ((var peak1, var peak2) in expected.Zip(actual)) {
                Assert.AreEqual(peak1.IDOrIndex, peak2.IDOrIndex);
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
                chrom.Add(new ChromatogramPeak(i, i, i, rand.NextDouble(), rand.NextDouble() * 10000000, new ChromXs(i / 100d)));
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
            foreach ((var peak1, var peak2) in expected.Zip(actual).Skip(width * 2).SkipLast(width * 2)) {
                Assert.AreEqual(peak1.IDOrIndex, peak2.IDOrIndex);
                Assert.AreEqual(peak1.ChromXs.Value, peak2.ChromXs.Value);
                Assert.AreEqual(peak1.Mass, peak2.Mass);
                Assert.AreEqual(peak1.Intensity, peak2.Intensity, 0.2);
                // Assert.AreEqual(peak1.Intensity, peak2.Intensity, 0.01);
            }
        }
    }
}