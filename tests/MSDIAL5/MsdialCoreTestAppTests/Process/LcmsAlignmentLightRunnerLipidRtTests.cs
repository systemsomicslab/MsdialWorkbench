#nullable enable
using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using CompMs.MsdialLcMsApi.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// Hook B in the Console's light-mode alignment hands the lipid RT check the annotators of the project's libraries of
/// predicted spectra, as PeakAligner is given them, so that a match from such a library is never tagged Confirmed (Q5).
/// </summary>
/// <remarks>
/// The case the library is needed for: a Lipidomics project whose MSP annotator table leaves a predicted row's
/// target_omics blank, so the annotator scores as lipidomics and records RuleBased, as the LBM does. The spots are the
/// TG grid of the reference's synthetic tests and two unannotated spots; TG 52:1 is named by the predicted library's
/// annotator. The library is empty: no library content.
/// </remarks>
[TestClass]
public sealed class LcmsAlignmentLightRunnerLipidRtTests
{
    [TestMethod]
    public void LightModeDoesNotEvaluateAMatchFromALibraryOfPredictedSpectra() {
        var spots = TgGrid();

        var run = LcmsAlignmentLightRunner.RunLipidRtCheck(spots, Storage(withPredictedLibrary: true));

        Assert.IsTrue(run.Ran, run.Describe());
        var tg521 = Find(spots, 52, 1);
        Assert.IsFalse(tg521.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
        Assert.AreEqual(LipidRtOutcome.NotEvaluable, tg521.LipidRtVerdict.Outcome);
        Assert.AreEqual(LipidRtReason.Ineligible, tg521.LipidRtVerdict.Reason);
        StringAssert.Contains(tg521.LipidRtVerdict.Evidence, "in silico");
        Assert.IsTrue(Find(spots, 52, 2).TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "the LBM-named neighbour is still confirmed");

        var control = TgGrid();
        LcmsAlignmentLightRunner.RunLipidRtCheck(control, Storage(withPredictedLibrary: false));
        Assert.IsTrue(Find(control, 52, 1).TagCollection.IsSelected(PeakSpotTag.CONFIRMED),
            "the control: without the library, the RuleBased record alone does not keep the match out");
    }

    /// <summary>
    /// The test above calls the method that reads the set; this one pins that the light-mode alignment does too. Run needs
    /// saved peaks and raw data, so its compiled body is read instead: it must call
    /// <see cref="LcmsAlignmentLightRunner.RunLipidRtCheck"/> and must not run the hook itself, as it did before the set
    /// existed.
    /// </summary>
    [TestMethod]
    public void RunRunsHookBThroughTheMethodThatPassesTheSet() {
        var called = CalledMethods(typeof(LcmsAlignmentLightRunner).GetMethod(nameof(LcmsAlignmentLightRunner.Run))!);

        var hookB = typeof(LcmsAlignmentLightRunner).GetMethod(nameof(LcmsAlignmentLightRunner.RunLipidRtCheck), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(hookB);
        Assert.IsTrue(called.Contains(hookB), "Run calls RunLipidRtCheck");
        var runOnSpots = typeof(LipidRtCheckHooks).GetMethod(nameof(LipidRtCheckHooks.RunOnSpots))!;
        Assert.IsFalse(called.Contains(runOnSpots), "Run does not run hook B itself");
        Assert.IsTrue(CalledMethods(hookB).Contains(runOnSpots), "the control: the reading sees a call");
    }

    private static MsdialLcmsDataStorage Storage(bool withPredictedLibrary) {
        var parameter = new MsdialLcmsParameter { TargetOmics = TargetOmics.Lipidomics, IonMode = IonMode.Positive };
        var databases = DataBaseStorage.CreateEmpty();
        if (withPredictedLibrary) {
            // As LcmsProcess builds a predicted row whose target_omics is blank: the project's omics.
            var database = new MoleculeDataBase(new List<MoleculeMsReference>(), "glacier_db", DataBaseSource.PredictedMsp, SourceType.MspDB, "glacier.msp");
            var annotator = new LcmsMspAnnotator(database, parameter.MspSearchParam, parameter.TargetOmics, "glacier", 1, allowsSuggestions: false);
            databases.AddMoleculeDataBase(database, [
                new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, parameter.PeakPickBaseParam, parameter.MspSearchParam, ignoreIsotopicPeak: true)),
            ]);
        }
        return new MsdialLcmsDataStorage { MsdialLcmsParameter = parameter, DataBases = databases };
    }

    /// <summary>
    /// The methods a method's compiled body calls. Each call or callvirt opcode (0x28, 0x6F) is followed by a method token;
    /// the same byte inside another operand reads as a token that resolves to nothing or to some other member, which can
    /// only add to the set, so a call that is there is never missed.
    /// </summary>
    private static HashSet<MethodBase> CalledMethods(MethodInfo method) {
        var il = method.GetMethodBody()!.GetILAsByteArray()!;
        var called = new HashSet<MethodBase>();
        for (var i = 0; i + 4 < il.Length; i++) {
            if (il[i] != 0x28 && il[i] != 0x6F) {
                continue;
            }
            try {
                if (method.Module.ResolveMethod(BitConverter.ToInt32(il, i + 1)) is { } callee) {
                    called.Add(callee);
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is BadImageFormatException) {
                // not a method token
            }
        }
        return called;
    }

    private static AlignmentSpotProperty Find(IEnumerable<AlignmentSpotProperty> spots, int c, int db) {
        return spots.Single(s => s.Name.StartsWith($"TG {c}:{db}|", StringComparison.Ordinal));
    }

    private static List<AlignmentSpotProperty> TgGrid() {
        var spots = new List<AlignmentSpotProperty> { Spot(0, 0.5, 300.1, null), Spot(1, 19.5, 301.1, null) };
        var id = 2;
        foreach (var c in new[] { 48, 50, 52, 54, 56 }) {
            foreach (var db in new[] { 0, 1, 2, 3 }) {
                var rt = 2 + 0.15 * c - 0.25 * db;
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
                AnnotatorID = name.StartsWith("TG 52:1|", StringComparison.Ordinal) ? "glacier" : "lbm",
                EvidenceSource = AnnotationEvidenceSource.RuleBased,
                WeightedDotProduct = 0.8f,
                TotalScore = 0.8f,
            });
        }
        return spot;
    }
}
