#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Motifs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>The motif matcher of the ranking term (design 5.6): NCDK verdicts, the cache and parse failures.</summary>
[TestClass]
public class MotifMatcherTests
{
    private static readonly Lazy<DiagnosticRuleSet> Set = new Lazy<DiagnosticRuleSet>(DiagnosticRuleSet.LoadEmbedded);

    /// <summary>The matcher agrees with MotifTests' fresh-pattern verdicts: every rule's positive examples match its
    /// claim motif and its negative examples do not.</summary>
    [TestMethod]
    public void EveryRuleExampleMatchesItsClaimMotifAsItsRoleSays() {
        var matcher = new MotifMatcher(Set.Value);
        var failures = new List<string>();
        var checkedExamples = 0;
        foreach (var rule in Set.Value.Rules) {
            foreach (var (examples, expected) in new[] { (rule.Claim.PositiveExamples, true), (rule.Claim.NegativeExamples, false) }) {
                foreach (var example in examples) {
                    checkedExamples++;
                    if (matcher.Matches(example.Smiles, rule.Claim.MotifId) != expected) {
                        failures.Add(rule.RuleId + " " + example.PubchemCid + " expected " + expected);
                    }
                }
            }
        }
        Assert.IsTrue(checkedExamples > 100, checkedExamples.ToString());
        Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
    }

    [TestMethod]
    public void ResultsAreCachedBySmilesAndMotif() {
        var matcher = new MotifMatcher(Set.Value);
        var rule = Set.Value.Rules.First(r => r.Claim.PositiveExamples.Count > 0);
        var smiles = rule.Claim.PositiveExamples[0].Smiles;
        var motifs = Set.Value.Motifs.Select(m => m.MotifId).ToList();
        var first = motifs.Select(m => matcher.Matches(smiles, m)).ToList();
        Assert.AreEqual(1, matcher.UniqueSmiles);
        Assert.AreEqual(0, matcher.CacheHits);
        var second = motifs.Select(m => matcher.Matches(smiles, m)).ToList();
        CollectionAssert.AreEqual(first, second);
        Assert.AreEqual(motifs.Count, matcher.CacheHits);
        Assert.IsTrue(first.Contains(true));

        // A fresh matcher, motifs in another order, gives the same verdicts: no state leaks between motifs.
        var other = new MotifMatcher(Set.Value);
        var reversed = Enumerable.Reverse(motifs).Select(m => other.Matches(smiles, m)).Reverse().ToList();
        CollectionAssert.AreEqual(first, reversed);
    }

    [TestMethod]
    public void UnparsableSmilesMatchesNothing() {
        var matcher = new MotifMatcher(Set.Value);
        foreach (var motif in Set.Value.Motifs.Select(m => m.MotifId)) {
            foreach (var smiles in new[] { "", null, "C1CC", "not a smiles", "C(C", "OC1=CC=C(C=C1" }) {
                Assert.IsFalse(matcher.Matches(smiles!, motif), motif + " " + smiles);
            }
        }
    }

    [TestMethod]
    public void UnknownMotifIsAnError() {
        var matcher = new MotifMatcher(Set.Value);
        Assert.IsFalse(matcher.HasMotif("no_such_motif"));
        Assert.IsTrue(matcher.HasMotif(Set.Value.Motifs[0].MotifId));
        Assert.ThrowsException<ArgumentException>(() => matcher.Matches("CCO", "no_such_motif"));
        Assert.ThrowsException<ArgumentNullException>(() => new MotifMatcher(null!));
    }
}
