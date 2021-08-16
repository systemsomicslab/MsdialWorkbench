using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class LcmsTextDBAnnotatorTests
    {
        [TestMethod()]
        public void LcmsTextDBAnnotatorTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(db, "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var result = annotator.Annotate(target, target, null);

            Assert.AreEqual(db[1].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void AnnotateTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100.001, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(db, "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var result = annotator.Annotate(target, target, null);

            Assert.AreEqual(db[1].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void AnnotateRtNotUsedTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100.001, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = false,
                IsUseTimeForAnnotationScoring = false,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(db, "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var result = annotator.Annotate(target, target, null);

            Assert.AreEqual(db[4].InChIKey, result.InChIKey);
        }

        [TestMethod()]
        public void FindCandidatesTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(db, "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var results = annotator.FindCandidates(target, target, null);
            var expected = new List<string>
            {
                db[0].InChIKey, db[1].InChIKey, db[2].InChIKey,
            };

            CollectionAssert.AreEquivalent(expected, results.Select(result => result.InChIKey).ToList());
        }

        [TestMethod()]
        public void FindCandidatesRtNotUsedTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = false,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(db, "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var results = annotator.FindCandidates(target, target, null);
            var expected = new List<string>
            {
                db[0].InChIKey, db[1].InChIKey, db[2].InChIKey, db[4].InChIKey,
            };

            CollectionAssert.AreEquivalent(expected, results.Select(result => result.InChIKey).ToList());
        }

        [TestMethod()]
        public void CalculateScoreTest() {
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min),
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 0.5f,
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature {
                PrecursorMz = 810.604, ChromXs = new ChromXs(2.2, ChromXType.RT, ChromXUnit.Min),
            };

            var result = annotator.CalculateScore(target, target, null, reference, null);

            Console.WriteLine($"AccurateSimilarity: {result.AcurateMassSimilarity}");
            Console.WriteLine($"RtSimilarity: {result.RtSimilarity}");
            Console.WriteLine($"TotalScore: {result.TotalScore}");

            Assert.IsTrue(result.AcurateMassSimilarity > 0);
            Assert.IsTrue(result.RtSimilarity > 0);
            Assert.IsTrue(result.TotalScore > 0);
        }

        [TestMethod()]
        public void CalculateScoreRtNotUsedTest() {
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min),
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 0.5f,
                IsUseTimeForAnnotationScoring = false,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature {
                PrecursorMz = 810.604, ChromXs = new ChromXs(2.2, ChromXType.RT, ChromXUnit.Min),
            };

            var result = annotator.CalculateScore(target, target, null, reference, null);

            Console.WriteLine($"AccurateSimilarity: {result.AcurateMassSimilarity}");
            Console.WriteLine($"RtSimilarity: {result.RtSimilarity}");
            Console.WriteLine($"TotalScore: {result.TotalScore}");

            Assert.IsTrue(result.AcurateMassSimilarity > 0);
            Assert.IsTrue(result.RtSimilarity == 0);
            Assert.IsTrue(result.TotalScore > 0);
        }

        [TestMethod()]
        public void ReferTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { ScanID = 0, Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { ScanID = 1, Name = "B", InChIKey = "b", PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { ScanID = 2, Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { ScanID = 3, Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { ScanID = 4, Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(db, "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var result = annotator.Annotate(target, target, null);

            var reference = annotator.Refer(result);

            Assert.AreEqual(db[1], reference);
        }

        [TestMethod()]
        public void SearchTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = true,
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(db, "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var results = annotator.Search(target);

            CollectionAssert.AreEquivalent(db.GetRange(0, 3), results);
        }

        [TestMethod()]
        public void SearchRtNotUsedTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, ChromXs = new ChromXs(1.6, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, ChromXs = new ChromXs(2.4, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 99.9, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 100, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min) },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationFiltering = false,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(db, "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature { PrecursorMz = 100, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min) };
            var results = annotator.Search(target);

            CollectionAssert.AreEquivalent(new[] { db[0], db[1], db[2], db[4] }, results);
        }

        [TestMethod()]
        public void ValidateTest() {
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min),
                AdductType = new Common.DataObj.Property.AdductIon { AdductIonName = "[M+H]+" },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 0.5f,
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature {
                PrecursorMz = 810.604, ChromXs = new ChromXs(2.2, ChromXType.RT, ChromXUnit.Min),
            };

            var result = annotator.CalculateScore(target, target, null, reference, null);
            annotator.Validate(result, target, target, null, reference, null);

            Console.WriteLine($"IsPrecursorMzMatch: {result.IsPrecursorMzMatch}");
            Console.WriteLine($"IsRtMatch: {result.IsRtMatch}");

            Assert.IsTrue(result.IsPrecursorMzMatch);
            Assert.IsTrue(result.IsRtMatch);
        }

        [TestMethod()]
        public void ValidateRtNotUsedTest() {
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min),
                AdductType = new Common.DataObj.Property.AdductIon { AdductIonName = "[M+H]+" },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 0.5f,
                IsUseTimeForAnnotationScoring = false,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature {
                PrecursorMz = 810.604, ChromXs = new ChromXs(2.2, ChromXType.RT, ChromXUnit.Min),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 86.094, Intensity = 5, },
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 524.367, Intensity = 1, },
                    new SpectrumPeak { Mass = 810.604, Intensity = 25, },
                }
            };

            var result = annotator.CalculateScore(target, target, null, reference, null);
            annotator.Validate(result, target, target, null, reference, null);

            Console.WriteLine($"IsPrecursorMzMatch: {result.IsPrecursorMzMatch}");

            Assert.IsTrue(result.IsPrecursorMzMatch);
        }

        [TestMethod()]
        public void ValidateRtMatchTest() {
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min),
                AdductType = new Common.DataObj.Property.AdductIon { AdductIonName = "[M+H]+" },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                RtTolerance = 2f,
                IsUseTimeForAnnotationScoring = true,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");

            var target = new ChromatogramPeakFeature {
                PrecursorMz = 810.604, ChromXs = new ChromXs(2.51, ChromXType.RT, ChromXUnit.Min),
            };

            var result = annotator.CalculateScore(target, target, null, reference, null);
            annotator.Validate(result, target, target, null, reference, null);

            Console.WriteLine($"IsPrecursorMzMatch: {result.IsPrecursorMzMatch}");
            Console.WriteLine($"IsRtMatch: {result.IsRtMatch}");

            Assert.IsTrue(result.IsPrecursorMzMatch);
            Assert.IsFalse(result.IsRtMatch);
        }


        [TestMethod()]
        public void SelectTopHitTest() {
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), new MsRefSearchParameterBase(), "TextDB");
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
                TotalScoreCutoff = 0.5f,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = false, IsSpectrumMatch = false,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = false,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = false, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.4f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.4f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.4f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 2, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.4f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.4f },
            };

            var actuals = annotator.FilterByThreshold(results);
            var expected = new[]
            {
                results[0], results[2], results[4], results[5],
                results[6], results[7], results[8],
            };
            CollectionAssert.AreEquivalent(expected, actuals);
        }

        [TestMethod()]
        public void SelectReferenceMatchResultsTest() {
            var parameter = new MsRefSearchParameterBase
            {
                WeightedDotProductCutOff = 0.5f, SimpleDotProductCutOff = 0.5f, ReverseDotProductCutOff = 0.5f,
                MatchedPeaksPercentageCutOff = 0.5f, MinimumSpectrumMatch = 3,
                TotalScoreCutoff = 0.5f,
            };
            var annotator = new LcmsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB");
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = false, IsSpectrumMatch = false,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = false,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = false, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.4f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.4f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.4f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 2, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.4f,
                    TotalScore = 0.8f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.4f },
                new MsScanMatchResult {
                    IsPrecursorMzMatch = true, IsSpectrumMatch = true,
                    WeightedDotProduct = 0.8f, SimpleDotProduct = 0.8f, ReverseDotProduct = 0.8f,
                    MatchedPeaksCount = 6, MatchedPeaksPercentage = 0.8f,
                    TotalScore = 0.8f },
            };

            var actuals = annotator.SelectReferenceMatchResults(results);
            var expected = new[]
            {
                results[0], results[2], results[4], results[5],
                results[6], results[7], results[8], results[10],
            };
            CollectionAssert.AreEquivalent(expected, actuals);
        }
    }
}