using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using System;
using System.Collections.Generic;
using CompMs.Common.Components;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using System.Linq;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;

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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var result = annotator.Annotate(new AnnotationQuery(target, target, null, null));

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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var result = annotator.Annotate(new AnnotationQuery(target, target, null, null));

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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var result = annotator.Annotate(new AnnotationQuery(target, target, null, null));

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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var results = annotator.FindCandidates(new AnnotationQuery(target, target, null, null));
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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var results = annotator.FindCandidates(new AnnotationQuery(target, target, null, null));
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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");
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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");
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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");
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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");
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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var result = annotator.Annotate(new AnnotationQuery(target, target, null, null));

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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var results = annotator.Search(new AnnotationQuery(target, target, null, null));

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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, CollisionCrossSection = 100 };
            var results = annotator.Search(new AnnotationQuery(target, target, null, null));

            CollectionAssert.AreEquivalent(new[] { db[0], db[1], db[2], db[4], }, results);
        }

        [TestMethod()]
        public void CalculateScoreTest() {
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, CollisionCrossSection = 100,
                AdductType = new Common.DataObj.Property.AdductIon { AdductIonName = "[M+H]+" },
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
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");

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

            var result = annotator.CalculateScore(new AnnotationQuery(target, target, null, null), reference);

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
        public void CalculateScoreCcsMatchTest() {
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, CollisionCrossSection = 100,
                AdductType = new Common.DataObj.Property.AdductIon { AdductIonName = "[M+H]+" },
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
                CcsTolerance = 12f,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");

            var target = new ChromatogramPeakFeature {
                PrecursorMz = 810.604, CollisionCrossSection = 111,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 86.094, Intensity = 5, },
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 524.367, Intensity = 1, },
                    new SpectrumPeak { Mass = 810.604, Intensity = 25, },
                }
            };

            var result = annotator.CalculateScore(new AnnotationQuery(target, target, null, null), reference);

            Console.WriteLine($"CcsSimilarity: {result.CcsSimilarity}");
            Console.WriteLine($"IsCcsMatch: {result.IsCcsMatch}");

            Assert.IsTrue(result.CcsSimilarity > 0);
            Assert.IsFalse(result.IsCcsMatch);
        }

        [TestMethod()]
        public void CalculateScoreCcsNotUsedTest() {
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, CollisionCrossSection = 100,
                AdductType = new Common.DataObj.Property.AdductIon { AdductIonName = "[M+H]+" },
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
                IsUseCcsForAnnotationScoring = false,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");

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

            var result = annotator.CalculateScore(new AnnotationQuery(target, target, null, null), reference);

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
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), new MsRefSearchParameterBase(), Common.Enum.TargetOmics.Lipidomics, "MspDB");
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
                WeightedDotProductCutOff = 0.5f, SimpleDotProductCutOff = 0.5f, ReverseDotProductCutOff = 0.5f,
                MatchedPeaksPercentageCutOff = 0.5f, MinimumSpectrumMatch = 3,
                TotalScoreCutoff = 0.7f,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");
            var results = Enumerable.Repeat(0, 11).Select(_ => MatchResultBuilder()).ToList();
            results[1].IsPrecursorMzMatch = false;
            results[1].IsSpectrumMatch = false;
            results[2].IsSpectrumMatch = false;
            results[3].IsPrecursorMzMatch = false;
            results[4].WeightedDotProduct = 0.4f;
            results[5].SimpleDotProduct = 0.4f;
            results[6].ReverseDotProduct = 0.4f;
            results[7].MatchedPeaksCount = 2;
            results[8].MatchedPeaksPercentage = 0.4f;
            results[8].AcurateMassSimilarity = 1f;
            results[8].CcsSimilarity = 1f;
            results[9].AcurateMassSimilarity = 0.1f;
            results[10].CcsSimilarity = 0.1f;

            var actuals = annotator.FilterByThreshold(results);
            foreach (var actual in actuals) {
                Console.WriteLine(
                    $"IsPrecursorMzMatch: {actual.IsPrecursorMzMatch}" +
                    $"\tIsSpectrumMatch: {actual.IsSpectrumMatch}" +
                    $"\tAcurateMassSimilarity: {actual.AcurateMassSimilarity}" +
                    $"\tCcsSimilarity: {actual.CcsSimilarity}" +
                    $"\tWeightedDotProduct: {actual.WeightedDotProduct}" +
                    $"\tSimpleDotProduct: {actual.SimpleDotProduct}" +
                    $"\tReverseDotProduct: {actual.ReverseDotProduct}" +
                    $"\tMatchedPeaksCount: {actual.MatchedPeaksCount}" +
                    $"\tMatchedPeaksPercentage: {actual.MatchedPeaksPercentage}");
            }
            CollectionAssert.AreEquivalent(new[] {
                results[0], results[2], results[4], results[5],
                results[6], results[7], results[8],
            }, actuals);
        }

        [TestMethod()]
        public void SelectReferenceMatchResultsTest() {
            var parameter = new MsRefSearchParameterBase
            {
                WeightedDotProductCutOff = 0.5f, SimpleDotProductCutOff = 0.5f, ReverseDotProductCutOff = 0.5f,
                MatchedPeaksPercentageCutOff = 0.5f, MinimumSpectrumMatch = 3,
                TotalScoreCutoff = 0.7f, IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new ImmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB");
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    AcurateMassSimilarity = 0.8f, CcsSimilarity = 0.8f, IsotopeSimilarity = -1,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f, },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = false, IsSpectrumMatch = false,
                    AcurateMassSimilarity = 0.8f, CcsSimilarity = 0.8f, IsotopeSimilarity = -1,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f, },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = false,
                    AcurateMassSimilarity = 0.8f, CcsSimilarity = 0.8f, IsotopeSimilarity = -1,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f, },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = false, IsSpectrumMatch = true,
                    AcurateMassSimilarity = 0.8f, CcsSimilarity = 0.8f, IsotopeSimilarity = -1,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f, },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    AcurateMassSimilarity = 0.8f, CcsSimilarity = 0.8f, IsotopeSimilarity = -1,
                    WeightedDotProduct = 0.4f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f, },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    AcurateMassSimilarity = 0.8f, CcsSimilarity = 0.8f, IsotopeSimilarity = -1,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.4f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f, },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    AcurateMassSimilarity = 0.8f, CcsSimilarity = 0.8f, IsotopeSimilarity = -1,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.4f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f, },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    AcurateMassSimilarity = 0.8f, CcsSimilarity = 0.8f, IsotopeSimilarity = -1,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 2, MatchedPeaksPercentage = 0.8f, },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    AcurateMassSimilarity = 0.8f, CcsSimilarity = 0.8f, IsotopeSimilarity = -1,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.4f, },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    AcurateMassSimilarity = 0.8f, CcsSimilarity = 0.1f, IsotopeSimilarity = -1,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f, },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    AcurateMassSimilarity = 0.1f, CcsSimilarity = 0.8f, IsotopeSimilarity = -1,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f, },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    AcurateMassSimilarity = 0.8f, CcsSimilarity = 0.8f, IsotopeSimilarity = -1,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f, },
            };

            var actuals = annotator.SelectReferenceMatchResults(results);
            foreach (var actual in actuals) {
                Console.WriteLine($"IsPrecursorMzMatch: {actual.IsPrecursorMzMatch}");
                Console.WriteLine($"IsSpectrumMatch: {actual.IsSpectrumMatch}");
                Console.WriteLine($"AcurateMassSimilarity: {actual.AcurateMassSimilarity}");
                Console.WriteLine($"CcsSimilarity: {actual.CcsSimilarity}");
                Console.WriteLine($"WeightedDotProduct: {actual.WeightedDotProduct}");
                Console.WriteLine($"SimpleDotProduct: {actual.SimpleDotProduct}");
                Console.WriteLine($"ReverseDotProduct: {actual.ReverseDotProduct}");
                Console.WriteLine($"MatchedPeaksCount: {actual.MatchedPeaksCount}");
                Console.WriteLine($"MatchedPeaksPercentage: {actual.MatchedPeaksPercentage}");
            }
            CollectionAssert.AreEquivalent(new[] { results[0], results[11], }, actuals);
        }

        private static MsScanMatchResult MatchResultBuilder() {
            return new MsScanMatchResult
            {
                IsPrecursorMzMatch = true,
                IsSpectrumMatch = true,
                AcurateMassSimilarity = 0.8f,
                CcsSimilarity = 0.8f,
                IsotopeSimilarity = -1,
                WeightedDotProduct = 0.8f,
                SimpleDotProduct = 0.8f,
                ReverseDotProduct = 0.8f,
                MatchedPeaksCount = 6,
                MatchedPeaksPercentage = 0.8f,
            };
        }
    }
}