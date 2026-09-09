using CompMs.Common.DataObj.Result;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace CompMs.MsdialCore.DataObj.Tests
{
    /// <summary>
    /// A project written by an MS-DIAL that predates the evidence record must still load, and must
    /// not come back asserting evidence nobody established.
    /// </summary>
    /// <remarks>
    /// The evidence record was added as MessagePack keys 39-42 on <see cref="MsScanMatchResult"/>.
    /// Project IO goes through <c>MessagePackDefaultHandler</c>, which uses the stock
    /// <c>StandardResolver</c>: an int-keyed [MessagePackObject] is laid out as an array indexed by
    /// key, so a shorter array is read to its own length and a longer one is skipped past. Nothing
    /// in the repository pinned that behaviour for this type, and the whole safety argument for
    /// adding the members rests on it, so it is pinned here.
    ///
    /// One thing these tests establish that is easy to assume the other way round: a key absent
    /// from a short array is assigned <c>default(T)</c> by the generated deserializer. It does NOT
    /// keep the value a C# property initializer gave it. So a "not recorded" state has to BE the
    /// CLR default -- which is why the two counts here are <c>int?</c>. An <c>int</c> with
    /// <c>= -1</c> reads back as 0 from any project written before key 41, and 0 candidates scored
    /// is a claim about a run, not an absence of one. This test is what caught that.
    ///
    /// The old payload is built by re-encoding a real one at a shorter length rather than checked
    /// in as a binary fixture, because "written before key 39 existed" is then stated in the code
    /// rather than hidden in a blob.
    /// </remarks>
    [TestClass()]
    public class MsScanMatchResultBackwardCompatibilityTests
    {
        [TestMethod()]
        public void AProjectWrittenBeforeTheEvidenceRecordStillLoads() {
            var payload = SerializeAsArrayOfLength(39);

            var actual = MessagePackSerializer.Deserialize<MsScanMatchResult>(payload);

            Assert.IsNotNull(actual, "a shorter array must deserialize, not throw");
            Assert.AreEqual("an old name", actual.Name, "the members that were present must survive");
            Assert.AreEqual(0.7f, actual.TotalScore);
        }

        [TestMethod()]
        public void AProjectWrittenBeforeTheEvidenceRecordAssertsNoEvidence() {
            var payload = SerializeAsArrayOfLength(39);

            var actual = MessagePackSerializer.Deserialize<MsScanMatchResult>(payload);

            Assert.AreEqual(MeasuredTerms.None, actual.MeasuredTerms,
                "an old project must not claim a term was measured");
            Assert.AreEqual(AnnotationEvidenceSource.Unspecified, actual.EvidenceSource,
                "an old project must not claim a kind of evidence");
            Assert.IsNull(actual.CandidatesFound,
                "an old project must not claim zero candidates were scored");
            Assert.IsNull(actual.CandidatesAboveThreshold,
                "an old project must not claim zero candidates passed");
            Assert.IsNull(actual.CandidatesReferenceMatched,
                "an old project must not claim zero candidates were reference matches");
        }

        [TestMethod()]
        public void AProjectWrittenAfterAFutureMemberStillLoads() {
            // The other direction: an older MS-DIAL reading a project written by a newer one. The
            // trailing element stands for a key this build has never heard of.
            var payload = SerializeAsArrayOfLength(60);

            var actual = MessagePackSerializer.Deserialize<MsScanMatchResult>(payload);

            Assert.IsNotNull(actual, "an unknown trailing key must be skipped, not throw");
            Assert.AreEqual("an old name", actual.Name);
        }

        /// <summary>
        /// Re-encodes a real serialized result as an array of <paramref name="length"/> elements:
        /// the payload an MS-DIAL with that many keys would have written.
        /// </summary>
        /// <remarks>
        /// Built by truncating or extending a genuine payload rather than by writing Nil into every
        /// slot, because Nil in a slot the formatter maps to a non-nullable primitive throws
        /// ("code is invalid. code:192 format:nil") -- MS-DIAL never writes that, and a fixture that
        /// does would be testing a payload the program cannot produce. The unmapped hole at key 25
        /// is Nil in the real payload and is copied through as-is.
        /// </remarks>
        private static byte[] SerializeAsArrayOfLength(int length) {
            var full = MessagePackSerializer.Serialize(new MsScanMatchResult {
                Name = "an old name",
                TotalScore = 0.7f,
            });

            var readSize = 0;
            var elements = MessagePackBinary.ReadArrayHeader(full, 0, out readSize);
            var offset = readSize;

            var body = new byte[full.Length + 64];
            var written = MessagePackBinary.WriteArrayHeader(ref body, 0, length);
            for (var key = 0; key < length; key++) {
                if (key < elements) {
                    var block = MessagePackBinary.ReadNextBlock(full, offset);
                    MessagePackBinary.EnsureCapacity(ref body, written, block);
                    System.Array.Copy(full, offset, body, written, block);
                    written += block;
                    offset += block;
                }
                else {
                    // A key this build has never heard of. Nil is safe here precisely because the
                    // formatter has no property to map it to, so it is skipped rather than read.
                    written += MessagePackBinary.WriteNil(ref body, written);
                }
            }
            MessagePackBinary.FastResize(ref body, written);
            return body;
        }
    }
}
