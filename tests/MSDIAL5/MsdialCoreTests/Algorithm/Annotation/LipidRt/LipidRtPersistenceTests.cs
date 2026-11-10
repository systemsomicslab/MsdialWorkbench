using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt.Tests
{
    /// <summary>
    /// The new MessagePack keys: the verdict on ChromatogramPeakFeature (key 56) and AlignmentSpotProperty (key 66), and
    /// the check's parameter on ParameterBase (key 18). A file written before each key must load, and must not come back
    /// claiming a verdict or losing the check's defaults. A peak file must also still open in both directions with the
    /// MS-DIAL 5.6 line, which holds an enum at keys 54 and 55 of ChromatogramPeakFeature.
    /// </summary>
    /// <remarks>
    /// "Written before the key" is a real payload re-encoded shorter, as in MsScanMatchResultBackwardCompatibilityTests: an
    /// int-keyed object is an array indexed by key, so the payload of the version before is the current one without its
    /// last elements.
    /// </remarks>
    [TestClass]
    public class LipidRtPersistenceTests
    {
        private static LipidRtVerdict Verdict() {
            return new LipidRtVerdict
            {
                Outcome = LipidRtOutcome.Confirmed,
                Reason = LipidRtReason.ConfirmedRt,
                Score = 4.6,
                RuleVersion = LipidRtCheckParameter.RuleVersion,
                ParameterHash = "0123456789abcdef",
                CorroboratedByAdduct = true,
                CorroboratedByPolarity = new[] { 7 },
                Evidence = "Confirmed: TG 52:1 elutes after TG 50:1",
            };
        }

        [TestMethod]
        public void APeakRoundTripsItsVerdict() {
            var peak = Peak();
            peak.LipidRtVerdict = Verdict();

            var actual = MessagePackSerializer.Deserialize<ChromatogramPeakFeature>(MessagePackSerializer.Serialize(peak));

            AssertSameVerdict(peak.LipidRtVerdict, actual.LipidRtVerdict);
            Assert.AreEqual(42, actual.MasterPeakID);
        }

        [TestMethod]
        public void APeakWrittenBeforeKey56LoadsWithoutAVerdict() {
            var peak = Peak();
            peak.LipidRtVerdict = Verdict();
            var full = MessagePackSerializer.Serialize(peak);
            Assert.AreEqual(57, MessagePackBinary.ReadArrayHeader(full, 0, out _), "key 56 is the last key");

            var actual = MessagePackSerializer.Deserialize<ChromatogramPeakFeature>(Truncate(full, 54));

            Assert.IsNotNull(actual);
            Assert.AreEqual(42, actual.MasterPeakID, "the members that were present survive");
            Assert.AreEqual(760.5851, actual.PeakFeature.Mass, 1e-9);
            Assert.IsNull(actual.LipidRtVerdict, "an older peak file claims no verdict");
            Assert.AreEqual(0, actual.ReservedRawDataIDType);
        }

        [TestMethod]
        public void APeakFileOfTheMsdial56LineLoadsWithoutAVerdictAndKeepsItsKeys54And55() {
            // A 5.6 peak file: 56 elements, with SpectrumIDType integers (1 = Raw) at keys 54 and 55 and nothing after.
            var peak = Peak();
            peak.ReservedRawDataIDType = 1;
            peak.ReservedAccumulatedDataIDType = 1;
            var msdial56 = Truncate(MessagePackSerializer.Serialize(peak), 56);
            Assert.AreEqual(1, ReadInt32At(msdial56, 54));
            Assert.AreEqual(1, ReadInt32At(msdial56, 55));

            var actual = MessagePackSerializer.Deserialize<ChromatogramPeakFeature>(msdial56);

            Assert.AreEqual(42, actual.MasterPeakID);
            Assert.IsNull(actual.LipidRtVerdict, "the integer at key 54 is not read as a verdict");
            Assert.AreEqual(1, actual.ReservedRawDataIDType);
            Assert.AreEqual(1, actual.ReservedAccumulatedDataIDType);

            var saved = MessagePackSerializer.Serialize(actual);
            Assert.AreEqual(1, ReadInt32At(saved, 54), "saving here keeps what 5.6 wrote");
            Assert.AreEqual(1, ReadInt32At(saved, 55));
        }

        [TestMethod]
        public void APeakFileWrittenHereOpensInTheMsdial56Line() {
            var peak = Peak();
            peak.LipidRtVerdict = Verdict();
            var full = MessagePackSerializer.Serialize(peak);

            var read = MessagePackSerializer.Deserialize<PeakAsMsdial56ReadsIt>(full);

            Assert.AreEqual(Msdial56SpectrumIDType.Index, read.RawDataIDType, "the raw spectrum IDs here are indices");
            Assert.AreEqual(Msdial56SpectrumIDType.Index, read.AccumulatedDataIDType);
            Assert.AreEqual(42, read.MasterPeakID);
        }

        [TestMethod]
        public void TheMsdial56LineCannotReadNilAtKey54() {
            // Why keys 54 and 55 are written as integers rather than left to MessagePack, which writes nil for a key with
            // no member: the 5.6 reader's enum formatter does not accept nil, so such a file would not open there.
            var withNil = MessagePackSerializer.Serialize(new PeakWithNothingAtKeys54And55 { MasterPeakID = 42 });
            Assert.AreEqual(MessagePackType.Nil, MessagePackBinary.GetMessagePackType(withNil, OffsetOf(withNil, 54)));

            Assert.ThrowsException<System.InvalidOperationException>(() => MessagePackSerializer.Deserialize<PeakAsMsdial56ReadsIt>(withNil));
        }

        [TestMethod]
        public void ASpotRoundTripsItsVerdict() {
            var spot = Spot();
            spot.LipidRtVerdict = Verdict();

            var actual = MessagePackSerializer.Deserialize<AlignmentSpotProperty>(MessagePackSerializer.Serialize(spot));

            AssertSameVerdict(spot.LipidRtVerdict, actual.LipidRtVerdict);
        }

        [TestMethod]
        public void ASpotWrittenBeforeKey66LoadsWithoutAVerdict() {
            var spot = Spot();
            spot.LipidRtVerdict = Verdict();
            var full = MessagePackSerializer.Serialize(spot);
            Assert.AreEqual(67, MessagePackBinary.ReadArrayHeader(full, 0, out _), "key 66 is the last key");

            var actual = MessagePackSerializer.Deserialize<AlignmentSpotProperty>(Truncate(full, 66));

            Assert.IsNotNull(actual);
            Assert.AreEqual(7, actual.MasterAlignmentID);
            Assert.IsTrue(actual.IsRmdFilteredByPostCurator, "key 65, the last key before, survives");
            Assert.IsNull(actual.LipidRtVerdict);
        }

        [TestMethod]
        public void TheParameterRoundTripsOnParameterBase() {
            var parameter = new ParameterBase();
            parameter.LipidRtCheckParam.IsEnabled = false;
            parameter.LipidRtCheckParam.PlausibilityProfile = LipidRtPlausibilityProfileKind.Mammalian;
            parameter.LipidRtCheckParam.ClassLevelPolicy = LipidRtClassLevelPolicy.Consistent;

            var actual = MessagePackSerializer.Deserialize<ParameterBase>(MessagePackSerializer.Serialize(parameter));

            Assert.AreEqual(false, actual.LipidRtCheckParam.IsEnabled);
            Assert.AreEqual(LipidRtPlausibilityProfileKind.Mammalian, actual.LipidRtCheckParam.PlausibilityProfile);
            Assert.AreEqual(LipidRtClassLevelPolicy.Consistent, actual.LipidRtCheckParam.ClassLevelPolicy);
            Assert.AreEqual(parameter.LipidRtCheckParam.ComputeHash(), actual.LipidRtCheckParam.ComputeHash());
        }

        [TestMethod]
        public void AProjectWrittenBeforeKey18GetsTheDefaults() {
            var parameter = new ParameterBase { TargetOmics = TargetOmics.Lipidomics };
            parameter.LipidRtCheckParam.IsEnabled = false;
            var full = MessagePackSerializer.Serialize(parameter);
            Assert.AreEqual(19, MessagePackBinary.ReadArrayHeader(full, 0, out _), "key 18 is the last key of ParameterBase");

            var actual = MessagePackSerializer.Deserialize<ParameterBase>(Truncate(full, 18));

            Assert.AreEqual(TargetOmics.Lipidomics, actual.TargetOmics);
            Assert.IsNotNull(actual.LipidRtCheckParam, "never null: an older project gets the defaults");
            Assert.IsNull(actual.LipidRtCheckParam.IsEnabled);
            Assert.IsTrue(actual.LipidRtCheckParam.RunsFor(actual.TargetOmics), "an older lipidomics project runs the check");
            Assert.AreEqual(new LipidRtCheckParameter().ComputeHash(), actual.LipidRtCheckParam.ComputeHash());
        }

        [TestMethod]
        public void TheCheckRunsOnlyForLipidomics() {
            var parameter = new LipidRtCheckParameter();
            Assert.IsTrue(parameter.RunsFor(TargetOmics.Lipidomics));
            Assert.IsFalse(parameter.RunsFor(TargetOmics.Metabolomics));
            parameter.IsEnabled = true;
            Assert.IsFalse(parameter.RunsFor(TargetOmics.Metabolomics), "the rules are lipid rules, whatever the switch says");
            parameter.IsEnabled = false;
            Assert.IsFalse(parameter.RunsFor(TargetOmics.Lipidomics));
        }

        [TestMethod]
        public void TheParameterTextUsesTheMethodFileKeys() {
            var lines = new LipidRtCheckParameter().ParametersAsText(TargetOmics.Lipidomics);
            CollectionAssert.Contains(lines, "Lipid RT check: Auto");
            CollectionAssert.Contains(lines, "Lipid RT check operating point: HighPrecision");
            CollectionAssert.Contains(lines, "Lipid RT check chromatography: Auto");
            CollectionAssert.Contains(lines, "Lipid RT check plausibility profile: None");
            CollectionAssert.Contains(lines, "Lipid RT check class-level matches: Corroborated");
            CollectionAssert.Contains(lines, "Lipid RT check minor duplicates: Allow");
            CollectionAssert.Contains(lines, "Lipid RT check secondary-only features: Evaluate");
            CollectionAssert.Contains(lines, "Lipid RT check runs for this project: True");
            CollectionAssert.Contains(lines, "Lipid RT check rule version: 1.1");
            CollectionAssert.Contains(lines, "Lipid RT check parameter hash: " + new LipidRtCheckParameter().ComputeHash());
            CollectionAssert.Contains(new LipidRtCheckParameter { IsEnabled = false }.ParametersAsText(TargetOmics.Lipidomics), "Lipid RT check: False");
        }

        private static void AssertSameVerdict(LipidRtVerdict expected, LipidRtVerdict actual) {
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Outcome, actual.Outcome);
            Assert.AreEqual(expected.Reason, actual.Reason);
            Assert.AreEqual(expected.Score, actual.Score);
            Assert.AreEqual(expected.RuleVersion, actual.RuleVersion);
            Assert.AreEqual(expected.ParameterHash, actual.ParameterHash);
            Assert.AreEqual(expected.CorroboratedByAdduct, actual.CorroboratedByAdduct);
            CollectionAssert.AreEqual(expected.CorroboratedByPolarity, actual.CorroboratedByPolarity);
            Assert.AreEqual(expected.Evidence, actual.Evidence);
        }

        private static ChromatogramPeakFeature Peak() {
            return new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
            {
                Mass = 760.5851, PeakHeightTop = 1e6, ChromXsLeft = new ChromXs(10.4), ChromXsTop = new ChromXs(10.5), ChromXsRight = new ChromXs(10.6),
            })
            {
                MasterPeakID = 42,
                Name = "PC 34:1",
                Ontology = "PC",
            };
        }

        private static AlignmentSpotProperty Spot() {
            return new AlignmentSpotProperty
            {
                MasterAlignmentID = 7,
                AlignmentID = 7,
                TimesCenter = new ChromXs(12.25),
                MassCenter = 868.7372,
                Name = "TG 52:2",
                IsRmdFilteredByPostCurator = true,
            };
        }

        /// <summary>The key of ChromatogramPeakFeature.MasterPeakID, the same in both lines.</summary>
        private const int MasterPeakIdKey = 11;

        /// <summary>SpectrumIDType of the MS-DIAL 5.6 line, which this line does not have.</summary>
        public enum Msdial56SpectrumIDType
        {
            Index,
            Raw,
        }

        /// <summary>What a reader of the MS-DIAL 5.6 line reads at keys 54 and 55 of a peak, and one key before them.</summary>
        [MessagePackObject]
        public sealed class PeakAsMsdial56ReadsIt
        {
            [Key(MasterPeakIdKey)]
            public int MasterPeakID { get; set; }
            [Key(54)]
            public Msdial56SpectrumIDType RawDataIDType { get; set; }
            [Key(55)]
            public Msdial56SpectrumIDType AccumulatedDataIDType { get; set; }
        }

        /// <summary>
        /// A peak with no member at keys 54 and 55 and one after them, as ChromatogramPeakFeature would be with the verdict
        /// at key 56 and no reserved keys.
        /// </summary>
        [MessagePackObject]
        public sealed class PeakWithNothingAtKeys54And55
        {
            [Key(MasterPeakIdKey)]
            public int MasterPeakID { get; set; }
            [Key(56)]
            public LipidRtVerdict LipidRtVerdict { get; set; }
        }

        private static int OffsetOf(byte[] payload, int key) {
            MessagePackBinary.ReadArrayHeader(payload, 0, out var offset);
            for (var i = 0; i < key; i++) {
                offset += MessagePackBinary.ReadNextBlock(payload, offset);
            }
            return offset;
        }

        private static int ReadInt32At(byte[] payload, int key) {
            return MessagePackBinary.ReadInt32(payload, OffsetOf(payload, key), out _);
        }

        private static byte[] Truncate(byte[] full, int length) {
            var elements = MessagePackBinary.ReadArrayHeader(full, 0, out var readSize);
            var offset = readSize;
            var body = new byte[full.Length + 16];
            var written = MessagePackBinary.WriteArrayHeader(ref body, 0, length);
            for (var key = 0; key < length && key < elements; key++) {
                var block = MessagePackBinary.ReadNextBlock(full, offset);
                MessagePackBinary.EnsureCapacity(ref body, written, block);
                System.Array.Copy(full, offset, body, written, block);
                written += block;
                offset += block;
            }
            MessagePackBinary.FastResize(ref body, written);
            return body;
        }
    }
}
