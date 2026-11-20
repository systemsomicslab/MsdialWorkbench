#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using System.IO;
using static Riken.Metabolomics.MsfinderCommon.Tests.DiagnosticQueryFiles;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>Runtime creation (design 5.2).</summary>
[TestClass]
public class DiagnosticRuleRuntimeTests
{
    [TestMethod]
    public void EmbeddedSetWithoutRankingHasNoMatcher() {
        var runtime = DiagnosticRuleRuntime.Create(new MsfinderDiagnosticRuleSettings { Evaluation = true }, ProductTolerance.Da(0.01), out var error);
        Assert.IsNotNull(runtime, error);
        Assert.IsNull(error);
        Assert.IsNull(runtime!.Matcher);
        Assert.IsFalse(runtime.RankingOn);
        Assert.AreEqual(RuleSetSource.Embedded, runtime.RuleSet.Identity.Source);
        Assert.IsTrue(runtime.RuleSet.Identity.Pinned);
    }

    [TestMethod]
    public void PositiveWeightBuildsTheMatcher() {
        var runtime = DiagnosticRuleRuntime.Create(new MsfinderDiagnosticRuleSettings { Evaluation = true, RankingWeight = 0.25 }, ProductTolerance.Da(0.01), out var error);
        Assert.IsNotNull(runtime?.Matcher, error);
        Assert.IsTrue(runtime!.RankingOn);
        Assert.AreEqual(0.25, runtime.RankingWeight);
    }

    [TestMethod]
    public void SettingsErrorsRefuseToStart() {
        var settings = new MsfinderDiagnosticRuleSettings { Evaluation = true };
        settings.Errors.Add("DiagnosticRuleRankingWeight=abc");
        Assert.IsNull(DiagnosticRuleRuntime.Create(settings, ProductTolerance.Da(0.01), out var error));
        StringAssert.StartsWith(error, "rule_set_error:settings:");
    }

    [TestMethod]
    public void LoadFailureRefusesToStartAndHidesThePath() {
        var dir = NewTempDirectory();
        try {
            var missing = Path.Combine(dir, "no-such-ruleset");
            Assert.IsNull(DiagnosticRuleRuntime.Create(new MsfinderDiagnosticRuleSettings { Evaluation = true, RuleSetDirectory = missing }, ProductTolerance.Da(0.01), out var error));
            StringAssert.StartsWith(error, "rule_set_error:load:");
            Assert.IsFalse(error.Contains(dir), error);

            Assert.IsNull(DiagnosticRuleRuntime.Create(new MsfinderDiagnosticRuleSettings { Evaluation = true, RuleSetDirectory = dir }, ProductTolerance.Da(0.01), out error));
            StringAssert.StartsWith(error, "rule_set_error:load:");
            Assert.IsFalse(error.Contains(dir), error);
        }
        finally {
            DeleteDirectory(dir);
        }
    }

    [TestMethod]
    public void InvalidToleranceRefusesToStart() {
        Assert.IsNull(DiagnosticRuleRuntime.Create(new MsfinderDiagnosticRuleSettings { Evaluation = true }, ProductTolerance.Da(0.0000001), out var error));
        StringAssert.StartsWith(error, "rule_set_error:options:");
    }

    [TestMethod]
    public void StrictWithoutRefusalsStarts() {
        // The shipped set has no refused rule, so Strict starts; a refusal under Strict fails Create (refused:...).
        var runtime = CreateRuntime(new MsfinderDiagnosticRuleSettings { Evaluation = true, Strict = true });
        Assert.AreEqual(0, runtime.Evaluator.Refusals.Count);
    }

    [TestMethod]
    public void ToleranceFollowsMassTolType() {
        var da = DiagnosticRuleRuntime.ToleranceOf(MassToleranceType.Da, 0.01);
        Assert.AreEqual(0.01, da.AbsDa);
        Assert.AreEqual(0.0, da.Ppm);
        var ppm = DiagnosticRuleRuntime.ToleranceOf(MassToleranceType.Ppm, 10);
        Assert.AreEqual(0.0, ppm.AbsDa);
        Assert.AreEqual(10.0, ppm.Ppm);
        var param = new AnalysisParamOfMsfinder();
        var fromParam = DiagnosticRuleRuntime.ToleranceOf(param);
        Assert.AreEqual(param.MassTolType == MassToleranceType.Ppm ? param.Mass2Tolerance : 0.0, fromParam.Ppm);
    }
}
