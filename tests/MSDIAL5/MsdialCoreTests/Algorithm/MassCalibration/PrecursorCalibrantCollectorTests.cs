using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration.Tests;

[TestClass]
public class PrecursorCalibrantCollectorTests
{
    private const double C13 = 1.0033548378d;
    private const double Precursor = 500.2345d;

    private static double Shift(double mz, double ppm) => mz * (1d + ppm * 1e-6);

    private static RawPeakElement Peak(double mz, double intensity) => new RawPeakElement { Mz = mz, Intensity = intensity };

    private static RawSpectrum Ms1(int index, params RawPeakElement[] peaks) {
        return new RawSpectrum { Index = index, MsLevel = 1, ScanStartTime = 5d, Spectrum = peaks.OrderBy(peak => peak.Mz).ToArray() };
    }

    private static RawSpectrum SwathScan(int index, double lower, double upper, params RawPeakElement[] peaks) {
        return new RawSpectrum
        {
            Index = index,
            MsLevel = 2,
            ScanStartTime = 5.001d,
            ExperimentID = 7,
            CollisionEnergy = 35d,
            Precursor = new RawPrecursorIon
            {
                SelectedIonMz = (lower + upper) / 2d,
                IsolationTargetMz = (lower + upper) / 2d,
                IsolationWindowLowerOffset = (upper - lower) / 2d,
                IsolationWindowUpperOffset = (upper - lower) / 2d,
            },
            Spectrum = peaks.OrderBy(peak => peak.Mz).ToArray(),
        };
    }

    private static ChromatogramPeakFeature Feature(int peakId, double mass, int ms1, int ms2, int isotopeWeightNumber = 0, int charge = 1) {
        var feature = new ChromatogramPeakFeature
        {
            PeakID = peakId,
            MS1RawSpectrumIdTop = ms1,
            MS2RawSpectrumID = ms2,
        };
        feature.PeakFeature.Mass = mass;
        feature.PeakFeature.PeakHeightTop = 1e5;
        feature.PeakCharacter.IsotopeWeightNumber = isotopeWeightNumber;
        feature.PeakCharacter.Charge = charge;
        return feature;
    }

    [TestMethod]
    public void PairsAFeatureWithItsUnfragmentedPrecursorAndConfirmsItByTheIsotope() {
        var spectra = new List<RawSpectrum>
        {
            Ms1(0, Peak(Precursor, 1e5), Peak(Precursor + C13, 3e4)),
            SwathScan(1, 490d, 515d,
                Peak(180.0634d, 5e4),
                Peak(Shift(Precursor, 30d), 2e4),
                Peak(Shift(Precursor + C13, 30d), 6e3),
                Peak(Shift(Precursor, 120d), 1e3)),
        };

        var pairs = new PrecursorCalibrantCollector(0.01d).Collect(spectra, new[] { Feature(7, Precursor + 0.001d, 0, 1) });

        Assert.AreEqual(1, pairs.Count);
        var pair = pairs[0];
        Assert.AreEqual(7, pair.PeakId);
        Assert.AreEqual(Precursor, pair.Ms1Mz, 1e-9, "the MS1 m/z is read from the peak-top scan, not the feature's mass");
        Assert.AreEqual(30d, pair.ErrorPpm, 1e-6);
        Assert.IsTrue(pair.IsotopeConfirmed);
        Assert.AreEqual(490d, pair.IsolationLowerMz, 1e-9);
        Assert.AreEqual(515d, pair.IsolationUpperMz, 1e-9);
        Assert.AreEqual(7, pair.ExperimentId);
        Assert.AreEqual(35d, pair.CollisionEnergy);
        Assert.AreEqual(1, pair.Ms2ScanIndex);
        Assert.AreEqual(5e4 + 2e4 + 6e3 + 1e3, pair.Ms2ScanTotalIntensity, 1e-6);
        Assert.AreEqual(5.001d, pair.RetentionTime, 1e-9);
    }

    [TestMethod]
    public void AnUnrelatedIonTakenForThePrecursorIsNotIsotopeConfirmed() {
        // The precursor did not survive; an ion 80 ppm away, with no isotope 1.003 above it, did.
        var spectra = new List<RawSpectrum>
        {
            Ms1(0, Peak(Precursor, 1e5), Peak(Precursor + C13, 3e4)),
            SwathScan(1, 490d, 515d, Peak(Shift(Precursor, 80d), 4e4)),
        };

        var pair = new PrecursorCalibrantCollector(0.01d).Collect(spectra, new[] { Feature(1, Precursor, 0, 1) }).Single();

        Assert.AreEqual(80d, pair.ErrorPpm, 1e-6);
        Assert.IsFalse(pair.IsotopeConfirmed);
    }

