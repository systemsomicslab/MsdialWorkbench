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
        [DataRow(false, false, false, new[] { 8, 9, 10, 11, 12, 13, 14, 15, })]
        [DataRow(false, false, true,  new[] {       10, 11,         14, 15, })]
        [DataRow(false, true,  false, new[] {               12, 13, 14, 15, })]
        [DataRow(false, true,  true,  new[] {                       14, 15, })]
        [DataRow(true,  false, false, new[] { 8, 9, 10, 11, 12, 13, 14, 15, })]
        [DataRow(true,  false, true,  new[] {       10, 11,         14, 15, })]
        [DataRow(true,  true,  false, new[] {               12, 13, 14, 15, })]
        [DataRow(true,  true,  true,  new[] {                       14, 15, })]
        public void FilterByThresholdTest(bool accountSpectrum, bool isUseTime, bool isUseCcs, int[] expected) {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = isUseTime,
                IsUseCcsForAnnotationFiltering = isUseCcs,
            };
            var evaluator = CreateEvaluator(accountSpectrum, parameter);
            var results = CreateResults();

            var actuals = evaluator.FilterByThreshold(results);
            var expectedResults = expected.Select(e => results[e]).ToArray();

            CollectionAssert.AreEquivalent(expectedResults, actuals);
        }

        [TestMethod()]
        [DataRow(false, false, false, new[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, })]
        [DataRow(false, false, true,  new[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, })]
        [DataRow(false, true,  false, new[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, })]
        [DataRow(false, true,  true,  new[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, })]
        [DataRow(true,  false, false, new[] { false, false, false, false, false, false, false, false, true,  false, true,  false, true,  false, true,  false, })]
        [DataRow(true,  false, true,  new[] { false, false, false, false, false, false, false, false, false, false, true,  false, false, false, true,  false, })]
        [DataRow(true,  true,  false, new[] { false, false, false, false, false, false, false, false, false, false, false, false, true,  false, true,  false, })]
        [DataRow(true,  true,  true,  new[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, true,  false, })]
        public void IsAnnotationSuggestedTest(bool accountSpectrum, bool isUseTime, bool isUseCcs, bool[] expected) {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = isUseTime,
                IsUseCcsForAnnotationFiltering = isUseCcs,
            };
            var evaluator = CreateEvaluator(accountSpectrum, parameter);
            var results = CreateResults();

            var actuals = results.Select(result => evaluator.IsAnnotationSuggested(result)).ToList();
            actuals.ForEach(Console.WriteLine);
            CollectionAssert.AreEqual(expected, actuals);
        }

        [TestMethod()]
        [DataRow(false, false, false, new[] { false, false, false, false, false, false, false, false, true,  true,  true,  true,  true,  true,  true,  true,  })]
        [DataRow(false, false, true,  new[] { false, false, false, false, false, false, false, false, false, false, true,  true,  false, false, true,  true,  })]
        [DataRow(false, true,  false, new[] { false, false, false, false, false, false, false, false, false, false, false, false, true,  true,  true,  true,  })]
        [DataRow(false, true,  true,  new[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, true,  true,  })]
        [DataRow(true,  false, false, new[] { false, false, false, false, false, false, false, false, false, true,  false, true,  false, true,  false, true,  })]
        [DataRow(true,  false, true,  new[] { false, false, false, false, false, false, false, false, false, false, false, true,  false, false, false, true,  })]
        [DataRow(true,  true,  false, new[] { false, false, false, false, false, false, false, false, false, false, false, false, false, true,  false, true,  })]
        [DataRow(true,  true,  true,  new[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true,  })]
        public void IsReferenceMatchedTest(bool accountSpectrum, bool isUseTime, bool isUseCcs, bool[] expected) {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = isUseTime,
                IsUseCcsForAnnotationFiltering = isUseCcs,
            };
            var evaluator = CreateEvaluator(accountSpectrum, parameter);
            var results = CreateResults();

            var actuals = results.Select(result => evaluator.IsReferenceMatched(result)).ToList();
            actuals.ForEach(Console.WriteLine);

            CollectionAssert.AreEqual(expected, actuals);
        }

        [TestMethod()]
        [DataRow(false, false, false, new[] { 8, 9, 10, 11, 12, 13, 14, 15, })]
        [DataRow(false, false, true,  new[] {       10, 11,         14, 15, })]
        [DataRow(false, true,  false, new[] {               12, 13, 14, 15, })]
        [DataRow(false, true,  true,  new[] {                       14, 15, })]
        [DataRow(true,  false, false, new[] {    9,     11,     13,     15, })]
        [DataRow(true,  false, true,  new[] {           11,             15, })]
        [DataRow(true,  true,  false, new[] {                   13,     15, })]
        [DataRow(true,  true,  true,  new[] {                           15, })]
        public void SelectReferenceMatchResultsTest(bool accountSpectrum, bool isUseTime, bool isUseCcs, int[] expected) {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = isUseTime,
                IsUseCcsForAnnotationFiltering = isUseCcs,
            };
            var evaluator = CreateEvaluator(accountSpectrum, parameter);
            var results = CreateResults();

            var actuals = evaluator.SelectReferenceMatchResults(results);

            var expectedResult = expected.Select(e => results[e]).ToList();
            CollectionAssert.AreEquivalent(expectedResult, actuals);
        }

        [TestMethod()]
        [DataRow(false)]
        [DataRow(true)]
        public void SelectTopHitTest(bool accountSpectrum) {
            var parameter = new MsRefSearchParameterBase();
            var annotator = CreateEvaluator(accountSpectrum, parameter);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { TotalScore = 0.5f },
                new MsScanMatchResult { TotalScore = 0.3f },
                new MsScanMatchResult { TotalScore = 0.8f },
                new MsScanMatchResult { TotalScore = 0.1f },
                new MsScanMatchResult { TotalScore = 0.5f },
                new MsScanMatchResult { TotalScore = 0.4f },
            };

            var result = annotator.SelectTopHit(results);
            Assert.AreEqual(results[2], result);
        }

        private static MsScanMatchResultEvaluator CreateEvaluator(bool accountSpectrum, MsRefSearchParameterBase searchParameter) {
            return accountSpectrum ? MsScanMatchResultEvaluator.CreateEvaluatorWithSpectrum(searchParameter) : MsScanMatchResultEvaluator.CreateEvaluator(searchParameter);
        }

        private static List<MsScanMatchResult> CreateResults() {
            return new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsRtMatch = false, IsCcsMatch = false, IsSpectrumMatch = false, },
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsRtMatch = false, IsCcsMatch = false, IsSpectrumMatch = true, },
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsRtMatch = false, IsCcsMatch = true, IsSpectrumMatch = false, },
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsRtMatch = false, IsCcsMatch = true, IsSpectrumMatch = true, },
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsRtMatch = true, IsCcsMatch = false, IsSpectrumMatch = false, },
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsRtMatch = true, IsCcsMatch = false, IsSpectrumMatch = true, },
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsRtMatch = true, IsCcsMatch = true, IsSpectrumMatch = false, },
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsRtMatch = true, IsCcsMatch = true, IsSpectrumMatch = true, },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsRtMatch = false, IsCcsMatch = false, IsSpectrumMatch = false, },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsRtMatch = false, IsCcsMatch = false, IsSpectrumMatch = true, },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsRtMatch = false, IsCcsMatch = true, IsSpectrumMatch = false, },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsRtMatch = false, IsCcsMatch = true, IsSpectrumMatch = true, },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsRtMatch = true, IsCcsMatch = false, IsSpectrumMatch = false, },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsRtMatch = true, IsCcsMatch = false, IsSpectrumMatch = true, },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsRtMatch = true, IsCcsMatch = true, IsSpectrumMatch = false, },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsRtMatch = true, IsCcsMatch = true, IsSpectrumMatch = true, },
            };
        }
    }
}