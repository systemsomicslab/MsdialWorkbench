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
            }, "DB", DataBaseSource.Msp, SourceType.MspDB);
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
            }, "DB", DataBaseSource.Msp, SourceType.MspDB);
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
            }, "DB", DataBaseSource.Msp, SourceType.MspDB);
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
            }, "DB", DataBaseSource.Msp, SourceType.MspDB);
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
            }, "DB", DataBaseSource.Msp, SourceType.MspDB);
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
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

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
            Console.WriteLine($"WeightedDotProduct: {result.WeightedDotProduct}");
            Console.WriteLine($"SimpleDotProduct: {result.SimpleDotProduct}");
            Console.WriteLine($"ReverseDotProduct: {result.ReverseDotProduct}");
            Console.WriteLine($"MatchedPeaksPercentage: {result.MatchedPeaksPercentage}");
            Console.WriteLine($"MatchedPeaksCount: {result.MatchedPeaksCount}");
            Console.WriteLine($"TotalScore: {result.TotalScore}");

            Assert.IsTrue(result.AcurateMassSimilarity > 0);
            Assert.IsTrue(result.RtSimilarity > 0);
            Assert.IsTrue(result.WeightedDotProduct > 0);
            Assert.IsTrue(result.SimpleDotProduct > 0);
            Assert.IsTrue(result.ReverseDotProduct > 0);
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
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);

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
            Console.WriteLine($"WeightedDotProduct: {result.WeightedDotProduct}");
            Console.WriteLine($"SimpleDotProduct: {result.SimpleDotProduct}");
            Console.WriteLine($"ReverseDotProduct: {result.ReverseDotProduct}");
            Console.WriteLine($"MatchedPeaksPercentage: {result.MatchedPeaksPercentage}");
            Console.WriteLine($"MatchedPeaksCount: {result.MatchedPeaksCount}");
            Console.WriteLine($"TotalScore: {result.TotalScore}");

            Assert.IsTrue(result.AcurateMassSimilarity > 0);
            Assert.IsTrue(result.RtSimilarity == 0);
            Assert.IsTrue(result.WeightedDotProduct > 0);
            Assert.IsTrue(result.SimpleDotProduct > 0);
            Assert.IsTrue(result.ReverseDotProduct > 0);
            Assert.IsTrue(result.MatchedPeaksPercentage > 0);
            Assert.IsTrue(result.MatchedPeaksCount > 0);

            var expectedScores = new List<double>
            {
                result.AcurateMassSimilarity,
                (result.WeightedDotProduct * 1 + result.SimpleDotProduct * 1 + result.ReverseDotProduct * 2 + result.MatchedPeaksPercentage * 3) / 7 * 3,
            }.Average();
            Assert.AreEqual((float)expectedScores, result.TotalScore);
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
            }, "DB", DataBaseSource.Msp, SourceType.MspDB);
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
            }, "DB", DataBaseSource.Msp, SourceType.MspDB);
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
            }, "DB", DataBaseSource.Msp, SourceType.MspDB);
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
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB), new MsRefSearchParameterBase(), Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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
            var annotator = new LcmsMspAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "DB", DataBaseSource.Msp, SourceType.MspDB), parameter, Common.Enum.TargetOmics.Lipidomics, "MspDB", -1);
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

        private AnnotationQuery BuildQuery(ChromatogramPeakFeature target, MsRefSearchParameterBase parameter, LcmsMspAnnotator annotator) {
            return new AnnotationQuery(target, target, null, null, parameter, annotator, ignoreIsotopicPeak: false);
        }
    }
}