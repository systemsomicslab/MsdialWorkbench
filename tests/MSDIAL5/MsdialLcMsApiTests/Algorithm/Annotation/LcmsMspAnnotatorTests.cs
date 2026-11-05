using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.Common.DataObj.Result;

namespace CompMs.MsdialLcMsApi.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class LcmsMspAnnotatorTests
    {
        [TestMethod()]
        public void LcmsMspAnnotatorTest() {
            var db = new MoleculeDataBase(new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            }, "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath");
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcmsMspAnnotator(db, parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var result = annotator.Annotate(BuildQuery(target, parameter, annotator));

            Assert.AreEqual(db.Database[1].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void AnnotateTest() {
            var db = new MoleculeDataBase(new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100.001, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            }, "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath");
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcmsMspAnnotator(db, parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var result = annotator.Annotate(BuildQuery(target, parameter, annotator));

            Assert.AreEqual(db.Database[1].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void AnnotateRtNotUsedTest() {
            var db = new MoleculeDataBase(new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100.001, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            }, "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath");
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = false,
                IsUseTimeForAnnotationScoring = false,
            };
            var annotator = new LcmsMspAnnotator(db, parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var result = annotator.Annotate(BuildQuery(target, parameter, annotator));

            Assert.AreEqual(db.Database[4].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void FindCandidatesTest() {
            var db = new MoleculeDataBase(new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            }, "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath");
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
            };
            var annotator = new LcmsMspAnnotator(db, parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var results = annotator.FindCandidates(BuildQuery(target, parameter, annotator));
            var expected = new List<string>
            {
                db.Database[0].InChIKey, db.Database[1].InChIKey, db.Database[2].InChIKey,
            };

            CollectionAssert.AreEquivalent(expected, results.Select(result => result.InChIKey).ToList());
        }

        [TestMethod()]
        public void FindCandidatesRtNotUsedTest() {
            var db = new MoleculeDataBase(new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            }, "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath");
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = false,
            };
            var annotator = new LcmsMspAnnotator(db, parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var results = annotator.FindCandidates(BuildQuery(target, parameter, annotator));
            var expected = new List<string>
            {
                db.Database[0].InChIKey, db.Database[1].InChIKey, db.Database[2].InChIKey, db.Database[4].InChIKey,
            };

            CollectionAssert.AreEquivalent(expected, results.Select(result => result.InChIKey).ToList());
        }

        [TestMethod()]
        public void CalculateScoreTest() {
            var reference = new MoleculeMsReference
            {
                Name = "PC 18:0_20:4",
                CompoundClass = "PC",
                InChIKey = "DUMMYINCHIKEY",
                PrecursorMz = 810.601,
                ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min),
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
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath"), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature
            {
                PrecursorMz = 810.604,
                ChromXs = new ChromXs(2.2, ChromXType.RT, ChromXUnit.Min),
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
            Console.WriteLine($"RtSimilarity: {result.RtSimilarity}");
            Console.WriteLine($"SquaredWeightedDotProduct: {result.SquaredWeightedDotProduct}");
            Console.WriteLine($"SquaredSimpleDotProduct: {result.SquaredSimpleDotProduct}");
            Console.WriteLine($"SquaredReverseDotProduct: {result.SquaredReverseDotProduct}");
            Console.WriteLine($"MatchedPeaksPercentage: {result.MatchedPeaksPercentage}");
            Console.WriteLine($"MatchedPeaksCount: {result.MatchedPeaksCount}");
            Console.WriteLine($"TotalScore: {result.TotalScore}");

            Assert.IsTrue(result.AcurateMassSimilarity > 0);
            Assert.IsTrue(result.RtSimilarity > 0);
            Assert.IsTrue(result.SquaredWeightedDotProduct > 0);
            Assert.IsTrue(result.SquaredSimpleDotProduct > 0);
            Assert.IsTrue(result.SquaredReverseDotProduct > 0);
            Assert.IsTrue(result.MatchedPeaksPercentage > 0);
            Assert.IsTrue(result.MatchedPeaksCount > 0);
            Assert.IsTrue(result.TotalScore > 0);

            var expectedScores = new List<double>
            {
                result.AcurateMassSimilarity,
                (result.WeightedDotProduct * 1 + result.SimpleDotProduct * 1 + result.ReverseDotProduct * 2 + result.MatchedPeaksPercentage * 3) / 7 * 3,
                result.RtSimilarity * .5d,
            }.Average();
            Assert.AreEqual(expectedScores, result.TotalScore, 1e-6);
        }

        [TestMethod()]
        public void CalculateScoreRtNotUsedTest() {
            var reference = new MoleculeMsReference
            {
                Name = "PC 18:0_20:4",
                CompoundClass = "PC",
                PrecursorMz = 810.601,
                InChIKey = "DUMMYINCHIKEY",
                ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min),
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
                IsUseTimeForAnnotationScoring = false,
            };
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath"), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature
            {
                PrecursorMz = 810.604,
                ChromXs = new ChromXs(2.2, ChromXType.RT, ChromXUnit.Min),
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
            Console.WriteLine($"RtSimilarity: {result.RtSimilarity}");
            Console.WriteLine($"SquaredWeightedDotProduct: {result.SquaredWeightedDotProduct}");
            Console.WriteLine($"SquaredSimpleDotProduct: {result.SquaredSimpleDotProduct}");
            Console.WriteLine($"SquaredReverseDotProduct: {result.SquaredReverseDotProduct}");
            Console.WriteLine($"MatchedPeaksPercentage: {result.MatchedPeaksPercentage}");
            Console.WriteLine($"MatchedPeaksCount: {result.MatchedPeaksCount}");
            Console.WriteLine($"TotalScore: {result.TotalScore}");

            Assert.IsTrue(result.AcurateMassSimilarity > 0);
            Assert.IsTrue(result.RtSimilarity == 0);
            Assert.IsTrue(result.SquaredWeightedDotProduct > 0);
            Assert.IsTrue(result.SquaredSimpleDotProduct > 0);
            Assert.IsTrue(result.SquaredReverseDotProduct > 0);
            Assert.IsTrue(result.MatchedPeaksPercentage > 0);
            Assert.IsTrue(result.MatchedPeaksCount > 0);

            var expectedScores = new List<double>
            {
                result.AcurateMassSimilarity,
                (result.WeightedDotProduct * 1 + result.SimpleDotProduct * 1 + result.ReverseDotProduct * 2 + result.MatchedPeaksPercentage * 3) / 7 * 3,
            }.Average();
            Assert.AreEqual((float)expectedScores, result.TotalScore, 1e-6);
        }

        [TestMethod()]
        public void ReferTest() {
            var db = new MoleculeDataBase(new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { ScanID = 0, Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { ScanID = 1, Name = "B", InChIKey = "b", PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { ScanID = 2, Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { ScanID = 4, Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { ScanID = 5, Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            }, "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath");
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcmsMspAnnotator(db, parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var result = annotator.Annotate(BuildQuery(target, parameter, annotator));

            var reference = annotator.Refer(result);

            Assert.AreEqual(db.Database[1], reference);
        }

        [TestMethod()]
        public void SearchTest() {
            var db = new MoleculeDataBase(new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            }, "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath");
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcmsMspAnnotator(db, parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var results = annotator.Search(BuildQuery(target, parameter, annotator));

            CollectionAssert.AreEquivalent(new[] { db.Database[0], db.Database[1], db.Database[2], }, results);
        }

        [TestMethod()]
        public void SearchRtNotUsedTest() {
            var db = new MoleculeDataBase(new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            }, "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath");
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = false,
            };
            var annotator = new LcmsMspAnnotator(db, parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var results = annotator.Search(BuildQuery(target, parameter, annotator));

            CollectionAssert.AreEquivalent(new[] { db.Database[0], db.Database[1], db.Database[2], db.Database[4] }, results);
        }

        [TestMethod()]
        public void SelectTopHitTest() {
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath"), new MsRefSearchParameterBase(), Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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
            };
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase([], "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath"), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, },
            };

            var actuals = annotator.FilterByThreshold(results);
            CollectionAssert.AreEquivalent(new[] { results[2], results[3], results[4], results[5], }, actuals);
        }

        [TestMethod()]
        public void FilterByThresholdRtNotUsedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = false,
            };
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase([], "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath"), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, },
            };

            var actuals = annotator.FilterByThreshold(results);
            CollectionAssert.AreEquivalent(new[] { results[2], results[3], results[4], results[5], }, actuals);
        }

        [TestMethod()]
        public void SelectReferenceMatchResultsTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = true,
            };
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase([], "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath"), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, },
            };

            var actuals = annotator.SelectReferenceMatchResults(results);
            CollectionAssert.AreEquivalent(new[] { results[4], results[5], }, actuals);
        }

        [TestMethod()]
        public void SelectReferenceMatchResultsRtNotUsedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = false,
            };
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase([], "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath"), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, },
            };

            var actuals = annotator.SelectReferenceMatchResults(results);
            CollectionAssert.AreEquivalent(new[] { results[4], results[5], }, actuals);
        }

        [TestMethod()]
        public void IsReferenceMatchTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = true,
            };
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase([], "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath"), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, },
            };

            var actuals = results.Select(result => annotator.IsReferenceMatched(result)).ToList();
            CollectionAssert.AreEqual(
                new[] {
                    false, false,
                    false, false,
                    true, true, 
                },
                actuals);
        }

        [TestMethod()]
        public void IsReferenceMatchRtNotUsedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = false,
            };
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase([], "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath"), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, },
            };

            var actuals = results.Select(result => annotator.IsReferenceMatched(result)).ToList();
            CollectionAssert.AreEqual(
                new[] {
                    false, false,
                    false, false,
                    true, true,
                },
                actuals);
        }

        [TestMethod()]
        public void IsSuggestedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = true,
            };
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase([], "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath"), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, },
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

        [TestMethod()]
        public void IsSuggestedRtNotUsedTest() {
            var parameter = new MsRefSearchParameterBase
            {
                IsUseTimeForAnnotationFiltering = false,
            };
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase([], "DB", DataBaseSource.Msp, SourceType.MspDB, "DBPath"), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, IsRtMatch = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = false, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, IsRtMatch = true, },
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

        /// <summary>
        /// An annotator built without saying anything about suggestions suggests, as every annotator did before.
        /// </summary>
        [TestMethod()]
        public void AnAnnotatorSuggestsByDefault() {
            var parameter = InSilicoParameter();
            var annotator = new LcmsMspAnnotator(InSilicoDatabase(), parameter, Common.Enum.TargetOmics.Metabolomics, "glacier", 0);

            Assert.IsTrue(annotator.AllowsSuggestions);
            var accepted = annotator.CalculateScore(BuildQuery(Measured(), parameter, annotator), Agreeing());
            Assert.IsTrue(accepted.IsReferenceMatched);
            Assert.IsFalse(accepted.IsAnnotationSuggested);
            var fellShort = annotator.CalculateScore(BuildQuery(Measured(), parameter, annotator), Disagreeing());
            Assert.IsFalse(fellShort.IsReferenceMatched);
            Assert.IsTrue(fellShort.IsAnnotationSuggested, "a compared spectrum that fell short stands as a suggestion");
            var noMs2 = annotator.CalculateScore(BuildQuery(WithoutMs2(), parameter, annotator), Agreeing());
            Assert.IsTrue(noMs2.IsAnnotationSuggested, "a feature without a product-ion spectrum gets a precursor-only suggestion");
        }

        /// <summary>
        /// With suggestions off, a reference match is untouched and nothing else stands.
        /// </summary>
        /// <remarks>
        /// The in-silico tier of the reanalysis campaign: a generated library holds hundreds of records in every mass window,
        /// so a suggestion from it would name nearly every feature after one of them.
        /// </remarks>
        [TestMethod()]
        public void WithSuggestionsOffOnlyTheReferenceMatchStands() {
            var parameter = InSilicoParameter();
            var annotator = new LcmsMspAnnotator(InSilicoDatabase(), parameter, Common.Enum.TargetOmics.Metabolomics, "glacier", 0, allowsSuggestions: false);

            var accepted = annotator.CalculateScore(BuildQuery(Measured(), parameter, annotator), Agreeing());
            Assert.IsTrue(accepted.IsReferenceMatched, "the switch does not touch a reference match");
            Assert.AreEqual(AnnotationEvidenceSource.BySpectrumPredictionTool, accepted.EvidenceSource);
            var fellShort = annotator.CalculateScore(BuildQuery(Measured(), parameter, annotator), Disagreeing());
            Assert.IsFalse(fellShort.IsReferenceMatched);
            Assert.IsFalse(fellShort.IsAnnotationSuggested);
            var noMs2 = annotator.CalculateScore(BuildQuery(WithoutMs2(), parameter, annotator), Agreeing());
            Assert.IsFalse(noMs2.IsReferenceMatched);
            Assert.IsFalse(noMs2.IsAnnotationSuggested);

            var kept = annotator.FilterByThreshold(annotator.FindCandidates(BuildQuery(Measured(), parameter, annotator)));
            CollectionAssert.AreEqual(new[] { "agreeing" }, kept.Select(result => result.Name).ToArray(),
                "the candidate that fell short is not stored");
            Assert.AreEqual(0, annotator.FilterByThreshold(annotator.FindCandidates(BuildQuery(WithoutMs2(), parameter, annotator))).Count,
                "nor is a precursor-only one");
        }

        // The thresholds the author set for the in-silico tier on 2026-10-25.
        private static MsRefSearchParameterBase InSilicoParameter() {
            return new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                WeightedDotProductCutOff = 0.5f,
                SimpleDotProductCutOff = 0.5f,
                ReverseDotProductCutOff = 0.5f,
                MatchedPeaksPercentageCutOff = 0f,
                MinimumSpectrumMatch = 3,
                IsUseTimeForAnnotationScoring = false,
                IsUseTimeForAnnotationFiltering = false,
            };
        }

        private static MoleculeDataBase InSilicoDatabase() {
            return new MoleculeDataBase(new List<MoleculeMsReference> { Agreeing(), Disagreeing(), }, "GLACIER", DataBaseSource.PredictedMsp, SourceType.MspDB, "in_silico.msp");
        }

        private static MoleculeMsReference Agreeing() {
            return new MoleculeMsReference
            {
                ScanID = 0,
                Name = "agreeing",
                InChIKey = "AGREEING",
                PrecursorMz = 300.1,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 100.05, Intensity = 40 },
                    new SpectrumPeak { Mass = 150.07, Intensity = 100 },
                    new SpectrumPeak { Mass = 200.09, Intensity = 30 },
                    new SpectrumPeak { Mass = 250.11, Intensity = 20 },
                },
            };
        }

        private static MoleculeMsReference Disagreeing() {
            return new MoleculeMsReference
            {
                ScanID = 1,
                Name = "disagreeing",
                InChIKey = "DISAGREEING",
                PrecursorMz = 300.102,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 60.02, Intensity = 100 },
                    new SpectrumPeak { Mass = 72.04, Intensity = 50 },
                    new SpectrumPeak { Mass = 88.06, Intensity = 20 },
                },
            };
        }

        private static ChromatogramPeakFeature Measured() {
            return new ChromatogramPeakFeature
            {
                PrecursorMz = 300.1004,
                ChromXs = new ChromXs(5, ChromXType.RT, ChromXUnit.Min),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 100.051, Intensity = 35 },
                    new SpectrumPeak { Mass = 150.069, Intensity = 100 },
                    new SpectrumPeak { Mass = 200.092, Intensity = 25 },
                    new SpectrumPeak { Mass = 250.108, Intensity = 15 },
                },
            };
        }

        private static ChromatogramPeakFeature WithoutMs2() {
            return new ChromatogramPeakFeature
            {
                PrecursorMz = 300.1004,
                ChromXs = new ChromXs(5, ChromXType.RT, ChromXUnit.Min),
                Spectrum = new List<SpectrumPeak>(),
            };
        }

        private AnnotationQuery BuildQuery(ChromatogramPeakFeature target, MsRefSearchParameterBase parameter, LcmsMspAnnotator annotator) {
            return new AnnotationQuery(target, target, null, null, parameter, annotator, ignoreIsotopicPeak: false);
        }
    }
}