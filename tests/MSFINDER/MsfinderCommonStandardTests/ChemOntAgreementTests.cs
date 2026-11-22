#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using System.Linq;
using static Riken.Metabolomics.MsfinderCommon.Tests.DiagnosticQueryFiles;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>The ChemOnt agreement (design 5.7), by name against motifs.json, report only.</summary>
[TestClass]
public class ChemOntAgreementTests
{
    private static MotifRow MotifWithBoth() => Runtime.RuleSet.Motifs.First(m => m.Compatible.Count > 0 && m.Broader.Count > 0);

    [TestMethod]
    public void RelationByName() {
        var motif = MotifWithBoth();
        Assert.AreEqual(ChemOntAgreement.Agree, ChemOntAgreement.Relation(motif, motif.Compatible[0].Name));
        Assert.AreEqual(ChemOntAgreement.Agree, ChemOntAgreement.Relation(motif, "  " + motif.Compatible[0].Name + " "), "trimmed");
        Assert.AreEqual(ChemOntAgreement.Broader, ChemOntAgreement.Relation(motif, motif.Broader[0].Name));
        Assert.AreEqual(ChemOntAgreement.Differs, ChemOntAgreement.Relation(motif, "Organic acids and derivatives (synthetic test name)"));
        Assert.AreEqual(ChemOntAgreement.Differs, ChemOntAgreement.Relation(motif, motif.Compatible[0].Name.ToUpperInvariant()), "by exact name");
        Assert.AreEqual(ChemOntAgreement.Differs, ChemOntAgreement.Relation(motif, motif.Compatible[0].Id), "an id is not a name");
    }

    [TestMethod]
    public void NoTermOrNoMappingIsNotComparable() {
        var motif = MotifWithBoth();
        Assert.AreEqual(ChemOntAgreement.NotComparable, ChemOntAgreement.Relation(motif, null!));
        Assert.AreEqual(ChemOntAgreement.NotComparable, ChemOntAgreement.Relation(motif, ""));
        Assert.AreEqual(ChemOntAgreement.NotComparable, ChemOntAgreement.Relation(motif, "  "));
        Assert.AreEqual(ChemOntAgreement.NotComparable, ChemOntAgreement.Relation(Runtime.RuleSet, "no_such_motif", motif.Compatible[0].Name));
        foreach (var unmapped in Runtime.RuleSet.Motifs.Where(m => m.Compatible.Count == 0 && m.Broader.Count == 0)) {
            Assert.AreEqual(ChemOntAgreement.NotComparable, ChemOntAgreement.Relation(unmapped, "Flavonoids"));
        }
    }

    /// <summary>
    /// Per outcome: under Reviewed every 0.1.0 outcome is ineligible, so every relation is not_comparable (OQ06);
    /// under Measured the fired outcomes are compared, (a), (b) and (c) each with its own term.
    /// </summary>
    [TestMethod]
    public void CompareFollowsTheScope() {
        var dir = NewTempDirectory();
        try {
            var (f, e) = Fixtures.Select((x, i) => (x, Runtime.Evaluate(WriteAndRead(dir, "c" + i + ".msp", Msp(x, null)), false)))
                                 .First(p => AnyFired(p.Item2.Result));
            var set = Runtime.RuleSet;
            var fired = e.Result!.Outcomes.First(o => o.Fired);
            var motif = ChemOntAgreement.Motif(set, fired.MotifId);
            var compatible = motif.Compatible.Count > 0 ? motif.Compatible[0].Name : "x";
            var broader = motif.Broader.Count > 0 ? motif.Broader[0].Name : "x";

            var reviewed = ChemOntAgreement.Compare(e.Result, set, DiagnosticRuleRankingScope.Reviewed, compatible, broader, "Something else");
            Assert.AreEqual(e.Result.Outcomes.Count, reviewed.Count);
            Assert.IsTrue(reviewed.All(r => r.TopFormula == ChemOntAgreement.NotComparable && r.BaseRank1 == ChemOntAgreement.NotComparable
                                            && r.RankedRank1 == ChemOntAgreement.NotComparable), f.Label);

            var measured = ChemOntAgreement.Compare(e.Result, set, DiagnosticRuleRankingScope.Measured, compatible, broader, "Something else");
            var row = measured.First(r => r.RuleId == fired.RuleId);
            Assert.AreEqual(ChemOntAgreement.Relation(motif, compatible), row.TopFormula);
            Assert.AreEqual(ChemOntAgreement.Relation(motif, broader), row.BaseRank1);
            Assert.AreEqual(motif.Compatible.Count + motif.Broader.Count == 0 ? ChemOntAgreement.NotComparable : ChemOntAgreement.Differs, row.RankedRank1);
            foreach (var (o, r) in e.Result.Outcomes.Zip(measured, (o, r) => (o, r)).Where(p => !p.o.Fired)) {
                Assert.AreEqual(ChemOntAgreement.NotComparable, r.TopFormula, o.RuleId);
            }
            Assert.AreEqual(0, ChemOntAgreement.Compare(null!, set, DiagnosticRuleRankingScope.Measured, "a", "b", "c").Count);
        }
        finally {
            DeleteDirectory(dir);
        }
    }
}
