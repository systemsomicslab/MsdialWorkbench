using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class ImmsMspAnnotatorTests
    {
        [TestMethod()]
        public void ImmsMspAnnotatorTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, CollisionCrossSection = 96 },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, CollisionCrossSection = 104 },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, CollisionCrossSection = 106 },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseCcsForAnnotationFiltering = true,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var result = annotator.Annotate(BuildQuery(target, parameter, annotator));

            Assert.AreEqual(db[1].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void AnnotateTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, CollisionCrossSection = 96 },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, CollisionCrossSection = 104 },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, CollisionCrossSection = 106 },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseCcsForAnnotationFiltering = true,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var result = annotator.Annotate(BuildQuery(target, parameter, annotator));

            Assert.AreEqual(db[1].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void AnnotateCcsNotUsedTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, CollisionCrossSection = 96 },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100.001, CollisionCrossSection = 104 },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, CollisionCrossSection = 106 },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseCcsForAnnotationFiltering = false,
                IsUseCcsForAnnotationScoring = false,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var result = annotator.Annotate(BuildQuery(target, parameter, annotator));

            Assert.AreEqual(db[4].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void FindCandidatesTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, CollisionCrossSection = 96 },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, CollisionCrossSection = 104 },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, CollisionCrossSection = 106 },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseCcsForAnnotationFiltering = true,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var results = annotator.FindCandidates(BuildQuery(target, parameter, annotator));
            var expected = new[]
            {
                db[0].InChIKey, db[1].InChIKey, db[2].InChIKey,
            };

            CollectionAssert.AreEquivalent(expected, results.Select(result => result.InChIKey).ToArray());
        }

        [TestMethod()]
        public void FindCandidatesCcsNotUsedTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, CollisionCrossSection = 96 },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, CollisionCrossSection = 104 },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, CollisionCrossSection = 106 },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseCcsForAnnotationFiltering = false,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var results = annotator.FindCandidates(BuildQuery(target, parameter, annotator));
            var expected = new[]
            {
                db[0].InChIKey, db[1].InChIKey, db[2].InChIKey, db[4].InChIKey,
            };

            CollectionAssert.AreEquivalent(expected, results.Select(result => result.InChIKey).ToArray());
        }

        [TestMethod()]
        public void CalculatedAnnotatedScoreTest() {
            var result = new MsScanMatchResult
            {
                AcurateMassSimilarity = 0.8f,
                CcsSimilarity = 0.6f,
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
                CcsTolerance = 5f,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
            var expected = new[]
            {
                result.AcurateMassSimilarity,
                result.CcsSimilarity,
                new[]
                {
                    result.WeightedDotProduct,
                    result.SimpleDotProduct,
                    result.ReverseDotProduct,
                }.Average(),
                result.MatchedPeaksPercentage,
            }.Average();
            var actual = annotator.CalculateAnnotatedScore(result);

            Assert.AreEqual(expected, (float)actual);
        }

        [TestMethod()]
        public void CalculatedAnnotatedScoreCcsNotUsedTest() {
            var result = new MsScanMatchResult
            {
                AcurateMassSimilarity = 0.8f,
                CcsSimilarity = 0.6f,
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
                CcsTolerance = 5f,
                IsUseCcsForAnnotationScoring = false,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
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

            Assert.AreEqual(expected, (float)actual);
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
                CcsTolerance = 5f,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
            var expected = new[]
            {
                result.AcurateMassSimilarity,
                result.CcsSimilarity,
            }.Average();
            var actual = annotator.CalculateSuggestedScore(result);

            Assert.AreEqual(expected, (float)actual);
        }

        [TestMethod()]
        public void CalculatedSuggestedScoreCcsNotUsedTest() {
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
                CcsTolerance = 5f,
                IsUseCcsForAnnotationScoring = false,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
            var expected = new[]
            {
                result.AcurateMassSimilarity,
            }.Average();
            var actual = annotator.CalculateSuggestedScore(result);

            Assert.AreEqual(expected, (float)actual);
        }

        [TestMethod()]
        public void ReferTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { ScanID = 0, Name = "A", InChIKey = "a", PrecursorMz = 99.991, CollisionCrossSection = 96 },
                    new MoleculeMsReference { ScanID = 1, Name = "B", InChIKey = "b", PrecursorMz = 100, CollisionCrossSection = 104 },
                    new MoleculeMsReference { ScanID = 2, Name = "C", InChIKey = "c", PrecursorMz = 100.009, CollisionCrossSection = 100 },
                    new MoleculeMsReference { ScanID = 3, Name = "D", InChIKey = "d", PrecursorMz = 99.9, CollisionCrossSection = 100 },
                    new MoleculeMsReference { ScanID = 4, Name = "E", InChIKey = "e", PrecursorMz = 100, CollisionCrossSection = 106 },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseCcsForAnnotationFiltering = true,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var result = annotator.Annotate(BuildQuery(target, parameter, annotator));

            var reference = annotator.Refer(result);

            Assert.AreEqual(db[1], reference);
        }

        [TestMethod()]
        public void SearchTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, CollisionCrossSection = 96 },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, CollisionCrossSection = 104 },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, CollisionCrossSection = 106 },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseCcsForAnnotationFiltering = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var results = annotator.Search(BuildQuery(target, parameter, annotator));

            CollectionAssert.AreEquivalent(new[] { db[0], db[1], db[2], }, results);
        }

        [TestMethod()]
        public void SearchCcsNotUsedTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, CollisionCrossSection = 96 },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, CollisionCrossSection = 104 },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, CollisionCrossSection = 100 },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, CollisionCrossSection = 106 },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseCcsForAnnotationFiltering = false,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var results = annotator.Search(BuildQuery(target, parameter, annotator));

            CollectionAssert.AreEquivalent(new[] { db[0], db[1], db[2], db[4], }, results);
        }

        [TestMethod()]
        public void CalculateScoreTest() {
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, CollisionCrossSection = 100,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
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
                CcsTolerance = 5f,
                ReverseDotProductCutOff = .7f,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature {
                PrecursorMz = 810.604, CollisionCrossSection = 102,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 86.094, Intensity = 5, },
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 524.367, Intensity = 1, },
                    new SpectrumPeak { Mass = 810.604, Intensity = 25, },
                }
            };

            var result = annotator.CalculateScore(BuildQuery(target, parameter, annotator), reference);

            Console.WriteLine($"AccurateSimilarity: {result.AcurateMassSimilarity}");
            Console.WriteLine($"CcsSimilarity: {result.CcsSimilarity}");
            Console.WriteLine($"WeightedDotProduct: {result.WeightedDotProduct}");
            Console.WriteLine($"SimpleDotProduct: {result.SimpleDotProduct}");
            Console.WriteLine($"ReverseDotProduct: {result.ReverseDotProduct}");
            Console.WriteLine($"MatchedPeaksPercentage: {result.MatchedPeaksPercentage}");
            Console.WriteLine($"MatchedPeaksCount: {result.MatchedPeaksCount}");
            Console.WriteLine($"TotalScore: {result.TotalScore}");
            Console.WriteLine($"IsPrecursorMzMatch: {result.IsPrecursorMzMatch}");
            Console.WriteLine($"IsCcsMatch: {result.IsCcsMatch}");
            Console.WriteLine($"IsSpectrumMatch: {result.IsSpectrumMatch}");
            Console.WriteLine($"IsLipidClassMatch: {result.IsLipidClassMatch}");
            Console.WriteLine($"IsLipidChainsMatch: {result.IsLipidChainsMatch}");
            Console.WriteLine($"IsLipidPositionMatch: {result.IsLipidPositionMatch}");
            Console.WriteLine($"IsOtherLipidMatch: {result.IsOtherLipidMatch}");

            Assert.IsTrue(result.AcurateMassSimilarity > 0);
            Assert.IsTrue(result.CcsSimilarity > 0);
            Assert.IsTrue(result.WeightedDotProduct > 0);
            Assert.IsTrue(result.SimpleDotProduct > 0);
            Assert.IsTrue(result.ReverseDotProduct > 0);
            Assert.AreEqual(3d/6 + 0.5, result.MatchedPeaksPercentage);
            Assert.AreEqual(6, result.MatchedPeaksCount);
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
                result.CcsSimilarity,
            }.Average();
            Assert.AreEqual(expected, result.TotalScore);
            Assert.IsTrue(result.IsPrecursorMzMatch);
            Assert.IsTrue(result.IsCcsMatch);
            Assert.IsTrue(result.IsSpectrumMatch);
            Assert.IsTrue(result.IsLipidClassMatch);
            Assert.IsFalse(result.IsLipidChainsMatch);
            Assert.IsFalse(result.IsLipidPositionMatch);
            Assert.IsFalse(result.IsOtherLipidMatch);
        }

        [TestMethod()]
        public void CalculateScoreCcsNotUsedTest() {
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, CollisionCrossSection = 100,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
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
                CcsTolerance = 5f,
                ReverseDotProductCutOff = .7f,
                IsUseCcsForAnnotationScoring = false,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature {
                PrecursorMz = 810.604, CollisionCrossSection = 102,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 86.094, Intensity = 5, },
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 524.367, Intensity = 1, },
                    new SpectrumPeak { Mass = 810.604, Intensity = 25, },
                }
            };

            var result = annotator.CalculateScore(BuildQuery(target, parameter, annotator), reference);

            Console.WriteLine($"AccurateSimilarity: {result.AcurateMassSimilarity}");
            Console.WriteLine($"CcsSimilarity: {result.CcsSimilarity}");
            Console.WriteLine($"WeightedDotProduct: {result.WeightedDotProduct}");
            Console.WriteLine($"SimpleDotProduct: {result.SimpleDotProduct}");
            Console.WriteLine($"ReverseDotProduct: {result.ReverseDotProduct}");
            Console.WriteLine($"MatchedPeaksPercentage: {result.MatchedPeaksPercentage}");
            Console.WriteLine($"MatchedPeaksCount: {result.MatchedPeaksCount}");
            Console.WriteLine($"TotalScore: {result.TotalScore}");
            Console.WriteLine($"IsPrecursorMzMatch: {result.IsPrecursorMzMatch}");
            Console.WriteLine($"IsCcsMatch: {result.IsCcsMatch}");
            Console.WriteLine($"IsSpectrumMatch: {result.IsSpectrumMatch}");
            Console.WriteLine($"IsLipidClassMatch: {result.IsLipidClassMatch}");
            Console.WriteLine($"IsLipidChainsMatch: {result.IsLipidChainsMatch}");
            Console.WriteLine($"IsLipidPositionMatch: {result.IsLipidPositionMatch}");
            Console.WriteLine($"IsOtherLipidMatch: {result.IsOtherLipidMatch}");

            Assert.IsTrue(result.AcurateMassSimilarity > 0);
            Assert.IsTrue(result.CcsSimilarity == 0);
            Assert.IsTrue(result.WeightedDotProduct > 0);
            Assert.IsTrue(result.SimpleDotProduct > 0);
            Assert.IsTrue(result.ReverseDotProduct > 0);
            Assert.IsTrue(result.MatchedPeaksPercentage > 0);
            Assert.IsTrue(result.MatchedPeaksCount > 0);
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
            Assert.AreEqual((float)expected, result.TotalScore);
            Assert.IsTrue(result.IsPrecursorMzMatch);
            Assert.IsTrue(result.IsCcsMatch);
            Assert.IsTrue(result.IsSpectrumMatch);
            Assert.IsTrue(result.IsLipidClassMatch);
            Assert.IsFalse(result.IsLipidChainsMatch);
            Assert.IsFalse(result.IsLipidPositionMatch);
            Assert.IsFalse(result.IsOtherLipidMatch);
        }

        [TestMethod()]
        public void SelectTopHitTest() {
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), new MsRefSearchParameterBase(), Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
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

        [TestMethod()]
        public void FilterByThresholdTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseCcsForAnnotationFiltering = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = true, IsCcsMatch = true, },
            };

            var actuals = annotator.FilterByThreshold(results);
            CollectionAssert.AreEquivalent(new[] { results[2], results[3], results[4], results[5], results[6], results[7], }, actuals);
        }

        [TestMethod()]
        public void FilterByThresholdCcsNotUsedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseCcsForAnnotationFiltering = false,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = true, IsCcsMatch = true, },
            };

            var actuals = annotator.FilterByThreshold(results);
            CollectionAssert.AreEquivalent(new[] { results[2], results[3], results[4], results[5], results[6], results[7] }, actuals);
        }

        [TestMethod()]
        public void SelectReferenceMatchResultsTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseCcsForAnnotationFiltering = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = true, },
            };

            var actuals = annotator.SelectReferenceMatchResults(results);
            CollectionAssert.AreEquivalent(new[] { results[4], results[5], }, actuals);
        }

        [TestMethod()]
        public void SelectReferenceMatchResultsCcsNotUsedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseCcsForAnnotationFiltering = false,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = true, },
            };

            var actuals = annotator.SelectReferenceMatchResults(results);
            CollectionAssert.AreEquivalent(new[] { results[4], results[5] }, actuals);
        }

        [TestMethod()]
        public void IsReferenceMatchTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseCcsForAnnotationFiltering = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = true, },
            };

            var actuals = results.Select(result => annotator.IsReferenceMatched(result)).ToList();
            CollectionAssert.AreEqual(
                new[] {
                    false, false,
                    false, false,
                    true,  true,
                },
                actuals);
        }

        [TestMethod()]
        public void IsReferenceMatchCcsNotUsedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseCcsForAnnotationFiltering = false,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = true, },
            };

            var actuals = results.Select(result => annotator.IsReferenceMatched(result)).ToList();
            CollectionAssert.AreEqual(
                new[] {
                    false, false,
                    false, false,
                    true,  true,
                },
                actuals);
        }

        [TestMethod()]
        public void IsSuggestedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseCcsForAnnotationFiltering = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = true, },
            };

            var actuals = results.Select(result => annotator.IsAnnotationSuggested(result)).ToList();
            CollectionAssert.AreEqual(
                new[] {
                    false, false,
                    true,  true,
                    false, false,
                },
                actuals);
        }

        [TestMethod()]
        public void IsSuggestedCcsNotUsedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseCcsForAnnotationFiltering = false,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsCcsMatch = true, },
            };

            var actuals = results.Select(result => annotator.IsAnnotationSuggested(result)).ToList();
            CollectionAssert.AreEqual(
                new[] {
                    false, false,
                    true, true,
                    false, false,
                },
                actuals);
        }

        private AnnotationQuery BuildQuery(ChromatogramPeakFeature target, MsRefSearchParameterBase parameter, ImmsMspAnnotator annotator) {
            return new AnnotationQuery(target, target, null, null, parameter ?? new MsRefSearchParameterBase(), annotator, ignoreIsotopicPeak: false);
        }
    }
}