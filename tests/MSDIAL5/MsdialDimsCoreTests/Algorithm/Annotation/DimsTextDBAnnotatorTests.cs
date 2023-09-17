using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class DimsTextDBAnnotatorTests
    {
        [TestMethod()]
        public void AnnotateTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.990, },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.019, },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 101, },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 102, },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                TotalScoreCutoff = 0,
            };
            var annotator = new DimsTextDBAnnotator(new MoleculeDataBase(db, "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100.009 };
            var result = annotator.Annotate(BuildQuery(target, annotator));

            Assert.AreEqual(db[1].Name, result.Name);
        }

        [TestMethod()]
        public void FindCandidatesTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.991, },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.009, },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 101, },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 102, },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
            };
            var annotator = new DimsTextDBAnnotator(new MoleculeDataBase(db, "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100 };
            var results = annotator.FindCandidates(BuildQuery(target, annotator));
            var expected = new[]
            {
                db[0].Name, db[1].Name, db[2].Name,
            };

            CollectionAssert.AreEquivalent(expected, results.Select(result => result.Name).ToArray());
        }

        [TestMethod()]
        public void SearchTest() {
            var db = new List<MoleculeMsReference>
            {
                    new MoleculeMsReference { Name = "A", InChIKey = "a", PrecursorMz = 99.990, },
                    new MoleculeMsReference { Name = "B", InChIKey = "b", PrecursorMz = 100, },
                    new MoleculeMsReference { Name = "C", InChIKey = "c", PrecursorMz = 100.017, },
                    new MoleculeMsReference { Name = "D", InChIKey = "d", PrecursorMz = 101, },
                    new MoleculeMsReference { Name = "E", InChIKey = "e", PrecursorMz = 102, },
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
            };
            var annotator = new DimsTextDBAnnotator(new MoleculeDataBase(db, "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100.008 };
            var results = annotator.Search(BuildQuery(target, annotator));

            CollectionAssert.AreEqual(db.GetRange(1, 2), results);
        }

        [TestMethod()]
        public void CalculateScoreTest() {
            var reference = new MoleculeMsReference
            {
                Name = "PC 18:0_20:4",
                CompoundClass = "PC",
                PrecursorMz = 810.601,
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
            };
            var annotator = new DimsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB", -1);

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
            Console.WriteLine($"TotalScore: {result.TotalScore}");
            Console.WriteLine($"IsPrecursorMzMatch: {result.IsPrecursorMzMatch}");

            Assert.IsTrue(result.AcurateMassSimilarity > 0);
            Assert.IsTrue(result.TotalScore > 0);
            Assert.IsTrue(result.IsPrecursorMzMatch);

            var expected = new[]
            {
                result.AcurateMassSimilarity,
            }.Average();
            Assert.AreEqual((float)expected, result.TotalScore);
        }

        [TestMethod()]
        public void IsAnnotationSuggestedTest() {
            var parameter = new MsRefSearchParameterBase();
            var annotator = new DimsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsSpectrumMatch = false, },
                new MsScanMatchResult { IsPrecursorMzMatch = false, IsSpectrumMatch = true, },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsSpectrumMatch = false, },
                new MsScanMatchResult { IsPrecursorMzMatch = true, IsSpectrumMatch = true, },
            };

            var actuals = results.Select(result => annotator.IsAnnotationSuggested(result)).ToList();
            CollectionAssert.AreEqual(new[] { false, false, false, false, }, actuals);
        }

        [TestMethod()]
        public void IsReferenceMatchedTest() {
            var parameter = new MsRefSearchParameterBase();
            var annotator = new DimsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, },
            };

            var actuals = results.Select(result => annotator.IsReferenceMatched(result)).ToList();
            CollectionAssert.AreEqual(new[] { false, false, true, }, actuals);
        }

        [TestMethod()]
        public void FilterByThresholdTest() {
            var parameter = new MsRefSearchParameterBase();
            var annotator = new DimsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, },
            };

            var actuals = annotator.FilterByThreshold(results);
            CollectionAssert.AreEquivalent(new[] { results[1], results[2], }, actuals);

            CollectionAssert.AreEquivalent(new MsScanMatchResult[] { }, annotator.FilterByThreshold(new MsScanMatchResult[] { }));
        }

        [TestMethod()]
        public void SelectReferenceMatchResultsTest() {
            var parameter = new MsRefSearchParameterBase();
            var annotator = new DimsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB", -1);
            var results = new List<MsScanMatchResult>
            {
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = false, },
                new MsScanMatchResult { IsReferenceMatched = false, IsAnnotationSuggested = true, },
                new MsScanMatchResult { IsReferenceMatched = true, IsAnnotationSuggested = false, },
            };

            var actuals = annotator.SelectReferenceMatchResults(results);
            CollectionAssert.AreEquivalent(new[] { results[2], }, actuals);

            CollectionAssert.AreEquivalent(new MsScanMatchResult[] { }, annotator.SelectReferenceMatchResults(new MsScanMatchResult[] { }));
        }

        [TestMethod()]
        public void SelectTopHitTest() {
            var parameter = new MsRefSearchParameterBase();
            var annotator = new DimsTextDBAnnotator(new MoleculeDataBase(Enumerable.Empty<MoleculeMsReference>(), "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB", -1);
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

            Assert.AreEqual(null, annotator.SelectTopHit(new MsScanMatchResult[] { }));
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
            var annotator = new DimsTextDBAnnotator(new MoleculeDataBase(db, "TextDB", DataBaseSource.Text, SourceType.TextDB), parameter, "TextDB", -1);

            var target = new ChromatogramPeakFeature { PrecursorMz = 100.009 };
            var result = annotator.Annotate(BuildQuery(target, annotator));

            var reference = annotator.Refer(result);

            Assert.AreEqual(db[1], reference);
        }

        private AnnotationQuery BuildQuery(ChromatogramPeakFeature target, DimsTextDBAnnotator annotator) {
            return new AnnotationQuery(target, target, null, null, new MsRefSearchParameterBase(), annotator, ignoreIsotopicPeak: false);
        }
    }
}