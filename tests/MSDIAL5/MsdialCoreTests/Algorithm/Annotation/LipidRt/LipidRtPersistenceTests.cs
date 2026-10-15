using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt.Tests
{
    /// <summary>
    /// The new MessagePack keys: the verdict on ChromatogramPeakFeature (key 54) and AlignmentSpotProperty (key 66), and
    /// the check's parameter on ParameterBase (key 18). A file written before each key must load, and must not come back
    /// claiming a verdict or losing the check's defaults.
    /// </summary>
    /// <remarks>
    /// "Written before the key" is a real payload re-encoded one element shorter, as in
    /// MsScanMatchResultBackwardCompatibilityTests: an int-keyed object is an array indexed by key, so the payload of the
    /// version before is the current one without its last element.
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
        public void APeakWrittenBeforeKey54LoadsWithoutAVerdict() {
            var peak = Peak();
            peak.LipidRtVerdict = Verdict();
            var full = MessagePackSerializer.Serialize(peak);
            Assert.AreEqual(55, MessagePackBinary.ReadArrayHeader(full, 0, out _), "key 54 is the last key");

            var actual = MessagePackSerializer.Deserialize<ChromatogramPeakFeature>(Truncate(full, 54));

            Assert.IsNotNull(actual);
            Assert.AreEqual(42, actual.MasterPeakID, "the members that were present survive");
            Assert.AreEqual(760.5851, actual.PeakFeature.Mass, 1e-9);
            Assert.IsNull(actual.LipidRtVerdict, "an older peak file claims no verdict");
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
            CollectionAssert.Contains(lines, "Lipid RT check rule version: 1.0");
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
