using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt.Tests
{
    /// <summary>
    /// The exact maximum-weight consistent subset against exhaustive enumeration on small random posets (SPEC 13.1
    /// t16, poset.self_test), with fixed seeds.
    /// </summary>
    [TestClass]
    public class LipidRtAntichainTests
    {
        [DataTestMethod]
        [DataRow(1, 300)]
        [DataRow(3, 200)]
        [DataRow(11, 300)]
        public void StatusesAndMarginsAgreeWithBruteForce(int seed, int trials) {
            var random = new Random(seed);
            for (int trial = 0; trial < trials; trial++) {
                var n = random.Next(1, 9);
                // random points in (C, DBe) with random RTs -> a discordance order, transitive by construction
                var points = Enumerable.Range(0, n).Select(_ => (C: random.Next(0, 5), D: random.Next(0, 4), Rt: random.NextDouble() * 3)).ToArray();
                const double eps = 0.2;
                var pairs = new List<(int, int)>();
                for (int i = 0; i < n; i++) {
                    for (int j = 0; j < n; j++) {
                        if (i == j) continue;
                        var (ci, di, ri) = points[i];
                        var (cj, dj, rj) = points[j];
                        if (ci >= cj && di <= dj && (ci, di) != (cj, dj) && ri < rj - eps) {
                            pairs.Add((i, j));
                        }
                    }
                }
                var weights = Enumerable.Range(0, n).Select(_ => (long)random.Next(1, 5)).ToList();
                var result = LipidRtAntichain.Analyze(n, pairs, weights);
                var (best, sets) = BruteForce(n, pairs, weights);
                Assert.AreEqual(best, result.Weight, $"trial {trial}");
                for (int i = 0; i < n; i++) {
                    var inAll = sets.All(s => s.Contains(i));
                    var inNone = sets.All(s => !s.Contains(i));
                    var status = result.Status[i];
                    if (status == LipidRtFeatureStatus.Free) {
                        Assert.IsTrue(inAll, $"trial {trial}: a free element is in every maximum subset");
                    }
                    else {
                        var expected = inAll ? LipidRtFeatureStatus.Core : inNone ? LipidRtFeatureStatus.Excluded : LipidRtFeatureStatus.Ambiguous;
                        Assert.AreEqual(expected, status, $"trial {trial}, element {i}");
                    }
                    if (status == LipidRtFeatureStatus.Core) {
                        Assert.IsTrue(result.Essential[i] > 0);
                        Assert.AreEqual(best - BruteForce(n, pairs, weights, exclude: new[] { i }).Best, result.Essential[i]);
                    }
                    if (status == LipidRtFeatureStatus.Excluded) {
                        Assert.IsTrue(result.Deficit[i] > 0);
                        var excluded = new HashSet<int>(result.Partners[i]) { i };
                        Assert.AreEqual(best - (weights[i] + BruteForce(n, pairs, weights, exclude: excluded).Best), result.Deficit[i]);
                    }
                }
            }
        }

        [TestMethod]
        public void TheResultDoesNotDependOnTheOrderOfThePairs() {
            var random = new Random(5);
            for (int trial = 0; trial < 100; trial++) {
                var n = random.Next(2, 9);
                var points = Enumerable.Range(0, n).Select(_ => (C: random.Next(0, 5), D: random.Next(0, 4), Rt: random.NextDouble() * 3)).ToArray();
                var pairs = new List<(int, int)>();
                for (int i = 0; i < n; i++) {
                    for (int j = 0; j < n; j++) {
                        if (i != j && points[i].C >= points[j].C && points[i].D <= points[j].D && (points[i].C, points[i].D) != (points[j].C, points[j].D)
                            && points[i].Rt < points[j].Rt - 0.2) {
                            pairs.Add((i, j));
                        }
                    }
                }
                var weights = Enumerable.Range(0, n).Select(_ => (long)random.Next(1, 5)).ToList();
                var a = LipidRtAntichain.Analyze(n, pairs, weights);
                var b = LipidRtAntichain.Analyze(n, pairs.OrderBy(_ => random.Next()).ToList(), weights);
                CollectionAssert.AreEqual(a.Status.ToList(), b.Status.ToList());
                CollectionAssert.AreEqual(a.Essential.ToList(), b.Essential.ToList());
                CollectionAssert.AreEqual(a.Deficit.ToList(), b.Deficit.ToList());
            }
        }

        private static (long Best, List<HashSet<int>> Sets) BruteForce(int n, List<(int, int)> pairs, IReadOnlyList<long> weights, IEnumerable<int> exclude = null) {
            var related = new HashSet<(int, int)>(pairs.Concat(pairs.Select(p => (p.Item2, p.Item1))));
            var skip = new HashSet<int>(exclude ?? Enumerable.Empty<int>());
            long best = -1;
            var sets = new List<HashSet<int>>();
            for (int mask = 0; mask < (1 << n); mask++) {
                var members = Enumerable.Range(0, n).Where(i => (mask & (1 << i)) != 0).ToList();
                if (members.Any(skip.Contains)) {
                    continue;
                }
                var consistent = true;
                for (int x = 0; x < members.Count && consistent; x++) {
                    for (int y = x + 1; y < members.Count; y++) {
                        if (related.Contains((members[x], members[y]))) {
                            consistent = false;
                            break;
                        }
                    }
                }
                if (!consistent) {
                    continue;
                }
                var weight = members.Sum(i => weights[i]);
                if (weight > best) {
                    best = weight;
                    sets = new List<HashSet<int>> { new HashSet<int>(members) };
                }
                else if (weight == best) {
                    sets.Add(new HashSet<int>(members));
                }
            }
            return (best, sets);
        }
    }
}
