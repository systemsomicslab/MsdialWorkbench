using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration.Tests;

[TestClass]
public class MassRecalibrationBeanTests
{
    private static MassErrorModelBean Offset(double ppm) {
        return new MassErrorModelBean
        {
            Terms = MassErrorTerms.Offset,
            ErrorAtReferencePpm = ppm,
            ReferenceMz = 500d,
            ReferenceRetentionTime = 5d,
            MinimumMz = 100d,
            MaximumMz = 1000d,
            MinimumRetentionTime = 0d,
            MaximumRetentionTime = 10d,
        };
    }

    private static RawSpectrum Scan(int level, double rt, double[] mzs, RawPrecursorIon? precursor = null) {
        var peaks = mzs.Select(mz => new RawPeakElement { Mz = mz, Intensity = 1000d }).ToArray();
        return new RawSpectrum
        {
            MsLevel = level,
            ScanStartTime = rt,
            Spectrum = peaks,
            BasePeakMz = mzs.Length > 0 ? mzs[0] : 0d,
            LowestObservedMz = mzs.Length > 0 ? mzs.Min() : 0d,
            HighestObservedMz = mzs.Length > 0 ? mzs.Max() : 0d,
            Precursor = precursor,
        };
    }

    private static RawPrecursorIon Window(double target, double halfWidth) {
        return new RawPrecursorIon { SelectedIonMz = target, IsolationTargetMz = target, IsolationWindowLowerOffset = halfWidth, IsolationWindowUpperOffset = halfWidth };
    }

    private static void AssertPpm(double expectedPpm, double observed, double corrected, string message) {
        Assert.AreEqual(expectedPpm, (observed - corrected) / corrected * 1e6, 1e-6, message);
    }

    [TestMethod]
    public void EachLevelIsCorrectedByItsOwnModelAndEachSwathWindowByItsOffset() {
        var ms2 = Offset(-25d);
        ms2.Windows.Add(new MassErrorWindowBean { LowerMz = 300d, UpperMz = 500d, OffsetPpm = -10d });
        var recalibration = new MassRecalibrationBean { Ms1 = Offset(8d), Ms2 = ms2 };
        var survey = Scan(1, 2d, new[] { 200d, 400d, 800d });
        var inWindow = Scan(2, 2d, new[] { 150d, 350d }, Window(400d, 100d));
        var otherWindow = Scan(2, 2d, new[] { 150d, 350d }, Window(600d, 100d));

        recalibration.ApplyTo(new[] { survey, inWindow, otherWindow });

        AssertPpm(8d, 400d, survey.Spectrum[1].Mz, "a survey m/z by the MS1 model");
        AssertPpm(8d, 200d, survey.BasePeakMz, "the base peak with its spectrum");
        AssertPpm(8d, 800d, survey.HighestObservedMz, "the range with its spectrum");
        AssertPpm(-35d, 350d, inWindow.Spectrum[1].Mz, "a product ion by the MS2 model and its window's offset");
        AssertPpm(-25d, 350d, otherWindow.Spectrum[1].Mz, "a window the model has no offset for takes none");
        Assert.AreEqual(400d, inWindow.Precursor!.SelectedIonMz, "a SWATH window is a setting and is left as it is");
        Assert.AreEqual(100d, inWindow.Precursor.IsolationWindowLowerOffset);
    }

    [TestMethod]
    public void TheSurveyMeasuredPrecursorOfADdaScanIsCorrectedByTheMs1Model() {
        var recalibration = new MassRecalibrationBean { Ms1 = Offset(8d), Ms2 = Offset(-25d) };
        var dda = Scan(2, 2d, new[] { 184.07d }, Window(760.5851d, 0.5d));

        recalibration.ApplyTo(new[] { dda });

        AssertPpm(8d, 760.5851d, dda.Precursor!.SelectedIonMz, "selected ion");
        AssertPpm(8d, 760.5851d, dda.Precursor.IsolationTargetMz, "isolation target");
        AssertPpm(-25d, 184.07d, dda.Spectrum[0].Mz, "the product ion by the MS2 model");
    }

    [TestMethod]
    public void ALevelWithoutAModelIsReadAsRecorded() {
        var recalibration = new MassRecalibrationBean { Ms2 = Offset(-25d) };
        var survey = Scan(1, 2d, new[] { 200d, 400d });
        var dda = Scan(2, 2d, new[] { 184.07d }, Window(760.5851d, 0.5d));

        recalibration.ApplyTo(new[] { survey, dda });

        CollectionAssert.AreEqual(new[] { 200d, 400d }, survey.Spectrum.Select(peak => peak.Mz).ToArray());
        Assert.AreEqual(760.5851d, dda.Precursor!.SelectedIonMz, "without an MS1 model the precursor is left too");
        AssertPpm(-25d, 184.07d, dda.Spectrum[0].Mz, "the product ion is corrected");
    }

    [TestMethod]
    public void TheRecordedModelPredictsWhatTheFittedModelPredicts() {
        var random = new Random(5);
        var windowOffsets = new[] { -5d, 7d, -17d, 3d };
        var bounds = new[] { (100d, 300d), (300d, 500d), (500d, 700d), (700d, 900d) };
        var calibrants = new List<AnnotationCalibrant>();
        for (var i = 0; i < 600; i++) {
            var mz = 120d + 760d * random.NextDouble();
            var w = Math.Min(3, (int)((mz - 100d) / 200d));
            var rt = 1d + 9d * random.NextDouble();
            var ppm = -30d + 600d / Math.Sqrt(mz) + 0.5d * (rt - 5d) + windowOffsets[w] + random.NextDouble() - 0.5d;
            calibrants.Add(new AnnotationCalibrant(i, rt, 2, mz * (1d + ppm * 1e-6), mz, AnnotationCalibrant.FromLibraryFragment, 1e4, 900d, false, "x", "lbm",
                DataBaseSource.Lbm, 0.9d, i, bounds[w].Item1, bounds[w].Item2));
        }
        var points = MassCalibrationPoint.FromAnnotation(calibrants, 2, out var windowBounds);
        var model = MassCalibrationDecision.Decide(points, 100d).Model;
        Assert.IsTrue((model.Terms & MassErrorTerms.Window) != 0, MassErrorModel.Describe(model.Terms));

        var bean = MassErrorModelBean.FromModel(model, windowBounds);
        var offsets = bean.WindowOffsets();

        foreach (var (mz, rt) in new[] { (50d, 0d), (150d, 2d), (450d, 5d), (650d, 9d), (850d, 12d) }) {
            var w = Math.Max(0, Math.Min(3, (int)((mz - 100d) / 200d)));
            var key = MassCalibrationPoint.WindowIndex.Key(bounds[w].Item1, bounds[w].Item2)!.Value;
            Assert.AreEqual(model.PredictPpm(mz, rt, w), bean.PredictPpm(mz, rt, offsets[key]), 1e-9, $"m/z {mz}, RT {rt}");
        }
    }

    [TestMethod]
    public void AModelWithNoTermIsNotRecorded() {
        Assert.ThrowsException<ArgumentException>(() => MassErrorModelBean.FromModel(MassErrorModel.None, Array.Empty<(double, double)>()));
    }
}
