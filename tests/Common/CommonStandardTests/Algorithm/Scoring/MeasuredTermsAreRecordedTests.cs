using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Algorithm.Scoring.Tests;

/// <summary>
/// The comparison functions in <see cref="MsScanMatching"/> record which terms they computed.
/// </summary>
/// <remarks>
/// Every basic and EI comparison funnels through one leaf, CompareBasicMSScanProperties with
/// explicit tolerances, so that is where the record is written and these tests come in through the
/// public overloads that reach it. The GC-MS path is the reason this matters here rather than only
/// in the annotators: it has no annotator of its own and goes straight through
/// CompareEIMSScanProperties.
/// </remarks>
[TestClass()]
public class MeasuredTermsAreRecordedTests
{
    [TestMethod()]
    public void ABasicComparisonRecordsSpectrumMassAndTime() {
        var result = MsScanMatching.CompareBasicMSScanProperties(
            Scan(retentionTime: 2.2, retentionIndex: 1100), Reference(retentionTime: 2.0, retentionIndex: 1090),
            Parameter(), ms2Tol: 0.05f, massRangeBegin: 0f, massRangeEnd: 2000f);

        Assert.AreEqual(
            MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass | MeasuredTerms.RetentionTime | MeasuredTerms.RetentionIndex,
            result.MeasuredTerms);
    }

    [TestMethod()]
    public void ABasicComparisonLeavesTimeClearWhenTheReferenceHasNone() {
        var result = MsScanMatching.CompareBasicMSScanProperties(
            Scan(retentionTime: 2.2, retentionIndex: 1100), Reference(retentionTime: 0, retentionIndex: 0),
            Parameter(), ms2Tol: 0.05f, massRangeBegin: 0f, massRangeEnd: 2000f);

        Assert.AreEqual(
            MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass,
            result.MeasuredTerms,
            "a reference with neither time nor index cannot have had either compared");
    }

    [TestMethod()]
    public void ABasicComparisonLeavesSpectrumClearWithNoReferenceSpectrum() {
        var reference = Reference(retentionTime: 2.0, retentionIndex: 1090);
        reference.Spectrum = new List<SpectrumPeak>();

        var result = MsScanMatching.CompareBasicMSScanProperties(
            Scan(retentionTime: 2.2, retentionIndex: 1100), reference,
            Parameter(), ms2Tol: 0.05f, massRangeBegin: 0f, massRangeEnd: 2000f);

        Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.Spectrum));
        Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.AccurateMass));
    }

    [TestMethod()]
    public void AnEiComparisonRecordsTheSameTerms() {
        // The GC-MS entry point. It adds the matched-peaks terms on top of the leaf's dot
        // products, which share their availability gate, so the leaf's answer still holds.
        var result = MsScanMatching.CompareEIMSScanProperties(
            Scan(retentionTime: 2.2, retentionIndex: 1100), Reference(retentionTime: 2.0, retentionIndex: 1090),
            Parameter(), isUseRetentionIndex: true);

        Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.Spectrum));
        Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.RetentionIndex));
    }

    [TestMethod()]
    public void AnMs2ComparisonRecordsIsotopeAndCcsOnTopOfTheLeafsTerms() {
        var scanIsotopes = new List<IsotopicPeak>
        {
            new IsotopicPeak { RelativeAbundance = 1, },
            new IsotopicPeak { RelativeAbundance = 3.5, },
        };
        var referenceIsotopes = new List<IsotopicPeak>
        {
            new IsotopicPeak { RelativeAbundance = 1, },
            new IsotopicPeak { RelativeAbundance = 4, },
        };
        var reference = Reference(retentionTime: 2.0, retentionIndex: 1090);
        reference.CollisionCrossSection = 100;

        var result = MsScanMatching.CompareMS2ScanProperties(
            Scan(retentionTime: 2.2, retentionIndex: 1100), reference, Parameter(),
            TargetOmics.Metabolomics, scanCCS: 102, scanIsotopes: scanIsotopes, refIsotopes: referenceIsotopes);

        Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.Isotope));
        Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.Ccs));
        Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.Spectrum),
            "the terms the leaf recorded must survive");
    }

    [TestMethod()]
    public void AnMs2ComparisonLeavesIsotopeAndCcsClearWithNothingToCompare() {
        var result = MsScanMatching.CompareMS2ScanProperties(
            Scan(retentionTime: 2.2, retentionIndex: 1100), Reference(retentionTime: 2.0, retentionIndex: 1090),
            Parameter(), TargetOmics.Metabolomics);

        Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.Isotope),
            "no isotopic peaks were supplied on either side");
        Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.Ccs),
            "the default scanCCS of -1 is not a measurement");
    }

    private static MsRefSearchParameterBase Parameter() {
        return new MsRefSearchParameterBase
        {
            Ms1Tolerance = 0.01f,
            Ms2Tolerance = 0.05f,
            RtTolerance = 0.5f,
            RiTolerance = 20f,
            CcsTolerance = 5f,
            MassRangeBegin = 0f,
            MassRangeEnd = 2000f,
            MinimumSpectrumMatch = 1,
        };
    }

    private static MSScanProperty Scan(double retentionTime, double retentionIndex) {
        return new MSScanProperty
        {
            PrecursorMz = 810.604,
            ChromXs = TimeAndIndex(retentionTime, retentionIndex),
            Spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak { Mass = 86.094, Intensity = 5, },
                new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                new SpectrumPeak { Mass = 524.367, Intensity = 1, },
                new SpectrumPeak { Mass = 810.604, Intensity = 25, },
            },
        };
    }

    private static MoleculeMsReference Reference(double retentionTime, double retentionIndex) {
        return new MoleculeMsReference
        {
            ScanID = 0,
            Name = "PC 18:0_20:4",
            InChIKey = "DUMMYINCHIKEY",
            PrecursorMz = 810.601,
            ChromXs = TimeAndIndex(retentionTime, retentionIndex),
            Spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak { Mass = 184.073, Intensity = 100 },
                new SpectrumPeak { Mass = 506.361, Intensity = 5 },
                new SpectrumPeak { Mass = 524.372, Intensity = 5 },
                new SpectrumPeak { Mass = 526.330, Intensity = 5 },
                new SpectrumPeak { Mass = 544.340, Intensity = 5 },
                new SpectrumPeak { Mass = 810.601, Intensity = 30 },
            },
        };
    }

    /// <summary>
    /// The leaf reads RT and RI from the same ChromXs, so both have to be populated at once. The
    /// single-value constructors set one and leave the other at its default.
    /// </summary>
    private static ChromXs TimeAndIndex(double retentionTime, double retentionIndex) {
        return new ChromXs(
            new RetentionTime(retentionTime, ChromXUnit.Min),
            new RetentionIndex(retentionIndex),
            DriftTime.Default,
            MzValue.Default,
            ChromXType.RT);
    }
}
