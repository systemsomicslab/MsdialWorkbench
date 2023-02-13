using CompMs.Common.DataObj.Result;
using CompMs.Common.DataObj.Result.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.MsdialCore.DataObj.Tests
{
    public static class MsScanMatchResultContainerTestHelper {
        public static void AreEqual(this Assert assert, MsScanMatchResultContainer expected, MsScanMatchResultContainer actual) {
            Assert.AreEqual(expected.MatchResults.Count, actual.MatchResults.Count);
            foreach (var (e, a) in expected.MatchResults.Zip(actual.MatchResults, (e, a) => (e, a))) {
                assert.AreEqual(e, a);
            }
            Assert.AreEqual(expected.MSRawID2MspBasedMatchResult.Count, actual.MSRawID2MspBasedMatchResult.Count);
            foreach (var (e, a) in expected.MSRawID2MspBasedMatchResult.Zip(actual.MSRawID2MspBasedMatchResult, (p, q) => (p.Value, q.Value))) {
                assert.AreEqual(e, a);
            }
            Assert.AreEqual(expected.TextDbBasedMatchResults.Count, actual.TextDbBasedMatchResults.Count);
            foreach (var (e, a) in expected.TextDbBasedMatchResults.Zip(actual.TextDbBasedMatchResults, (e, a) => (e, a))) {
                assert.AreEqual(e, a);
            }
        }

        public static void ExistsMspResultAndAreEqual(this Assert assert, MsScanMatchResult expected, int key, MsScanMatchResultContainer container) {
            Assert.IsTrue(container.MSRawID2MspBasedMatchResult.ContainsKey(key));
            Assert.AreEqual(expected, container.MSRawID2MspBasedMatchResult[key]);
        }
    }
}