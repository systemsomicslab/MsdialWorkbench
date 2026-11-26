using CompMs.Common.DataObj.Result;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration.Tests;

[TestClass]
public class MassCalibrationDecisionTests
{
    private const double SearchPpm = 100d;

    // Features eluting over 1-20 min, each giving ionsPerFeature ions over m/z 100-1000 that read the true error plus
    // normal scatter, among wrong matches spread evenly over the ±100 ppm window. With windows > 0, an ion's window is
    // its m/z's share of 100-1000.
    private static List<MassCalibrationPoint> Points(
        Func<double, double, int, double> error,
        int features,
        int ionsPerFeature,
        double sd,
        int backgroundCount,
        int seed,
        int windows = 0,
        double lowestIntensity = 1e3,
        double highestIntensity = 1e5,
        int firstFeature = 0) {
        var random = new Random(seed);
        var points = new List<MassCalibrationPoint>();
        for (var f = 0; f < features; f++) {
            var rt = 1d + 19d * random.NextDouble();
            for (var i = 0; i < ionsPerFeature; i++) {
                var mz = 100d + 900d * random.NextDouble();
                var window = windows > 0 ? Math.Min(windows - 1, (int)((mz - 100d) / 900d * windows)) : MassCalibrationPoint.NoWindow;
                var intensity = lowestIntensity * Math.Pow(highestIntensity / lowestIntensity, random.NextDouble());
                points.Add(new MassCalibrationPoint(mz, error(mz, rt, window) + sd * Normal(random), rt, intensity, firstFeature + f, window));
            }
        }
        for (var i = 0; i < backgroundCount; i++) {
            var mz = 100d + 900d * random.NextDouble();
            var window = windows > 0 ? Math.Min(windows - 1, (int)((mz - 100d) / 900d * windows)) : MassCalibrationPoint.NoWindow;
            points.Add(new MassCalibrationPoint(mz, -SearchPpm + 2d * SearchPpm * random.NextDouble(), 1d + 19d * random.NextDouble(),
                lowestIntensity * Math.Pow(highestIntensity / lowestIntensity, random.NextDouble()), random.Next(features), window));
        }
        return points;
    }

    private static double Normal(Random random) {
        var u1 = 1d - random.NextDouble();
        var u2 = random.NextDouble();
        return Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
    }

    [TestMethod]
    [DataRow(8d)]
    [DataRow(-25d)]
    [DataRow(-60d)]
    public void AConstantErrorIsModelledAsAnOffsetAndApplied(double offset) {
        var points = Points((mz, rt, w) => offset, features: 150, ionsPerFeature: 2, sd: 2d, backgroundCount: 300, seed: 3);

        var decision = MassCalibrationDecision.Decide(points, SearchPpm);

        Assert.AreEqual(MassErrorTerms.Offset, decision.Model.Terms, decision.Reason);
        Assert.AreEqual(offset, decision.Model.ErrorAtReferencePpm, 0.5d);
        Assert.IsTrue(decision.IsApplied, decision.Reason);
        Assert.IsTrue(decision.HeldOutLossWithoutModel > decision.Candidates[0].HeldOutLoss);
        Assert.AreEqual(0d, decision.ResidualEstimate!.OffsetPpm, 0.5d, "the model leaves no offset behind");
    }

    [TestMethod]
    public void AnErrorThatFollowsOneOverTheSquareRootOfMzIsModelledWithThatTerm() {
        // A time-of-flight offset t0 that is off: +30 ppm at m/z 100, -11 ppm at m/z 1000.
        static double Truth(double mz) => -30d + 600d / Math.Sqrt(mz);
        var points = Points((mz, rt, w) => Truth(mz), features: 200, ionsPerFeature: 3, sd: 2d, backgroundCount: 400, seed: 5);

        var decision = MassCalibrationDecision.Decide(points, SearchPpm);

        Assert.IsTrue((decision.Model.Terms & MassErrorTerms.InverseSqrtMz) != 0, MassErrorModel.Describe(decision.Model.Terms));
        Assert.AreEqual(600d, decision.Model.InverseSqrtMzCoefficient, 60d);
        foreach (var mz in new[] { 120d, 300d, 600d, 950d }) {
            Assert.AreEqual(Truth(mz), decision.Model.PredictPpm(mz, 10d), 1d, $"at m/z {mz}");
        }
        Assert.IsTrue(decision.IsApplied, decision.Reason);
        Assert.IsTrue(decision.LargestErrorPpm > 20d && decision.SmallestErrorPpm < -8d);
    }

