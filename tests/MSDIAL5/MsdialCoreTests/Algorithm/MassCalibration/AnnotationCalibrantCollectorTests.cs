using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration.Tests;

[TestClass]
public class AnnotationCalibrantCollectorTests
{
    private const double Ms1Shift = 40d;
    private const double Ms2Shift = -25d;

    // Caffeine, [M+H]+, with three fragments whose m/z are taken as computed.
    private static MoleculeMsReference Caffeine(string name = "Caffeine", int id = 1, string inChIKey = "RYYVLZVUVIJVGH-UHFFFAOYSA-N") {
        var formula = FormulaStringParcer.OrganicElementsReader("C8H10N4O2");
        var adduct = AdductIon.GetAdductIon("[M+H]+");
        return new MoleculeMsReference
        {
            ScanID = id,
            Name = name,
            InChIKey = inChIKey,
            Formula = formula,
            AdductType = adduct,
            PrecursorMz = adduct.ConvertToMz(formula.Mass),
            Spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(110.07127, 20d),
                new SpectrumPeak(138.06619, 100d),
                new SpectrumPeak(adduct.ConvertToMz(formula.Mass), 40d),
            },
        };
    }

    // PE 16:0_18:2, [M-H]-, with its two fatty acid anions and its lyso fragment, as an LBM generates it.
    private static MoleculeMsReference GeneratedLipid() {
        var formula = FormulaStringParcer.OrganicElementsReader("C39H74NO8P");
        var adduct = AdductIon.GetAdductIon("[M-H]-");
        return new MoleculeMsReference
        {
            ScanID = 11,
            Name = "PE 16:0_18:2",
            CompoundClass = "PE",
            IonMode = IonMode.Negative,
            Formula = formula,
            AdductType = adduct,
            PrecursorMz = adduct.ConvertToMz(formula.Mass),
            Spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(255.23295, 100d),
                new SpectrumPeak(279.23295, 80d),
                new SpectrumPeak(452.27826, 30d),
            },
        };
    }

    private static MoleculeMsReference WithoutFormula(string name, double precursorMz, int id) {
        return new MoleculeMsReference
        {
            ScanID = id,
            Name = name,
            InChIKey = "AAAAAAAAAAAAAA-UHFFFAOYSA-N",
            PrecursorMz = precursorMz,
            Spectrum = new List<SpectrumPeak> { new SpectrumPeak(0.5d * precursorMz, 100d) },
        };
    }

    private static ChromatogramPeakFeature Feature(int peakId, double mz, int isotopeWeightNumber = 0) {
        var feature = new ChromatogramPeakFeature { MasterPeakID = peakId, PeakID = peakId };
        feature.PeakFeature.Mass = mz;
        feature.PeakFeature.PeakHeightTop = 2e5;
        feature.PeakFeature.ChromXsTop = new ChromXs(3.2d);
        feature.PeakCharacter.IsotopeWeightNumber = isotopeWeightNumber;
        return feature;
    }

    // The deconvoluted spectrum of a caffeine whose product ions read Ms2Shift off, with a weaker unrelated ion
    // beside one of them.
    private static MSDecResult CaffeineSpectrum(MoleculeMsReference caffeine) {
        var shift = 1d + Ms2Shift * 1e-6;
        var result = new MSDecResult();
        result.Spectrum.Add(new SpectrumPeak(110.07127 * shift, 3000d));
        result.Spectrum.Add(new SpectrumPeak(138.06619 * shift, 15000d));
        result.Spectrum.Add(new SpectrumPeak(138.06619 * (1d + 60e-6), 400d));
        result.Spectrum.Add(new SpectrumPeak(caffeine.PrecursorMz * shift, 6000d));
        return result;
    }

    private static MsScanMatchResult Match(MoleculeMsReference reference, double dotProduct, bool spectrumMatch = true) {
        var squared = (float)(dotProduct * dotProduct);
        return new MsScanMatchResult
        {
            Name = reference.Name,
            LibraryID = reference.ScanID,
            InChIKey = reference.InChIKey,
            SquaredWeightedDotProduct = squared,
            SquaredSimpleDotProduct = squared,
            SquaredReverseDotProduct = squared,
            MatchedPeaksPercentage = (float)dotProduct,
            AcurateMassSimilarity = 0.1f,
            IsSpectrumMatch = spectrumMatch,
        };
    }

    private static CalibrationLibrary Library(string id, DataBaseSource source, params (MoleculeMsReference Reference, MsScanMatchResult Result)[] candidates) {
        return new CalibrationLibrary(new FakeAnnotator(id, candidates), new MsRefSearchParameterBase(), TargetOmics.Metabolomics, source);
    }

    [TestMethod]
    public void TheReferenceIsChosenByItsSpectrumNotByTheNearestPrecursorMz() {
        var caffeine = Caffeine();
        var observed = caffeine.PrecursorMz * (1d + Ms1Shift * 1e-6);
        // Another compound right at the observed m/z, whose spectrum matches less well: the total score would have
        // taken it for its precursor m/z, and read the error as zero.
        var nearer = WithoutFormula("Nearer", observed, 2);
        var collector = new AnnotationCalibrantCollector(new[] { Library("msp", DataBaseSource.Msp, (caffeine, Match(caffeine, 0.9)), (nearer, Match(nearer, 0.6))) });

        var calibrants = collector.Collect(new[] { Feature(7, observed) }, new[] { CaffeineSpectrum(caffeine) });

        var ms1 = calibrants.Single(c => c.MsLevel == 1);
        Assert.AreEqual("Caffeine", ms1.ReferenceName);
        Assert.AreEqual(Ms1Shift, ms1.ErrorPpm, 1e-6);
        Assert.AreEqual(AnnotationCalibrant.FromFormula, ms1.ReferenceMzSource);
        Assert.AreEqual(7, ms1.PeakId);
        Assert.AreEqual(3.2d, ms1.RetentionTime, 1e-9);
        Assert.AreEqual(2e5, ms1.ObservedIntensity);
        Assert.IsFalse(calibrants.Any(c => c.MsLevel == 2), "an acquired library's fragment m/z are not theoretical");
    }

    [TestMethod]
    public void ACandidateWhoseSpectrumDoesNotMatchIsNotTaken() {
        var caffeine = Caffeine();
        var observed = caffeine.PrecursorMz * (1d + Ms1Shift * 1e-6);
        var collector = new AnnotationCalibrantCollector(new[] { Library("msp", DataBaseSource.Msp, (caffeine, Match(caffeine, 0.95, spectrumMatch: false))) });

        Assert.AreEqual(0, collector.Collect(new[] { Feature(1, observed) }, new[] { CaffeineSpectrum(caffeine) }).Count);
    }

    [TestMethod]
    public void ProductIonsAreMeasuredAgainstAnInSilicoLibrary() {
        var caffeine = Caffeine();
        var observed = caffeine.PrecursorMz * (1d + Ms1Shift * 1e-6);
        var collector = new AnnotationCalibrantCollector(new[] { Library("insilico", DataBaseSource.PredictedMsp, (caffeine, Match(caffeine, 0.9))) });

        var calibrants = collector.Collect(new[] { Feature(1, observed) }, new[] { CaffeineSpectrum(caffeine) });

        var ms2 = calibrants.Where(c => c.MsLevel == 2).OrderBy(c => c.ReferenceMz).ToList();
        Assert.AreEqual(3, ms2.Count);
        Assert.IsTrue(ms2.All(c => Math.Abs(c.ErrorPpm - Ms2Shift) < 1e-6), "the strongest ion in each window, not the weaker one beside it");
        CollectionAssert.AreEqual(new[] { false, false, true }, ms2.Select(c => c.IsPrecursorIon).ToArray());
        Assert.IsTrue(ms2.All(c => c.ReferenceMzSource == AnnotationCalibrant.FromLibraryFragment && c.DataBaseSource == DataBaseSource.PredictedMsp && c.PrecursorMz == observed));
        Assert.AreEqual(15000d, ms2[1].ObservedIntensity);
        Assert.AreEqual(Ms1Shift, calibrants.Single(c => c.MsLevel == 1).ErrorPpm, 1e-6);
    }

    [TestMethod]
    public void OfAnLbmOnlyTheGeneratedLipidsGiveProductIons() {
        var lipid = GeneratedLipid();
        var lipidMz = lipid.PrecursorMz * (1d + Ms1Shift * 1e-6);
        var lipidSpectrum = new MSDecResult();
        foreach (var fragment in lipid.Spectrum) {
            lipidSpectrum.Spectrum.Add(new SpectrumPeak(fragment.Mass * (1d + Ms2Shift * 1e-6), 100d * fragment.Intensity));
        }
        // An acquired spectrum that an LBM converted from an MSP carries: its fragment m/z were measured.
        var caffeine = Caffeine();
        var caffeineMz = caffeine.PrecursorMz * (1d + Ms1Shift * 1e-6);
        var collector = new AnnotationCalibrantCollector(new[] { Library("lbm", DataBaseSource.Lbm, (lipid, Match(lipid, 0.9)), (caffeine, Match(caffeine, 0.9))) });

        var calibrants = collector.Collect(new[] { Feature(1, lipidMz), Feature(2, caffeineMz) }, new[] { lipidSpectrum, CaffeineSpectrum(caffeine) });

        Assert.IsTrue(CalibrationLibraryIsGeneratedLipid(lipid));
        Assert.IsFalse(CalibrationLibraryIsGeneratedLipid(caffeine));
        Assert.AreEqual(2, calibrants.Count(c => c.MsLevel == 1), "both precursors are measured");
        var ms2 = calibrants.Where(c => c.MsLevel == 2).ToList();
        Assert.AreEqual(3, ms2.Count);
        Assert.IsTrue(ms2.All(c => c.PeakId == 1 && Math.Abs(c.ErrorPpm - Ms2Shift) < 1e-6), "the lipid's fragments, and none of the acquired record's");
    }

    private static bool CalibrationLibraryIsGeneratedLipid(MoleculeMsReference reference) => CalibrationLibrary.IsGeneratedLipid(reference);

    [TestMethod]
    public void TheProductIonsComeFromTheBestComputedLibraryMatchEvenWhenAnAcquiredOneMatchesBetter() {
        var caffeine = Caffeine();
        var inSilico = Caffeine("Caffeine (LBM-like)", id: 5);
        var observed = caffeine.PrecursorMz * (1d + Ms1Shift * 1e-6);
        var collector = new AnnotationCalibrantCollector(new[]
        {
            Library("msp", DataBaseSource.Msp, (caffeine, Match(caffeine, 0.95))),
            Library("insilico", DataBaseSource.PredictedMsp, (inSilico, Match(inSilico, 0.7))),
        });

        var calibrants = collector.Collect(new[] { Feature(1, observed) }, new[] { CaffeineSpectrum(caffeine) });

        Assert.AreEqual("msp", calibrants.Single(c => c.MsLevel == 1).AnnotatorId);
        Assert.IsTrue(calibrants.Where(c => c.MsLevel == 2).All(c => c.AnnotatorId == "insilico"));
        Assert.AreEqual(3, calibrants.Count(c => c.MsLevel == 2));
    }

    [TestMethod]
    public void TiedCandidatesGiveAPrecursorOnlyWhenTheyShareItsMzAndNoProductIons() {
        var caffeine = Caffeine();
        var observed = caffeine.PrecursorMz * (1d + Ms1Shift * 1e-6);
        var isomer = Caffeine("Isomer of caffeine", id: 2, inChIKey: "BBBBBBBBBBBBBB-UHFFFAOYSA-N");
        var other = WithoutFormula("Other formula", caffeine.PrecursorMz * (1d + 70e-6), 3);

        var isomers = new AnnotationCalibrantCollector(new[] { Library("insilico", DataBaseSource.PredictedMsp, (caffeine, Match(caffeine, 0.8)), (isomer, Match(isomer, 0.8))) })
            .Collect(new[] { Feature(1, observed) }, new[] { CaffeineSpectrum(caffeine) });
        var formulas = new AnnotationCalibrantCollector(new[] { Library("insilico", DataBaseSource.PredictedMsp, (caffeine, Match(caffeine, 0.8)), (other, Match(other, 0.8))) })
            .Collect(new[] { Feature(1, observed) }, new[] { CaffeineSpectrum(caffeine) });

        Assert.AreEqual(1, isomers.Count(c => c.MsLevel == 1), "an isomer has the same m/z");
        Assert.AreEqual(0, isomers.Count(c => c.MsLevel == 2), "but not necessarily the same fragments");
        Assert.AreEqual(0, formulas.Count);
    }

    [TestMethod]
    public void IsotopePeaksAndFeaturesWithoutASpectrumAreNotSearched() {
        var caffeine = Caffeine();
        var annotator = new FakeAnnotator("lbm", new[] { (caffeine, Match(caffeine, 0.9)) });
        var collector = new AnnotationCalibrantCollector(new[] { new CalibrationLibrary(annotator, new MsRefSearchParameterBase(), TargetOmics.Metabolomics, DataBaseSource.Lbm) });

        var calibrants = collector.Collect(
            new[] { Feature(1, caffeine.PrecursorMz, isotopeWeightNumber: 1), Feature(2, caffeine.PrecursorMz) },
            new[] { CaffeineSpectrum(caffeine), new MSDecResult() });

        Assert.AreEqual(0, calibrants.Count);
        Assert.AreEqual(0, annotator.Queries.Count);
    }

    [TestMethod]
    [DataRow(150d, 0.025d)]
    [DataRow(300d, 0.03d)]
    [DataRow(800d, 0.08d)]
    public void TheSearchWindowIsTheSamePpmAtEveryMzAndTheMs2ToleranceIsNeverNarrowed(double mz, double ms2Tolerance) {
        var library = new MsRefSearchParameterBase { Ms1Tolerance = 0.01f, Ms2Tolerance = 0.025f, SquaredWeightedDotProductCutOff = 0.3f };

        var wide = AnnotationCalibrantCollector.Widen(library, mz, 100d);

        Assert.AreEqual(100e-6 * mz, MolecularFormulaUtility.FixMassTolerance(wide.Ms1Tolerance, mz), 1e-6);
        Assert.AreEqual(ms2Tolerance, wide.Ms2Tolerance, 1e-6);
        Assert.AreEqual(0.3f, wide.SquaredWeightedDotProductCutOff, "the spectrum cut-offs are the library's own");
        Assert.AreEqual(0.01f, library.Ms1Tolerance, "the library's settings are left alone");
    }

    [TestMethod]
    public void TheCollectorSearchesWithTheWidenedWindow() {
        var caffeine = Caffeine();
        // 80 ppm off: outside the library's own 0.01 Da, inside the ±100 ppm search.
        var observed = caffeine.PrecursorMz * (1d + 80e-6);
        var annotator = new FakeAnnotator("msp", new[] { (caffeine, Match(caffeine, 0.9)) });
        var collector = new AnnotationCalibrantCollector(new[] { new CalibrationLibrary(annotator, new MsRefSearchParameterBase { Ms1Tolerance = 0.01f }, TargetOmics.Metabolomics, DataBaseSource.Msp) });

        var calibrants = collector.Collect(new[] { Feature(1, observed) }, new[] { CaffeineSpectrum(caffeine) });

        Assert.AreEqual(80d, calibrants.Single().ErrorPpm, 1e-6);
        Assert.IsTrue(annotator.Queries.Single().Parameter.Ms1Tolerance > 0.01f, "the query carried the wider window");
    }

    [TestMethod]
    public void TheScoreLeavesOutThePrecursorMzAndKeepsTheRestOfTheTotalScore() {
        var caffeine = Caffeine();
        var near = Match(caffeine, 0.8);
        near.AcurateMassSimilarity = 1f;
        var far = Match(caffeine, 0.8);
        far.AcurateMassSimilarity = 0f;
        var noInChIKey = Match(caffeine, 0.8);
        noInChIKey.InChIKey = string.Empty;
        var withRt = Match(caffeine, 0.8);
        withRt.RtSimilarity = 0f;

        var score = AnnotationCalibrantCollector.ScoreWithoutPrecursorMz(near, TargetOmics.Metabolomics, useRetentionTime: false);

        Assert.AreEqual(score, AnnotationCalibrantCollector.ScoreWithoutPrecursorMz(far, TargetOmics.Metabolomics, useRetentionTime: false));
        Assert.AreEqual(0.9d * score, AnnotationCalibrantCollector.ScoreWithoutPrecursorMz(noInChIKey, TargetOmics.Metabolomics, useRetentionTime: false), 1e-6);
        Assert.IsTrue(AnnotationCalibrantCollector.ScoreWithoutPrecursorMz(withRt, TargetOmics.Metabolomics, useRetentionTime: true) < score, "a retention time the library scores still counts");
    }

    [TestMethod]
    public void ThePrecursorMzIsTheFormulasThroughTheAdductUnlessTheRecordContradictsIt() {
        var caffeine = Caffeine();
        var computed = AnnotationCalibrantCollector.TheoreticalPrecursorMz(caffeine, out var source);
        Assert.AreEqual(195.08765d, computed, 1e-4);
        Assert.AreEqual(AnnotationCalibrant.FromFormula, source);

        var contradicted = Caffeine();
        contradicted.PrecursorMz += 0.01d;
        Assert.IsTrue(double.IsNaN(AnnotationCalibrantCollector.TheoreticalPrecursorMz(contradicted, out _)), "51 ppm between the formula and the record");

        var roundedRecord = Caffeine();
        roundedRecord.PrecursorMz = Math.Round(roundedRecord.PrecursorMz, 3);
        Assert.AreEqual(computed, AnnotationCalibrantCollector.TheoreticalPrecursorMz(roundedRecord, out _), 1e-9, "a record rounded to 3 decimals still agrees, and the formula's m/z is used");

        var noFormula = WithoutFormula("x", 300.1234d, 9);
        Assert.AreEqual(300.1234d, AnnotationCalibrantCollector.TheoreticalPrecursorMz(noFormula, out var librarySource));
        Assert.AreEqual(AnnotationCalibrant.FromLibraryPrecursor, librarySource);
    }

    [TestMethod]
    public void AFragmentIsPairedWithTheStrongestPeakInItsWindowAndCrowdedFragmentsAreLeftOut() {
        var observed = new[] { new SpectrumPeak(99.9950, 10d), new SpectrumPeak(100.0020, 500d), new SpectrumPeak(100.0200, 900d), new SpectrumPeak(250.0000, 40d) };
        var reference = new List<SpectrumPeak>
        {
            new SpectrumPeak(100.0000, 100d),
            new SpectrumPeak(180.0000, 50d),
            new SpectrumPeak(250.0100, 30d),
            new SpectrumPeak(250.0300, 30d),
        };

        var pairs = AnnotationCalibrantCollector.PairFragments(observed, reference, 100d);

        Assert.AreEqual(1, pairs.Count, "180 has no peak; 250.01 and 250.03 lie within two windows of each other");
        Assert.AreEqual(100.0000d, pairs[0].Reference.Mass);
        Assert.AreEqual(100.0020d, pairs[0].Observed.Mass, "100.0200 is 200 ppm away, outside the window");
    }

    [TestMethod]
    public void APeakThatMayBeTheM2IsotopeOfAnIonWithOneMoreDoubleBondIsLeftOut() {
        // LPE 18:3 and LPE 18:2 fragments, 2H apart. The M+2 of 474.2626 lies 0.0089 Da below 476.2783.
        var reference = new List<SpectrumPeak> { new SpectrumPeak(476.27826, 100d) };
        var isotopeOnly = new[] { new SpectrumPeak(474.26261, 10000d), new SpectrumPeak(476.26932, 400d) };
        var weakerParent = new[] { new SpectrumPeak(474.26261, 1000d), new SpectrumPeak(476.27826, 5000d) };

        Assert.AreEqual(0, AnnotationCalibrantCollector.PairFragments(isotopeOnly, reference, 100d).Count, "the peak in the window is the other ion's M+2");
        var pair = AnnotationCalibrantCollector.PairFragments(weakerParent, reference, 100d).Single();
        Assert.AreEqual(476.27826d, pair.Observed.Mass, "an ion five times stronger than the one 2H below keeps its pair");
    }

    [TestMethod]
    public void TheLighterIonIsLookedForInTheRawScanWhereDeconvolutionMayHaveLeftItOut() {
        // The neighbouring lipid's 474.2626 went to its own deconvoluted spectrum; its M+2 stayed in this one.
        var reference = new List<SpectrumPeak> { new SpectrumPeak(476.27826, 100d) };
        var deconvoluted = new[] { new SpectrumPeak(476.26932, 400d) };
        var raw = new[] { new RawPeakElement { Mz = 474.26261, Intensity = 9000d }, new RawPeakElement { Mz = 476.26932, Intensity = 450d } };
        var rawWithoutIt = new[] { new RawPeakElement { Mz = 476.26932, Intensity = 450d } };

        Assert.AreEqual(1, AnnotationCalibrantCollector.PairFragments(deconvoluted, reference, 100d).Count, "the deconvoluted spectrum alone does not show it");
        Assert.AreEqual(0, AnnotationCalibrantCollector.PairFragments(deconvoluted, reference, 100d, raw).Count);
        Assert.AreEqual(1, AnnotationCalibrantCollector.PairFragments(deconvoluted, reference, 100d, rawWithoutIt).Count);
    }

    [TestMethod]
    public void TheCollectorReadsTheRawScanTheSpectrumWasDeconvolutedAt() {
        var lipid = GeneratedLipid();
        var observedMz = lipid.PrecursorMz;
        var spectrum = new MSDecResult { RawSpectrumID = 1 };
        foreach (var fragment in lipid.Spectrum) {
            spectrum.Spectrum.Add(new SpectrumPeak(fragment.Mass, 100d * fragment.Intensity));
        }
        // An ion 2H below the lyso fragment, ten times stronger, in the raw scan only.
        var rawSpectra = new List<RawSpectrum>
        {
            new RawSpectrum { Index = 0, MsLevel = 1, Spectrum = new[] { new RawPeakElement { Mz = observedMz, Intensity = 1e5 } } },
            new RawSpectrum
            {
                Index = 1,
                MsLevel = 2,
                Spectrum = lipid.Spectrum.Select(fragment => new RawPeakElement { Mz = fragment.Mass, Intensity = 100d * fragment.Intensity })
                    .Append(new RawPeakElement { Mz = 452.27826 - 2.01565, Intensity = 30000d })
                    .OrderBy(peak => peak.Mz).ToArray(),
            },
        };
        var collector = new AnnotationCalibrantCollector(new[] { Library("lbm", DataBaseSource.Lbm, (lipid, Match(lipid, 0.9))) });

        var withRaw = collector.Collect(new[] { Feature(1, observedMz) }, new[] { spectrum }, rawSpectra);
        var withoutRaw = collector.Collect(new[] { Feature(1, observedMz) }, new[] { spectrum });

        CollectionAssert.AreEquivalent(new[] { 255.23295, 279.23295 }, withRaw.Where(c => c.MsLevel == 2).Select(c => c.ReferenceMz).ToArray());
        Assert.AreEqual(3, withoutRaw.Count(c => c.MsLevel == 2));
    }

    private sealed class FakeAnnotator : IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>
    {
        private readonly (MoleculeMsReference Reference, MsScanMatchResult Result)[] _candidates;

        public FakeAnnotator(string id, IEnumerable<(MoleculeMsReference, MsScanMatchResult)> candidates) {
            Id = id;
            _candidates = candidates.ToArray();
        }

        public List<IAnnotationQuery<MsScanMatchResult>> Queries { get; } = new List<IAnnotationQuery<MsScanMatchResult>>();

        public string Id { get; }

        public int Priority => 1;

        public string Key => Id;

        // As LcmsMspAnnotator searches: the records within the query's MS1 tolerance of the feature.
        public List<MsScanMatchResult> FindCandidates(IAnnotationQuery<MsScanMatchResult> query) {
            Queries.Add(query);
            var tolerance = MolecularFormulaUtility.FixMassTolerance(query.Parameter.Ms1Tolerance, query.Property.PrecursorMz);
            return _candidates.Where(candidate => Math.Abs(candidate.Reference.PrecursorMz - query.Property.PrecursorMz) <= tolerance).Select(candidate => candidate.Result).ToList();
        }

        public MoleculeMsReference Refer(MsScanMatchResult result) => _candidates.First(candidate => ReferenceEquals(candidate.Result, result)).Reference;

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) => throw new NotSupportedException();

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) => throw new NotSupportedException();

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) => throw new NotSupportedException();

        public bool IsReferenceMatched(MsScanMatchResult result) => throw new NotSupportedException();

        public bool IsAnnotationSuggested(MsScanMatchResult result) => throw new NotSupportedException();
    }
}
