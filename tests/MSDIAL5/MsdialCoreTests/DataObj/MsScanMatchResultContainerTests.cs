using CompMs.Common.DataObj.Result;
// The per-result field-by-field comparison lives here. Without it, Assert.That.AreEqual on two
// MsScanMatchResult instances binds the built-in static Assert.AreEqual<T> and fails to compile.
using CompMs.Common.DataObj.Result.Tests;
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

        /// <summary>
        /// Every serialized member of <see cref="MsScanMatchResult"/> carries a distinct
        /// non-default value here, so that a wrong or missing [Key] attribute fails.
        /// </summary>
        /// <remarks>
        /// <see cref="SaveAndLoadTest"/> above sets only Source and TotalScore, so every other
        /// assertion in MsScanMatchResultHelper.AreEqual compares a default against a default and
        /// cannot fail. That made the round-trip witness a witness in name only: a member added
        /// with a duplicate or omitted [Key] would round-trip untested and the suite would stay
        /// green. This test exists so that the [Key] allocation is actually proven.
        /// </remarks>
        [TestMethod()]
        public void EverySerializedMemberSurvivesARoundTrip() {
            var expected = new MsScanMatchResult {
                Name = "a name",
                InChIKey = "AAAAAAAAAAAAAA-BBBBBBBBFB-C",
                TotalScore = 0.11f,
                SquaredWeightedDotProduct = 0.12f,
                SquaredSimpleDotProduct = 0.13f,
                SquaredReverseDotProduct = 0.14f,
                MatchedPeaksCount = 0.15f,
                MatchedPeaksPercentage = 0.16f,
                EssentialFragmentMatchedScore = 0.17f,
                AndromedaScore = 0.18f,
                PEPScore = 0.19f,
                RtSimilarity = 0.21f,
                RiSimilarity = 0.22f,
                CcsSimilarity = 0.23f,
                IsotopeSimilarity = 0.24f,
                AcurateMassSimilarity = 0.25f,
                LibraryID = 31,
                LibraryIDWhenOrdered = 32,
                IsPrecursorMzMatch = true,
                IsSpectrumMatch = true,
                IsRtMatch = true,
                IsRiMatch = true,
                IsCcsMatch = true,
                IsLipidClassMatch = true,
                IsLipidChainsMatch = true,
                IsLipidPositionMatch = true,
                IsLipidDoubleBondPositionMatch = true,
                IsOtherLipidMatch = true,
                Source = SourceType.MspDB | SourceType.Manual,
                AnnotatorID = "an annotator",
                SpectrumID = 33,
                IsDecoy = true,
                Priority = 34,
                IsReferenceMatched = true,
                IsAnnotationSuggested = true,
                CollisionEnergy = 35d,
                EnhancedDotProduct = 0.36f,
                SpectralEntropy = 0.37f,
                MeasuredTerms = MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass | MeasuredTerms.Ccs,
                EvidenceSource = AnnotationEvidenceSource.ReferenceSpectrum,
                CandidatesFound = 41,
                CandidatesAboveThreshold = 42,
                CandidatesReferenceMatched = 43,
            };

            // A fixture is only a witness while every field it carries differs from the default,
            // so this asserts that before trusting the round trip. It also fails when a new
            // serialized member is added above without being given a value here, which is the
            // point at which the omission is cheap to fix.
            AssertNoSerializedMemberIsLeftAtItsDefault(expected);

            var memory = new MemoryStream();
            Common.MessagePack.MessagePackDefaultHandler.SaveToStream(expected, memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = Common.MessagePack.MessagePackDefaultHandler.LoadFromStream<MsScanMatchResult>(memory);

            Assert.That.AreEqual(expected, actual);
        }

        private static void AssertNoSerializedMemberIsLeftAtItsDefault(MsScanMatchResult fixture) {
            var reference = new MsScanMatchResult();
            var unset = new List<string>();
            foreach (var property in typeof(MsScanMatchResult).GetProperties()) {
                if (!property.CanRead || !property.CanWrite) {
                    continue;
                }
                if (property.GetCustomAttributes(typeof(MessagePack.KeyAttribute), inherit: false).Length == 0) {
                    continue;
                }
                if (Equals(property.GetValue(fixture), property.GetValue(reference))) {
                    unset.Add(property.Name);
                }
            }
            Assert.AreEqual(
                0, unset.Count,
                $"These serialized members are still at their default value in the fixture, so the " +
                $"round trip cannot detect a wrong [Key] on them: {string.Join(", ", unset)}.");
        }
    }
}