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
        static List<ChromatogramPeak> Original_SimpleMovingAverage(List<ChromatogramPeak> peaklist, int smoothingLevel)
        {
            var smoothedPeaklist = new List<ChromatogramPeak>();
            double sum;
            int normalizationValue = 2 * smoothingLevel + 1;

            for (int i = 0; i < peaklist.Count; i++)
            {
                var smoothedPeakIntensity = 0.0;
                sum = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += peaklist[i].Intensity;
                    else sum += peaklist[i + j].Intensity;
                }
                smoothedPeakIntensity = (double)(sum / normalizationValue);
                var smoothedPeak = new ChromatogramPeak() {
                    ID = i, ChromXs = peaklist[i].ChromXs, Mass = peaklist[i].Mass, Intensity = smoothedPeakIntensity
                }; 
                smoothedPeaklist.Add(smoothedPeak);
            }
            return smoothedPeaklist;
        }

        [TestMethod()]
        public void SimpleMovingAverageTest() {
            // var rand = new Random(1659);
            var rand = new Random();
            var timer = new Stopwatch();
            var times_original = new List<long>(3);
            var times_new = new List<long>(3);

            foreach(var _ in Enumerable.Range(0, 3)) {
                var chrom = new List<ChromatogramPeak>(1000000);

                for (int i = 0; i < 1000000; i++) {
                    chrom.Add(new ChromatogramPeak { ID = i, ChromXs = new ChromXs(i / 100d), Mass = rand.NextDouble(), Intensity = rand.NextDouble() * 10000000 });
                }

                timer.Start();
                var expected = Original_SimpleMovingAverage(chrom, 30);
                timer.Stop();
                times_original.Add(timer.ElapsedMilliseconds);
                timer.Reset();

                timer.Start();
                var actual = Smoothing.SimpleMovingAverage(chrom, 30);
                timer.Stop();
                times_new.Add(timer.ElapsedMilliseconds);
                timer.Reset();

                foreach((var peak1, var peak2) in expected.Zip(actual)) {
                    Assert.AreEqual(peak1.ID, peak2.ID);
                    Assert.AreEqual(peak1.ChromXs.Value, peak2.ChromXs.Value);
                    Assert.AreEqual(peak1.Mass, peak2.Mass);
                    Assert.AreEqual(peak1.Intensity, peak2.Intensity, 0.00001);
                }
            }

            Console.WriteLine($"Original method: {times_original.Average()}");
            Console.WriteLine($"New method: {times_new.Average()}");
        }
    }
}