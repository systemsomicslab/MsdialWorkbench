using CompMs.Common.DataObj.Result;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class FacadeMatchResultEvaluatorTests
    {
        [TestMethod()]
        [DynamicData(nameof(GetFilterTestDatas), DynamicDataSourceType.Method)]
        public void FilterByThresholdTest(MsScanMatchResult[] data, MsScanMatchResult[] expected) {
            var evaluator = Create();

            var actual = evaluator.FilterByThreshold(data);

            Assert.AreEqual(expected.Length, actual.Count);
            foreach (var (e, a) in expected.Zip(actual, (e, a) => (e, a))) {
                Assert.AreEqual(e.Name, a.Name);
            }
        }

        public static IEnumerable<object[]> GetFilterTestDatas() {
            var result1A = new MsScanMatchResult { AnnotatorID = "A", Name = "1A" };
            var result2A = new MsScanMatchResult { AnnotatorID = "A", Name = "2A" };
            var result1B = new MsScanMatchResult { AnnotatorID = "B", Name = "1B" };
            var result2B = new MsScanMatchResult { AnnotatorID = "B", Name = "2B" };

            yield return new object[] { new[] { result1A, result2A, }, new[] { result1A, result2A, } };
            yield return new object[] { new[] { result1B, result2B, }, new MsScanMatchResult[0] };
            yield return new object[] { new[] { result1A, result2A, result1B, result2B, }, new[] { result1A, result2A, } };
        }

        [TestMethod()]
        [DynamicData(nameof(GetSuggestedTestDatas), DynamicDataSourceType.Method)]
        public void IsAnnotationSuggestedTest(MsScanMatchResult data, bool expected) {
            var evaluator = Create();

            var actual = evaluator.IsAnnotationSuggested(data);

            Assert.AreEqual(expected, actual);
        }

        public static IEnumerable<object[]> GetSuggestedTestDatas() {
            var result1A = new MsScanMatchResult { AnnotatorID = "A", };
            var result2A = new MsScanMatchResult { AnnotatorID = "A", };
            var result1B = new MsScanMatchResult { AnnotatorID = "B", };
            var result2B = new MsScanMatchResult { AnnotatorID = "B", };

            yield return new object[] { result1A, true };
            yield return new object[] { result2A, true };
            yield return new object[] { result1B, false };
            yield return new object[] { result2B, false };
        }

        [TestMethod()]
        [DynamicData(nameof(GetRefMatchedTestDatas), DynamicDataSourceType.Method)]
        public void IsReferenceMatchedTest(MsScanMatchResult data, bool expected) {
            var evaluator = Create();

            var actual = evaluator.IsReferenceMatched(data);

            Assert.AreEqual(expected, actual);
        }

        public static IEnumerable<object[]> GetRefMatchedTestDatas() {
            var result1A = new MsScanMatchResult { AnnotatorID = "A", };
            var result2A = new MsScanMatchResult { AnnotatorID = "A", };
            var result1B = new MsScanMatchResult { AnnotatorID = "B", };
            var result2B = new MsScanMatchResult { AnnotatorID = "B", };

            yield return new object[] { result1A, true };
            yield return new object[] { result2A, true };
            yield return new object[] { result1B, false };
            yield return new object[] { result2B, false };
        }

        [TestMethod()]
        [DynamicData(nameof(GetSelectRefMatchesTestDatas), DynamicDataSourceType.Method)]
        public void SelectReferenceMatchResultsTest(MsScanMatchResult[] data, MsScanMatchResult[] expected) {
            var evaluator = Create();

            var actual = evaluator.SelectReferenceMatchResults(data);

            Assert.AreEqual(expected.Length, actual.Count);
            foreach (var (e, a) in expected.Zip(actual, (e, a) => (e, a))) {
                Assert.AreEqual(e.Name, a.Name);
            }
        }

        public static IEnumerable<object[]> GetSelectRefMatchesTestDatas() {
            var result1A = new MsScanMatchResult { AnnotatorID = "A", Name = "1A", };
            var result2A = new MsScanMatchResult { AnnotatorID = "A", Name = "1B", };
            var result1B = new MsScanMatchResult { AnnotatorID = "B", Name = "2A", };
            var result2B = new MsScanMatchResult { AnnotatorID = "B", Name = "2B", };

            yield return new object[] { new[] { result1A, result2A, }, new[] { result1A, result2A, } };
            yield return new object[] { new[] { result1B, result2B, }, new MsScanMatchResult[0] };
            yield return new object[] { new[] { result1A, result2A, result1B, result2B, }, new[] { result1A, result2A, } };
        }

        [TestMethod()]
        [DynamicData(nameof(GetSelectTopHitTestDatas), DynamicDataSourceType.Method)]
        public void SelectTopHitTest(MsScanMatchResult[] data, MsScanMatchResult expected) {
            var evaluator = Create();

            var actual = evaluator.SelectTopHit(data);

            Assert.AreEqual(expected.Name, actual.Name);
        }

        public static IEnumerable<object[]> GetSelectTopHitTestDatas() {
            var result1A = new MsScanMatchResult { AnnotatorID = "A", Name = "1A", Source = (SourceType)1, Priority = 1, TotalScore = 10, };
            var result2A = new MsScanMatchResult { AnnotatorID = "A", Name = "2A", Source = (SourceType)1, Priority = 1, TotalScore = 20, };
            var result3A = new MsScanMatchResult { AnnotatorID = "A", Name = "3A", Source = (SourceType)1, Priority = 2, TotalScore = 10, };
            var result4A = new MsScanMatchResult { AnnotatorID = "A", Name = "4A", Source = (SourceType)1, Priority = 2, TotalScore = 20, };
            var result5A = new MsScanMatchResult { AnnotatorID = "A", Name = "5A", Source = (SourceType)2, Priority = 1, TotalScore = 10, };
            var result6A = new MsScanMatchResult { AnnotatorID = "A", Name = "6A", Source = (SourceType)2, Priority = 1, TotalScore = 20, };
            var result7A = new MsScanMatchResult { AnnotatorID = "A", Name = "7A", Source = (SourceType)2, Priority = 2, TotalScore = 10, };
            var result8A = new MsScanMatchResult { AnnotatorID = "A", Name = "8A", Source = (SourceType)2, Priority = 2, TotalScore = 20, };
            var result1B = new MsScanMatchResult { AnnotatorID = "B", Name = "1B", Source = (SourceType)1, Priority = 1, TotalScore = 10, };
            var result2B = new MsScanMatchResult { AnnotatorID = "B", Name = "2B", Source = (SourceType)1, Priority = 1, TotalScore = 20, };
            var result3B = new MsScanMatchResult { AnnotatorID = "B", Name = "3B", Source = (SourceType)1, Priority = 2, TotalScore = 10, };
            var result4B = new MsScanMatchResult { AnnotatorID = "B", Name = "4B", Source = (SourceType)1, Priority = 2, TotalScore = 20, };
            var result5B = new MsScanMatchResult { AnnotatorID = "B", Name = "5B", Source = (SourceType)2, Priority = 1, TotalScore = 10, };
            var result6B = new MsScanMatchResult { AnnotatorID = "B", Name = "6B", Source = (SourceType)2, Priority = 1, TotalScore = 20, };
            var result7B = new MsScanMatchResult { AnnotatorID = "B", Name = "7B", Source = (SourceType)2, Priority = 2, TotalScore = 10, };
            var result8B = new MsScanMatchResult { AnnotatorID = "B", Name = "8B", Source = (SourceType)2, Priority = 2, TotalScore = 20, };

            yield return new object[] { new[] { result1A, result2A, result3A, result4A, result5A, result6A, result7A, result8A, result1B, result2B, result3B, result4B, result5B, result6B, result7B, result8B, }, result8A, };
            yield return new object[] { new[] { result1B, result2B, result3B, result4B, result5B, result6B, result7B, result8B, result1A, result2A, result3A, result4A, result5A, result6A, result7A, result8A, }, result8B, };
            yield return new object[] { new[] { result1A, result2A, result3A, result4A, result5A, result6A, result7A, result8A, }, result8A, };
            yield return new object[] { new[] { result1B, result2B, result3B, result4B, result5B, result6B, result7B, result8B, }, result8B, };
            yield return new object[] { new[] { result1A, result3A, result5A, result7A, result2B, result4B, result6B, result8B, }, result8B, };
            yield return new object[] { new[] { result1A, result2A, result3A, result4A, result5A, }, result5A, };
            yield return new object[] { new[] { result1A, result2A, result3A, }, result3A, };
            yield return new object[] { new[] { result1A, result2A, }, result2A, };
        }

        private FacadeMatchResultEvaluator Create() {
            var evaluators = new FacadeMatchResultEvaluator();
            evaluators.Add("A", new FakeEvaluatorA());
            evaluators.Add("B", new FakeEvaluatorB());
            return evaluators;
        }

        class FakeEvaluatorA : IMatchResultEvaluator<MsScanMatchResult>
        {
            public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
                return results.ToList();
            }

            public bool IsAnnotationSuggested(MsScanMatchResult result) {
                return true;
            }

            public bool IsReferenceMatched(MsScanMatchResult result) {
                return true;
            }

            public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
                return results.ToList();
            }

            public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
                return results.FirstOrDefault();
            }
        }

        class FakeEvaluatorB : IMatchResultEvaluator<MsScanMatchResult>
        {
            public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
                return new List<MsScanMatchResult>(0);
            }

            public bool IsAnnotationSuggested(MsScanMatchResult result) {
                return false;
            }

            public bool IsReferenceMatched(MsScanMatchResult result) {
                return false;
            }

            public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
                return new List<MsScanMatchResult>(0);
            }

            public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
                return null;
            }
        }
    }
}