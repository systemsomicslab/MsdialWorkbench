using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Export.Tests
{
    /// <summary>
    /// The analysis (.mdpeak) and the alignment (.mdalign) export must report the same annotation score
    /// columns the same way for the same annotation outcome, and "null" must mean "never computed"
    /// rather than "zero".
    /// </summary>
    /// <remarks>
    /// These two exports used to disagree. The analysis accessor bound the nullable ValueOrNull overload
    /// and printed 0.000 for a precursor-only suggestion whose score was never computed; the alignment
    /// accessor bound a float overload and printed "null" for any value within 1e-10 of zero, which
    /// discarded a reverse dot product that a real spectral comparison had produced as 0. Both are
    /// asserted here for all four MS-DIAL annotation outcomes, plus a text-database annotation, which
    /// never performs a spectral comparison either.
    /// </remarks>
    [TestClass]
    public class AnnotationScoreRepresentationTests
    {
        private static readonly string[] SharedScoreColumns = new[] {
            "Simple dot product",
            "Weighted dot product",
            "Reverse dot product",
            "Matched peaks count",
            "Matched peaks percentage",
        };

        [TestMethod]
        public void MsmsMatchedReportsEveryScoreInBothFormats() {
            var peak = ExportAnalysisRow(MsmsMatched(), hadProductIonSpectrum: true);
            var spot = ExportAlignmentRow(MsmsMatched(), hadProductIonSpectrum: true);

            foreach (var content in new[] { peak, spot }) {
                Assert.AreEqual("0.900", content["Simple dot product"]);
                Assert.AreEqual("0.800", content["Weighted dot product"]);
                Assert.AreEqual("0.960", content["Reverse dot product"]);
                Assert.AreEqual("6.00", content["Matched peaks count"]);
                Assert.AreEqual("0.75", content["Matched peaks percentage"]);
            }
        }

        /// <summary>
        /// A product-ion spectrum was compared against the reference and nothing overlapped. The zeros
        /// are measurements and must survive, which is what .mdalign used to lose.
        /// </summary>
        [TestMethod]
        public void LowScoreComparisonThatScoredZeroReportsZeroInBothFormats() {
            var peak = ExportAnalysisRow(LowScoreComparedAndScoredZero(), hadProductIonSpectrum: true);
            var spot = ExportAlignmentRow(LowScoreComparedAndScoredZero(), hadProductIonSpectrum: true);

            foreach (var content in new[] { peak, spot }) {
                Assert.AreEqual("0.000", content["Simple dot product"]);
                Assert.AreEqual("0.000", content["Weighted dot product"]);
                Assert.AreEqual("0.000", content["Reverse dot product"]);
                Assert.AreEqual("0.00", content["Matched peaks count"]);
                Assert.AreEqual("0.00", content["Matched peaks percentage"]);
            }
        }

        /// <summary>
        /// No product-ion spectrum existed, so nothing was compared. This is the row MS-DIAL names
        /// "no MS2: ", and it is what .mdpeak used to report as 0.000.
        /// </summary>
        [TestMethod]
        public void PrecursorOnlySuggestionReportsNullInBothFormats() {
            var peak = ExportAnalysisRow(PrecursorOnlySuggestion(), hadProductIonSpectrum: false);
            var spot = ExportAlignmentRow(PrecursorOnlySuggestion(), hadProductIonSpectrum: false);

            foreach (var content in new[] { peak, spot }) {
                foreach (var column in SharedScoreColumns) {
                    Assert.AreEqual(AnnotationScoreFormat.NotComputed, content[column], column);
                }
            }
        }

        [TestMethod]
        public void UnknownReportsNullInBothFormats() {
            var peak = ExportAnalysisRow(null, hadProductIonSpectrum: true);
            var spot = ExportAlignmentRow(null, hadProductIonSpectrum: true);

            foreach (var content in new[] { peak, spot }) {
                foreach (var column in SharedScoreColumns) {
                    Assert.AreEqual(AnnotationScoreFormat.NotComputed, content[column], column);
                }
            }
        }

        /// <summary>
        /// A text database holds no reference spectrum, so its annotator never scores one even when the
        /// peak does carry a product-ion spectrum. The spectral fields keep their default 0.
        /// </summary>
        [TestMethod]
        public void TextDatabaseAnnotationReportsNullInBothFormats() {
            var peak = ExportAnalysisRow(TextDatabaseAnnotation(), hadProductIonSpectrum: true);
            var spot = ExportAlignmentRow(TextDatabaseAnnotation(), hadProductIonSpectrum: true);

            foreach (var content in new[] { peak, spot }) {
                foreach (var column in SharedScoreColumns) {
                    Assert.AreEqual(AnnotationScoreFormat.NotComputed, content[column], column);
                }
            }
        }

        [TestMethod]
        public void BothFormatsAgreeOnEveryAnnotationOutcome() {
            var outcomes = new (string Outcome, MsScanMatchResult Result, bool HadProductIonSpectrum)[] {
                ("msms matched", MsmsMatched(), true),
                ("low score", LowScoreComparedAndScoredZero(), true),
                ("precursor only", PrecursorOnlySuggestion(), false),
                ("text database", TextDatabaseAnnotation(), true),
                ("unknown", null, true),
            };

            foreach (var outcome in outcomes) {
                var peak = ExportAnalysisRow(outcome.Result, outcome.HadProductIonSpectrum);
                var spot = ExportAlignmentRow(outcome.Result, outcome.HadProductIonSpectrum);
                foreach (var column in SharedScoreColumns) {
                    Assert.AreEqual(peak[column], spot[column], $"{outcome.Outcome}: {column}");
                }
            }
        }

        /// <summary>
        /// The -1 the scoring functions return for "nothing to compare" is only visible in the squared
        /// fields, because the dot-product getters clamp it to 0.
        /// </summary>
        [TestMethod]
        public void NotComputedSentinelIsOnlyVisibleInTheSquaredFields() {
            var result = PrecursorOnlySuggestion();

            Assert.AreEqual(-1f, result.SquaredSimpleDotProduct);
            Assert.AreEqual(0f, result.SimpleDotProduct);
            Assert.IsFalse(result.IsSpectrumComparisonPerformed);
            Assert.IsTrue(MsmsMatched().IsSpectrumComparisonPerformed);
            Assert.IsTrue(LowScoreComparedAndScoredZero().IsSpectrumComparisonPerformed);
            Assert.IsFalse(TextDatabaseAnnotation().IsSpectrumComparisonPerformed);
        }

        private static MsScanMatchResult MsmsMatched() {
            return new MsScanMatchResult
            {
                Name = "Reference compound",
                Source = SourceType.MspDB,
                AnnotatorID = "msp_annotator_1",
                IsPrecursorMzMatch = true,
                IsSpectrumMatch = true,
                IsReferenceMatched = true,
                SimpleDotProduct = 0.9f,
                WeightedDotProduct = 0.8f,
                ReverseDotProduct = 0.96f,
                MatchedPeaksCount = 6f,
                MatchedPeaksPercentage = 0.75f,
                AcurateMassSimilarity = 1f,
                TotalScore = 1.706f,
            };
        }

        private static MsScanMatchResult LowScoreComparedAndScoredZero() {
            return new MsScanMatchResult
            {
                Name = "Reference compound",
                Source = SourceType.MspDB,
                AnnotatorID = "msp_annotator_1",
                IsPrecursorMzMatch = true,
                IsSpectrumMatch = false,
                IsAnnotationSuggested = true,
                SimpleDotProduct = 0f,
                WeightedDotProduct = 0f,
                ReverseDotProduct = 0f,
                MatchedPeaksCount = 0f,
                MatchedPeaksPercentage = 0f,
                AcurateMassSimilarity = 1f,
                TotalScore = 0.494f,
            };
        }

        private static MsScanMatchResult PrecursorOnlySuggestion() {
            return new MsScanMatchResult
            {
                Name = "Reference compound",
                Source = SourceType.MspDB,
                AnnotatorID = "msp_annotator_1",
                IsPrecursorMzMatch = true,
                IsSpectrumMatch = false,
                IsAnnotationSuggested = true,
                // MsScanMatching returns -1 from every spectral scoring function when there is nothing
                // to compare. The squared fields are assigned directly because the SimpleDotProduct
                // setter squares its argument, so assigning -1 there would store +1 instead.
                SquaredSimpleDotProduct = -1f,
                SquaredWeightedDotProduct = -1f,
                SquaredReverseDotProduct = -1f,
                MatchedPeaksCount = -1f,
                MatchedPeaksPercentage = -1f,
                AcurateMassSimilarity = 1f,
                TotalScore = -0.143f,
            };
        }

        private static MsScanMatchResult TextDatabaseAnnotation() {
            return new MsScanMatchResult
            {
                Name = "Reference compound",
                Source = SourceType.TextDB,
                AnnotatorID = "text_annotator_1",
                IsPrecursorMzMatch = true,
                IsRtMatch = true,
                AcurateMassSimilarity = 1f,
                RtSimilarity = 1f,
                TotalScore = 1f,
            };
        }

        private static Dictionary<string, string> ExportAnalysisRow(MsScanMatchResult result, bool hadProductIonSpectrum) {
            var parameter = new ParameterBase { MachineCategory = MachineCategory.LCMS, CentroidMs1Tolerance = 0.01f, MS2DataType = MSDataType.Centroid, };
            var accessor = new TestAnalysisMetadataAccessor(new MockRefer(), parameter, ExportspectraType.deconvoluted);
            var basePeak = new BaseChromatogramPeakFeature { PeakHeightTop = 1000d, PeakAreaAboveZero = 900d, Mass = 700d, };
            var feature = new ChromatogramPeakFeature(basePeak)
            {
                MasterPeakID = 1,
                Name = result?.Name ?? "Unknown",
                MS1RawSpectrumIdTop = 2,
                MS2RawSpectrumID = hadProductIonSpectrum ? 3 : -1,
                AdductType = AdductIon.GetAdductIon("[M-H]-"),
                MatchResults = new MsScanMatchResultContainer(),
            };
            if (result is not null) {
                feature.MatchResults.AddResults(new List<MsScanMatchResult> { result, });
            }
            var msdec = new MSDecResult
            {
                RawSpectrumID = hadProductIonSpectrum ? 3 : -1,
                Spectrum = hadProductIonSpectrum
                    ? new List<SpectrumPeak> { new SpectrumPeak { Mass = 200.00003, Intensity = 50, }, }
                    : new List<SpectrumPeak>(),
            };
            var analysisFile = new AnalysisFileBean { AcquisitionType = AcquisitionType.DDA, };
            return accessor.GetContent(feature, msdec, new MockDataProvider(), analysisFile, new ExportStyle());
        }

        private static IReadOnlyDictionary<string, string> ExportAlignmentRow(MsScanMatchResult result, bool hadProductIonSpectrum) {
            var parameter = new ParameterBase { MachineCategory = MachineCategory.LCMS, };
            IMetadataAccessor accessor = new TestMetadataAccessor(new MockRefer(), parameter);
            var spot = new AlignmentSpotProperty
            {
                MasterAlignmentID = 1,
                Name = result?.Name ?? "Unknown",
                RepresentativeFileID = 0,
                MatchResults = new MsScanMatchResultContainer(),
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    new AlignmentChromPeakFeature
                    {
                        FileID = 0,
                        FileName = "sample",
                        MS2RawSpectrumID2CE = hadProductIonSpectrum
                            ? new Dictionary<int, double> { { 3, 0d }, }
                            : new Dictionary<int, double>(),
                    },
                },
            };
            spot.SetAdductType(AdductIon.GetAdductIon("[M-H]-"));
            if (result is not null) {
                spot.MatchResults.AddResults(new List<MsScanMatchResult> { result, });
            }
            var msdec = new MSDecResult
            {
                Spectrum = hadProductIonSpectrum
                    ? new List<SpectrumPeak> { new SpectrumPeak { Mass = 200.00003, Intensity = 50, }, }
                    : new List<SpectrumPeak>(),
            };
            return accessor.GetContent(spot, msdec);
        }

        private class TestAnalysisMetadataAccessor : BaseAnalysisMetadataAccessor
        {
            public TestAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter, ExportspectraType type) : base(refer, parameter, type) {

            }
        }

        private class TestMetadataAccessor : BaseMetadataAccessor
        {
            public TestMetadataAccessor(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter) : base(refer, parameter) {

            }
        }

        private class MockRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
        {
            public string Key => "Mock";

            public MoleculeMsReference Refer(MsScanMatchResult result) {
                if (result is null || result.IsUnknown) {
                    return null;
                }
                return new MoleculeMsReference
                {
                    Formula = new Formula { FormulaString = "C5H10O2", },
                    Ontology = "FA",
                    InChIKey = "inchikey",
                    SMILES = "smiles",
                };
            }
        }

        private class MockDataProvider : IDataProvider
        {
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
                return new List<RawSpectrum>
                {
                    new RawSpectrum
                    {
                        OriginalIndex = 2,
                        Spectrum = new[] { new RawPeakElement { Mz = 700d, Intensity = 1000d, }, },
                    },
                }.AsReadOnly();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
                return Task.FromResult(LoadMs1Spectrums());
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
                throw new System.NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
                throw new System.NotImplementedException();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
                throw new System.NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
                throw new System.NotImplementedException();
            }
        }
    }
}
