using CompMs.Common.DataObj.NodeEdge;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.Common.Algorithm.Function.Tests
{
    [TestClass]
    public class ExternalEdgeSorterTests
    {
        [TestMethod]
        public void Sort_PreservesStableScoreOrderAcrossMultipleMergePasses() {
            var edges = Enumerable.Range(0, 200).Select(i => new EdgeData {
                source = i, target = i + 1, score = (i * 17 % 11) / 10d,
                matchpeakcount = i + 0.5, linecolor = "red",
            }).ToArray();
            var expected = edges.OrderByDescending(e => e.score).ToArray();
            // More than 32 runs forces a second merge pass.
            var actual = ExternalEdgeSorter.Sort(edges, chunkSize: 3).ToArray();
            CollectionAssert.AreEqual(expected.Select(e => e.source).ToArray(), actual.Select(e => e.source).ToArray());
            for (int i = 0; i < actual.Length; i++) {
                Assert.AreEqual(expected[i].target, actual[i].target);
                Assert.AreEqual(expected[i].score, actual[i].score);
                Assert.AreEqual(expected[i].matchpeakcount, actual[i].matchpeakcount);
                Assert.AreEqual(expected[i].linecolor, actual[i].linecolor);
            }
        }

        [TestMethod]
        public void Sort_RemovesTemporaryFilesOnFailureAndEarlyDisposal() {
            var output = Path.Combine(Path.GetTempPath(), "msn-sort-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            try {
                string folder = null;
                IEnumerable<EdgeData> Candidates(bool fail) {
                    folder = Directory.GetDirectories(output, "msdial-msn-*").Single();
                    for (int i = 0; i < 10; i++) yield return new EdgeData { source = i, score = i };
                    if (fail) throw new InvalidOperationException("candidate generation failed");
                }
                Assert.ThrowsExactly<InvalidOperationException>(() => ExternalEdgeSorter.Sort(Candidates(true), 3, output).ToArray());
                Assert.IsFalse(Directory.Exists(folder));
                using (var sorted = ExternalEdgeSorter.Sort(Candidates(false), 3, output).GetEnumerator()) {
                    Assert.IsTrue(sorted.MoveNext());
                    Assert.IsTrue(Directory.Exists(folder));
                }
                Assert.IsFalse(Directory.Exists(folder));
                ExternalEdgeSorter.Sort(Candidates(false), 3, output).ToArray();
                Assert.IsFalse(Directory.Exists(folder));
                Assert.IsTrue(Directory.Exists(output), "The output directory must remain.");
            }
            finally {
                Directory.Delete(output, true);
            }
        }
    }
}