    [TestMethod]
    public void TheIsotopeOfADoublyChargedPrecursorIsHalfASpacingAway() {
        var spectra = new List<RawSpectrum>
        {
            Ms1(0, Peak(Precursor, 1e5), Peak(Precursor + C13 / 2d, 5e4)),
            SwathScan(1, 490d, 515d, Peak(Shift(Precursor, -15d), 2e4), Peak(Shift(Precursor + C13 / 2d, -15d), 1e4)),
        };

        var pair = new PrecursorCalibrantCollector(0.01d).Collect(spectra, new[] { Feature(1, Precursor, 0, 1, charge: 2) }).Single();

        Assert.AreEqual(-15d, pair.ErrorPpm, 1e-6);
        Assert.IsTrue(pair.IsotopeConfirmed);
    }

    [TestMethod]
    public void SkipsIsotopeFeaturesFeaturesWithoutAProductIonScanAndScansWithoutAnIonNearby() {
        var spectra = new List<RawSpectrum>
        {
            Ms1(0, Peak(Precursor, 1e5), Peak(Precursor + C13, 3e4), Peak(300.1d, 1e4)),
            SwathScan(1, 490d, 515d, Peak(Shift(Precursor, 10d), 2e4), Peak(Shift(Precursor + C13, 10d), 6e3)),
            SwathScan(2, 290d, 315d, Peak(Shift(300.1d, 250d), 2e4)),
        };
        var features = new[]
        {
            Feature(1, Precursor + C13, 0, 1, isotopeWeightNumber: 1),
            Feature(2, Precursor, 0, -1),
            Feature(3, 300.1d, 0, 2),
            Feature(4, Precursor, 0, 1),
        };

        var pairs = new PrecursorCalibrantCollector(0.01d).Collect(spectra, features);

        CollectionAssert.AreEqual(new[] { 4 }, pairs.Select(pair => pair.PeakId).ToArray(), "only the monoisotopic feature whose scan holds an ion within 200 ppm");
    }

    [TestMethod]
    public void WithoutAnMs1ScanTheFeatureMassIsTheReferenceAndNothingIsConfirmed() {
        var spectra = new List<RawSpectrum>
        {
            SwathScan(0, 490d, 515d, Peak(Shift(Precursor, 20d), 2e4), Peak(Shift(Precursor + C13, 20d), 6e3)),
        };

        var pair = new PrecursorCalibrantCollector(0.01d).Collect(spectra, new[] { Feature(1, Precursor, -1, 0) }).Single();

        Assert.AreEqual(20d, pair.ErrorPpm, 1e-6);
        Assert.IsFalse(pair.IsotopeConfirmed);
    }

    [TestMethod]
    public void AScanWithoutAPrecursorRecordHasNoIsolationWindow() {
        var ms2 = SwathScan(1, 490d, 515d, Peak(Precursor, 2e4));
        ms2.Precursor = null;
        var spectra = new List<RawSpectrum> { Ms1(0, Peak(Precursor, 1e5)), ms2 };

        var pair = new PrecursorCalibrantCollector(0.01d).Collect(spectra, new[] { Feature(1, Precursor, 0, 1) }).Single();

        Assert.IsFalse(pair.HasIsolationWindow);
        Assert.AreEqual(0d, pair.ErrorPpm, 1e-9);
    }

    [TestMethod]
    public void MostIntenseFindsTheTallestPeakInsideTheBoundsOnly() {
        var peaks = new[] { Peak(99.9d, 1e6), Peak(100d, 10d), Peak(100.05d, 30d), Peak(100.1d, 20d), Peak(100.2d, 1e6) };

        Assert.AreEqual(2, PrecursorCalibrantCollector.MostIntense(peaks, 100d, 100.1d));
        Assert.AreEqual(-1, PrecursorCalibrantCollector.MostIntense(peaks, 100.11d, 100.19d));
        Assert.AreEqual(-1, PrecursorCalibrantCollector.MostIntense(Array.Empty<RawPeakElement>(), 0d, 1000d));
    }

    [TestMethod]
    public void RefusesAToleranceOrWindowThatIsNotPositive() {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new PrecursorCalibrantCollector(0d));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new PrecursorCalibrantCollector(0.01d, searchPpm: -1d));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new PrecursorCalibrantCollector(0.01d, isotopeConsistencyPpm: double.NaN));
    }
}
