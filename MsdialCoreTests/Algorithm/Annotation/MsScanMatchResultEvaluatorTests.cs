using CompMs.Common.Parameter;
using CompMs.Common.DataObj.Result;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class MsScanMatchResultEvaluatorTests
    {
        [DataTestMethod()]
        [DataRow(false, false, new[] { 1, 2, })]
        [DataRow(false, true,  new[] { 1, 2, })]
        [DataRow(true,  false, new[] { 1, 2, })]
        [DataRow(true,  true,  new[] { 1, 2, })]
        public void FilterByThresholdTest(bool isUseTime, bool isUseCcs, int[] expected) {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = isUseTime,
                IsUseCcsForAnnotationFiltering = isUseCcs,
            };
            var evaluator = new MsScanMatchResultEvaluator(parameter);
            var results = CreateResults();

            var actuals = evaluator.FilterByThreshold(results);
            var expectedResults = expected.Select(e => results[e]).ToArray();

            CollectionAssert.AreEquivalent(expectedResults, actuals);
        }

        [TestMethod()]
        [DataRow(false, false, new[] { false, false, true, })]
        [DataRow(false, true,  new[] { false, false, true, })]
        [DataRow(true,  false, new[] { false, false, true, })]
        [DataRow(true,  true,  new[] { false, false, true, })]
        public void IsAnnotationSuggestedTest(bool isUseTime, bool isUseCcs, bool[] expected) {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = isUseTime,
                IsUseCcsForAnnotationFiltering = isUseCcs,
            };
            var evaluator = new MsScanMatchResultEvaluator(parameter);
            var results = CreateResults();

            var actuals = results.Select(result => evaluator.IsAnnotationSuggested(result)).ToList();
            actuals.ForEach(Console.WriteLine);
            CollectionAssert.AreEqual(expected, actuals);
        }

        [TestMethod()]
        [DataRow(false, false, new[] { false, true, false, })]
        [DataRow(false, true,  new[] { false, true, false, })]
        [DataRow(true,  false, new[] { false, true, false, })]
        [DataRow(true,  true,  new[] { false, true, false, })]
        public void IsReferenceMatchedTest(bool isUseTime, bool isUseCcs, bool[] expected) {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = isUseTime,
                IsUseCcsForAnnotationFiltering = isUseCcs,
            };
            var evaluator = new MsScanMatchResultEvaluator(parameter);
            var results = CreateResults();

            var actuals = results.Select(result => evaluator.IsReferenceMatched(result)).ToList();
            actuals.ForEach(Console.WriteLine);

            CollectionAssert.AreEqual(expected, actuals);
        }

        [TestMethod()]
        [DataRow(false, false, new[] { 1, })]
        [DataRow(false, true,  new[] { 1, })]
        [DataRow(true,  false, new[] { 1, })]
        [DataRow(true,  true,  new[] { 1, })]
        public void SelectReferenceMatchResultsTest(bool isUseTime, bool isUseCcs, int[] expected) {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = isUseTime,
                IsUseCcsForAnnotationFiltering = isUseCcs,
            };
            var evaluator = new MsScanMatchResultEvaluator(parameter);
            var results = CreateResults();

            var actuals = evaluator.SelectReferenceMatchResults(results);

            var expectedResult = expected.Select(e => results[e]).ToList();
            CollectionAssert.AreEquivalent(expectedResult, actuals);
        }

        private static List<MsScanMatchResult> CreateResults() {
            return new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, },
            };
        }
    }
}