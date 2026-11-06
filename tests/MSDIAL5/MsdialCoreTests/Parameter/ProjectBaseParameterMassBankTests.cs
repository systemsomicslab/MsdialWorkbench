using CompMs.Common.MessagePack;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace CompMs.MsdialCore.Parameter.Tests
{
    /// <summary>
    /// "Constrain to MassBank format" (key 24) and the MassBank contributor ID (key 25) of <see cref="ProjectBaseParameter"/>:
    /// they are saved with the project, and a project saved before they existed opens with the constraint off and no
    /// contributor ID, as it behaved before.
    /// </summary>
    /// <remarks>
    /// ProjectBaseParameter is an int-keyed MessagePack object laid out as an array indexed by key, so a project
    /// written before key 24 holds an array of 24 elements, and an absent key is assigned default(T) rather than the
    /// property initializer (see MsScanMatchResultBackwardCompatibilityTests). The old payload is made here by
    /// re-encoding a real one at the shorter length, so that "written before key 24" is stated in the code.
    /// </remarks>
    [TestClass()]
    public class ProjectBaseParameterMassBankTests
    {
        private const int ElementsBeforeMassBankKeys = 24;

        [TestMethod()]
        public void TheMassBankMembersRoundTrip() {
            var saved = Metadata(new ProjectBaseParameter());
            saved.ConstrainToMassBankFormat = true;
            saved.MassBankContributorId = "MSDIAL_TEST";

            var loaded = MessagePackSerializer.Deserialize<ProjectBaseParameter>(MessagePackSerializer.Serialize(saved));

            Assert.IsTrue(loaded.ConstrainToMassBankFormat);
            Assert.AreEqual("MSDIAL_TEST", loaded.MassBankContributorId);
            Assert.AreEqual("LC-ESI-QTOF", loaded.InstrumentType);
        }

        [TestMethod()]
        public void TheMassBankMembersRoundTripWithTheProject() {
            var saved = new ParameterBase();
            Metadata(saved.ProjectParam);
            saved.ConstrainToMassBankFormat = true;
            saved.MassBankContributorId = "MSDIAL_TEST";

            ParameterBase loaded;
            using (var stream = new MemoryStream()) {
                MessagePackDefaultHandler.SaveToStream(saved, stream);
                stream.Position = 0;
                loaded = MessagePackDefaultHandler.LoadFromStream<ParameterBase>(stream);
            }

            Assert.IsTrue(loaded.ConstrainToMassBankFormat);
            Assert.AreEqual("MSDIAL_TEST", loaded.MassBankContributorId);
            Assert.IsTrue(loaded.ProjectParam.ConstrainToMassBankFormat);
            Assert.AreEqual("MSDIAL_TEST", loaded.ProjectParam.MassBankContributorId);
        }

        [TestMethod()]
        public void ANewProjectIsNotConstrainedAndHasNoContributorId() {
            var parameter = new ProjectBaseParameter();
            Assert.IsFalse(parameter.ConstrainToMassBankFormat);
            Assert.AreEqual(string.Empty, parameter.MassBankContributorId);
            parameter.MassBankContributorId = null!;
            Assert.AreEqual(string.Empty, parameter.MassBankContributorId, "never null");
        }

        [TestMethod()]
        public void AProjectSavedBeforeTheMassBankKeysOpensUnconstrainedWithNoContributorId() {
            var full = MessagePackSerializer.Serialize(Metadata(new ProjectBaseParameter { ConstrainToMassBankFormat = true, MassBankContributorId = "WOULD_BE_NEW", }));
            var old = ReencodeArray(full, 0, ElementsBeforeMassBankKeys);
            Assert.AreEqual(ElementsBeforeMassBankKeys, MessagePackBinary.ReadArrayHeader(old, 0, out _), "the payload an MS-DIAL without keys 24 and 25 wrote");

            var loaded = MessagePackSerializer.Deserialize<ProjectBaseParameter>(old);

            Assert.IsFalse(loaded.ConstrainToMassBankFormat, "an old project keeps the free-text behaviour");
            Assert.AreEqual(string.Empty, loaded.MassBankContributorId, "an old project has no contributor ID, and not null");
            Assert.AreEqual("LC-ESI-QTOF", loaded.InstrumentType, "the members that were present survive");
            Assert.AreEqual("MS-DIAL test", loaded.Authors);
            Assert.AreEqual("CC BY", loaded.License);
            Assert.AreEqual("Saved before key 24", loaded.Comment);
            Assert.IsTrue(loaded.SpectrumCommentToColorBytes.Count > 0, "key 23, the last one an old project has, survives");
        }

        [TestMethod()]
        public void AWholeProjectSavedBeforeTheMassBankKeysOpensUnconstrained() {
            var saved = new ParameterBase();
            Metadata(saved.ProjectParam);
            saved.ConstrainToMassBankFormat = true;
            saved.MassBankContributorId = "WOULD_BE_NEW";
            var full = MessagePackSerializer.Serialize(saved);
            // ParameterBase keeps ProjectBaseParameter at key 0: shorten that element and keep the rest as written.
            var old = ReencodeFirstElement(full, element => ReencodeArray(element, 0, ElementsBeforeMassBankKeys));

            var loaded = MessagePackSerializer.Deserialize<ParameterBase>(old);

            Assert.IsFalse(loaded.ConstrainToMassBankFormat);
            Assert.AreEqual(string.Empty, loaded.MassBankContributorId);
            Assert.AreEqual("LC-ESI-QTOF", loaded.InstrumentType);
            Assert.AreEqual(saved.MspFilePath, loaded.MspFilePath, "the rest of the project is read as written");
        }

        private static ProjectBaseParameter Metadata(ProjectBaseParameter parameter) {
            parameter.InstrumentType = "LC-ESI-QTOF";
            parameter.Instrument = "TripleTOF 6600, SCIEX";
            parameter.Authors = "MS-DIAL test";
            parameter.License = "CC BY";
            parameter.CollisionEnergy = "35 +/- 15 V";
            parameter.Comment = "Saved before key 24";
            return parameter;
        }

        /// <summary>
        /// Re-encodes the array at <paramref name="offset"/> of <paramref name="payload"/> with its first
        /// <paramref name="length"/> elements, the payload an MS-DIAL with that many keys would have written.
        /// </summary>
        private static byte[] ReencodeArray(byte[] payload, int offset, int length) {
            var elements = MessagePackBinary.ReadArrayHeader(payload, offset, out var readSize);
            Assert.IsTrue(elements >= length, "the payload has the members being removed");
            offset += readSize;
            var body = new byte[payload.Length + 16];
            var written = MessagePackBinary.WriteArrayHeader(ref body, 0, length);
            for (var key = 0; key < length; key++) {
                var block = MessagePackBinary.ReadNextBlock(payload, offset);
                MessagePackBinary.EnsureCapacity(ref body, written, block);
                Array.Copy(payload, offset, body, written, block);
                written += block;
                offset += block;
            }
            MessagePackBinary.FastResize(ref body, written);
            return body;
        }

        /// <summary>Replaces the first element of the array in <paramref name="payload"/> with what <paramref name="replace"/> makes of it.</summary>
        private static byte[] ReencodeFirstElement(byte[] payload, Func<byte[], byte[]> replace) {
            var elements = MessagePackBinary.ReadArrayHeader(payload, 0, out var readSize);
            var offset = readSize;
            var firstSize = MessagePackBinary.ReadNextBlock(payload, offset);
            var first = new byte[firstSize];
            Array.Copy(payload, offset, first, 0, firstSize);
            var replaced = replace(first);

            var body = new byte[payload.Length + replaced.Length];
            var written = MessagePackBinary.WriteArrayHeader(ref body, 0, elements);
            Array.Copy(replaced, 0, body, written, replaced.Length);
            written += replaced.Length;
            var rest = payload.Length - offset - firstSize;
            Array.Copy(payload, offset + firstSize, body, written, rest);
            written += rest;
            MessagePackBinary.FastResize(ref body, written);
            return body;
        }
    }
}
