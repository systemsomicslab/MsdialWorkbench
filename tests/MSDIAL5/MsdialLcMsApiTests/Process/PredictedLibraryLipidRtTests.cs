using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using CompMs.MsdialLcMsApi.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcMsApi.Process.Tests;

/// <summary>
/// LC-MS processing hands the lipid RT check the annotators of the project's libraries of predicted spectra, so that a
/// match from such a library is never tagged Confirmed (Q5). Searched under lipidomics, that match records RuleBased, as
/// the LBM's does, and the library is the only thing that tells the two apart; the check's own parameter for it is
/// optional, so a caller that stops passing it still compiles. These tests pin hook A in <see cref="FileProcess"/> and
/// the aligner <see cref="LcmsAlignmentProcessFactory"/> makes for hook B.
/// </summary>
/// <remarks>
/// The peaks are the TG grid of the core tests (C 48-56 even, DB 0-3, RT = 2 + 0.15 C - 0.25 DB, [M+NH4]+, chain-level
/// LBM matches) and two unannotated peaks at 0.5 and 19.5 min, so that the run spans about 20 minutes. TG 52:1 is named
/// by the predicted library's annotator instead. The library is empty: no library content.
/// </remarks>
[TestClass]
public sealed class PredictedLibraryLipidRtTests
{
    private const double H = 1.007825, C12 = 12.0, O16 = 15.994915, Ammonium = 18.033823;

