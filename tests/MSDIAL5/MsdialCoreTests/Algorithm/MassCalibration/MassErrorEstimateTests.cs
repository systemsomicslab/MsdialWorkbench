using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration.Tests;

[TestClass]
public class MassErrorEstimateTests
{
    // True pairs, normally spread around the error, among unrelated ions spread evenly over the
    // ±200 ppm search window: three of every four candidates are background, as in a crowded
    // SWATH window.
    private static List<double> Candidates(double offset, double sd, int trueCount, int backgroundCount, int seed) {
        var random = new Random(seed);
        var errors = new List<double>();
        for (var i = 0; i < trueCount; i++) {
            var u1 = 1d - random.NextDouble();
            var u2 = random.NextDouble();
            errors.Add(offset + sd * Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2));
        }
        for (var i = 0; i < backgroundCount; i++) {
            errors.Add(-200d + 400d * random.NextDouble());
        }
        return errors;
    }

    [TestMethod]
    [DataRow(0d, 5d)]
    [DataRow(35d, 4d)]
    [DataRow(-60d, 7d)]
    [DataRow(12.5d, 3d)]
    public void FindsTheErrorOfTheTruePairsAmongABackgroundThreeTimesTheirNumber(double offset, double sd) {
        var estimate = MassErrorEstimate.Estimate(Candidates(offset, sd, trueCount: 500, backgroundCount: 1500, seed: 11));

        Assert.AreEqual(2000, estimate.PairCount);
        // Three standard errors of a median of the 500 true pairs.
        Assert.AreEqual(offset, estimate.OffsetPpm, Math.Max(0.5d, 3d * 1.2533d * sd / Math.Sqrt(500d)));
        // The background inside the peak widens it a little.
        Assert.AreEqual(sd, estimate.RobustSdPpm, 0.25d * sd + 0.5d);
        Assert.AreEqual(1500d / 400d, estimate.BackgroundPerPpm, 0.6d);
        Assert.AreEqual(500d, estimate.SignalCount, 60d, "the pairs in the peak above the background are about the true ones");
        Assert.IsTrue(estimate.StandardErrorPpm < 0.5d);
        Assert.IsTrue(estimate.IsReliable);
    }

    [TestMethod]
    public void AFewPairsOverABackgroundAreNotReliable() {
        // Four pairs at -66 ppm among 60 unrelated ions: a cluster that could be chance.
        var errors = Candidates(0d, 1d, trueCount: 0, backgroundCount: 60, seed: 9).Concat(new[] { -66d, -66.2d, -65.8d, -66.1d }).ToList();

        var estimate = MassErrorEstimate.Estimate(errors);

        Assert.IsTrue(estimate.IsEstimated);
        Assert.IsTrue(estimate.SignalCount < MassErrorEstimate.MinimumSignalPairs);
        Assert.IsFalse(estimate.IsReliable);
    }

    [TestMethod]
    public void TheMeanOfAllPairsIsPulledTowardZeroButTheEstimateIsNot() {
        var errors = Candidates(40d, 4d, trueCount: 300, backgroundCount: 1200, seed: 3);
        var naive = errors.Average();

        var estimate = MassErrorEstimate.Estimate(errors);

        Assert.IsTrue(naive < 15d, $"the mean of all pairs, {naive}, is pulled toward zero");
        Assert.AreEqual(40d, estimate.OffsetPpm, 0.9d);
    }

    [TestMethod]
    public void NoPairsGiveNoEstimate() {
        var estimate = MassErrorEstimate.Estimate(Array.Empty<double>());

        Assert.IsFalse(estimate.IsEstimated);
        Assert.AreEqual(0, estimate.PairCount);
        Assert.IsTrue(double.IsNaN(estimate.OffsetPpm));
        Assert.IsTrue(double.IsNaN(estimate.StandardErrorPpm));
        Assert.AreEqual(0d, estimate.SignalCount);
        Assert.IsFalse(estimate.IsReliable);
    }

    [TestMethod]
    public void ErrorsOutsideTheSearchWindowAreNotCounted() {
        var estimate = MassErrorEstimate.Estimate(new[] { 10d, 10.5d, 9.5d, 250d, -900d, double.NaN });

        Assert.AreEqual(3, estimate.PairCount);
        Assert.AreEqual(10d, estimate.OffsetPpm, 1e-9);
    }

    [TestMethod]
    public void IdenticalErrorsGiveThatErrorWithTheNarrowestPeak() {
        var estimate = MassErrorEstimate.Estimate(Enumerable.Repeat(-7d, 20));

        Assert.AreEqual(-7d, estimate.OffsetPpm, 1e-9);
        Assert.AreEqual(0d, estimate.RobustSdPpm, 1e-9);
        Assert.AreEqual(MassErrorEstimate.MinimumHalfWidthPpm, estimate.HalfWidthPpm, 1e-9);
        Assert.AreEqual(20, estimate.InlierCount);
        Assert.IsTrue(estimate.IsReliable, "twenty pairs over no background");
    }

    [TestMethod]
    public void TheSameErrorsInAnotherOrderGiveTheSameEstimate() {
        var errors = Candidates(-22d, 6d, trueCount: 200, backgroundCount: 600, seed: 5);
        var shuffled = errors.OrderBy(error => error.GetHashCode()).ThenBy(error => error).ToList();

        var first = MassErrorEstimate.Estimate(errors);
        var second = MassErrorEstimate.Estimate(shuffled);

        Assert.AreEqual(first.OffsetPpm, second.OffsetPpm);
        Assert.AreEqual(first.RobustSdPpm, second.RobustSdPpm);
        Assert.AreEqual(first.InlierCount, second.InlierCount);
    }
}
