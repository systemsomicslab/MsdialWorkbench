using CompMs.Common.DataObj.Result;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class MsScanMatchResultContainerTests
    {
        [TestMethod()]
        public void AddResultTest() {
            var container = new MsScanMatchResultContainer();
            var result = new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.7f, };
            container.AddResult(result);
            CollectionAssert.Contains(container.MatchResults, result);
            Assert.AreEqual(result, container.Representative);

            var result2 = new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.9f, };
            container.AddResult(result2);
            CollectionAssert.Contains(container.MatchResults, result2);
            Assert.AreEqual(result2, container.Representative);
        }

        [TestMethod()]
        public void AddResultsTest() {
            var container = new MsScanMatchResultContainer();
            var results = new[]{
                new MsScanMatchResult(),
                new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.7f, },
                new MsScanMatchResult(),
            };
            container.AddResults(results);
            CollectionAssert.IsSubsetOf(results, container.MatchResults);
            Assert.AreEqual(results[1], container.Representative);

            var results2 = new[]{
                new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.9f, },
                new MsScanMatchResult(),
            };
            container.AddResults(results2);
            CollectionAssert.IsSubsetOf(results2, container.MatchResults);
            Assert.AreEqual(results2[0], container.Representative);
        }

        [TestMethod()]
        public void ClearResultsTest() {
            var container = new MsScanMatchResultContainer();
            var results = new[]{
                new MsScanMatchResult(),
                new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.8f, },
                new MsScanMatchResult(),
            };
            container.AddResults(results);
            CollectionAssert.IsSubsetOf(results, container.MatchResults);
            Assert.AreEqual(results[1], container.Representative);

            container.ClearResults();
            foreach (var result in results) {
                CollectionAssert.DoesNotContain(container.MatchResults, result);
            }
            CollectionAssert.DoesNotContain(results, container.Representative);
        }

        [TestMethod()]
        public void RemoveManuallyResultsTest() {
            var container = new MsScanMatchResultContainer();
            var results = new[]{
                new MsScanMatchResult { Source = SourceType.Manual, TotalScore = 0.5f, },
                new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.8f, },
                new MsScanMatchResult { Source = SourceType.Manual | SourceType.MspDB, TotalScore = 0.7f, },
            };
            container.AddResults(results);
            CollectionAssert.IsSubsetOf(results, container.MatchResults);
            Assert.AreEqual(results[2], container.Representative);

            container.RemoveManuallyResults();
            CollectionAssert.Contains(container.MatchResults, results[1]);
            CollectionAssert.DoesNotContain(container.MatchResults, results[0]);
            CollectionAssert.DoesNotContain(container.MatchResults, results[2]);
            Assert.AreEqual(results[1], container.Representative);
        }

        [TestMethod()]
        public void AddMspResultTest() {
            var container = new MsScanMatchResultContainer();
            var result = new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.4f, };
            container.AddMspResult(1, result);
            CollectionAssert.Contains(container.MatchResults, result);
            Assert.That.ExistsMspResultAndAreEqual(result, 1, container);
            Assert.AreEqual(result, container.MspBasedMatchResult);
            Assert.AreEqual(result, container.Representative);

            var result2 = new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.6f, };
            container.AddMspResult(2, result2);
            CollectionAssert.Contains(container.MatchResults, result2);
            Assert.That.ExistsMspResultAndAreEqual(result2, 2, container);
            Assert.AreEqual(result2, container.MspBasedMatchResult);
            Assert.AreEqual(result2, container.Representative);
        }

        [TestMethod()]
        public void AddMspResultsTest() {
            var container = new MsScanMatchResultContainer();
            var results = new Dictionary<int, MsScanMatchResult>
            {
                {1, new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.7f, } },
                {2, new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.8f, } },
                {4, new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.3f, } },
            };
            container.AddMspResults(results);
            CollectionAssert.IsSubsetOf(results.Values, container.MatchResults);
            Assert.That.ExistsMspResultAndAreEqual(results[1], 1, container);
            Assert.That.ExistsMspResultAndAreEqual(results[2], 2, container);
            Assert.That.ExistsMspResultAndAreEqual(results[4], 4, container);
            Assert.AreEqual(results[2], container.MspBasedMatchResult);
            Assert.AreEqual(results[2], container.Representative);

            var results2 = new Dictionary<int, MsScanMatchResult>
            {
                {3, new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.9f, } },
            };
            container.AddMspResults(results2);
            CollectionAssert.IsSubsetOf(results2.Values, container.MatchResults);
            Assert.That.ExistsMspResultAndAreEqual(results2[3], 3, container);
            Assert.AreEqual(results2[3], container.MspBasedMatchResult);
            Assert.AreEqual(results2[3], container.Representative);
        }

        [TestMethod()]
        public void ClearMspResultsTest() {
            var container = new MsScanMatchResultContainer();
            var result2 = new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.7f, };
            container.AddResult(result2);

            var results = new Dictionary<int, MsScanMatchResult>
            {
                {1, new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.7f, } },
                {2, new MsScanMatchResult { Source = SourceType.MspDB | SourceType.Manual, TotalScore = 0.8f, } },
                {4, new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.3f, } },
            };
            container.AddMspResults(results);
            CollectionAssert.IsSubsetOf(results.Values, container.MatchResults);
            Assert.AreEqual(results[2], container.MspBasedMatchResult);
            Assert.AreEqual(results[2], container.Representative);

            container.ClearMspResults();
            foreach (var result in results.Values) {
                CollectionAssert.DoesNotContain(container.MatchResults, result);
                CollectionAssert.DoesNotContain(container.MSRawID2MspBasedMatchResult.Values, result);
            }

            Assert.IsNull(container.MspBasedMatchResult);
            Assert.AreEqual(result2, container.Representative);
        }

        [TestMethod()]
        public void AddTextDbResultTest() {
            var container = new MsScanMatchResultContainer();
            var result1 = new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.7f, };
            container.AddTextDbResult(result1);

            CollectionAssert.Contains(container.MatchResults, result1);
            CollectionAssert.Contains(container.TextDbBasedMatchResults, result1);
            Assert.AreEqual(result1, container.Representative);

            var result2 = new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.8f, };
            container.AddTextDbResult(result2);

            CollectionAssert.Contains(container.MatchResults, result2);
            CollectionAssert.Contains(container.TextDbBasedMatchResults, result2);
            Assert.AreEqual(result2, container.Representative);
        }

        [TestMethod()]
        public void AddTextDbResultsTest() {
            var container = new MsScanMatchResultContainer();
            var results1 = new[]
            {
                new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.7f, },
                new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.8f, },
                new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.3f, },
            };
            container.AddTextDbResults(results1);

            CollectionAssert.IsSubsetOf(results1, container.MatchResults);
            CollectionAssert.AreEquivalent(results1, container.TextDbBasedMatchResults);
            Assert.AreEqual(results1[1], container.Representative);

            var results2 = new[]
            {
                new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.9f, },
            };
            container.AddTextDbResults(results2);

            CollectionAssert.IsSubsetOf(results2, container.MatchResults);
            CollectionAssert.IsSubsetOf(results2, container.TextDbBasedMatchResults);
            Assert.AreEqual(results2[0], container.Representative);
        }

        [TestMethod()]
        public void ClearTextDbResultsTest() {
            var container = new MsScanMatchResultContainer();
            var result2 = new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.6f, };
            container.AddResult(result2);
            var results = new[]
            {
                new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.7f, },
                new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.8f, },
                new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.3f, },
            };
            container.AddTextDbResults(results);

            CollectionAssert.IsSubsetOf(results, container.MatchResults);
            CollectionAssert.AreEquivalent(results, container.TextDbBasedMatchResults);
            Assert.AreEqual(results[1], container.Representative);

            container.ClearTextDbResults();
            foreach (var result in results) {
                CollectionAssert.DoesNotContain(container.MatchResults, result);
                CollectionAssert.DoesNotContain(container.TextDbBasedMatchResults, result);
            }
            Assert.AreEqual(result2, container.Representative);
        }

        [TestMethod()]
        public void MergeContainersTest() {
            var container1 = new MsScanMatchResultContainer();
            var results1 = new[]
            {
                new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.7f, Priority = 2, },
                new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.8f, Priority = 1, },
            };
            container1.AddResults(results1);
            CollectionAssert.IsSubsetOf(results1, container1.MatchResults);
            Assert.AreEqual(results1[0], container1.Representative);

            var container2 = new MsScanMatchResultContainer();
            var results2 = new[]
            {
                new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.4f, Priority = 2, },
                new MsScanMatchResult { Source = SourceType.MspDB | SourceType.Manual, TotalScore = 0.3f, Priority = 1, },
                new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.6f, Priority = 1, },
            };
            container2.AddResults(results2);
            CollectionAssert.IsSubsetOf(results2, container2.MatchResults);
            Assert.AreEqual(results2[1], container2.Representative);

            container1.MergeContainers(container2);
            Assert.AreEqual(results2[1], container1.Representative);
        }

        [TestMethod()]
        public void MergeShrinkTest() {
            var container1 = new MsScanMatchResultContainer();
            Assert.AreEqual(1, container1.MatchResults.Count); // Unkonown result exists.
            var commonResult = new MsScanMatchResult { Source = SourceType.TextDB, TotalScore = 0.7f, Priority = 2, };
            var results1 = new[]
            {
                commonResult,
                new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.8f, Priority = 1, },
            };
            container1.AddResults(results1);
            Assert.AreEqual(2, container1.MatchResults.Count);

            var container2 = new MsScanMatchResultContainer();
            var results2 = new[]
            {
                commonResult,
                new MsScanMatchResult { Source = SourceType.MspDB | SourceType.Manual, TotalScore = 0.3f, Priority = 1, },
                new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.6f, Priority = 1, },
            };
            container2.AddResults(results2);
            Assert.AreEqual(3, container2.MatchResults.Count);

            container1.MergeContainers(container2);
            Assert.AreEqual(4, container1.MatchResults.Count);
        }

        [TestMethod()]
        public void SaveAndLoadTest() {
            var container = new MsScanMatchResultContainer();
            var results = new[]{
                new MsScanMatchResult(),
                new MsScanMatchResult { Source = SourceType.MspDB, TotalScore = 0.7f, },
                new MsScanMatchResult(),
            };
            container.AddResults(results);
            var memory = new MemoryStream();
            Common.MessagePack.MessagePackDefaultHandler.SaveToStream(container, memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = Common.MessagePack.MessagePackDefaultHandler.LoadFromStream<MsScanMatchResultContainer>(memory);
            Assert.That.AreEqual(container, actual);
        }
    }
}