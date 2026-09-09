using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialLcImMsApi.Algorithm.Annotation.Tests
{
    /// <summary>
    /// The LC-IM-MS annotators record which terms they computed. This mode has the most of them.
    /// </summary>
    /// <remarks>
    /// A witness per acquisition mode, because the mode-specific annotators build their results
    /// from near-identical code that differs only in which terms the mode has. An omission in one
    /// leaves that mode's evidence blank while every other mode's tests still pass, and there is
    /// nothing in a match result afterwards that says which annotator produced it.
    /// </remarks>
    [TestClass()]
    public class MeasuredTermsAreRecordedTests
    {
        [TestMethod()]
        public void TheMspAnnotatorRecordsEveryTermItComputes() {
            var parameter = Parameter();
            var annotator = new LcimmsMspAnnotator(Database(SourceType.MspDB), parameter, TargetOmics.Metabolomics, "MspDB", -1);

            var result = annotator.CalculateScore(Query(annotator, parameter), Reference());

            Assert.AreEqual(
                MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass | MeasuredTerms.RetentionTime | MeasuredTerms.Ccs | MeasuredTerms.Isotope,
                result.MeasuredTerms);
        }

        [TestMethod()]
        public void TheMspAnnotatorLeavesTimeAndCcsClearWhenTheRunScoresNeither() {
            var parameter = Parameter();
            parameter.IsUseTimeForAnnotationScoring = false;
            parameter.IsUseCcsForAnnotationScoring = false;
            var annotator = new LcimmsMspAnnotator(Database(SourceType.MspDB), parameter, TargetOmics.Metabolomics, "MspDB", -1);

            var result = annotator.CalculateScore(Query(annotator, parameter), Reference());

            Assert.AreEqual(
                MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass | MeasuredTerms.Isotope,
                result.MeasuredTerms);
        }

        [TestMethod()]
        public void TheTextDbAnnotatorRecordsEverythingButSpectrum() {
            var parameter = Parameter();
            var annotator = new LcimmsTextDBAnnotator(Database(SourceType.TextDB), parameter, "TextDB", -1);

            var result = annotator.CalculateScore(Query(annotator, parameter), Reference());

            Assert.AreEqual(
                MeasuredTerms.AccurateMass | MeasuredTerms.RetentionTime | MeasuredTerms.Ccs | MeasuredTerms.Isotope,
                result.MeasuredTerms,
                "a text database holds no reference spectrum, so nothing here opens one");
        }

        [TestMethod()]
        public void TheMspAnnotatorLeavesTimeAndCcsClearWhenTheReferenceHasNeither() {
            // Distinct from the run not scoring them: here the run does score both, the annotator
            // does enter both blocks, and there is nothing on the reference side to compare. Only
            // the recording rule separates the two cases.
            var parameter = Parameter();
            var annotator = new LcimmsMspAnnotator(Database(SourceType.MspDB), parameter, TargetOmics.Metabolomics, "MspDB", -1);
            var reference = Reference();
            reference.ChromXs = new ChromXs(0, ChromXType.RT, ChromXUnit.Min);
            reference.CollisionCrossSection = 0;

            var result = annotator.CalculateScore(Query(annotator, parameter), reference);

            Assert.AreEqual(
                MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass | MeasuredTerms.Isotope,
                result.MeasuredTerms);
        }

        [TestMethod()]
        public void TheTextDbAnnotatorLeavesIsotopeClearWithNoIsotopicPeaks() {
            var parameter = Parameter();
            var annotator = new LcimmsTextDBAnnotator(Database(SourceType.TextDB), parameter, "TextDB", -1);
            var reference = Reference();
            reference.IsotopicPeaks = null;

            var result = annotator.CalculateScore(Query(annotator, parameter), reference);

            Assert.AreEqual(
                MeasuredTerms.AccurateMass | MeasuredTerms.RetentionTime | MeasuredTerms.Ccs,
                result.MeasuredTerms);
        }

        private static MsRefSearchParameterBase Parameter() {
            return new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                CcsTolerance = 5f,
                MassRangeBegin = 0f,
                MassRangeEnd = 2000f,
                MinimumSpectrumMatch = 1,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationScoring = true,
                IsUseCcsForAnnotationScoring = true,
            };
        }

        private static MoleculeDataBase Database(SourceType source) {
            return new MoleculeDataBase(
                new List<MoleculeMsReference> { Reference(), },
                source.ToString(), source == SourceType.MspDB ? DataBaseSource.Msp : DataBaseSource.Text, source, "DatabasePath");
        }

        private static AnnotationQuery Query(IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator, MsRefSearchParameterBase parameter) {
            var target = Target();
            return new AnnotationQuery(target, target, ScanIsotopes(), null, parameter, annotator, ignoreIsotopicPeak: false);
        }

        private static ChromatogramPeakFeature Target() {
            return new ChromatogramPeakFeature
            {
                PrecursorMz = 810.604,
                CollisionCrossSection = 102,
                ChromXs = new ChromXs(2.2, ChromXType.RT, ChromXUnit.Min),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 86.094, Intensity = 5, },
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 524.367, Intensity = 1, },
                    new SpectrumPeak { Mass = 810.604, Intensity = 25, },
                },
            };
        }

        private static MoleculeMsReference Reference() {
            return new MoleculeMsReference
            {
                ScanID = 0,
                Name = "PC 18:0_20:4",
                CompoundClass = "PC",
                InChIKey = "DUMMYINCHIKEY",
                PrecursorMz = 810.601,
                CollisionCrossSection = 100,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                ChromXs = new ChromXs(2.0, ChromXType.RT, ChromXUnit.Min),
                IsotopicPeaks = ReferenceIsotopes(),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 184.073, Intensity = 100 },
                    new SpectrumPeak { Mass = 506.361, Intensity = 5 },
                    new SpectrumPeak { Mass = 524.372, Intensity = 5 },
                    new SpectrumPeak { Mass = 526.330, Intensity = 5 },
                    new SpectrumPeak { Mass = 544.340, Intensity = 5 },
                    new SpectrumPeak { Mass = 810.601, Intensity = 30 },
                },
            };
        }

        private static List<IsotopicPeak> ScanIsotopes() {
            return new List<IsotopicPeak>
            {
                new IsotopicPeak { RelativeAbundance = 1, },
                new IsotopicPeak { RelativeAbundance = 3.5, },
                new IsotopicPeak { RelativeAbundance = 4, },
            };
        }

        private static List<IsotopicPeak> ReferenceIsotopes() {
            return new List<IsotopicPeak>
            {
                new IsotopicPeak { RelativeAbundance = 1, },
                new IsotopicPeak { RelativeAbundance = 4, },
                new IsotopicPeak { RelativeAbundance = 6, },
            };
        }
    }
}
