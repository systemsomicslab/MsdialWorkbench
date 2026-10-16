using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialDimsCore.Algorithm.Annotation.Tests
{
    /// <summary>
    /// The DIMS annotators record which terms they computed.
    /// </summary>
    /// <remarks>
    /// A witness per acquisition mode, because the mode-specific annotators build their results
    /// from near-identical code that differs only in which terms the mode has. An omission in one
    /// leaves that mode's evidence blank while every other mode's tests still pass, and there is
    /// nothing in a match result afterwards that says which annotator produced it.
    ///
    /// The same file witnesses the evidence source, because it is recorded at the same
    /// sites and derived from the same terms: key 40 cannot be checked apart from key 39.
    ///
    /// These two get their terms through IMatchResult.Assign rather than from their own
    /// initializers, which is why both the metabolomics and the lipidomics spectrum calculators are
    /// exercised here.
    /// </remarks>
    [TestClass()]
    public class MeasuredTermsAreRecordedTests
    {
        [TestMethod()]
        public void TheMspAnnotatorRecordsSpectrumAndMass() {
            var parameter = Parameter();
            var annotator = new DimsMspAnnotator(Database(SourceType.MspDB), parameter, TargetOmics.Metabolomics, "MspDB", -1);

            var result = annotator.CalculateScore(Query(annotator, parameter), Reference());

            Assert.AreEqual(MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass, result.MeasuredTerms);
        }

        [TestMethod()]
        public void TheMspAnnotatorRecordsSpectrumAndMassOnTheLipidomicsPath() {
            var parameter = Parameter();
            var annotator = new DimsMspAnnotator(Database(SourceType.MspDB), parameter, TargetOmics.Lipidomics, "MspDB", -1);

            var result = annotator.CalculateScore(Query(annotator, parameter), Reference());

            Assert.AreEqual(MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass, result.MeasuredTerms);
        }

        [TestMethod()]
        public void TheMspAnnotatorLeavesSpectrumClearWithNoReferenceSpectrum() {
            var parameter = Parameter();
            var annotator = new DimsMspAnnotator(Database(SourceType.MspDB), parameter, TargetOmics.Metabolomics, "MspDB", -1);
            var reference = Reference();
            reference.Spectrum = new List<SpectrumPeak>();

            var result = annotator.CalculateScore(Query(annotator, parameter), reference);

            Assert.AreEqual(MeasuredTerms.AccurateMass, result.MeasuredTerms,
                "no reference spectrum means no spectrum comparison, whatever the spectral fields hold");
        }

        [TestMethod()]
        public void TheTextDbAnnotatorRecordsMassAlone() {
            var parameter = Parameter();
            var annotator = new DimsTextDBAnnotator(Database(SourceType.TextDB), parameter, "TextDB", -1);

            var result = annotator.CalculateScore(Query(annotator, parameter), Reference());

            Assert.AreEqual(MeasuredTerms.AccurateMass, result.MeasuredTerms,
                "a text database holds no reference spectrum, so nothing here opens one");
        }

        [TestMethod()]
        public void TheMspAnnotatorRecordsAReferenceSpectrum() {
            var parameter = Parameter();
            var annotator = new DimsMspAnnotator(Database(SourceType.MspDB), parameter, TargetOmics.Metabolomics, "MspDB", -1);

            var result = annotator.CalculateScore(Query(annotator, parameter), Reference());

            Assert.AreEqual(AnnotationEvidenceSource.ReferenceSpectrum, result.EvidenceSource);
        }

        [TestMethod()]
        public void TheMspAnnotatorRecordsRuleBasedOnALipidomicsRun() {
            var parameter = Parameter();
            var annotator = new DimsMspAnnotator(Database(SourceType.MspDB), parameter, TargetOmics.Lipidomics, "MspDB", -1);

            var result = annotator.CalculateScore(Query(annotator, parameter), Reference());

            Assert.AreEqual(AnnotationEvidenceSource.RuleBased, result.EvidenceSource,
                "in a lipidomics run the characteristic-ion rules establish the annotation");
        }

        [TestMethod()]
        public void TheMspAnnotatorFallsBackToPrecursorOnlyWithNoSpectrum() {
            var parameter = Parameter();
            var annotator = new DimsMspAnnotator(Database(SourceType.MspDB), parameter, TargetOmics.Metabolomics, "MspDB", -1);
            var reference = Reference();
            reference.Spectrum = new List<SpectrumPeak>();

            var result = annotator.CalculateScore(Query(annotator, parameter), reference);

            Assert.AreEqual(AnnotationEvidenceSource.PrecursorOnly, result.EvidenceSource);
        }

        [TestMethod()]
        public void TheTextDbAnnotatorRecordsPrecursorOnly() {
            var parameter = Parameter();
            var annotator = new DimsTextDBAnnotator(Database(SourceType.TextDB), parameter, "TextDB", -1);

            var result = annotator.CalculateScore(Query(annotator, parameter), Reference());

            Assert.AreEqual(AnnotationEvidenceSource.PrecursorOnly, result.EvidenceSource,
                "these names must stay separable in an export from MS/MS reference matches");
        }

        private static MsRefSearchParameterBase Parameter() {
            return new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                MassRangeBegin = 0f,
                MassRangeEnd = 2000f,
                MinimumSpectrumMatch = 1,
                TotalScoreCutoff = 0,
            };
        }

        private static MoleculeDataBase Database(SourceType source) {
            return new MoleculeDataBase(
                new List<MoleculeMsReference> { Reference(), },
                source.ToString(), source == SourceType.MspDB ? DataBaseSource.Msp : DataBaseSource.Text, source, "DatabasePath");
        }

        private static AnnotationQuery Query(IMatchResultFinder<AnnotationQuery, MsScanMatchResult> annotator, MsRefSearchParameterBase parameter) {
            var target = Target();
            return new AnnotationQuery(target, target, null, null, parameter, annotator, ignoreIsotopicPeak: false);
        }

        private static ChromatogramPeakFeature Target() {
            return new ChromatogramPeakFeature
            {
                PrecursorMz = 810.604,
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
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
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
    }
}