    [TestMethod]
    public void AnErrorThatDriftsThroughTheRunIsModelledWithTheRetentionTimeTerm() {
        var points = Points((mz, rt, w) => 4d + 0.8d * (rt - 10d), features: 200, ionsPerFeature: 2, sd: 2d, backgroundCount: 300, seed: 7);

        var decision = MassCalibrationDecision.Decide(points, SearchPpm);

        Assert.IsTrue((decision.Model.Terms & MassErrorTerms.RetentionTime) != 0, MassErrorModel.Describe(decision.Model.Terms));
        Assert.AreEqual(0.8d, decision.Model.RetentionTimeSlope, 0.08d);
        Assert.AreEqual(4d + 0.8d * (decision.Model.ReferenceRetentionTime - 10d), decision.Model.ErrorAtReferencePpm, 0.5d);
        Assert.IsTrue(decision.IsApplied, decision.Reason);
    }

    [TestMethod]
    public void IsolationWindowsThatReadApartGetOffsetsOfTheirOwn() {
        var windowOffsets = new[] { -5d, 7d, -17d, -5d, 7d, -17d, -5d, 7d };
        var points = Points((mz, rt, w) => windowOffsets[w], features: 400, ionsPerFeature: 2, sd: 2d, backgroundCount: 400, seed: 9, windows: 8);

        var decision = MassCalibrationDecision.Decide(points, SearchPpm);

        Assert.IsTrue((decision.Model.Terms & MassErrorTerms.Window) != 0, MassErrorModel.Describe(decision.Model.Terms));
        for (var w = 0; w < windowOffsets.Length; w++) {
            var mz = 100d + 900d * (w + 0.5d) / windowOffsets.Length;
            Assert.AreEqual(windowOffsets[w], decision.Model.PredictPpm(mz, 10d, w), 1.5d, $"window {w}");
        }
        Assert.IsTrue(decision.IsApplied, decision.Reason);
    }

    [TestMethod]
    public void NoErrorIsLeftAlone() {
        var points = Points((mz, rt, w) => 0d, features: 200, ionsPerFeature: 2, sd: 3d, backgroundCount: 300, seed: 13);

        var decision = MassCalibrationDecision.Decide(points, SearchPpm);

        Assert.IsFalse(decision.IsApplied, decision.Reason);
    }

    [TestMethod]
    public void AnErrorBelowTheMinimumCorrectionIsNotApplied() {
        var points = Points((mz, rt, w) => 1d, features: 1000, ionsPerFeature: 2, sd: 1d, backgroundCount: 200, seed: 17);

        var decision = MassCalibrationDecision.Decide(points, SearchPpm);

        Assert.AreEqual(MassErrorTerms.Offset, decision.Model.Terms, "a 1 ppm error over 2000 points is plain to see");
        Assert.IsFalse(decision.IsApplied);
        StringAssert.Contains(decision.Reason, "smaller than the minimum correction of 1.5 ppm");

        Assert.IsTrue(MassCalibrationDecision.Decide(points, SearchPpm, minimumCorrectionPpm: 0.5d).IsApplied, "a lower minimum lets it through");
    }

    [TestMethod]
    public void AnErrorNoLargerThanItsScatterOverAFewPointsIsNotApplied() {
        // 12 points scattered by 10 ppm: an offset of 2 ppm is no more than chance.
        var points = Points((mz, rt, w) => 2d, features: 12, ionsPerFeature: 1, sd: 10d, backgroundCount: 0, seed: 19);

        var decision = MassCalibrationDecision.Decide(points, SearchPpm);

        Assert.IsFalse(decision.IsApplied, decision.Reason);
    }

