using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt.Tests
{
    /// <summary>
    /// The persisted parts of the lipid RT check: the parameter (for ParameterBase) and the verdict record.
    /// </summary>
    /// <remarks>
    /// Both are int-keyed MessagePack objects, laid out as arrays: a shorter array is read to its own length and a key
    /// absent from it is assigned default(T), not the property initializer (see
    /// MsScanMatchResultBackwardCompatibilityTests). So "not recorded" must be the CLR default, which these pin.
    /// </remarks>
    [TestClass]
    public class LipidRtSerializationTests
    {
        [TestMethod]
        public void TheParameterRoundTrips() {
            var parameter = new LipidRtCheckParameter
            {
                IsEnabled = false,
                OperatingPoint = LipidRtOperatingPoint.Mid,
                Chromatography = LipidRtChromatography.ReversedPhase,
                PlausibilityProfile = LipidRtPlausibilityProfileKind.Mammalian,
                ClassLevelPolicy = LipidRtClassLevelPolicy.Never,
                MinorDuplicatePolicy = LipidRtMinorDuplicatePolicy.Block,
                SecondaryOnlyPolicy = LipidRtSecondaryOnlyPolicy.Block,
                CrossPolarity = false,
                DiscordanceFraction = 0.003,
                MagnitudeK = 0.5,
                ArtefactKinds = LipidRtArtefactKinds.Default | LipidRtArtefactKinds.Water,
                PromotableReasons = new List<int> { 14 },
                PolarityConflict = LipidRtPolarityConflictPolicy.Demote,
                MaxEvidenceItems = 6,
            };
            var actual = MessagePackSerializer.Deserialize<LipidRtCheckParameter>(MessagePackSerializer.Serialize(parameter));
            Assert.AreEqual(false, actual.IsEnabled);
            Assert.AreEqual(LipidRtOperatingPoint.Mid, actual.OperatingPoint);
            Assert.AreEqual(LipidRtChromatography.ReversedPhase, actual.Chromatography);
            Assert.AreEqual(LipidRtPlausibilityProfileKind.Mammalian, actual.PlausibilityProfile);
            Assert.AreEqual(LipidRtClassLevelPolicy.Never, actual.ClassLevelPolicy);
            Assert.AreEqual(LipidRtMinorDuplicatePolicy.Block, actual.MinorDuplicatePolicy);
            Assert.AreEqual(LipidRtSecondaryOnlyPolicy.Block, actual.SecondaryOnlyPolicy);
            Assert.IsFalse(actual.CrossPolarity);
            Assert.AreEqual(0.003, actual.DiscordanceFraction);
            Assert.AreEqual(0.5, actual.MagnitudeK);
            Assert.AreEqual(LipidRtArtefactKinds.Default | LipidRtArtefactKinds.Water, actual.ArtefactKinds);
            CollectionAssert.AreEqual(new List<int> { 14 }, actual.PromotableReasons);
            Assert.AreEqual(LipidRtPolarityConflictPolicy.Demote, actual.PolarityConflict);
            Assert.AreEqual(6, actual.MaxEvidenceItems);
            Assert.AreEqual(parameter.ComputeHash(), actual.ComputeHash());
        }

        [TestMethod]
        public void IsEnabledDefaultsToTheProjectsTargetOmics() {
            var parameter = new LipidRtCheckParameter();
            Assert.IsNull(parameter.IsEnabled);
            Assert.IsTrue(parameter.IsEnabledFor(TargetOmics.Lipidomics));
            Assert.IsFalse(parameter.IsEnabledFor(TargetOmics.Metabolomics));
            parameter.IsEnabled = true;
            Assert.IsTrue(parameter.IsEnabledFor(TargetOmics.Metabolomics), "an explicit value wins");
            parameter.IsEnabled = false;
            Assert.IsFalse(parameter.IsEnabledFor(TargetOmics.Lipidomics));
        }

        [TestMethod]
        public void TheHashFollowsTheValuesThatChangeAnOutcome() {
            var a = new LipidRtCheckParameter();
            var b = new LipidRtCheckParameter();
            Assert.AreEqual(a.ComputeHash(), b.ComputeHash());
            b.IsEnabled = true;
            Assert.AreEqual(a.ComputeHash(), b.ComputeHash(), "whether the check runs is not what it concludes");
            b.ClassLevelPolicy = LipidRtClassLevelPolicy.Consistent;
            Assert.AreNotEqual(a.ComputeHash(), b.ComputeHash());
            var c = new LipidRtCheckParameter { MagnitudeK = 0.7 };
            Assert.AreNotEqual(a.ComputeHash(), c.ComputeHash());
            var d = new LipidRtCheckParameter { PromotableReasons = new List<int> { 13, 12, 14 } };
            Assert.AreEqual(a.ComputeHash(), d.ComputeHash(), "an explicit default promotable set is the default");
        }

        [TestMethod]
        public void TheVerdictRoundTrips() {
            var verdict = new LipidRtVerdict
            {
                Outcome = LipidRtOutcome.Confirmed,
                Reason = LipidRtReason.ConfirmedPromotedByPolarity,
                Score = 2.75,
                RuleVersion = "1.0",
                ParameterHash = "0123456789abcdef",
                CorroboratedByAdduct = true,
                CorroboratedByPolarity = new[] { 12, 34 },
                Evidence = "Confirmed in the other polarity",
            };
            var actual = MessagePackSerializer.Deserialize<LipidRtVerdict>(MessagePackSerializer.Serialize(verdict));
            Assert.AreEqual(LipidRtOutcome.Confirmed, actual.Outcome);
            Assert.AreEqual(LipidRtReason.ConfirmedPromotedByPolarity, actual.Reason);
            Assert.AreEqual(2.75, actual.Score);
            Assert.AreEqual("1.0", actual.RuleVersion);
            Assert.AreEqual("0123456789abcdef", actual.ParameterHash);
            Assert.IsTrue(actual.CorroboratedByAdduct);
            CollectionAssert.AreEqual(new[] { 12, 34 }, actual.CorroboratedByPolarity);
            Assert.AreEqual("Confirmed in the other polarity", actual.Evidence);
        }

        [TestMethod]
        public void ANotEvaluatedScoreStaysNull() {
            var verdict = new LipidRtVerdict { Outcome = LipidRtOutcome.NotEvaluable, Reason = LipidRtReason.Ineligible, Score = null };
            var actual = MessagePackSerializer.Deserialize<LipidRtVerdict>(MessagePackSerializer.Serialize(verdict));
            Assert.IsNull(actual.Score, "an object that was not evaluated has no score, which is not a score of 0");
        }

        [TestMethod]
        public void AShorterVerdictReadsAsNotRecorded() {
            var full = MessagePackSerializer.Serialize(new LipidRtVerdict { Outcome = LipidRtOutcome.Confirmed, Reason = LipidRtReason.ConfirmedRt, Score = 1.5 });
            var actual = MessagePackSerializer.Deserialize<LipidRtVerdict>(Truncate(full, 3));
            Assert.AreEqual(LipidRtOutcome.Confirmed, actual.Outcome);
            Assert.AreEqual(1.5, actual.Score);
            Assert.IsNull(actual.RuleVersion);
            Assert.IsFalse(actual.CorroboratedByAdduct);
            Assert.IsNull(actual.CorroboratedByPolarity);
        }

        [TestMethod]
        public void AnEmptyVerdictReadsAsNotEvaluated() {
            var full = MessagePackSerializer.Serialize(new LipidRtVerdict());
            var actual = MessagePackSerializer.Deserialize<LipidRtVerdict>(Truncate(full, 0));
            Assert.AreEqual(LipidRtOutcome.NotEvaluated, actual.Outcome);
            Assert.AreEqual(LipidRtReason.None, actual.Reason);
            Assert.IsNull(actual.Score);
        }

        [TestMethod]
        public void AShorterParameterReadsWithTheProjectDefaultForEnabled() {
            var full = MessagePackSerializer.Serialize(new LipidRtCheckParameter());
            var actual = MessagePackSerializer.Deserialize<LipidRtCheckParameter>(Truncate(full, 1));
            Assert.IsNull(actual.IsEnabled, "a missing switch means the default for the project's target omics");
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
