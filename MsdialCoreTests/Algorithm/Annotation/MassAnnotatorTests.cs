using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class MassAnnotatorTests
    {
        [TestMethod()]
        public void MassAnnotatorTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { ScanID = 0, Name = "A", InChIKey = "a", PrecursorMz = 99.990, },
                    new MoleculeMsReference { ScanID = 1, Name = "B", InChIKey = "b", PrecursorMz = 100, },
                    new MoleculeMsReference { ScanID = 2, Name = "C", InChIKey = "c", PrecursorMz = 100.019, },
                    new MoleculeMsReference { ScanID = 3, Name = "D", InChIKey = "d", PrecursorMz = 101, },
                    new MoleculeMsReference { ScanID = 4, Name = "E", InChIKey = "e", PrecursorMz = 102, },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                TotalScoreCutoff = 0,
            };
            var annotator = new MassAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100.009 };
            var result = annotator.Annotate(BuildQuery(target, annotator));

            Assert.AreEqual(db[1].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void AnnotateTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference {ScanID = 0,  Name = "A", InChIKey = "a", PrecursorMz = 99.990, },
                    new MoleculeMsReference {ScanID = 1,  Name = "B", InChIKey = "b", PrecursorMz = 100, },
                    new MoleculeMsReference {ScanID = 2,  Name = "C", InChIKey = "c", PrecursorMz = 100.019, },
                    new MoleculeMsReference {ScanID = 3,  Name = "D", InChIKey = "d", PrecursorMz = 101, },
                    new MoleculeMsReference {ScanID = 4,  Name = "E", InChIKey = "e", PrecursorMz = 102, },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                TotalScoreCutoff = 0,
            };
            var annotator = new MassAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100.009 };
            var result = annotator.Annotate(BuildQuery(target, annotator));

            Assert.AreEqual(db[1].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void FindCandidatesTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference {ScanID = 0,  Name = "A", InChIKey = "a", PrecursorMz = 99.991, },
                    new MoleculeMsReference {ScanID = 1,  Name = "B", InChIKey = "b", PrecursorMz = 100, },
                    new MoleculeMsReference {ScanID = 2,  Name = "C", InChIKey = "c", PrecursorMz = 100.009, },
                    new MoleculeMsReference {ScanID = 3,  Name = "D", InChIKey = "d", PrecursorMz = 101, },
                    new MoleculeMsReference {ScanID = 4,  Name = "E", InChIKey = "e", PrecursorMz = 102, },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                TotalScoreCutoff = 0,
            };
            var annotator = new MassAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100 };
            var results = annotator.FindCandidates(BuildQuery(target, annotator));
            var expected = new[]
            {
                db[0].Name, db[1].Name, db[2].Name,
            };

            CollectionAssert.AreEquivalent(expected, results.Select(result => result.Name).ToArray());
        }

        [TestMethod()]
        public void CalculateScoreTest() {
            var reference = new MoleculeMsReference
            {
                Name = "PC 18:0_20:4",
                CompoundClass = "PC",
                PrecursorMz = 810.601,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 184.073, Intensity = 100 },
                    new SpectrumPeak { Mass = 506.361, Intensity = 5 },
                    new SpectrumPeak { Mass = 524.372, Intensity = 5 },
                    new SpectrumPeak { Mass = 526.330, Intensity = 5 },
                    new SpectrumPeak { Mass = 544.340, Intensity = 5 },
                    new SpectrumPeak { Mass = 810.601, Intensity = 30 },
                }
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
            };
            var annotator = new MassAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);

            var target = new ChromatogramPeakFeature
            {
                PrecursorMz = 810.604,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 86.094, Intensity = 5, },
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 524.367, Intensity = 1, },
                    new SpectrumPeak { Mass = 810.604, Intensity = 25, },
                }
            };

            var result = annotator.CalculateScore(BuildQuery(target, annotator), reference);

            Console.WriteLine($"AccurateSimilarity: {result.AcurateMassSimilarity}");
            Console.WriteLine($"WeightedDotProduct: {result.WeightedDotProduct}");
            Console.WriteLine($"SimpleDotProduct: {result.SimpleDotProduct}");
            Console.WriteLine($"ReverseDotProduct: {result.ReverseDotProduct}");
            Console.WriteLine($"MatchedPeaksPercentage: {result.MatchedPeaksPercentage}");
            Console.WriteLine($"MatchedPeaksCount: {result.MatchedPeaksCount}");
            Console.WriteLine($"TotalScore: {result.TotalScore}");

            Assert.IsTrue(result.AcurateMassSimilarity > 0);
            Assert.IsTrue(result.WeightedDotProduct > 0);
            Assert.IsTrue(result.SimpleDotProduct > 0);
            Assert.IsTrue(result.ReverseDotProduct > 0);
            Assert.IsTrue(result.MatchedPeaksPercentage > 0);
            Assert.IsTrue(result.MatchedPeaksCount > 0);

            Assert.AreEqual((float)annotator.CalculateAnnotatedScore(result), result.TotalScore);
        }

        [TestMethod()]
        public void CalculatedAnnotatedScoreTest() {
            var result = new MsScanMatchResult
            {
                AcurateMassSimilarity = 0.8f,
                WeightedDotProduct = 0.7f,
                SimpleDotProduct = 0.6f,
                ReverseDotProduct = 0.8f,
                MatchedPeaksPercentage = 0.75f,
                IsotopeSimilarity = -1,
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
            };
            var annotator = new MassAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);
            var expected = new[]
            {
                result.AcurateMassSimilarity,
                new[]
                {
                    result.WeightedDotProduct,
                    result.SimpleDotProduct,
                    result.ReverseDotProduct,
                }.Average(),
                result.MatchedPeaksPercentage,
            }.Average();
            var actual = annotator.CalculateAnnotatedScore(result);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void CalculatedSuggestedScoreTest() {
            var result = new MsScanMatchResult
            {
                AcurateMassSimilarity = 0.8f,
                WeightedDotProduct = 0.7f,
                SimpleDotProduct = 0.6f,
                ReverseDotProduct = 0.8f,
                MatchedPeaksPercentage = 0.75f,
                IsotopeSimilarity = -1,
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
            };
            var annotator = new MassAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);
            var expected = new[]
            {
                result.AcurateMassSimilarity,
            }.Average();
            var actual = annotator.CalculateSuggestedScore(result);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void ReferTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { ScanID = 0, Name = "A", InChIKey = "a", PrecursorMz = 99.990, },
                    new MoleculeMsReference { ScanID = 1, Name = "B", InChIKey = "b", PrecursorMz = 100, },
                    new MoleculeMsReference { ScanID = 2, Name = "C", InChIKey = "c", PrecursorMz = 100.019, },
                    new MoleculeMsReference { ScanID = 3, Name = "D", InChIKey = "d", PrecursorMz = 101, },
                    new MoleculeMsReference { ScanID = 4, Name = "E", InChIKey = "e", PrecursorMz = 102, },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                TotalScoreCutoff = 0,
            };
            var annotator = new MassAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100.009 };
            var result = annotator.Annotate(BuildQuery(target, annotator));

            var reference = annotator.Refer(result);

            Assert.AreEqual(db[1], reference);
        }

        [TestMethod()]
        public void SearchTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference {ScanID = 0,  Name = "A", InChIKey = "a", PrecursorMz = 99.990, },
                    new MoleculeMsReference {ScanID = 1,  Name = "B", InChIKey = "b", PrecursorMz = 100, },
                    new MoleculeMsReference {ScanID = 2,  Name = "C", InChIKey = "c", PrecursorMz = 100.017, },
                    new MoleculeMsReference {ScanID = 3,  Name = "D", InChIKey = "d", PrecursorMz = 101, },
                    new MoleculeMsReference {ScanID = 4,  Name = "E", InChIKey = "e", PrecursorMz = 102, },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
            };
            var annotator = new MassAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100.008 };
            var results = annotator.Search(BuildQuery(target, annotator));

            CollectionAssert.AreEqual(db.GetRange(1, 2), results);
        }

        [TestMethod()]
        public void ValidateTest() {
            var reference = new MoleculeMsReference
            {
                Name = "PC 18:0_20:4",
                CompoundClass = "PC",
                PrecursorMz = 810.601,
                AdductType = CompMs.Common.Parser.AdductIonParser.GetAdductIonBean("[M+H]+"),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 184.073, Intensity = 100 },
                    new SpectrumPeak { Mass = 506.361, Intensity = 5 },
                    new SpectrumPeak { Mass = 524.372, Intensity = 5 },
                    new SpectrumPeak { Mass = 526.330, Intensity = 5 },
                    new SpectrumPeak { Mass = 544.340, Intensity = 5 },
                    new SpectrumPeak { Mass = 810.601, Intensity = 30 },
                }
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                TotalScoreCutoff = 0.7f,
            };
            var annotator = new MassAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);

            var target = new ChromatogramPeakFeature
            {
                PrecursorMz = 810.604,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 86.094, Intensity = 5, },
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 524.367, Intensity = 1, },
                    new SpectrumPeak { Mass = 810.604, Intensity = 25, },
                }
            };

            var result = annotator.CalculateScore(BuildQuery(target, annotator), reference);
            annotator.Validate(result, BuildQuery(target, annotator), reference);

            Console.WriteLine($"IsPrecursorMzMatch: {result.IsPrecursorMzMatch}");
            Console.WriteLine($"IsSpectrumMatch: {result.IsSpectrumMatch}");
            Console.WriteLine($"IsLipidClassMatch: {result.IsLipidClassMatch}");
            Console.WriteLine($"IsLipidChainsMatch: {result.IsLipidChainsMatch}");
            Console.WriteLine($"IsLipidPositionMatch: {result.IsLipidPositionMatch}");
            Console.WriteLine($"IsOtherLipidMatch: {result.IsOtherLipidMatch}");

            Assert.IsTrue(result.IsPrecursorMzMatch);
            Assert.IsTrue(result.IsSpectrumMatch);
            Assert.IsTrue(result.IsLipidClassMatch);
            Assert.IsFalse(result.IsLipidChainsMatch);
            Assert.IsFalse(result.IsLipidPositionMatch);
            Assert.IsFalse(result.IsOtherLipidMatch);
        }

        [TestMethod()]
        public void SelectTopHitTest() {
            var parameter = new MsRefSearchParameterBase();
            var annotator = new MassAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { TotalScore = 0.5f },
                new MsScanMatchResult { TotalScore = 0.3f },
                new MsScanMatchResult { TotalScore = 0.8f },
                new MsScanMatchResult { TotalScore = 0.1f },
                new MsScanMatchResult { TotalScore = 0.5f },
                new MsScanMatchResult { TotalScore = 0.4f },
            };

            var result = annotator.SelectTopHit(results, parameter);
            Assert.AreEqual(results[2], result);
        }

        [TestMethod()]
        public void FilterByThresholdTest() {
            var parameter = new MsRefSearchParameterBase { };
            var annotator = new MassAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsSpectrumMatch = false },
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsSpectrumMatch = true },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsSpectrumMatch = false },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsSpectrumMatch = true },
            };

            var actuals = annotator.FilterByThreshold(results, parameter);
            CollectionAssert.AreEquivalent(new[] { results[2], results[3], }, actuals);
        }

        [TestMethod()]
        public void SelectReferenceMatchResultsTest() {
            var parameter = new MsRefSearchParameterBase { };
            var annotator = new MassAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsSpectrumMatch = false },
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsSpectrumMatch = true },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsSpectrumMatch = false },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsSpectrumMatch = true },
            };

            var actuals = annotator.SelectReferenceMatchResults(results, parameter);
            CollectionAssert.AreEquivalent(new[] { results[3], }, actuals);
        }

        [TestMethod()]
        public void IsReferenceMatchTest() {
            var parameter = new MsRefSearchParameterBase { };
            var annotator = new MassAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsSpectrumMatch = false },
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsSpectrumMatch = true },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsSpectrumMatch = false },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsSpectrumMatch = true },
            };

            var actuals = results.Select(result => annotator.IsReferenceMatched(result, parameter)).ToArray();
            CollectionAssert.AreEqual(new[] { false, false, false, true, }, actuals);
        }

        [TestMethod()]
        public void IsSuggestedTest() {
            var parameter = new MsRefSearchParameterBase { };
            var annotator = new MassAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsSpectrumMatch = false },
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsSpectrumMatch = true },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsSpectrumMatch = false },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsSpectrumMatch = true },
            };

            var actuals = results.Select(result => annotator.IsAnnotationSuggested(result, parameter)).ToArray();
            CollectionAssert.AreEqual(new[] { false, false, true, false, }, actuals);
        }

        private AnnotationQuery BuildQuery(ChromatogramPeakFeature target, MassAnnotator annotator) {
            return new AnnotationQuery(target, target, null, null, new MsRefSearchParameterBase(), annotator);
        }
    }
}