    [TestMethod]
    public void TooFewPointsAboveTheBackgroundGiveNoModel() {
        var points = Points((mz, rt, w) => 20d, features: 6, ionsPerFeature: 1, sd: 1d, backgroundCount: 100, seed: 23);

        var decision = MassCalibrationDecision.Decide(points, SearchPpm);

        Assert.IsFalse(decision.IsApplied);
        Assert.AreEqual(MassErrorTerms.None, decision.Model.Terms);
        StringAssert.StartsWith(decision.Reason, "Fewer than 10 points above the background");
        Assert.AreEqual(0, decision.Candidates.Count);
    }

    [TestMethod]
    public void TheScatteredWeakIonsCountForLessThanTheStrongOnes() {
        // Strong ions read -3 ppm; a third of the ions are weak, a few ion counts each, and read +13 ppm with a wide scatter.
        var strong = Points((mz, rt, w) => -3d, features: 150, ionsPerFeature: 1, sd: 2d, backgroundCount: 100, seed: 29, lowestIntensity: 1e4, highestIntensity: 1e5);
        var weak = Points((mz, rt, w) => 13d, features: 75, ionsPerFeature: 1, sd: 11d, backgroundCount: 50, seed: 31, lowestIntensity: 10d, highestIntensity: 100d, firstFeature: 150);

        var decision = MassCalibrationDecision.Decide(strong.Concat(weak).ToList(), SearchPpm);

        Assert.AreEqual(-3d, decision.Model.ErrorAtReferencePpm, 1d);
        Assert.IsTrue(decision.IsApplied, decision.Reason);
    }

    [TestMethod]
    public void TheModelIsFoundAmongABackgroundTwiceTheTruePoints() {
        var points = Points((mz, rt, w) => -40d, features: 200, ionsPerFeature: 1, sd: 4d, backgroundCount: 400, seed: 37);

        var decision = MassCalibrationDecision.Decide(points, SearchPpm);

        Assert.AreEqual(-40d, decision.Model.ErrorAtReferencePpm, 1.5d);
        Assert.IsTrue(decision.IsApplied, decision.Reason);
    }

    [TestMethod]
    public void TheDecisionDoesNotDependOnTheOrderOfThePoints() {
        var points = Points((mz, rt, w) => -30d + 600d / Math.Sqrt(mz), features: 150, ionsPerFeature: 2, sd: 2d, backgroundCount: 200, seed: 41);
        var shuffled = points.OrderBy(point => point.Mz * 7919d % 1d).ToList();

        var first = MassCalibrationDecision.Decide(points, SearchPpm);
        var second = MassCalibrationDecision.Decide(shuffled, SearchPpm);

        Assert.AreEqual(first.Model.Terms, second.Model.Terms);
        Assert.AreEqual(first.Model.ErrorAtReferencePpm, second.Model.ErrorAtReferencePpm, 1e-6d);
        Assert.AreEqual(first.IsApplied, second.IsApplied);
    }

    [TestMethod]
    public void CorrectingAnMzTakesTheModelledErrorOutAndTheModelIsNotCarriedBeyondItsPoints() {
        var points = Points((mz, rt, w) => -30d + 600d / Math.Sqrt(mz), features: 200, ionsPerFeature: 2, sd: 1d, backgroundCount: 100, seed: 43);
        var model = MassCalibrationDecision.Decide(points, SearchPpm).Model;

        foreach (var trueMz in new[] { 150d, 500d, 900d }) {
            var observed = trueMz * (1d + model.PredictPpm(trueMz, 10d) * 1e-6);
            Assert.AreEqual(trueMz, model.CorrectMz(observed, 10d), trueMz * 1e-8);
        }
        var lowest = points.Min(point => point.Mz);
        Assert.AreEqual(model.PredictPpm(lowest, 10d), model.PredictPpm(50d, 10d), 0.5d, "below the lowest point the error stays as there");
        Assert.AreEqual(model.PredictPpm(10d, 1d), model.PredictPpm(10d, -5d), 1e-9, "before the first retention time too");

        Assert.AreEqual(0d, MassErrorModel.None.PredictPpm(500d, 10d));
        Assert.AreEqual(500d, MassErrorModel.None.CorrectMz(500d, 10d));
    }

