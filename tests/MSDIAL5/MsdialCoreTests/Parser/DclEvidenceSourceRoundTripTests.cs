using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.MSDec;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.Parser.Tests
{
    /// <summary>
    /// Pins a known limitation rather than a desired behaviour: the .dcl annotation block cannot
    /// carry the evidence record, so a result written to it and read back loses it.
    /// </summary>
    /// <remarks>
    /// The .dcl annotation block is a fixed byte layout. MsdecResultsWriter.SaveAnnotationInfo
    /// serialises an explicit list of members, MsdecResultsWriter.GetSavedAnnotationDataBytes sums
    /// the byte width of that same list, and MsdecResultsReader sizes its buffer from that function
    /// and reads each field back at a hard-coded offset. There is no version gate on the annotation
    /// layout, so adding a member to the written list would desynchronise the stream and make every
    /// existing .dcl unreadable. That puts extending it out of scope here.
    ///
    /// What this does NOT mean is that GC-MS loses the evidence record. A GC-MS annotation is
    /// stored twice. The primary store is MessagePack: the container travels in
    /// AnnotatedMSDecResult, whose hand-written formatter serialises MsScanMatchResultContainer
    /// through the standard resolver and so keeps every [Key] member, and that is the copy the
    /// exporter reads (GcmsAnalysisMetadataAccessor takes
    /// AnnotatedMSDecResult.MatchResults.Representative) and the copy alignment merges
    /// (DataObjConverter). The .dcl block is the second, lossy copy, reached through
    /// MSDecResult.MspBasedMatchResult and the legacy MSRawID2MspBasedMatchResult dictionary.
    ///
    /// So the loss is real but bounded, and worth having a name for: a reader who takes a GC-MS
    /// annotation from the .dcl-derived dictionary rather than from the container gets scores and
    /// verdicts with no provenance. Whoever version-gates the .dcl layout later will find this
    /// test telling them what it cannot currently carry.
    /// </remarks>
    [TestClass()]
    public class DclEvidenceSourceRoundTripTests
    {
        [TestMethod()]
        public void ADclRoundTripDiscardsTheEvidenceSource() {
            var file = Path.GetTempFileName();
            try {
                var written = Result(AnnotationEvidenceSource.ReferenceSpectrum, MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass);
                MsdecResultsWriter.Write(file, new List<MSDecResult> { written, }, isAnnotationInfoIncluded: true);

                var read = MsdecResultsReader.ReadMSDecResults(file, out _, out _);

                Assert.AreEqual(1, read.Count);
                Assert.AreEqual(AnnotationEvidenceSource.Unspecified, read[0].MspBasedMatchResult.EvidenceSource,
                    "the .dcl annotation block has no field for it, so it comes back not recorded");
                Assert.AreEqual(MeasuredTerms.None, read[0].MspBasedMatchResult.MeasuredTerms,
                    "and the same is true of the measured terms");
                Assert.AreEqual(0.75f, read[0].MspBasedMatchResult.TotalScore,
                    "while the members the layout does carry come back intact, so the loss is "
                    + "specific to the evidence record and not a broken round trip");
                Assert.IsTrue(read[0].MspBasedMatchResult.IsSpectrumMatch,
                    "including the verdict booleans");
                Assert.IsNull(read[0].MspBasedMatchResult.Name,
                    "the block carries no strings at all -- not even Name or InChIKey. It is a "
                    + "scores-and-verdicts store, and the annotation's identity is recovered by "
                    + "looking LibraryID back up in the library. So the evidence record could not "
                    + "live here even if the layout were extended by one field: what a reader of a "
                    + "reloaded .dcl has is scores, not provenance.");
            }
            finally {
                if (File.Exists(file)) {
                    File.Delete(file);
                }
            }
        }

        private static MSDecResult Result(AnnotationEvidenceSource evidence, MeasuredTerms measured) {
            return new MSDecResult
            {
                ScanID = 0,
                Spectrum = new List<SpectrumPeak> { new SpectrumPeak { Mass = 100, Intensity = 500, }, },
                MspBasedMatchResult = new MsScanMatchResult
                {
                    Name = "a name the layout does not carry",
                    Source = SourceType.MspDB,
                    TotalScore = 0.75f,
                    IsSpectrumMatch = true,
                    EvidenceSource = evidence,
                    MeasuredTerms = measured,
                },
            };
        }
    }
}
