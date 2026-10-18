using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Export.Tests
{
    /// <summary>
    /// Header/content parity for the two GC-MS accessors, neither of which had any test at all.
    /// </summary>
    /// <remarks>
    /// This is where a mismatch was most likely to reach a user. GcmsAnalysisMetadataAccessor is the
    /// only accessor in the tree whose header array AND content dictionary are both hand-written and
    /// local -- everywhere else the content comes from a shared base -- so the two can drift apart
    /// with nothing to notice. A header with no key throws KeyNotFoundException the moment
    /// AnalysisCSVExporter indexes the content by header name, which would have meant a GC-MS peak
    /// export failing outright; a key with no header is silently dropped, which is quieter and worse.
    ///
    /// GcmsAlignmentMetadataAccessor is the opposite shape: it replaces the base header list and
    /// keeps the base content, so it deliberately drops keys. Those drops are declared here with
    /// reasons. Declaring them is the point -- it does not forbid dropping a key, it forbids
    /// dropping one by accident.
    /// </remarks>
    [TestClass()]
    public class GcmsMetadataAccessorParityTests
    {
        [TestMethod()]
        public void TheAnalysisAccessorsHeadersAndContentAgree() {
            var accessor = new GcmsAnalysisMetadataAccessor(new MockRefer(), new DelegateMsScanPropertyLoader<SpectrumFeature>(f => f.AnnotatedMSDecResult.MSDecResult));

            var content = accessor.GetContent(Feature());

            foreach (var header in accessor.GetHeaders()) {
                Assert.IsTrue(content.ContainsKey(header),
                    $"header \"{header}\" has no content key, so a GC-MS peak export would throw");
            }
            foreach (var key in content.Keys) {
                Assert.IsTrue(accessor.GetHeaders().Contains(key),
                    $"content key \"{key}\" has no header, so its value never reaches the file");
            }
        }

        [TestMethod()]
        public void TheAnalysisAccessorWritesTheEvidenceRecord() {
            // GC-MS reads its match result from AnnotatedMSDecResult, whose MessagePack formatter
            // keeps every [Key] member -- unlike the lossy .dcl copy of the same annotation, which
            // comes back with no evidence at all. So these values are real here.
            var accessor = new GcmsAnalysisMetadataAccessor(new MockRefer(), new DelegateMsScanPropertyLoader<SpectrumFeature>(f => f.AnnotatedMSDecResult.MSDecResult));

            var content = accessor.GetContent(Feature(recorded: true));

            Assert.AreEqual("Spectrum|RetentionIndex", content["Measured terms"]);
            Assert.AreEqual("ReferenceSpectrum", content["Evidence source"]);
            Assert.AreEqual("7", content["Candidates found"]);
            Assert.AreEqual("0", content["Candidates reference matched"],
                "zero is a measurement. Note this file renders other absent values as -1, which a "
                + "count may never use: -1 candidates would read as data");
        }

        [TestMethod()]
        public void TheAnalysisAccessorWritesNotRecordedWhenThereIsNoAnnotation() {
            var accessor = new GcmsAnalysisMetadataAccessor(new MockRefer(), new DelegateMsScanPropertyLoader<SpectrumFeature>(f => f.AnnotatedMSDecResult.MSDecResult));

            var content = accessor.GetContent(Feature());

            Assert.AreEqual("null", content["Measured terms"]);
            Assert.AreEqual("null", content["Evidence source"]);
            Assert.AreEqual("null", content["Candidates found"]);
        }

        [TestMethod()]
        public void TheAlignmentAccessorsHeadersAndContentAgree() {
            IMetadataAccessor accessor = new GcmsAlignmentMetadataAccessor(new MockRefer(), new ParameterBase { MachineCategory = Common.Enum.MachineCategory.GCMS, });
            var spot = new AlignmentSpotProperty { TimesCenter = new ChromXs(5.0, ChromXType.RT, ChromXUnit.Min), };

            var content = accessor.GetContent(spot, null);

            // Declared drops: this accessor replaces the base header list with a GC-MS-shaped one,
            // so the base's LC-specific keys arrive and are dropped on purpose. Adding to this list
            // is a decision; arriving here by surprise is a bug.
            // Declared drops: this accessor replaces the base header list with a GC-MS-shaped one,
            // so base keys that mean nothing in electron-ionisation arrive and are dropped on
            // purpose. Adding to this list is a decision; arriving here by surprise is a bug.
            var declaredDrops = new HashSet<string>
            {
                // No precursor in EI, so no adduct and no precursor-mass agreement.
                "Adduct type",
                "m/z similarity",
                "MS/MS assigned",
                // The GC-MS pipeline has no post-curation step and no isotope tracking.
                "Post curation result",
                "Isotope tracking parent ID",
                "Isotope tracking weight number",
                // Replaced by the GC-MS-specific spelling: the deconvoluted EI spectrum is copied
                // to "EI spectrum" below, and the fragment agreement is reported as
                // "Fragment presence %".
                "MS1 isotopic spectrum",
                "MS/MS spectrum",
                "Matched peaks percentage",
                // Genuinely absent with no GC-MS equivalent. Pre-existing, and noted rather than
                // fixed here: adding a column changes the published GC-MS column set, which is a
                // compatibility decision of its own.
                "Matched peaks count",
            };
            foreach (var header in accessor.GetHeaders()) {
                Assert.IsTrue(content.ContainsKey(header),
                    $"header \"{header}\" has no content key, so a GC-MS alignment export would throw");
            }
            var undeclared = content.Keys.Where(k => !declaredDrops.Contains(k) && !accessor.GetHeaders().Contains(k)).ToArray();
            Assert.AreEqual(0, undeclared.Length,
                "content keys with no header, so their values never reach the file: " + string.Join(", ", undeclared));
        }

        [TestMethod()]
        public void TheAlignmentAccessorKeepsTheEvidenceColumns() {
            IMetadataAccessor accessor = new GcmsAlignmentMetadataAccessor(new MockRefer(), new ParameterBase { MachineCategory = Common.Enum.MachineCategory.GCMS, });

            var headers = accessor.GetHeaders();

            foreach (var column in new[] { "Measured terms", "Evidence source", "Candidates found", "Candidates above threshold", "Candidates reference matched", }) {
                CollectionAssert.Contains(headers, column);
            }
        }

        private static SpectrumFeature Feature(bool recorded = false) {
            var msdec = new MSDecResult
            {
                RawSpectrumID = 1,
                ChromXs = new ChromXs(5.0, ChromXType.RT, ChromXUnit.Min),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 73, Intensity = 100, },
                    new SpectrumPeak { Mass = 147, Intensity = 40, },
                },
            };
            var results = new MsScanMatchResultContainer();
            if (recorded) {
                results.AddResults(new List<MsScanMatchResult>
                {
                    new MsScanMatchResult
                    {
                        Source = SourceType.MspDB,
                        AnnotatorID = "MspDB",
                        IsSpectrumMatch = true,
                        TotalScore = 0.9f,
                        MeasuredTerms = MeasuredTerms.Spectrum | MeasuredTerms.RetentionIndex,
                        EvidenceSource = AnnotationEvidenceSource.ReferenceSpectrum,
                        CandidatesFound = 7,
                        CandidatesAboveThreshold = 2,
                        CandidatesReferenceMatched = 0,
                    },
                });
            }
            // The three-argument constructor: the two-argument one leaves Molecule null and the
            // accessor dereferences it unguarded on the very first column.
            var annotated = new AnnotatedMSDecResult(msdec, results, new MoleculeMsReference { Name = "Unknown", });
            var peak = new QuantifiedChromatogramPeak(
                new BaseChromatogramPeakFeature { Mass = 73d, PeakHeightTop = 1000d, PeakAreaAboveZero = 900d, },
                new ChromatogramPeakShape(), 1, 0, 2);
            return new SpectrumFeature(annotated, peak);
        }

        private class MockRefer : IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>
        {
            public string Key => "Mock";
            public MoleculeMsReference? Refer(MsScanMatchResult? result) => null;
        }
    }
}