    [TestMethod]
    public void PointsAreTakenFromTheAnnotationCalibrantsOfOneLevelWithTheFixedWindowsNumberedInMzOrder() {
        AnnotationCalibrant Calibrant(int level, double lower, double upper) {
            return new AnnotationCalibrant(1, 5d, level, 300.003d, 300d, AnnotationCalibrant.FromLibraryFragment, 1e4, 500d, false, "x", "lbm", DataBaseSource.Lbm, 0.9d, 7, lower, upper);
        }
        var calibrants = new[]
        {
            Calibrant(2, 525d, 550d),
            Calibrant(2, 500d, 525d),
            Calibrant(2, 499.5d, 500.5d),
            Calibrant(2, double.NaN, double.NaN),
            Calibrant(1, double.NaN, double.NaN),
        };

        var ms2 = MassCalibrationPoint.FromAnnotation(calibrants, 2);
        var ms1 = MassCalibrationPoint.FromAnnotation(calibrants, 1);

        CollectionAssert.AreEqual(new[] { 1, 0, MassCalibrationPoint.NoWindow, MassCalibrationPoint.NoWindow }, ms2.Select(point => point.Window).ToArray());
        Assert.AreEqual(1, ms1.Count);
        Assert.AreEqual(MassCalibrationPoint.NoWindow, ms1[0].Window);
        Assert.AreEqual(10d, ms1[0].ErrorPpm, 1e-6);
        Assert.AreEqual(1e4, ms1[0].Intensity);
    }

    [TestMethod]
    public void TheReportHasOneColumnPerHeaderAndSaysWhy() {
        var applied = MassCalibrationDecision.Decide(Points((mz, rt, w) => 8d, features: 100, ionsPerFeature: 2, sd: 2d, backgroundCount: 100, seed: 47), SearchPpm);
        var tooFew = MassCalibrationDecision.Decide(Points((mz, rt, w) => 8d, features: 3, ionsPerFeature: 1, sd: 2d, backgroundCount: 20, seed: 53), SearchPpm);
        var text = new StringWriter(CultureInfo.InvariantCulture);

        MassCalibrationReport.WriteHeader(text);
        MassCalibrationReport.WriteRow(text, "s1", "MS1 precursor", applied);
        MassCalibrationReport.WriteRow(text, "s1", "MS2 product ion", tooFew);

        var lines = text.ToString().TrimEnd('\r', '\n').Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var header = lines[0].Split('\t').ToList();
        Assert.AreEqual(3, lines.Length);
        Assert.IsTrue(lines.All(line => line.Split('\t').Length == header.Count));
        var first = lines[1].Split('\t');
        Assert.AreEqual(MassCalibrationReport.Apply, first[header.IndexOf("Decision")]);
        Assert.AreEqual("Offset", first[header.IndexOf("Model")]);
        StringAssert.StartsWith(first[header.IndexOf("Held-out loss of models tried")], "Offset ");
        var second = lines[2].Split('\t');
        Assert.AreEqual(MassCalibrationReport.DoNotApply, second[header.IndexOf("Decision")]);
        Assert.AreEqual("None", second[header.IndexOf("Model")]);
        StringAssert.StartsWith(second[header.IndexOf("Reason")], "Fewer than 10");

        StringAssert.EndsWith(MassCalibrationReport.Describe(applied), ": apply");
        StringAssert.StartsWith(MassCalibrationReport.Describe(applied), "Offset, modelled error ");
        StringAssert.StartsWith(MassCalibrationReport.Describe(tooFew), "do not apply: Fewer than 10");
    }
}
