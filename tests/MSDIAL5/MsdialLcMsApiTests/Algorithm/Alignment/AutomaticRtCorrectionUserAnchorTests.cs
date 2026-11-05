using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment.Tests;

/// <summary>
/// The anchor library of the automatic alignment RT correction: anchors a person names (UserOnly),
/// or names and excludes beside the automatic selection (Hybrid).
/// </summary>
[TestClass]
public class AutomaticRtCorrectionUserAnchorTests {
    [TestMethod]
    public void UserOnly_MapsEachFileOntoTheReferenceFilesObservedRts_NotTheLibraryRts() {
        var files = new[] {
            File(0, "early", AnalysisFileType.Sample, 1),
            File(1, "middle", AnalysisFileType.QC, 2),
            File(2, "late", AnalysisFileType.Sample, 3),
            File(3, "blank", AnalysisFileType.Blank, 4),
        };
        var library = new[] {
            Anchor(0, "A", 150d, 2.3d),
            Anchor(1, "B", 250d, 6.3d),
            Anchor(2, "C", 350d, 10.3d),
            Anchor(3, "D", 450d, 14d),
            Anchor(4, "not an anchor", 550d, 8d, use: false),
        };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 2.1d, 6.2d, 10.3d, null },
            [1] = new double?[] { 2.0d, 6.0d, 10.0d, null },
            [2] = new double?[] { 1.9d, 5.8d, null, null },
            // The Blank has more anchors than any other file, all at their medians: were a Blank
            // eligible, it would be the reference.
            [3] = new double?[] { 2.0d, 6.0d, 10.15d, 14d },
        };
        var parameter = UserOnly(minimumAnchorCount: 2);

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library, Observed(observed));

        Assert.AreEqual(1, result.Correction.ReferenceFileId, "the non-Blank file with the most anchors, then the anchor RTs closest to their medians");
        Assert.AreEqual(AutomaticRtCorrectionAnchorMode.UserOnly, result.AnchorMode);
        Assert.AreEqual(2.0d, result.Correction.Correct(0, 2.1d), 1e-9);
        Assert.AreEqual(6.0d, result.Correction.Correct(0, 6.2d), 1e-9);
        Assert.AreEqual(10.0d, result.Correction.Correct(0, 10.3d), 1e-9);
        Assert.AreEqual(2.0d, result.Correction.Correct(2, 1.9d), 1e-9);
        Assert.AreEqual(6.0d, result.Correction.Correct(2, 5.8d), 1e-9);
        Assert.AreEqual(10.0d, result.Correction.Correct(2, 9.8d), 1e-9, "the last offset is held beyond the last anchor");

        var anchors = result.Anchors.Where(anchor => anchor.FileId == 1 && anchor.Used).OrderBy(anchor => anchor.AnchorId).ToList();
        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, anchors.Select(anchor => anchor.AnchorName).ToArray());
        Assert.IsTrue(anchors.All(anchor => anchor.Source == AutomaticRtCorrectionAnchorSource.User));
        CollectionAssert.AreEqual(new[] { 2.0d, 6.0d, 10.0d }, anchors.Select(anchor => anchor.ReferenceRt).ToArray());
        CollectionAssert.AreEqual(new double?[] { 2.3d, 6.3d, 10.3d }, anchors.Select(anchor => anchor.LibraryRt).ToArray());
        Assert.IsTrue(result.Anchors.All(anchor => anchor.AnchorName != "not an anchor"), "a False row is no anchor");

        var missing = result.Anchors.Single(anchor => anchor.FileId == 2 && anchor.AnchorName == "C");
        Assert.AreEqual("Missing", missing.Status);
        Assert.IsFalse(missing.Used);
        Assert.AreEqual(2d / 3d, missing.SampleCoverage, 1e-9);

        var d = result.Anchors.Where(anchor => anchor.AnchorName == "D").OrderBy(anchor => anchor.FileId).ToList();
        Assert.AreEqual(4, d.Count);
        Assert.IsTrue(d.All(anchor => anchor.Status == "MissingInReference" && double.IsNaN(anchor.ReferenceRt) && !anchor.Used));
        Assert.AreEqual(14d, d[3].OriginalRt!.Value, 1e-9, "the Blank's detection is still recorded");

        Assert.AreEqual(2, result.Files.Single(file => file.FileId == 2).UserAnchorsDetected);
        Assert.IsTrue(result.Files.All(file => file.ExcludedAutomaticCandidates is null));
        Assert.AreEqual(AutomaticRtCorrectionModelSource.NearestBlank, result.Files.Single(file => file.FileId == 3).ModelSource);
    }

    [TestMethod]
    public void UserOnly_AFileThatLostOneOfThreeAnchorsIsCorrectedFromTheOtherTwo() {
        var files = new[] {
            File(0, "reference", AnalysisFileType.Sample, 1),
            File(1, "lost one", AnalysisFileType.Sample, 2),
            File(2, "lost two", AnalysisFileType.Sample, 3),
        };
        var library = new[] { Anchor(0, "IS1", 150d, 3.5d, rtTolerance: 3d), Anchor(1, "IS2", 250d, 9.5d, rtTolerance: 3d), Anchor(2, "IS3", 350d, 17.4d, rtTolerance: 3d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 3.5d, 9.5d, 17.4d },
            [1] = new double?[] { 3.6d, null, 17.9d },
            [2] = new double?[] { null, null, 17.0d },
        };
        var parameter = UserOnly(minimumAnchorCount: 3);
        parameter.ReferenceFileId = 0;

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library, Observed(observed));

        var lostOne = result.Files.Single(file => file.FileId == 1);
        Assert.AreEqual(AutomaticRtCorrectionModelSource.DetectedAnchors, lostOne.ModelSource, "two anchors draw a map");
        Assert.AreEqual(2, lostOne.UsedAnchorCount);
        Assert.AreEqual(3.5d, result.Correction.Correct(1, 3.6d), 1e-9);
        Assert.AreEqual(17.4d, result.Correction.Correct(1, 17.9d), 1e-9);
        Assert.AreEqual(AutomaticRtCorrectionModelSource.Uncorrected, result.Files.Single(file => file.FileId == 2).ModelSource, "one anchor draws none");
    }

    [TestMethod]
    public void UserOnly_TheFileWithMoreAnchorsIsTheReferenceEvenWhenAnotherIsMoreCentral() {
        var files = Enumerable.Range(0, 4).Select(id => File(id, $"f{id}", AnalysisFileType.Sample, id + 1)).ToArray();
        var library = new[] { Anchor(0, "A", 150d, 2d), Anchor(1, "B", 250d, 6d), Anchor(2, "C", 350d, 10d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 2.5d, 6.5d, 10.5d },
            [1] = new double?[] { 2d, 6d, null },
            [2] = new double?[] { 2d, 6d, null },
            [3] = new double?[] { 2d, 6d, null },
        };
        var parameter = UserOnly(minimumAnchorCount: 2);
        parameter.ReferenceCentralityWeight = 1f;

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library, Observed(observed));

        Assert.AreEqual(0, result.Correction.ReferenceFileId, "an anchor missing from the reference corrects no file");
        Assert.IsTrue(result.Files.Single(file => file.FileId == 1).ReferenceScore > result.Files.Single(file => file.FileId == 0).ReferenceScore,
            "file 1 scores higher, and loses on the count");
    }

    [TestMethod]
    public void UserOnly_ARequestedReferenceWithTooFewAnchorsIsRefused() {
        var files = new[] { File(0, "a", AnalysisFileType.Sample, 1), File(1, "b", AnalysisFileType.Sample, 2) };
        var library = new[] { Anchor(0, "A", 150d, 2d), Anchor(1, "B", 250d, 6d), Anchor(2, "C", 350d, 10d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 2d, 6d, 10d },
            [1] = new double?[] { 2d, null, 10d },
        };
        var parameter = UserOnly(minimumAnchorCount: 3);
        parameter.ReferenceFileId = 1;

        var error = Assert.ThrowsException<AutomaticRtCorrectionFailedException>(
            () => AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library, Observed(observed)));
        StringAssert.Contains(error.Message, "reference file ID 1");
    }

    [TestMethod]
    public void UserOnly_RefusesALibraryWithFewerTrueRowsThanTheMinimumAnchorCount() {
        var files = new[] { File(0, "a", AnalysisFileType.Sample, 1) };
        var library = new[] { Anchor(0, "A", 150d, 2d), Anchor(1, "B", 250d, 6d), Anchor(2, "C", 350d, 10d, use: false) };

        Assert.ThrowsException<InvalidDataException>(
            () => AutomaticAlignmentRetentionTimeCorrection.Build(files, UserOnly(minimumAnchorCount: 3), 0.01d, AutomaticPeaks, library, Observed(new Dictionary<int, double?[]>())));
    }

    [TestMethod]
    public void AModeThatReadsALibrary_IsRefusedByTheOverloadThatTakesNone() {
        var files = new[] { File(0, "a", AnalysisFileType.Sample, 1) };
        foreach (var mode in new[] { AutomaticRtCorrectionAnchorMode.UserOnly, AutomaticRtCorrectionAnchorMode.Hybrid }) {
            var parameter = UserOnly(minimumAnchorCount: 2);
            parameter.AnchorMode = mode;
            Assert.ThrowsException<InvalidDataException>(
                () => AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, _ => Ms1CycleProfile.Empty),
                mode.ToString());
        }
    }

    [TestMethod]
    public void Check_RefusesRowsItCannotSearch() {
        var parameter = UserOnly(minimumAnchorCount: 2);
        var good = new[] { Anchor(0, "A", 150d, 2d), Anchor(1, "B", 250d, 6d) };
        AutomaticRtCorrectionAnchorLibrary.Check(good, parameter);

        foreach (var broken in new Action<MoleculeMsReference>[] {
            row => row.PrecursorMz = 0d,
            row => row.MassTolerance = 0f,
            row => row.RetentionTimeTolerance = 0f,
            row => row.MinimumPeakHeight = -1f,
        }) {
            var rows = new[] { Anchor(0, "A", 150d, 2d), Anchor(1, "B", 250d, 6d) };
            broken(rows[1]);
            var error = Assert.ThrowsException<InvalidDataException>(() => AutomaticRtCorrectionAnchorLibrary.Check(rows, parameter));
            StringAssert.Contains(error.Message, "row 2 (B)");
        }

        parameter.AnchorMode = AutomaticRtCorrectionAnchorMode.Hybrid;
        Assert.ThrowsException<InvalidDataException>(() => AutomaticRtCorrectionAnchorLibrary.Check(Array.Empty<MoleculeMsReference>(), parameter));
        var exclusionWithoutWindow = Anchor(0, "X", 150d, 2d, use: false);
        exclusionWithoutWindow.RetentionTimeTolerance = 0f;
        Assert.ThrowsException<InvalidDataException>(() => AutomaticRtCorrectionAnchorLibrary.Check(new[] { exclusionWithoutWindow }, parameter));

        parameter.AnchorMode = AutomaticRtCorrectionAnchorMode.Automatic;
        AutomaticRtCorrectionAnchorLibrary.Check(new[] { exclusionWithoutWindow }, parameter);
    }

    [TestMethod]
    public void ValidateParameter_NamesTheSettingItRefuses() {
        var parameter = UserOnly(minimumAnchorCount: 1);

        var error = Assert.ThrowsException<ArgumentOutOfRangeException>(() => AutomaticAlignmentRetentionTimeCorrection.ValidateParameter(parameter, 0.01d));

        Assert.AreEqual(nameof(parameter.MinimumAnchorCount), error.ParamName);
        StringAssert.Contains(error.Message, "minimum anchor count must be 2 or more");
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => AutomaticAlignmentRetentionTimeCorrection.ValidateParameter(UserOnly(minimumAnchorCount: 2), double.NaN));
        foreach (var weight in new[] { -0.1f, 1.1f, float.NaN }) {
            var weighted = UserOnly(minimumAnchorCount: 2);
            weighted.AnchorPeakSelectionRtWeight = weight;
            Assert.AreEqual(nameof(weighted.AnchorPeakSelectionRtWeight),
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => AutomaticAlignmentRetentionTimeCorrection.ValidateParameter(weighted, 0.01d)).ParamName);
        }
    }

    [TestMethod]
    public void Hybrid_KeepsUserAnchorsWhateverTheirCoverage_AndFillsOnlyTheOtherBins() {
        var files = HybridFiles();
        var library = new[] { Anchor(0, "U", 555d, 5.5d, rtTolerance: 0.5d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5.5d },
            [1] = new double?[] { 5.6d },
            [2] = new double?[] { null },
        };

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, Hybrid(), 0.01d, HybridPeaks, library, Observed(observed));

        Assert.AreEqual(0, result.Correction.ReferenceFileId);
        var reference = UsedInReference(result);
        Assert.AreEqual(5, reference.Count, "the maximum anchor count, the user anchor included");
        var user = reference.Single(anchor => anchor.Source == AutomaticRtCorrectionAnchorSource.User);
        Assert.AreEqual(5.5d, user.ReferenceRt, 1e-9);
        Assert.AreEqual(2d / 3d, user.SampleCoverage, 1e-9, "below the minimum coverage, and still an anchor");
        CollectionAssert.AreEqual(new[] { 2d, 4d, 5.5d, 8d, 10d }, reference.Select(anchor => anchor.ReferenceRt).ToArray());

        Assert.AreEqual(5.5d, result.Correction.Correct(1, 5.6d), 1e-9);
        Assert.AreEqual("Missing", result.Anchors.Single(anchor => anchor.FileId == 2 && anchor.Source == AutomaticRtCorrectionAnchorSource.User).Status);
        Assert.AreEqual(1, result.Files.Single(file => file.FileId == 1).UserAnchorsDetected);
        Assert.AreEqual(0, result.Files.Single(file => file.FileId == 0).ExcludedAutomaticCandidates);
        Assert.IsNull(result.Files.Single(file => file.FileId == 1).ExcludedAutomaticCandidates, "counted on the reference file only");
    }

    [TestMethod]
    public void Hybrid_ABinHoldingAUserAnchorOffersNoAutomaticOne() {
        var files = HybridFiles();
        var library = new[] { Anchor(0, "U", 555d, 5.5d, rtTolerance: 0.5d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5.5d },
            [1] = new double?[] { 5.6d },
            [2] = new double?[] { 5.4d },
        };
        var parameter = Hybrid();
        parameter.MaximumAnchorCount = 6;

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, HybridPeaks, library, Observed(observed));

        // Six bins of 1.5 min over 1-10 min; the user anchor holds [5.5, 7), so its bin offers
        // nothing, and the 6-min candidate, which would win a place on quality, is not taken.
        CollectionAssert.AreEqual(new[] { 2d, 3d, 5d, 5.5d, 8d, 10d }, UsedInReference(result).Select(anchor => anchor.ReferenceRt).ToArray());
    }

    [TestMethod]
    public void Hybrid_TakesCandidatesFromAUserAnchorsBinOnlyToReachTheMinimum() {
        var files = HybridFiles();
        var library = new[] { Anchor(0, "U", 555d, 1d, rtTolerance: 0.5d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 1d },
            [1] = new double?[] { 1.1d },
            [2] = new double?[] { 0.9d },
        };
        var parameter = Hybrid();
        parameter.MinimumAnchorCount = 4;
        parameter.MaximumAnchorCount = 4;
        List<ChromatogramPeakFeature> Peaks(AnalysisFileBean file) => ShiftedPeaks(file, 1.1d, 1.2d, 1.3d, 1.4d, 10d);

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, Peaks, library, Observed(observed));

        // The four bins of 2.25 min put every candidate but the 10-min one beside the user anchor.
        // The other bins give one anchor, two short of the minimum, so two come from the user
        // anchor's bin, the farthest from the anchors already taken first. (The 0.5-min candidate
        // bin keeps three of the four early peaks; the 1.1-min one is dropped there.)
        CollectionAssert.AreEqual(new[] { 1d, 1.2d, 1.4d, 10d }, UsedInReference(result).Select(anchor => anchor.ReferenceRt).ToArray());
    }

    [TestMethod]
    public void Hybrid_WhenEveryCandidateFits_AllAreTakenButTheUserAnchorsOwnPeak() {
        var files = HybridFiles();
        // The user anchor names the 5-min candidate's compound: m/z 110, the second candidate.
        var library = new[] { Anchor(0, "U", 110d, 5d, rtTolerance: 0.5d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5d },
            [1] = new double?[] { 5.1d },
            [2] = new double?[] { 4.9d },
        };
        var parameter = Hybrid();
        parameter.MaximumAnchorCount = 6;
        List<ChromatogramPeakFeature> Peaks(AnalysisFileBean file) => ShiftedPeaks(file, 2d, 5d, 8d);

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, Peaks, library, Observed(observed));

        var reference = UsedInReference(result);
        CollectionAssert.AreEqual(new[] { 2d, 5d, 8d }, reference.Select(anchor => anchor.ReferenceRt).ToArray());
        Assert.AreEqual(AutomaticRtCorrectionAnchorSource.User, reference[1].Source, "the 5-min anchor is the user's, once");
    }

    [TestMethod]
    public void Hybrid_AFileWithFewCandidatesIsEligibleAsTheReferenceThroughItsUserAnchors() {
        var files = HybridFiles();
        var library = new[] { Anchor(0, "U", 555d, 5d, rtTolerance: 0.5d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5d },
            [1] = new double?[] { 5.1d },
            [2] = new double?[] { 4.9d },
        };
        // Two candidates per file, one fewer than the minimum anchor count of 3.
        List<ChromatogramPeakFeature> Peaks(AnalysisFileBean file) => ShiftedPeaks(file, 2d, 8d);

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, Hybrid(), 0.01d, Peaks, library, Observed(observed));

        Assert.AreEqual(0, result.Correction.ReferenceFileId);
        CollectionAssert.AreEqual(new[] { 2d, 5d, 8d }, UsedInReference(result).Select(anchor => anchor.ReferenceRt).ToArray());
    }

    [TestMethod]
    public void Hybrid_AFalseRowKeepsTheCandidateItCoversOutOfTheSelection() {
        var files = HybridFiles();
        var library = new[] {
            Anchor(0, "U", 555d, 5.5d, rtTolerance: 0.5d),
            Anchor(1, "rejected on review", 110d, 2.05d, rtTolerance: 0.2d, use: false),
            Anchor(2, "written for another run", 999d, 4d, rtTolerance: 0.2d, use: false),
        };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5.5d },
            [1] = new double?[] { 5.6d },
            [2] = new double?[] { 5.4d },
        };

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, Hybrid(), 0.01d, HybridPeaks, library, Observed(observed));

        CollectionAssert.AreEqual(new[] { 1d, 4d, 5.5d, 8d, 10d }, UsedInReference(result).Select(anchor => anchor.ReferenceRt).ToArray(),
            "the 2-min candidate is excluded, so its bin gives the 1-min one");
        Assert.IsFalse(result.Anchors.Any(anchor => Math.Abs(anchor.Mass - 110d) < 1e-9));
        Assert.AreEqual(1, result.Files.Single(file => file.FileId == 0).ExcludedAutomaticCandidates);
        CollectionAssert.AreEqual(new[] { 1, 0 }, result.Exclusions.Select(exclusion => exclusion.ExcludedCandidates).ToArray());
        Assert.IsTrue(result.Exclusions.All(exclusion => exclusion.ReferenceFileName == "reference"));

        var folder = TemporaryFolder();
        try {
            result.WriteAudit(folder);
            var lines = System.IO.File.ReadAllLines(Path.Combine(folder, AutomaticAlignmentRetentionTimeCorrectionResult.ExclusionsFileName));
            Assert.AreEqual("Anchor name\tm/z\tRT (min)\tm/z tolerance\tRT tolerance (min)\tReference file ID\tReference file name\tExcluded automatic candidates", lines[0]);
            Assert.AreEqual(3, lines.Length);
            var first = lines[1].Split('\t');
            Assert.AreEqual("rejected on review", first[0]);
            Assert.AreEqual(110d, double.Parse(first[1], CultureInfo.InvariantCulture), 1e-9);
            Assert.AreEqual(2.05d, double.Parse(first[2], CultureInfo.InvariantCulture), 1e-9);
            Assert.AreEqual("1", first[7]);
            StringAssert.EndsWith(lines[2], "\t0\treference\t0");
        }
        finally {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void Hybrid_ALibraryOfFalseRowsOnlyIsTheAutomaticSelectionWithoutThem() {
        var files = HybridFiles();
        var library = new[] { Anchor(0, "rejected on review", 110d, 2.05d, rtTolerance: 0.2d, use: false) };
        var automatic = Hybrid();
        automatic.AnchorMode = AutomaticRtCorrectionAnchorMode.Automatic;

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, Hybrid(), 0.01d, HybridPeaks, library, Observed(EmptyObservations(files)));
        var withoutLibrary = AutomaticAlignmentRetentionTimeCorrection.Build(files, automatic, 0.01d, HybridPeaks, _ => Ms1CycleProfile.Empty);

        CollectionAssert.AreEqual(new[] { 2d, 4d, 6d, 8d, 10d }, UsedInReference(withoutLibrary).Select(anchor => anchor.ReferenceRt).ToArray());
        CollectionAssert.AreEqual(new[] { 1d, 4d, 6d, 8d, 10d }, UsedInReference(result).Select(anchor => anchor.ReferenceRt).ToArray());
        Assert.IsTrue(result.Anchors.All(anchor => anchor.Source == AutomaticRtCorrectionAnchorSource.Automatic));
    }

    [TestMethod]
    public void Hybrid_FailureMessageSaysHowManyAnchorsWereUserAnchors() {
        var files = HybridFiles();
        var library = new[] { Anchor(0, "U", 555d, 5d, rtTolerance: 0.5d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5d },
            [1] = new double?[] { 5.1d },
            [2] = new double?[] { 4.9d },
        };
        // Two candidates, one of them excluded: two anchors for a minimum of three.
        var withExclusion = library.Concat(new[] { Anchor(1, "X", 100d, 2d, rtTolerance: 0.2d, use: false) }).ToArray();
        List<ChromatogramPeakFeature> Peaks(AnalysisFileBean file) => ShiftedPeaks(file, 2d, 8d);

        var error = Assert.ThrowsException<AutomaticRtCorrectionFailedException>(
            () => AutomaticAlignmentRetentionTimeCorrection.Build(files, Hybrid(), 0.01d, Peaks, withExclusion, Observed(observed)));

        StringAssert.Contains(error.Message, "retained only 2 anchor(s) at distinct RTs, 1 of them user anchors; of the 2 automatic candidate(s) that passed cross-sample coverage, 1 were excluded by the library's False rows and 0 lie at a user anchor's peak");
        // The exclusion that caused it goes into the audit with the failure.
        Assert.AreEqual("X", error.PartialAudit.Exclusions.Single().AnchorName);
        Assert.AreEqual(1, error.PartialAudit.Exclusions.Single().ExcludedCandidates);
        Assert.AreEqual(3, error.PartialAudit.Anchors.Count, "the user anchor in each of the three files");
    }

    [TestMethod]
    public void UserAnchorTheOutlierTestDisagreesWith_IsKeptAndFlagged() {
        var files = new[] { File(0, "reference", AnalysisFileType.Sample, 1), File(1, "target", AnalysisFileType.Sample, 2) };
        var library = Enumerable.Range(0, 5).Select(index => Anchor(index, $"A{index}", 150d + 100d * index, 2d + 2d * index)).ToArray();
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 2d, 4d, 6d, 8d, 10d },
            [1] = new double?[] { 2.10d, 4.11d, 6.6d, 8.09d, 10.10d },
        };
        var parameter = UserOnly(minimumAnchorCount: 3);
        parameter.ReferenceFileId = 0;

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library, Observed(observed));

        var flagged = result.Anchors.Single(anchor => anchor.FileId == 1 && anchor.AnchorName == "A2");
        Assert.IsTrue(flagged.OutlierFlagged);
        Assert.IsTrue(flagged.Used);
        Assert.AreEqual("Used", flagged.Status);
        Assert.AreEqual("Global", flagged.OutlierTest);
        Assert.AreEqual(6d, result.Correction.Correct(1, 6.6d), 1e-9);
        Assert.IsTrue(result.Anchors.Where(anchor => anchor.AnchorName != "A2").All(anchor => !anchor.OutlierFlagged));
    }

    [TestMethod]
    public void ThreeUserAnchorsOnADriftThatGrowsWithRt_AreNotFlagged() {
        var files = new[] { File(0, "reference", AnalysisFileType.Sample, 1), File(1, "target", AnalysisFileType.Sample, 2) };
        var library = new[] { Anchor(0, "A", 150d, 2d), Anchor(1, "B", 250d, 6d), Anchor(2, "C", 350d, 10d) };
        // Offsets -0.05, -0.1 and -0.6: the last is 6.7 robust scales from the median of three.
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 2d, 6d, 10d },
            [1] = new double?[] { 2.05d, 6.1d, 10.6d },
        };
        var parameter = UserOnly(minimumAnchorCount: 3);
        parameter.ReferenceFileId = 0;

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library, Observed(observed));

        var target = result.Anchors.Where(anchor => anchor.FileId == 1).ToList();
        Assert.IsTrue(target.All(anchor => anchor.Used && !anchor.OutlierFlagged));
        Assert.AreEqual(10d, result.Correction.Correct(1, 10.6d), 1e-9);
    }

    [TestMethod]
    public void OutOfOrderUserAnchors_LoseTheOneFartherFromTheMedianOffset_LaterOrEarlier() {
        var files = new[] { File(0, "reference", AnalysisFileType.Sample, 1), File(1, "target", AnalysisFileType.Sample, 2) };
        var library = Enumerable.Range(0, 4).Select(index => Anchor(index, $"A{index}", 150d + 100d * index, 2d + 2d * index)).ToArray();
        var parameter = UserOnly(minimumAnchorCount: 3);
        parameter.ReferenceFileId = 0;

        // A2 was detected at 3.9 min, before A1: the earlier of the pair is the wrong one.
        var earlierWrong = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library, Observed(new Dictionary<int, double?[]> {
            [0] = new double?[] { 2d, 4d, 6d, 8d },
            [1] = new double?[] { 2.1d, 4.1d, 3.9d, 8.1d },
        }));
        // A1 was detected at 6.3 min, after A2: the later of the pair is the wrong one.
        var laterWrong = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library, Observed(new Dictionary<int, double?[]> {
            [0] = new double?[] { 2d, 4d, 6d, 8d },
            [1] = new double?[] { 2.1d, 6.3d, 6.1d, 8.1d },
        }));

        var first = earlierWrong.Anchors.Where(anchor => anchor.FileId == 1).ToDictionary(anchor => anchor.AnchorName);
        Assert.AreEqual("NonMonotonic", first["A2"].Status);
        Assert.IsTrue(first["A1"].Used);
        Assert.AreEqual(4d, earlierWrong.Correction.Correct(1, 4.1d), 1e-9);
        var second = laterWrong.Anchors.Where(anchor => anchor.FileId == 1).ToDictionary(anchor => anchor.AnchorName);
        Assert.AreEqual("NonMonotonic", second["A1"].Status);
        Assert.IsTrue(second["A2"].Used);
        Assert.AreEqual(6d, laterWrong.Correction.Correct(1, 6.1d), 1e-9);
    }

    [TestMethod]
    public void TwoUserAnchorsOnOnePeakInAFile_AreAnOrderConflict_NotASilentlyMergedPoint() {
        var files = new[] { File(0, "reference", AnalysisFileType.Sample, 1), File(1, "target", AnalysisFileType.Sample, 2) };
        // A and B share an m/z and their 3-min windows overlap; in the target both take 12.5 min.
        var library = new[] {
            Anchor(0, "A", 150d, 10d, rtTolerance: 3d),
            Anchor(1, "B", 150d, 14d, rtTolerance: 3d),
            Anchor(2, "C", 350d, 20d, rtTolerance: 3d),
        };
        var parameter = UserOnly(minimumAnchorCount: 2);
        parameter.ReferenceFileId = 0;

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library, Observed(new Dictionary<int, double?[]> {
            [0] = new double?[] { 10d, 14d, 20d },
            [1] = new double?[] { 12.5d, 12.5d, 20.1d },
        }));

        var target = result.Anchors.Where(anchor => anchor.FileId == 1).ToDictionary(anchor => anchor.AnchorName);
        Assert.AreEqual("NonMonotonic", target["A"].Status, "A's offset, -2.5, is the farther from the median -0.1");
        Assert.IsTrue(target["B"].Used);
        Assert.AreEqual(14d, result.Correction.Correct(1, 12.5d), 1e-9);
        Assert.AreEqual(2, result.Files.Single(file => file.FileId == 1).UsedAnchorCount);

        // With only A and B, one anchor is left: the file stays uncorrected, and the run goes on.
        var twoRows = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library.Take(2).ToArray(), Observed(new Dictionary<int, double?[]> {
            [0] = new double?[] { 10d, 14d },
            [1] = new double?[] { 12.5d, 12.5d },
        }));
        Assert.AreEqual(AutomaticRtCorrectionModelSource.Uncorrected, twoRows.Files.Single(file => file.FileId == 1).ModelSource);
    }

    [TestMethod]
    public void TwoRowsOnOneReferencePeak_AreOneAnchor_TheLaterReportedAsDuplicate() {
        var files = new[] {
            File(0, "a", AnalysisFileType.Sample, 1),
            File(1, "b", AnalysisFileType.Sample, 2),
            File(2, "c", AnalysisFileType.Sample, 3),
        };
        var library = new[] {
            Anchor(0, "IS1", 150d, 3.5d, rtTolerance: 3d),
            Anchor(1, "IS2", 250d, 9.5d, rtTolerance: 3d),
            Anchor(2, "IS2 again", 250d, 9.5d, rtTolerance: 3d),
            Anchor(3, "IS3", 350d, 17.4d, rtTolerance: 3d),
        };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 3.5d, 9.5d, 9.5d, 17.4d },
            [1] = new double?[] { 3.6d, 9.7d, 9.7d, 17.8d },
            [2] = new double?[] { 3.4d, 9.3d, 9.3d, 17.0d },
        };
        var parameter = UserOnly(minimumAnchorCount: 3);

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library, Observed(observed));

        Assert.AreEqual(0, result.Correction.ReferenceFileId);
        Assert.IsTrue(result.Files.All(file => file.UsedAnchorCount == 3), "three anchors in every file, not three in the reference and two elsewhere");
        Assert.AreEqual(9.5d, result.Correction.Correct(1, 9.7d), 1e-9);
        var duplicates = result.Anchors.Where(anchor => anchor.AnchorName == "IS2 again").ToList();
        Assert.AreEqual(3, duplicates.Count);
        Assert.IsTrue(duplicates.All(anchor => anchor.Status == "Duplicate" && !anchor.Used));
        StringAssert.Contains(duplicates[0].Detection, "IS2");

        // Two rows on one peak and one more: two anchors for a minimum of three. The run fails,
        // and what was detected is still there to be written.
        var error = Assert.ThrowsException<AutomaticRtCorrectionFailedException>(
            () => AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library.Take(3).ToArray(), Observed(observed.ToDictionary(pair => pair.Key, pair => pair.Value.Take(3).ToArray()))));
        StringAssert.Contains(error.Message, "at least 3 of the 3 user anchors");
        var partial = error.PartialAudit;
        Assert.AreEqual(9, partial.Anchors.Count, "three rows in three files");
        Assert.IsTrue(partial.Anchors.All(anchor => double.IsNaN(anchor.ReferenceRt)));
        Assert.IsTrue(partial.Anchors.Where(anchor => anchor.AnchorName != "IS2 again").All(anchor => anchor.Status == "Detected"));
        // The audit says what the message says: the second IS2 row is the first one again.
        var again = partial.Anchors.Where(anchor => anchor.AnchorName == "IS2 again").ToList();
        Assert.IsTrue(again.All(anchor => anchor.Status == "Duplicate"));
        StringAssert.Contains(again[0].Detection, "within two MS1 scans of IS2's");
        Assert.IsTrue(partial.Files.All(file => file.UserAnchorsDetected == 2), "two distinct, as the message counts them");
        Assert.IsTrue(partial.Files.All(file => file.ModelSource == AutomaticRtCorrectionModelSource.Uncorrected && file.Note.StartsWith("Not corrected: ")));
        Assert.AreEqual(9.7d, partial.Anchors.Single(anchor => anchor.FileId == 1 && anchor.AnchorName == "IS2").OriginalRt!.Value, 1e-9);
        var folder = TemporaryFolder();
        try {
            partial.WriteAudit(folder);
            var rows = System.IO.File.ReadAllLines(Path.Combine(folder, AutomaticAlignmentRetentionTimeCorrectionResult.AnchorsFileName)).Skip(1).ToList();
            Assert.AreEqual(9, rows.Count);
            Assert.IsTrue(rows.All(row => row.Split('\t')[4] == string.Empty), "no reference RT");
        }
        finally {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void Hybrid_OutOfOrderUserAndAutomaticAnchors_LoseTheAutomaticOne() {
        var files = HybridFiles();
        var library = new[] { Anchor(0, "U", 555d, 5.5d, rtTolerance: 0.5d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5.5d },
            // Found after the 8-min automatic anchor's 8.1 min in this file.
            [1] = new double?[] { 8.5d },
            [2] = new double?[] { 5.4d },
        };

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, Hybrid(), 0.01d, HybridPeaks, library, Observed(observed));

        var target = result.Anchors.Where(anchor => anchor.FileId == 1).ToList();
        var user = target.Single(anchor => anchor.Source == AutomaticRtCorrectionAnchorSource.User);
        Assert.IsTrue(user.Used);
        Assert.IsTrue(user.OutlierFlagged, "its offset disagrees with every other anchor");
        Assert.AreEqual("NonMonotonic", target.Single(anchor => Math.Abs(anchor.ReferenceRt - 8d) < 1e-9).Status);
    }

    [TestMethod]
    public void Hybrid_AUserAnchorNotDetectedInTheReference_IsReportedAndWrittenWithoutAReferenceRt() {
        var files = HybridFiles();
        var library = new[] {
            Anchor(0, "U", 555d, 5.5d, rtTolerance: 0.5d),
            Anchor(1, "V", 666d, 9.5d, rtTolerance: 0.5d),
        };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5.5d, null },
            [1] = new double?[] { 5.6d, 9.6d },
            [2] = new double?[] { 5.4d, 9.4d },
        };

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, Hybrid(), 0.01d, HybridPeaks, library, Observed(observed));

        var missing = result.Anchors.Where(anchor => anchor.AnchorName == "V").OrderBy(anchor => anchor.FileId).ToList();
        Assert.AreEqual(3, missing.Count, "one row per file");
        Assert.IsTrue(missing.All(anchor => anchor.Status == "MissingInReference" && !anchor.Used && double.IsNaN(anchor.ReferenceRt)));
        Assert.IsNull(missing[0].OriginalRt);
        Assert.AreEqual(9.6d, missing[1].OriginalRt!.Value, 1e-9);
        Assert.AreEqual(2d / 3d, missing[1].SampleCoverage, 1e-9);
        Assert.IsTrue(missing[0].AnchorId > result.Anchors.Where(anchor => anchor.FileId == 0 && anchor.Used).Max(anchor => anchor.AnchorId));

        var folder = TemporaryFolder();
        try {
            result.WriteAudit(folder);
            var lines = System.IO.File.ReadAllLines(Path.Combine(folder, AutomaticAlignmentRetentionTimeCorrectionResult.AnchorsFileName));
            var header = lines[0].Split('\t');
            CollectionAssert.AreEqual(
                new[] { "Anchor source", "Anchor name", "Library RT (min)", "Outlier flagged", "Peak height", "Observed m/z", "Detection" },
                header.Skip(16).ToArray());
            var rows = lines.Skip(1).Select(line => line.Split('\t')).ToList();
            Assert.IsTrue(rows.All(cells => cells.Length == header.Length));
            var column = header.Select((name, index) => (name, index)).ToDictionary(pair => pair.name, pair => pair.index);
            var v = rows.Single(cells => cells[column["File ID"]] == "1" && cells[column["Anchor name"]] == "V");
            Assert.AreEqual(string.Empty, v[column["Reference RT (min)"]]);
            Assert.AreEqual(string.Empty, v[column["Offset (min)"]]);
            Assert.AreEqual(9.6d, double.Parse(v[column["Original RT (min)"]], CultureInfo.InvariantCulture), 1e-12);
            Assert.AreEqual(9.5d, double.Parse(v[column["Library RT (min)"]], CultureInfo.InvariantCulture), 1e-12);
            Assert.AreEqual(666d, double.Parse(v[column["Observed m/z"]], CultureInfo.InvariantCulture), 1e-12);
            Assert.AreEqual("User", v[column["Anchor source"]]);
            Assert.AreEqual(string.Empty, v[column["Quality score"]], "a user anchor is not scored");
            var automatic = rows.First(cells => cells[column["Anchor source"]] == "Automatic");
            Assert.AreEqual(string.Empty, automatic[column["Anchor name"]]);
            Assert.AreNotEqual(string.Empty, automatic[column["Quality score"]]);

            var summary = System.IO.File.ReadAllLines(Path.Combine(folder, AutomaticAlignmentRetentionTimeCorrectionResult.SummaryFileName));
            var summaryHeader = summary[0].Split('\t');
            CollectionAssert.AreEqual(
                new[] { "Anchor mode", "User anchors detected", "Excluded automatic candidates", "Run", "Anchor library", "Anchor library SHA-256" },
                summaryHeader.Skip(17).ToArray());
            var summaryColumn = summaryHeader.Select((name, index) => (name, index)).ToDictionary(pair => pair.name, pair => pair.index);
            var summaryRows = summary.Skip(1).Select(line => line.Split('\t')).ToDictionary(cells => cells[summaryColumn["File ID"]]);
            Assert.AreEqual("Hybrid", summaryRows["0"][summaryColumn["Anchor mode"]]);
            Assert.AreEqual("1", summaryRows["0"][summaryColumn["User anchors detected"]]);
            Assert.AreEqual("2", summaryRows["1"][summaryColumn["User anchors detected"]]);
            Assert.AreEqual("0", summaryRows["0"][summaryColumn["Excluded automatic candidates"]]);
            Assert.AreEqual(string.Empty, summaryRows["1"][summaryColumn["Excluded automatic candidates"]]);
            Assert.IsFalse(System.IO.File.Exists(Path.Combine(folder, AutomaticAlignmentRetentionTimeCorrectionResult.ExclusionsFileName)), "no False rows, no exclusions file");
        }
        finally {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void Hybrid_OpenBinsGiveTheirOtherCandidatesBeforeAUserAnchorsBinGivesAny() {
        var files = HybridFiles();
        var library = new[] { Anchor(0, "U", 555d, 5.5d, rtTolerance: 0.5d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5.5d },
            [1] = new double?[] { 5.6d },
            [2] = new double?[] { 5.4d },
        };
        List<ChromatogramPeakFeature> Peaks(AnalysisFileBean file) => ShiftedPeaks(file, 1d, 1.5d, 5d, 6d, 10d);

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, Hybrid(), 0.01d, Peaks, library, Observed(observed));

        // Five bins of 1.8 min: [1, 2.8) offers 1.5, [8.2, 10] offers 10, and the user anchor's
        // bin holds 5 and 6. The fourth anchor is the other candidate of an open bin, 1 min, not
        // a candidate beside the user anchor.
        CollectionAssert.AreEqual(new[] { 1d, 1.5d, 5.5d, 10d }, UsedInReference(result).Select(anchor => anchor.ReferenceRt).ToArray());
    }

    [TestMethod]
    public void Hybrid_ACandidateAtTheUserAnchorsPeakTopIsNotTaken_WhateverItsMz() {
        var files = HybridFiles();
        // The 5-min candidate (m/z 110) co-elutes with the user anchor (m/z 555), within a scan.
        var library = new[] { Anchor(0, "U", 555d, 5d, rtTolerance: 0.5d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5.005d },
            [1] = new double?[] { 5.105d },
            [2] = new double?[] { 4.905d },
        };
        var parameter = Hybrid();
        parameter.MaximumAnchorCount = 6;
        List<ChromatogramPeakFeature> Peaks(AnalysisFileBean file) => ShiftedPeaks(file, 2d, 5d, 8d);

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, Peaks, library, Observed(observed));

        var reference = UsedInReference(result);
        CollectionAssert.AreEqual(new[] { 2d, 5.005d, 8d }, reference.Select(anchor => anchor.ReferenceRt).ToArray());
        Assert.AreEqual(AutomaticRtCorrectionAnchorSource.User, reference[1].Source);
    }

    [TestMethod]
    public void Hybrid_TheUserAnchorsOwnPeakIsFoundAtItsObservedMz_NotItsLibraryMz() {
        var files = HybridFiles();
        // Library m/z 110.000 with a 0.03 tolerance, observed at 110.027. The 5.1-min candidate
        // at m/z 110.036, ten scans from the anchor's peak top, is 0.009 from the observed m/z,
        // the anchor's own peak, and 0.036 from the library m/z, beyond either tolerance.
        var anchor = Anchor(0, "U", 110d, 5d, rtTolerance: 0.5d);
        anchor.MassTolerance = 0.03f;
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5d },
            [1] = new double?[] { 5.1d },
            [2] = new double?[] { 4.9d },
        };
        var parameter = Hybrid();
        parameter.MaximumAnchorCount = 6;
        List<ChromatogramPeakFeature> Peaks(AnalysisFileBean file) {
            var shift = file.AnalysisFileId switch { 1 => 0.1d, 2 => -0.1d, _ => 0d };
            return new List<ChromatogramPeakFeature> {
                Peak(0, 100d, 2d + shift, 10000d),
                Peak(1, 110.036d, 5.1d + shift, 11000d),
                Peak(2, 120d, 8d + shift, 12000d),
            };
        }

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, Peaks, new[] { anchor }, Observed(observed, mz: 110.027d));

        CollectionAssert.AreEqual(new[] { 2d, 5d, 8d }, UsedInReference(result).Select(anchor => anchor.ReferenceRt).ToArray());
    }

    [TestMethod]
    public void Hybrid_WithoutAReferenceFileId_ChoosesTheReferenceAsAutomaticModeDoes() {
        var files = new[] {
            File(0, "early", AnalysisFileType.Sample, 1),
            File(1, "middle", AnalysisFileType.Sample, 2),
            File(2, "late", AnalysisFileType.Sample, 3),
        };
        List<ChromatogramPeakFeature> Peaks(AnalysisFileBean file) {
            var shift = file.AnalysisFileId switch { 0 => -0.2d, 2 => 0.2d, _ => 0d };
            return Enumerable.Range(1, 10).Select(index => Peak(index, 100d + 10d * index, index + shift, 10000d + 1000d * index)).ToList();
        }
        var hybrid = Hybrid();
        hybrid.ReferenceFileId = -1;
        hybrid.MatchRtTolerance = 0.5f;
        hybrid.ReferenceCentralityWeight = 0.9f;
        var automatic = Hybrid();
        automatic.ReferenceFileId = -1;
        automatic.MatchRtTolerance = 0.5f;
        automatic.ReferenceCentralityWeight = 0.9f;
        automatic.AnchorMode = AutomaticRtCorrectionAnchorMode.Automatic;
        var library = new[] { Anchor(0, "U", 555d, 5.5d, rtTolerance: 1d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5.3d },
            [1] = new double?[] { 5.5d },
            [2] = new double?[] { 5.7d },
        };

        var withUser = AutomaticAlignmentRetentionTimeCorrection.Build(files, hybrid, 0.01d, Peaks, library, Observed(observed));
        var withoutUser = AutomaticAlignmentRetentionTimeCorrection.Build(files, automatic, 0.01d, Peaks, _ => Ms1CycleProfile.Empty);

        Assert.AreEqual(1, withoutUser.Correction.ReferenceFileId, "the central file");
        Assert.AreEqual(withoutUser.Correction.ReferenceFileId, withUser.Correction.ReferenceFileId);
    }

    [TestMethod]
    public void AnchorsAtOneOriginalRt_TheModelKeepsOne_AndTheOtherSaysSo() {
        var files = new[] { File(0, "reference", AnalysisFileType.Sample, 1), File(1, "target", AnalysisFileType.Sample, 2) };
        // Two candidates of other m/z 0.4 min apart in the reference elute at one RT in the target.
        var peaks = new Dictionary<int, List<ChromatogramPeakFeature>> {
            [0] = new List<ChromatogramPeakFeature> { Peak(0, 100d, 1d, 10000d), Peak(1, 110d, 4d, 11000d), Peak(2, 120d, 4.4d, 12000d), Peak(3, 130d, 8d, 13000d) },
            [1] = new List<ChromatogramPeakFeature> { Peak(0, 100d, 1d, 10000d), Peak(1, 110d, 4.2d, 11000d), Peak(2, 120d, 4.2d, 12000d), Peak(3, 130d, 8d, 13000d) },
        };
        var parameter = new AutomaticAlignmentRetentionTimeCorrectionParameter {
            Execute = true,
            ReferenceFileId = 0,
            RtBinWidth = 0.3f,
            MatchRtTolerance = 0.3f,
            MinimumAnchorCount = 3,
            MaximumAnchorCount = 10,
            MinimumSampleCoverage = 0.5f,
            IntensityQuantile = 0f,
            MaximumPeakWidthQuantile = 1f,
            MinimumSignalToNoise = 3f,
        };

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, file => peaks[file.AnalysisFileId], _ => Ms1CycleProfile.Empty);

        var target = result.Anchors.Where(anchor => anchor.FileId == 1).ToList();
        Assert.AreEqual(1, target.Count(anchor => anchor.Status == "SameOriginalRt" && !anchor.Used));
        Assert.AreEqual(3, target.Count(anchor => anchor.Used));
        Assert.AreEqual(3, result.Files.Single(file => file.FileId == 1).UsedAnchorCount, "the anchor rows agree with the file's count");
    }

    [TestMethod]
    public void Automatic_AFileIsCorrectedOnlyWhenItsAnchorsReachTheMinimumAtDistinctRts() {
        var files = new[] { File(0, "reference", AnalysisFileType.Sample, 1), File(1, "target", AnalysisFileType.Sample, 2) };
        // In the target, the 5- and 5.4-min anchors elute at one RT and the 3-min one is missing:
        // four anchors accepted, three distinct RTs, for a minimum of four.
        var peaks = new Dictionary<int, List<ChromatogramPeakFeature>> {
            [0] = new List<ChromatogramPeakFeature> { Peak(0, 100d, 1d, 10000d), Peak(1, 110d, 3d, 11000d), Peak(2, 120d, 5d, 12000d), Peak(3, 130d, 5.4d, 13000d), Peak(4, 140d, 8d, 14000d) },
            [1] = new List<ChromatogramPeakFeature> { Peak(0, 100d, 1d, 10000d), Peak(2, 120d, 5.2d, 12000d), Peak(3, 130d, 5.2d, 13000d), Peak(4, 140d, 8d, 14000d) },
        };
        var parameter = new AutomaticAlignmentRetentionTimeCorrectionParameter {
            Execute = true,
            ReferenceFileId = 0,
            RtBinWidth = 0.3f,
            MatchRtTolerance = 0.3f,
            MinimumAnchorCount = 4,
            MaximumAnchorCount = 10,
            MinimumSampleCoverage = 0.5f,
            IntensityQuantile = 0f,
            MaximumPeakWidthQuantile = 1f,
            MinimumSignalToNoise = 3f,
        };

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, file => peaks[file.AnalysisFileId], _ => Ms1CycleProfile.Empty);

        var target = result.Files.Single(file => file.FileId == 1);
        Assert.AreEqual(AutomaticRtCorrectionModelSource.Uncorrected, target.ModelSource);
        Assert.AreEqual("Only 3 usable anchor(s); original RT is retained.", target.Note);
    }

    [TestMethod]
    public void Hybrid_AnIonAndItsIsotopeOnOnePeakTopAreOneAutomaticAnchor() {
        var files = HybridFiles();
        var library = new[] { Anchor(0, "U", 555d, 4d, rtTolerance: 0.5d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 4d },
            [1] = new double?[] { 4.1d },
            [2] = new double?[] { 3.9d },
        };
        var parameter = Hybrid();
        parameter.MaximumAnchorCount = 6;
        // 450.2 and its isotope 451.2 share the 6-min peak top.
        List<ChromatogramPeakFeature> Peaks(AnalysisFileBean file) {
            var shift = file.AnalysisFileId switch { 1 => 0.1d, 2 => -0.1d, _ => 0d };
            return new List<ChromatogramPeakFeature> {
                Peak(0, 300.1d, 2d + shift, 10000d),
                Peak(1, 450.2d, 6d + shift, 30000d),
                Peak(2, 451.2d, 6d + shift, 9000d),
                Peak(3, 600.3d, 9d + shift, 12000d),
            };
        }

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, Peaks, library, Observed(observed));

        var reference = result.Anchors.Where(anchor => anchor.FileId == 0).OrderBy(anchor => anchor.ReferenceRt).ToList();
        CollectionAssert.AreEqual(new[] { 2d, 4d, 6d, 9d }, reference.Select(anchor => anchor.ReferenceRt).ToArray(), "one anchor at 6 min");
        Assert.AreEqual(450.2d, reference[2].Mass, 1e-9, "the taller ion stands for the pair");
        Assert.IsTrue(result.Files.All(file => file.UsedAnchorCount == 4));
    }

    [TestMethod]
    public void UserAnchorsAreCountedAsTheReferenceKeepsThem_NotAsAChain() {
        var files = new[] { File(0, "a", AnalysisFileType.Sample, 1), File(1, "b", AnalysisFileType.Sample, 2) };
        // Peaks two scans apart in a row: the second is a duplicate of the first, the third is not.
        var library = new[] { Anchor(0, "A", 150d, 5d), Anchor(1, "B", 151d, 5d), Anchor(2, "C", 152d, 5d) };
        var observed = new Dictionary<int, double?[]> {
            [0] = new double?[] { 5.00d, 5.02d, 5.04d },
            [1] = new double?[] { 5.10d, 5.12d, 5.14d },
        };
        var parameter = UserOnly(minimumAnchorCount: 2);

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, AutomaticPeaks, library, Observed(observed));

        var reference = result.Anchors.Where(anchor => anchor.FileId == result.Correction.ReferenceFileId).ToDictionary(anchor => anchor.AnchorName);
        Assert.IsTrue(reference["A"].Used);
        Assert.AreEqual("Duplicate", reference["B"].Status);
        Assert.IsTrue(reference["C"].Used, "two distinct anchors make the file eligible");
    }

    [TestMethod]
    public void Automatic_AFailureToFindAReferenceReadsNoRawData() {
        var files = HybridFiles();
        var parameter = Hybrid();
        parameter.AnchorMode = AutomaticRtCorrectionAnchorMode.Automatic;
        var reads = 0;

        Assert.ThrowsException<AutomaticRtCorrectionFailedException>(() => AutomaticAlignmentRetentionTimeCorrection.Build(
            files, parameter, 0.01d, _ => new List<ChromatogramPeakFeature>(), Array.Empty<MoleculeMsReference>(),
            (file, anchors) => {
                reads++;
                return new AutomaticRtCorrectionRawFileReading(Ms1CycleProfile.Empty, Array.Empty<AutomaticRtCorrectionUserAnchorObservation?>());
            }));

        Assert.AreEqual(0, reads);
    }

    [TestMethod]
    public void WriteAudit_NamesTheRunAndTheAnchorLibrary() {
        var result = AutomaticAlignmentRetentionTimeCorrection.Build(HybridFiles(), Hybrid(), 0.01d, HybridPeaks,
            new[] { Anchor(0, "U", 555d, 5.5d, rtTolerance: 0.5d) },
            Observed(new Dictionary<int, double?[]> { [0] = new double?[] { 5.5d }, [1] = new double?[] { 5.6d }, [2] = new double?[] { 5.4d } }));
        var folder = TemporaryFolder();
        try {
            result.WriteAudit(folder, "Preview", "anchors.txt", "abc123");
            var lines = System.IO.File.ReadAllLines(Path.Combine(folder, AutomaticAlignmentRetentionTimeCorrectionResult.SummaryFileName));
            var header = lines[0].Split('\t').ToList();
            Assert.IsTrue(lines.Skip(1).Select(line => line.Split('\t')).All(cells =>
                cells[header.IndexOf("Run")] == "Preview" && cells[header.IndexOf("Anchor library")] == "anchors.txt" && cells[header.IndexOf("Anchor library SHA-256")] == "abc123"));
        }
        finally {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void WriteAudit_RemovesAnEarlierRunsAuditFirst() {
        var folder = TemporaryFolder();
        try {
            Directory.CreateDirectory(folder);
            var stale = Path.Combine(folder, AutomaticAlignmentRetentionTimeCorrectionResult.ExclusionsFileName);
            System.IO.File.WriteAllText(stale, "from an earlier run");

            var result = AutomaticAlignmentRetentionTimeCorrection.Build(HybridFiles(), Hybrid(), 0.01d, HybridPeaks,
                new[] { Anchor(0, "U", 555d, 5.5d, rtTolerance: 0.5d) },
                Observed(new Dictionary<int, double?[]> { [0] = new double?[] { 5.5d }, [1] = new double?[] { 5.6d }, [2] = new double?[] { 5.4d } }));
            result.WriteAudit(folder);

            Assert.IsFalse(System.IO.File.Exists(stale));
            AutomaticAlignmentRetentionTimeCorrectionResult.RemoveAudit(folder);
            Assert.AreEqual(0, Directory.GetFiles(folder).Length);
        }
        finally {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void Automatic_ReadsNoAnchorsAndWritesTheModeWithEmptyUserColumns() {
        var files = HybridFiles();
        var parameter = Hybrid();
        parameter.AnchorMode = AutomaticRtCorrectionAnchorMode.Automatic;
        var library = new[] { Anchor(0, "ignored", 555d, 5.5d) };
        var anchorsAsked = new List<int>();

        var result = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, HybridPeaks, library,
            (file, anchors) => {
                anchorsAsked.Add(anchors.Count);
                return new AutomaticRtCorrectionRawFileReading(Ms1CycleProfile.Empty, Array.Empty<AutomaticRtCorrectionUserAnchorObservation?>());
            });

        CollectionAssert.AreEqual(new[] { 0, 0, 0 }, anchorsAsked, "each file is read once, and for no anchors");
        Assert.IsTrue(result.Anchors.All(anchor => anchor.Source == AutomaticRtCorrectionAnchorSource.Automatic));
        Assert.IsTrue(result.Files.All(file => file.UserAnchorsDetected is null && file.ExcludedAutomaticCandidates is null));
        Assert.AreEqual(0, result.Exclusions.Count);
        var withoutLibrary = AutomaticAlignmentRetentionTimeCorrection.Build(files, parameter, 0.01d, HybridPeaks, _ => Ms1CycleProfile.Empty);
        CollectionAssert.AreEqual(
            withoutLibrary.Anchors.Select(anchor => (anchor.FileId, anchor.AnchorId, anchor.ReferenceRt, anchor.OriginalRt, anchor.Status)).ToArray(),
            result.Anchors.Select(anchor => (anchor.FileId, anchor.AnchorId, anchor.ReferenceRt, anchor.OriginalRt, anchor.Status)).ToArray());
    }

    [TestMethod]
    public void Load_ReadsTrueAndFalseRows() {
        var path = Path.Combine(Path.GetTempPath(), "autort-anchors-" + Guid.NewGuid().ToString("N") + ".txt");
        try {
            System.IO.File.WriteAllText(path,
                "Name\tRT(min)\tRT tol.(min)\tm/z\tm/z tol.\tMinimum height\tT/F\r\n"
                + "A\t3.5\t3\t150.1\t0.01\t10000\tTRUE\r\n"
                + "B\t9.5\t3\t250.2\t0.01\t10000\tFALSE\r\n");
            var rows = AutomaticRtCorrectionAnchorLibrary.Load(path);
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("A", AutomaticRtCorrectionAnchorLibrary.Anchors(rows).Single().Name);
            Assert.AreEqual("B", AutomaticRtCorrectionAnchorLibrary.Exclusions(rows).Single().Name);
            Assert.AreEqual(3f, rows[0].RetentionTimeTolerance);
            Assert.AreEqual(10000f, rows[0].MinimumPeakHeight);
            Assert.AreEqual(150.1d, rows[0].PrecursorMz, 1e-9);
            Assert.AreEqual(1, rows[1].ScanID);
        }
        finally {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
        Assert.ThrowsException<FileNotFoundException>(() => AutomaticRtCorrectionAnchorLibrary.Load(path));
        Assert.ThrowsException<InvalidDataException>(() => AutomaticRtCorrectionAnchorLibrary.Load(string.Empty));
    }

    [TestMethod]
    public void Parse_ReadsTheFlagStrictly_TAndFIncluded() {
        const string header = "Name\tRT(min)\tRT tol.(min)\tm/z\tm/z tol.\tMinimum height\tT/F";
        var rows = AutomaticRtCorrectionAnchorLibrary.Parse(new[] {
            header,
            "A\t1\t1\t100\t0.01\t0\tT",
            "B\t2\t1\t200\t0.01\t0\tf",
            "C\t3\t1\t300\t0.01\t0\t True ",
            "D\t4\t1\t400\t0.01\t0\tfalse",
        });
        CollectionAssert.AreEqual(new[] { true, false, true, false }, rows.Select(row => row.IsTargetMolecule).ToArray());

        foreach (var flag in new[] { "yes", "0", "1", "exclude", "" }) {
            var error = Assert.ThrowsException<InvalidDataException>(
                () => AutomaticRtCorrectionAnchorLibrary.Parse(new[] { header, "A\t1\t1\t100\t0.01\t0\tTrue", $"B\t2\t1\t200\t0.01\t0\t{flag}" }),
                $"'{flag}' is no flag");
            StringAssert.Contains(error.Message, "line 3");
        }
    }

    [TestMethod]
    public void Parse_SkipsBlankLinesInsteadOfStoppingAtThem() {
        var rows = AutomaticRtCorrectionAnchorLibrary.Parse(new[] {
            "Name\tRT(min)\tRT tol.(min)\tm/z\tm/z tol.\tMinimum height\tT/F",
            "IS1\t3.5\t3\t150\t0.01\t10000\tTrue",
            "",
            "   ",
            "rejected\t2.05\t0.2\t110\t0.01\t0\tFalse",
            "",
        });
        CollectionAssert.AreEqual(new[] { "IS1", "rejected" }, rows.Select(row => row.Name).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 1 }, rows.Select(row => row.ScanID).ToArray());
    }

    [TestMethod]
    public void Parse_RefusesWhatItCannotReadExactly() {
        const string header = "Name\tRT(min)\tRT tol.(min)\tm/z\tm/z tol.\tMinimum height\tT/F";
        // No header: the first line is a row, and would be lost as the header.
        StringAssert.Contains(Assert.ThrowsException<InvalidDataException>(
            () => AutomaticRtCorrectionAnchorLibrary.Parse(new[] { "IS1\t3.5\t3\t150\t0.01\t10000\tTrue" })).Message, "looks like a row");
        // Six columns: the flag would be a guess.
        StringAssert.Contains(Assert.ThrowsException<InvalidDataException>(
            () => AutomaticRtCorrectionAnchorLibrary.Parse(new[] { header, "IS1\t3.5\t3\t150\t0.01\t10000" })).Message, "seven");
        // A decimal comma: the audit writes a point, and a comma read as a group separator would
        // turn 3,5 into 35.
        StringAssert.Contains(Assert.ThrowsException<InvalidDataException>(
            () => AutomaticRtCorrectionAnchorLibrary.Parse(new[] { header, "IS1\t3,5\t3\t150\t0.01\t10000\tTrue" })).Message, "decimal point");
        Assert.ThrowsException<InvalidDataException>(() => AutomaticRtCorrectionAnchorLibrary.Parse(new[] { "", "  " }));
    }

    [TestMethod]
    public void Detector_FindsEachAnchorOnTheRawChromatogramWithinItsOwnWindowAndHeight() {
        var provider = new StandardDataProvider(RawSpectraWithPeaks());
        var parameter = DetectorParameter();
        // The run's own minimum peak height plays no part.
        parameter.MinimumAmplitude = 1e9f;
        var file = RawFile();
        var anchors = new[] {
            Anchor(0, "single", 200d, 5.2d, rtTolerance: 0.5d),
            Anchor(1, "two peaks", 300d, 4.8d, rtTolerance: 2.5d),
            Anchor(2, "one in window", 300d, 3.3d, rtTolerance: 1d),
            Anchor(3, "too low", 400d, 6d, rtTolerance: 1d),
            Anchor(4, "absent", 500d, 6d, rtTolerance: 1d),
            Anchor(5, "outside window", 200d, 8d, rtTolerance: 1d),
        };

        var highest = AutomaticRtCorrectionUserAnchorDetector.Detect(file, provider, parameter, anchors, RetentionTimeCorrectionPeakSelectionMode.HighestIntensity, 0.5d);
        var closest = AutomaticRtCorrectionUserAnchorDetector.Detect(file, provider, parameter, anchors, RetentionTimeCorrectionPeakSelectionMode.ClosestToReferenceRt, 0.5d);
        var weightedOnRt = AutomaticRtCorrectionUserAnchorDetector.Detect(file, provider, parameter, anchors, RetentionTimeCorrectionPeakSelectionMode.Weighted, 1d);
        var weightedOnHeight = AutomaticRtCorrectionUserAnchorDetector.Detect(file, provider, parameter, anchors, RetentionTimeCorrectionPeakSelectionMode.Weighted, 0d);

        Assert.AreEqual(5d, highest.Observations[0]!.Rt, 0.011d);
        Assert.AreEqual(100000d, highest.Observations[0]!.Height, 5000d);
        Assert.AreEqual(200d, highest.Observations[0]!.Mz, 0.01d);
        Assert.AreEqual(7d, highest.Observations[1]!.Rt, 0.011d, "the taller of the two peaks in the window");
        Assert.AreEqual(3d, closest.Observations[1]!.Rt, 0.011d, "the nearer of the two peaks to the library RT");
        Assert.AreEqual(3d, weightedOnRt.Observations[1]!.Rt, 0.011d, "an RT weight of 1 is the RT alone");
        Assert.AreEqual(7d, weightedOnHeight.Observations[1]!.Rt, 0.011d, "an RT weight of 0 is the height alone");
        Assert.AreEqual(3d, highest.Observations[2]!.Rt, 0.011d, "the taller peak lies outside this anchor's window");
        Assert.IsNull(highest.Observations[3], "below the anchor's minimum height");
        Assert.IsNull(highest.Observations[4]);
        Assert.IsNull(highest.Observations[5]);

        Assert.AreEqual("The only peak in the window.", highest.Notes[0]);
        Assert.AreEqual("The highest of 2 peaks in the window.", highest.Notes[1]);
        Assert.AreEqual("The closest to the library RT of 2 peaks in the window.", closest.Notes[1]);
        Assert.AreEqual("No peak on the extracted ion chromatogram rises above the minimum height.", highest.Notes[3]);
        Assert.AreEqual("No signal at the anchor's m/z.", highest.Notes[4]);
        StringAssert.Contains(highest.Notes[5], "outside the RT window");
    }

    [TestMethod]
    public void Detector_SaysWhenTheExcludedMassListHidesAnAnchor() {
        var provider = new StandardDataProvider(RawSpectraWithPeaks());
        var parameter = DetectorParameter();
        parameter.ExcludedMassList = new List<MzSearchQuery> { new MzSearchQuery { Mass = 200d, MassTolerance = 0.01d } };

        var detection = AutomaticRtCorrectionUserAnchorDetector.Detect(RawFile(), provider, parameter, new[] { Anchor(0, "excluded", 200d, 5d) }, RetentionTimeCorrectionPeakSelectionMode.HighestIntensity, 0.5d);

        Assert.IsNull(detection.Observations[0]);
        Assert.AreEqual("The anchor's m/z window lies within an excluded mass, where no peak is detected.", detection.Notes[0]);

        // A window that only reaches into the excluded one: the peak may or may not have gone there.
        parameter.ExcludedMassList = new List<MzSearchQuery> { new MzSearchQuery { Mass = 200.012d, MassTolerance = 0.005d } };
        var unrelated = AutomaticRtCorrectionUserAnchorDetector.Detect(RawFile(), provider, parameter, new[] { Anchor(0, "unrelated", 400d, 6d) }, RetentionTimeCorrectionPeakSelectionMode.HighestIntensity, 0.5d);
        Assert.AreEqual("No peak on the extracted ion chromatogram rises above the minimum height.", unrelated.Notes[0], "m/z 400 is nowhere near the excluded 200.012");
        var near = Anchor(0, "near", 200d, 5d);
        near.MinimumPeakHeight = 1e9f;
        var reaching = AutomaticRtCorrectionUserAnchorDetector.Detect(RawFile(), provider, parameter, new[] { near }, RetentionTimeCorrectionPeakSelectionMode.HighestIntensity, 0.5d);
        StringAssert.Contains(reaching.Notes[0], "may have been dropped by the excluded mass 200.012");
    }

    [TestMethod]
    public void Detector_ARowsResultDoesNotDependOnTheOtherRowsOfTheLibrary() {
        var provider = new StandardDataProvider(RawSpectraWithPeaks());
        var parameter = DetectorParameter();
        var file = RawFile();
        // A 500-count peak on a 1000-count baseline: 1500 at the top, 500 above the baseline.
        var strict = Anchor(0, "minimum 1000", 600d, 4d, rtTolerance: 1d);
        var lenient = Anchor(1, "minimum 100", 600d, 4d, rtTolerance: 1d);
        lenient.MinimumPeakHeight = 100f;

        var alone = AutomaticRtCorrectionUserAnchorDetector.Detect(file, provider, parameter, new[] { strict }, RetentionTimeCorrectionPeakSelectionMode.HighestIntensity, 0.5d);
        var together = AutomaticRtCorrectionUserAnchorDetector.Detect(file, provider, parameter, new[] { strict, lenient }, RetentionTimeCorrectionPeakSelectionMode.HighestIntensity, 0.5d);

        Assert.AreEqual(4d, together.Observations[1]!.Rt, 0.011d);
        Assert.AreEqual(alone.Observations[0] is null, together.Observations[0] is null,
            "a lower minimum height on another row does not let this row's detection through");
        Assert.IsNull(alone.Observations[0], "500 above the baseline is below the row's 1000");
    }

    private static List<RawSpectrum> RawSpectraWithPeaks() {
        return Enumerable.Range(0, 1001)
            .Select(scan => {
                var rt = scan * 0.01d;
                return new RawSpectrum {
                    Index = scan,
                    ScanNumber = scan,
                    MsLevel = 1,
                    ScanPolarity = ScanPolarity.Positive,
                    ScanStartTime = rt,
                    Spectrum = new[] {
                        new RawPeakElement { Mz = 200d, Intensity = Gaussian(rt, 5d, 100000d) },
                        new RawPeakElement { Mz = 300d, Intensity = Gaussian(rt, 3d, 50000d) + Gaussian(rt, 7d, 200000d) },
                        new RawPeakElement { Mz = 400d, Intensity = Gaussian(rt, 6d, 500d) },
                        new RawPeakElement { Mz = 600d, Intensity = 1000d + Gaussian(rt, 4d, 500d) },
                    },
                };
            })
            .ToList();
    }

    private static MsdialLcmsParameter DetectorParameter() {
        return new MsdialLcmsParameter {
            IonMode = IonMode.Positive,
            MinimumDatapoints = 5,
            SmoothingLevel = 1,
            RetentionTimeBegin = 0f,
            RetentionTimeEnd = 100f,
        };
    }

    private static AnalysisFileBean RawFile() {
        var file = File(0, "raw", AnalysisFileType.Sample, 1);
        file.AcquisitionType = AcquisitionType.DDA;
        return file;
    }

    private static double Gaussian(double rt, double centre, double height) {
        const double sigma = 0.05d;
        return height * Math.Exp(-(rt - centre) * (rt - centre) / (2d * sigma * sigma));
    }

    private static List<AutomaticRtCorrectionAnchorAudit> UsedInReference(AutomaticAlignmentRetentionTimeCorrectionResult result) {
        return result.Anchors
            .Where(anchor => anchor.FileId == result.Correction.ReferenceFileId && anchor.Used)
            .OrderBy(anchor => anchor.ReferenceRt)
            .ToList();
    }

    private static string TemporaryFolder() {
        return Path.Combine(Path.GetTempPath(), "autort-user-audit-" + Guid.NewGuid().ToString("N"));
    }

    private static AutomaticAlignmentRetentionTimeCorrectionParameter UserOnly(int minimumAnchorCount) {
        return new AutomaticAlignmentRetentionTimeCorrectionParameter {
            Execute = true,
            AnchorMode = AutomaticRtCorrectionAnchorMode.UserOnly,
            MinimumAnchorCount = minimumAnchorCount,
            MaximumAnchorCount = 12,
        };
    }

    private static AutomaticAlignmentRetentionTimeCorrectionParameter Hybrid() {
        return new AutomaticAlignmentRetentionTimeCorrectionParameter {
            Execute = true,
            AnchorMode = AutomaticRtCorrectionAnchorMode.Hybrid,
            ReferenceFileId = 0,
            RtBinWidth = 0.5f,
            MatchRtTolerance = 0.3f,
            MinimumAnchorCount = 3,
            MaximumAnchorCount = 5,
            MinimumSampleCoverage = 1f,
            IntensityQuantile = 0f,
            MaximumPeakWidthQuantile = 1f,
            MinimumSignalToNoise = 3f,
        };
    }

    private static AnalysisFileBean[] HybridFiles() {
        return new[] {
            File(0, "reference", AnalysisFileType.Sample, 1),
            File(1, "later", AnalysisFileType.Sample, 2),
            File(2, "earlier", AnalysisFileType.Sample, 3),
        };
    }

    // Ten candidates, one a minute from 1 to 10 min, m/z 100 + 10 per minute, taller as they
    // elute; every file has them all, shifted by 0, +0.1 and -0.1 min.
    private static List<ChromatogramPeakFeature> HybridPeaks(AnalysisFileBean file) {
        return ShiftedPeaks(file, Enumerable.Range(1, 10).Select(rt => (double)rt).ToArray());
    }

    // Candidates at the reference RTs given, m/z 100, 110, ... and taller in that order, shifted
    // by 0, +0.1 and -0.1 min in files 0, 1 and 2.
    private static List<ChromatogramPeakFeature> ShiftedPeaks(AnalysisFileBean file, params double[] rts) {
        var shift = file.AnalysisFileId switch { 1 => 0.1d, 2 => -0.1d, _ => 0d };
        return rts
            .Select((rt, index) => Peak(index, 100d + 10d * index, rt + shift, 10000d + 1000d * index))
            .ToList();
    }

    // UserOnly reads the detected peaks only for the audit's counts.
    private static List<ChromatogramPeakFeature> AutomaticPeaks(AnalysisFileBean file) {
        return new List<ChromatogramPeakFeature> {
            Peak(0, 120d, 1d, 10000d),
            Peak(1, 220d, 5d, 20000d),
            Peak(2, 320d, 9d, 15000d),
        };
    }

    private static Dictionary<int, double?[]> EmptyObservations(IEnumerable<AnalysisFileBean> files) {
        return files.ToDictionary(file => file.AnalysisFileId, _ => Array.Empty<double?>());
    }

    // MS1 scans every 0.01 min: an outlier scale is never below one scan, so an offset several
    // scans from its neighbours' is judged even where they all agree exactly.
    private static readonly Ms1CycleProfile EveryHundredthMinute = new Ms1CycleProfile(Enumerable.Range(0, 2001).Select(scan => scan * 0.01d));

    // Each anchor observed at the RTs given, at its library m/z unless mz says otherwise.
    private static Func<AnalysisFileBean, IReadOnlyList<MoleculeMsReference>, AutomaticRtCorrectionRawFileReading> Observed(IReadOnlyDictionary<int, double?[]> rtsByFile, double? mz = null) {
        return (file, anchors) => new AutomaticRtCorrectionRawFileReading(
            EveryHundredthMinute,
            rtsByFile[file.AnalysisFileId]
                .Select((rt, index) => rt is null ? null : new AutomaticRtCorrectionUserAnchorObservation(rt.Value, mz ?? anchors[index].PrecursorMz, 50000d))
                .ToArray());
    }

    private static MoleculeMsReference Anchor(int id, string name, double mz, double rt, double rtTolerance = 1d, bool use = true) {
        return new MoleculeMsReference {
            ScanID = id,
            Name = name,
            PrecursorMz = mz,
            ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
            RetentionTimeTolerance = (float)rtTolerance,
            MassTolerance = 0.01f,
            MinimumPeakHeight = 1000f,
            IsTargetMolecule = use,
        };
    }

    private static AnalysisFileBean File(int id, string name, AnalysisFileType type, int order) {
        return new AnalysisFileBean {
            AnalysisFileId = id,
            AnalysisFileName = name,
            AnalysisFileType = type,
            AnalysisFileAnalyticalOrder = order,
        };
    }

    private static ChromatogramPeakFeature Peak(int id, double mz, double rt, double height) {
        return new ChromatogramPeakFeature(new BaseChromatogramPeakFeature {
            ChromXsLeft = new ChromXs(rt - 0.05d, ChromXType.RT, ChromXUnit.Min),
            ChromXsTop = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
            ChromXsRight = new ChromXs(rt + 0.05d, ChromXType.RT, ChromXUnit.Min),
            PeakHeightLeft = height * 0.1d,
            PeakHeightTop = height,
            PeakHeightRight = height * 0.1d,
            Mass = mz,
        }) {
            PeakID = id,
            MasterPeakID = id,
            PeakShape = new ChromatogramPeakShape {
                SignalToNoise = 20f,
                GaussianSimilarityValue = 0.95f,
                IdealSlopeValue = 0.95f,
                SymmetryValue = 0.95f,
            },
        };
    }
}
