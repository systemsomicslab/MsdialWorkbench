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

namespace CompMs.MsdialLcImMsApi.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class LcimmsMspAnnotatorTests
    {
        [TestMethod()]
        public void ImmsMspAnnotatorTest() {
            var db = new List<MoleculeMsReference>
            {
                BuildRef(0, "A", "a", 99.991, 1.6, 96),
                BuildRef(0, "B", "b", 100, 2, 104),
                BuildRef(0, "C", "c", 100.009, 2.4, 100),
                BuildRef(0, "D", "d", 99.9, 2, 100),
                BuildRef(0, "E", "e", 100, 2.51, 106),
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseCcsForAnnotationFiltering = true,
                IsUseCcsForAnnotationScoring = true,
                IsUseTimeForAnnotationFiltering = true,
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = BuildPeak(100, 2, 100);
            var result = annotator.Annotate(BuildQuery(target, parameter, annotator));

            Assert.AreEqual(db[1].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void AnnotateTest() {
            var db = new List<MoleculeMsReference>
            {
                BuildRef(0, "A", "a", 99.991, 1.6, 96),
                BuildRef(0, "B", "b", 100, 2, 100),
                BuildRef(0, "C", "c", 100.009, 2.4, 104),
                BuildRef(0, "D", "d", 99.9, 2, 100),
                BuildRef(0, "E", "e", 100, 2.51, 106),
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
                IsUseTimeForAnnotationScoring = true,
                IsUseCcsForAnnotationFiltering = true,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = BuildPeak(100, 2, 100);
            var result = annotator.Annotate(BuildQuery(target, parameter, annotator));

            Assert.AreEqual(db[1].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void AnnotateCcsNotUsedTest() {
            var db = new List<MoleculeMsReference>
            {
                BuildRef(0, "A", "a", 99.991, 1.6, 96),
                BuildRef(0, "B", "b", 100.001, 2, 104),
                BuildRef(0, "C", "c", 100.009, 2.4, 100),
                BuildRef(0, "D", "d", 99.9, 2, 100),
                BuildRef(0, "E", "e", 100, 2.51, 106),
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = false,
                IsUseTimeForAnnotationScoring = false,
                IsUseCcsForAnnotationFiltering = false,
                IsUseCcsForAnnotationScoring = false,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = BuildPeak(100, 2, 100);
            var result = annotator.Annotate(BuildQuery(target, parameter, annotator));

            Assert.AreEqual(db[4].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void FindCandidatesTest() {
            var db = new List<MoleculeMsReference>
            {
                BuildRef(0, "A", "a", 99.991, 1.6, 96),
                BuildRef(0, "B", "b", 100, 2, 104),
                BuildRef(0, "C", "c", 100.009, 2.4, 100),
                BuildRef(0, "D", "d", 99.9, 2, 100),
                BuildRef(0, "E", "e", 100, 2.51, 106),
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
                IsUseTimeForAnnotationScoring = true,
                IsUseCcsForAnnotationFiltering = true,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = BuildPeak(100, 2, 100);
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
                BuildRef(0, "A", "a", 99.991, 1.6, 96),
                BuildRef(0, "B", "b", 100, 2, 104),
                BuildRef(0, "C", "c", 100.009, 2.4, 100),
                BuildRef(0, "D", "d", 99.9, 2, 100),
                BuildRef(0, "E", "e", 100, 2.51, 106),
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = false,
                IsUseCcsForAnnotationFiltering = false,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = BuildPeak(100, 2, 100);
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
                RtSimilarity = 0.6f,
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
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                IsUseTimeForAnnotationScoring = true,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var expected = new[]
            {
                result.AcurateMassSimilarity,
                result.RtSimilarity,
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
                RtSimilarity = 0.6f,
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
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                IsUseTimeForAnnotationScoring = false,
                IsUseCcsForAnnotationScoring = false,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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
                RtSimilarity = 0.6f,
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
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                IsUseTimeForAnnotationScoring = true,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var expected = new[]
            {
                result.AcurateMassSimilarity,
                result.RtSimilarity,
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
                RtSimilarity = 0.6f,
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
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                IsUseTimeForAnnotationScoring = false,
                IsUseCcsForAnnotationScoring = false,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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
                BuildRef(0, "A", "a", 99.991, 1.6, 96),
                BuildRef(1, "B", "b", 100, 2, 104),
                BuildRef(2, "C", "c", 100.009, 2.4, 100),
                BuildRef(3, "D", "d", 99.9, 2, 100),
                BuildRef(4, "E", "e", 100, 2.51, 106),
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
                IsUseTimeForAnnotationScoring = true,
                IsUseCcsForAnnotationFiltering = true,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = BuildPeak(100, 2, 100);
            var result = annotator.Annotate(BuildQuery(target, parameter, annotator));

            var reference = annotator.Refer(result);

            Assert.AreEqual(db[1], reference);
        }

        [TestMethod()]
        public void SearchTest() {
            var db = new List<MoleculeMsReference>
            {
                BuildRef(0, "A", "a", 99.991, 1.6, 96),
                BuildRef(1, "B", "b", 100, 2, 104),
                BuildRef(2, "C", "c", 100.009, 2.4, 100),
                BuildRef(3, "D", "d", 99.9, 2, 100),
                BuildRef(4, "E", "e", 100, 2.51, 106),
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
                IsUseCcsForAnnotationFiltering = true,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = BuildPeak(100, 2, 100);
            var results = annotator.Search(BuildQuery(target, parameter, annotator));

            CollectionAssert.AreEquivalent(new[] { db[0], db[1], db[2], }, results);
        }

        [TestMethod()]
        public void SearchCcsNotUsedTest() {
            var db = new List<MoleculeMsReference>
            {
                BuildRef(0, "A", "a", 99.991, 1.6, 96),
                BuildRef(1, "B", "b", 100, 2, 104),
                BuildRef(2, "C", "c", 100.009, 2.4, 100),
                BuildRef(3, "D", "d", 99.9, 2, 100),
                BuildRef(4, "E", "e", 100, 2.51, 106),
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = false,
                IsUseCcsForAnnotationFiltering = false,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(db, "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = BuildPeak(100, 2, 100);
            var results = annotator.Search(BuildQuery(target, parameter, annotator));

            CollectionAssert.AreEquivalent(new[] { db[0], db[1], db[2], db[4], }, results);
        }

        [TestMethod()]
        public void CalculateScoreTest() {
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601,
                ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min),
                CollisionCrossSection = 100,
                AdductType = Common.DataObj.Property.AdductIon.GetAdductIon("[M+H]+"),
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
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                ReverseDotProductCutOff = .7f,
                IsUseTimeForAnnotationScoring = true,
                IsUseCcsForAnnotationScoring = true,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);

            var target = BuildPeak(810.604, 2, 102);
            target.Spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak { Mass = 86.094, Intensity = 5, },
                new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                new SpectrumPeak { Mass = 524.367, Intensity = 1, },
                new SpectrumPeak { Mass = 810.604, Intensity = 25, },
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
            Console.WriteLine($"IsRtMatch: {result.IsRtMatch}");
            Console.WriteLine($"IsCcsMatch: {result.IsCcsMatch}");
            Console.WriteLine($"IsSpectrumMatch: {result.IsSpectrumMatch}");
            Console.WriteLine($"IsLipidClassMatch: {result.IsLipidClassMatch}");
            Console.WriteLine($"IsLipidChainsMatch: {result.IsLipidChainsMatch}");
            Console.WriteLine($"IsLipidPositionMatch: {result.IsLipidPositionMatch}");
            Console.WriteLine($"IsOtherLipidMatch: {result.IsOtherLipidMatch}");

            Assert.IsTrue(result.AcurateMassSimilarity > 0);
            Assert.IsTrue(result.RtSimilarity > 0);
            Assert.IsTrue(result.CcsSimilarity > 0);
            Assert.IsTrue(result.WeightedDotProduct > 0);
            Assert.IsTrue(result.SimpleDotProduct > 0);
            Assert.IsTrue(result.ReverseDotProduct > 0);
            Assert.AreEqual(3d/6, result.MatchedPeaksPercentage);
            Assert.AreEqual(6, result.MatchedPeaksCount);
            var expected = new[]
            {
                result.AcurateMassSimilarity,
                result.RtSimilarity,
                result.CcsSimilarity,
                new[]
                {
                    result.WeightedDotProduct,
                    result.SimpleDotProduct,
                    result.ReverseDotProduct,
                }.Average(),
                result.MatchedPeaksPercentage,
            }.Average();
            Assert.AreEqual(expected, result.TotalScore);
            Assert.IsTrue(result.IsPrecursorMzMatch);
            Assert.IsTrue(result.IsRtMatch);
            Assert.IsTrue(result.IsCcsMatch);
            Assert.IsTrue(result.IsSpectrumMatch);
            Assert.IsFalse(result.IsLipidClassMatch);
            Assert.IsFalse(result.IsLipidChainsMatch);
            Assert.IsFalse(result.IsLipidPositionMatch);
            Assert.IsFalse(result.IsOtherLipidMatch);
        }

        [TestMethod()]
        public void CalculateScoreCcsNotUsedTest() {
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min), CollisionCrossSection = 100,
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
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                ReverseDotProductCutOff = .7f,
                TotalScoreCutoff = .7f,
                IsUseTimeForAnnotationScoring = false,
                IsUseCcsForAnnotationScoring = false,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Metabolomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature {
                PrecursorMz = 810.604, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min), CollisionCrossSection = 102,
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
            Console.WriteLine($"IsRtMatch: {result.IsRtMatch}");
            Console.WriteLine($"IsCcsMatch: {result.IsCcsMatch}");
            Console.WriteLine($"IsSpectrumMatch: {result.IsSpectrumMatch}");
            Console.WriteLine($"IsLipidClassMatch: {result.IsLipidClassMatch}");
            Console.WriteLine($"IsLipidChainsMatch: {result.IsLipidChainsMatch}");
            Console.WriteLine($"IsLipidPositionMatch: {result.IsLipidPositionMatch}");
            Console.WriteLine($"IsOtherLipidMatch: {result.IsOtherLipidMatch}");

            Assert.IsTrue(result.AcurateMassSimilarity > 0);
            Assert.IsTrue(result.RtSimilarity == 0);
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
            Assert.IsTrue(result.IsRtMatch);
            Assert.IsTrue(result.IsCcsMatch);
            Assert.IsTrue(result.IsSpectrumMatch);
            Assert.IsFalse(result.IsLipidClassMatch);
            Assert.IsFalse(result.IsLipidChainsMatch);
            Assert.IsFalse(result.IsLipidPositionMatch);
            Assert.IsFalse(result.IsOtherLipidMatch);
        }

        [TestMethod()]
        public void SelectTopHitTest() {
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), new MsRefSearchParameterBase(), Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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
                IsUseTimeForAnnotationFiltering = true,
                IsUseCcsForAnnotationFiltering = true,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
            };

            var actuals = annotator.FilterByThreshold(results);
            CollectionAssert.AreEquivalent(new[] { results[4], results[5], results[6], results[7], results[8], results[9], results[10], results[11], }, actuals);
        }

        [TestMethod()]
        public void FilterByThresholdCcsNotUsedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = false,
                IsUseCcsForAnnotationFiltering = false,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
            };

            var actuals = annotator.FilterByThreshold(results);
            CollectionAssert.AreEquivalent(new[] { results[4], results[5], results[6], results[7], results[8], results[9], results[10], results[11], }, actuals);
        }

        [TestMethod()]
        public void SelectReferenceMatchResultsTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = true,
                IsUseCcsForAnnotationFiltering = true,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
            };

            var actuals = annotator.SelectReferenceMatchResults(results);
            CollectionAssert.AreEquivalent(new[] { results[8], results[9], results[10], results[11], }, actuals);
        }

        [TestMethod()]
        public void SelectReferenceMatchResultsCcsNotUsedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = false,
                IsUseCcsForAnnotationFiltering = false,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
            };

            var actuals = annotator.SelectReferenceMatchResults(results);
            CollectionAssert.AreEquivalent(new[] { results[8], results[9], results[10], results[11] }, actuals);
        }

        [TestMethod()]
        public void IsReferenceMatchTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = true,
                IsUseCcsForAnnotationFiltering = true,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
            };

            var actuals = results.Select(result => annotator.IsReferenceMatched(result)).ToList();
            CollectionAssert.AreEqual(
                new[] {
                    false, false, false, false,
                    false, false, false, false,
                    true,  true,  true,  true,
                },
                actuals);
        }

        [TestMethod()]
        public void IsReferenceMatchCcsNotUsedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = false,
                IsUseCcsForAnnotationFiltering = false,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
            };

            var actuals = results.Select(result => annotator.IsReferenceMatched(result)).ToList();
            CollectionAssert.AreEqual(
                new[] {
                    false, false, false, false,
                    false, false, false, false,
                    true, true, true, true,
                },
                actuals);
        }

        [TestMethod()]
        public void IsSuggestedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = true,
                IsUseCcsForAnnotationFiltering = true,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
            };

            var actuals = results.Select(result => annotator.IsAnnotationSuggested(result)).ToList();
            CollectionAssert.AreEqual(
                new[] {
                    false, false, false, false,
                    true,  true,  true,  true,
                    false, false, false, false,
                },
                actuals);
        }

        [TestMethod()]
        public void IsSuggestedCcsNotUsedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = false,
                IsUseCcsForAnnotationFiltering = false,
            };
            var annotator = new LcimmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "MspDB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult> {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, IsCcsMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, IsCcsMatch = true, },
            };

            var actuals = results.Select(result => annotator.IsAnnotationSuggested(result)).ToList();
            CollectionAssert.AreEqual(
                new[] {
                    false, false, false, false,
                    true, true, true, true,
                    false, false, false, false,
                },
                actuals);
        }

        private ChromatogramPeakFeature BuildPeak(double mz, double rt, double ccs) {
            return new ChromatogramPeakFeature { PrecursorMz = mz, ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min), CollisionCrossSection = ccs };
        }

        private MoleculeMsReference BuildRef(int scanId, string name, string inChIKey, double mz, double rt, double ccs) {
            return new MoleculeMsReference {
                ScanID = scanId,
                Name = name,
                InChIKey = inChIKey,
                PrecursorMz = mz,
                ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
                CollisionCrossSection = ccs,
            };
        }

        private AnnotationQuery BuildQuery(ChromatogramPeakFeature target, MsRefSearchParameterBase parameter, LcimmsMspAnnotator annotator) {
            return new AnnotationQuery(target, target, null, null, parameter, annotator, ignoreIsotopicPeak: false);
        }
    }
}