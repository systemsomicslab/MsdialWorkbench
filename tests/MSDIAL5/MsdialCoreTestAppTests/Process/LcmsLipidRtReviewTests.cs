#nullable enable
using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The Console's review list of the lipid RT check: "&lt;alignment stem&gt;_lipidrt_review.tsv" next to the alignment
/// exports, written whenever the check ran (an empty list included) and never when it did not.
/// </summary>
[TestClass]
public sealed class LcmsLipidRtReviewTests
{
    private string _scratch = null!;

    [TestInitialize]
    public void Initialize() {
        _scratch = Path.Combine(Path.GetTempPath(), $"msdial-lipidrt-review-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_scratch);
    }

    [TestCleanup]
    public void Cleanup() {
        if (Directory.Exists(_scratch)) {
            Directory.Delete(_scratch, recursive: true);
        }
    }

    [TestMethod]
    public void TheReviewListIsWrittenNextToTheAlignmentExports() {
        var spots = TgGrid(shiftedC: 52, shiftedDb: 1, shift: 1.5);
        var run = LipidRtCheckHooks.RunOnSpots(spots, Lipidomics());
        var original = Console.Out;
        var captured = new StringWriter();
        string? file;
        try {
            Console.SetOut(captured);
            file = LcmsProcess.WriteLipidRtReview(run, _scratch, "AlignmentResult_2026_10_15");
        }
        finally {
            Console.SetOut(original);
        }

        Assert.AreEqual(Path.Combine(_scratch, "AlignmentResult_2026_10_15_lipidrt_review.tsv"), file);
        var lines = File.ReadAllLines(file!);
        Assert.AreEqual(string.Join("\t", LipidRtReviewList.Columns), lines[0]);
        var row = lines.Skip(1).Single().Split('\t');
        StringAssert.StartsWith(row[1], "TG 52:1");
        StringAssert.StartsWith(row[4], "InconsistentOrder");
        Assert.AreEqual("Inconsistent", row[5]);
        StringAssert.Contains(captured.ToString(), "Lipid RT check (alignment): ");
        StringAssert.Contains(captured.ToString(), "(1 rows)");
    }

    [TestMethod]
    public void AnEmptyReviewListIsStillWrittenWhenTheCheckRan() {
        var run = LipidRtCheckHooks.RunOnSpots(TgGrid(), Lipidomics());

        var file = LcmsProcess.WriteLipidRtReview(run, _scratch, "AlignmentResult");

        Assert.AreEqual(1, File.ReadAllLines(file!).Length, "the header alone: ran, nothing to review");
    }

    [TestMethod]
    public void NothingIsWrittenWhenTheCheckDidNotRun() {
        var metabolomics = new MsdialLcmsParameter { TargetOmics = TargetOmics.Metabolomics, IonMode = IonMode.Positive };
        var run = LipidRtCheckHooks.RunOnSpots(TgGrid(), metabolomics);

        Assert.IsNull(LcmsProcess.WriteLipidRtReview(run, _scratch, "AlignmentResult"));
        Assert.IsNull(LcmsProcess.WriteLipidRtReview(null, _scratch, "AlignmentResult"));
        Assert.AreEqual(0, Directory.GetFiles(_scratch).Length);
    }

    private static MsdialLcmsParameter Lipidomics() {
        return new MsdialLcmsParameter { TargetOmics = TargetOmics.Lipidomics, IonMode = IonMode.Positive };
    }

    /// <summary>
    /// The TG grid of the reference's synthetic tests, as alignment spots with chain-level LBM matches, and two
    /// unannotated spots so the run spans about 20 minutes.
    /// </summary>
    private static List<AlignmentSpotProperty> TgGrid(int shiftedC = 0, int shiftedDb = 0, double shift = 0d) {
        var spots = new List<AlignmentSpotProperty> { Spot(0, 0.5, 300.1, null), Spot(1, 19.5, 301.1, null) };
        var id = 2;
        foreach (var c in new[] { 48, 50, 52, 54, 56 }) {
            foreach (var db in new[] { 0, 1, 2, 3 }) {
                var rt = 2 + 0.15 * c - 0.25 * db + (c == shiftedC && db == shiftedDb ? shift : 0d);
                // TG neutral mass from the chain totals, [M+NH4]+
                var mz = 12d * (c + 3) + 1.007825 * (2 * c - 2 * db + 2) + 15.994915 * 6 + 18.033823;
                spots.Add(Spot(id++, rt, mz, $"TG {c}:{db}|TG 16:0_16:0_{c - 32}:{db}"));
            }
        }
        return spots;
    }

    private static AlignmentSpotProperty Spot(int id, double rt, double mz, string? name) {
        var spot = new AlignmentSpotProperty
        {
            MasterAlignmentID = id,
            AlignmentID = id,
            TimesCenter = new ChromXs(rt),
            MassCenter = mz,
            HeightAverage = 1e5f,
            Ontology = name is null ? string.Empty : "TG",
            Name = name ?? string.Empty,
        };
        spot.SetAdductType(AdductIon.GetAdductIon("[M+NH4]+"));
        if (name != null) {
            spot.MatchResults.AddResult(new MsScanMatchResult
            {
                Name = name,
                IsPrecursorMzMatch = true,
                IsSpectrumMatch = true,
                IsLipidClassMatch = true,
                IsLipidChainsMatch = true,
                IsReferenceMatched = true,
                Source = SourceType.MspDB,
                AnnotatorID = "lbm",
                WeightedDotProduct = 0.8f,
                TotalScore = 0.8f,
            });
        }
        return spot;
    }
}