    [TestMethod]
    public void HookAInFileProcessDoesNotEvaluateAMatchFromALibraryOfPredictedSpectra() {
        var storage = Storage(withPredictedLibrary: true);
        var process = new FileProcess(new StandardDataProviderFactory(), storage, new NoAnnotation(), FacadeMatchResultEvaluator.FromDataBases(storage.DataBases));
        var peaks = Peaks();

        var run = process.RunLipidRtCheck(new AnalysisFileBean { AnalysisFileName = "s1" }, peaks);

        Assert.IsTrue(run.Ran, run.Describe());
        var tg521 = Find(peaks, 52, 1);
        Assert.IsFalse(tg521.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
        Assert.AreEqual(LipidRtOutcome.NotEvaluable, tg521.LipidRtVerdict.Outcome);
        Assert.AreEqual(LipidRtReason.Ineligible, tg521.LipidRtVerdict.Reason);
        StringAssert.Contains(tg521.LipidRtVerdict.Evidence, "in silico");
        Assert.IsTrue(Find(peaks, 52, 2).TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "the LBM-named neighbour is still confirmed");

        var control = Peaks();
        new FileProcess(new StandardDataProviderFactory(), Storage(withPredictedLibrary: false), new NoAnnotation(), FacadeMatchResultEvaluator.FromDataBases(DataBaseStorage.CreateEmpty()))
            .RunLipidRtCheck(new AnalysisFileBean { AnalysisFileName = "s1" }, control);
        Assert.IsTrue(Find(control, 52, 1).TagCollection.IsSelected(PeakSpotTag.CONFIRMED),
            "the control: without the library, the RuleBased record alone does not keep the match out");
    }

    [TestMethod]
    public void TheAlignerForHookBIsGivenTheAnnotatorsOfTheLibrariesOfPredictedSpectra() {
        var storage = Storage(withPredictedLibrary: false);
        AddLibrary(storage.DataBases, "strict_db", DataBaseSource.Msp, "strict");
        var factory = new LcmsAlignmentProcessFactory(storage, FacadeMatchResultEvaluator.FromDataBases(storage.DataBases));

        CollectionAssert.AreEquivalent(Array.Empty<string>(), factory.CreatePeakAligner().PredictedLibraryAnnotators.ToArray());

        // Read when the aligner is made, so a library added to the project after the factory was made is seen.
        AddLibrary(storage.DataBases, "glacier_db", DataBaseSource.PredictedMsp, "glacier");
        CollectionAssert.AreEquivalent(new[] { "glacier" }, factory.CreatePeakAligner().PredictedLibraryAnnotators.ToArray());
    }

    private static MsdialLcmsDataStorage Storage(bool withPredictedLibrary) {
        var parameter = new MsdialLcmsParameter { TargetOmics = TargetOmics.Lipidomics, IonMode = IonMode.Positive, MachineCategory = MachineCategory.LCMS, };
        var databases = DataBaseStorage.CreateEmpty();
        if (withPredictedLibrary) {
            AddLibrary(databases, "glacier_db", DataBaseSource.PredictedMsp, "glacier");
        }
        return new MsdialLcmsDataStorage { MsdialLcmsParameter = parameter, DataBases = databases, };
    }

    // An empty MSP library searched under lipidomics, as a Lipidomics project's GUI builds its annotators.
    private static void AddLibrary(DataBaseStorage databases, string id, DataBaseSource kind, string annotatorId) {
        var database = new MoleculeDataBase(new List<MoleculeMsReference>(), id, kind, SourceType.MspDB, id + ".msp");
        var parameter = new MsdialLcmsParameter { TargetOmics = TargetOmics.Lipidomics, };
        var annotator = new LcmsMspAnnotator(database, parameter.MspSearchParam, TargetOmics.Lipidomics, annotatorId, 1);
        databases.AddMoleculeDataBase(database, new List<IAnnotatorParameterPair<MoleculeDataBase>>
        {
            new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, parameter.PeakPickBaseParam, parameter.MspSearchParam, ignoreIsotopicPeak: true)),
        });
    }

    private static List<ChromatogramPeakFeature> Peaks() {
        var objects = new List<(double Rt, double Mz, string Name)> { (0.5, 300.1, null), (19.5, 301.1, null) };
        foreach (var c in new[] { 48, 50, 52, 54, 56 }) {
            foreach (var db in new[] { 0, 1, 2, 3 }) {
                var mass = C12 * (c + 3) + H * (2 * c - 2 * db + 2) + O16 * 6;
                objects.Add((2 + 0.15 * c - 0.25 * db, mass + Ammonium, $"TG {c}:{db}|TG 16:0_16:0_{c - 32}:{db}"));
            }
        }
        var peaks = new List<ChromatogramPeakFeature>();
        for (var id = 0; id < objects.Count; id++) {
            var (rt, mz, name) = objects[id];
            var peak = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
            {
                Mass = mz, PeakHeightTop = 1e5, ChromXsLeft = new ChromXs(rt), ChromXsTop = new ChromXs(rt), ChromXsRight = new ChromXs(rt),
            })
            {
                MasterPeakID = id,
                Ontology = name is null ? string.Empty : "TG",
                Name = name ?? string.Empty,
            };
            peak.SetAdductType(AdductIon.GetAdductIon("[M+NH4]+"));
            if (name != null) {
                peak.MatchResults.AddResult(new MsScanMatchResult
                {
                    Name = name,
                    IsPrecursorMzMatch = true,
                    IsSpectrumMatch = true,
                    IsLipidClassMatch = true,
                    IsLipidChainsMatch = true,
                    IsReferenceMatched = true,
                    Source = SourceType.MspDB,
                    // TG 52:1 is named by the predicted library's annotator, recorded as one scoring under lipidomics records it.
                    AnnotatorID = name.StartsWith("TG 52:1|", StringComparison.Ordinal) ? "glacier" : "lbm",
                    EvidenceSource = AnnotationEvidenceSource.RuleBased,
                    WeightedDotProduct = 0.8f,
                    TotalScore = 0.8f,
                });
            }
            peaks.Add(peak);
        }
        return peaks;
    }

    private static ChromatogramPeakFeature Find(IEnumerable<ChromatogramPeakFeature> peaks, int c, int db) {
        return peaks.Single(p => p.Name.StartsWith($"TG {c}:{db}|", StringComparison.Ordinal));
    }

    private sealed class NoAnnotation : IAnnotationProcess
    {
        public Task RunAnnotationAsync(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, MSDecResultCollection msdecResult, IDataProvider provider, int numThread = 1, Action<double> reportAction = null, CancellationToken token = default) {
            return Task.CompletedTask;
        }
    }
}
