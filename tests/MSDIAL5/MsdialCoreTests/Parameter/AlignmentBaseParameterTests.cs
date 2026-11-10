using CompMs.Common.MessagePack;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Parameter.Tests
{
    /// <summary>
    /// AutomaticRtCorrection is MessagePack key 10, added after projects with keys 0-9 were saved. MessagePack 1.x
    /// gives an absent key its type's default, not the member initialiser, so the nested parameter loaded as null and
    /// the parameter export (and the GUI's alignment view) threw on an older project.
    /// </summary>
    [TestClass()]
    public class AlignmentBaseParameterTests
    {
        [TestMethod()]
        public void AProjectSavedBeforeAutomaticRtCorrectionExistedLoadsWithItsDefaults() {
            var saved = new ParameterWithoutAutomaticRtCorrection { AlignmentReferenceFileID = 3, RetentionTimeAlignmentTolerance = 0.2F, TogetherWithAlignment = false, };

            var loaded = SaveAndLoadAsProjectsDo<ParameterWithoutAutomaticRtCorrection, AlignmentBaseParameter>(saved);

            Assert.IsNotNull(loaded.AutomaticRtCorrection);
            Assert.IsFalse(loaded.AutomaticRtCorrection.Execute, "an older project aligns without automatic RT correction, as it was saved");
            Assert.AreEqual(new AutomaticAlignmentRetentionTimeCorrectionParameter().LocalSupportRtWindow, loaded.AutomaticRtCorrection.LocalSupportRtWindow);
            Assert.AreSame(loaded.AutomaticRtCorrection, loaded.AutomaticRtCorrection, "the defaults are created once and kept");
            Assert.AreEqual(3, loaded.AlignmentReferenceFileID);
            Assert.AreEqual(0.2F, loaded.RetentionTimeAlignmentTolerance);
            Assert.IsFalse(loaded.TogetherWithAlignment);
        }

        [TestMethod()]
        public void AnOlderProjectExportsItsParameters() {
            var loaded = SaveAndLoadAsProjectsDo<ParameterWithoutAutomaticRtCorrection, AlignmentBaseParameter>(new ParameterWithoutAutomaticRtCorrection());
            var parameter = new ParameterBase { AlignmentBaseParam = loaded, };

            var text = parameter.ParametersAsText();

            Assert.IsTrue(text.Contains("Execute automatic RT correction for alignment: False"));
        }

        [TestMethod()]
        public void AStoredAutomaticRtCorrectionIsKept() {
            var saved = new AlignmentBaseParameter();
            saved.AutomaticRtCorrection.Execute = true;
            saved.AutomaticRtCorrection.ReferenceFileId = 5;

            var loaded = SaveAndLoadAsProjectsDo<AlignmentBaseParameter, AlignmentBaseParameter>(saved);

            Assert.IsTrue(loaded.AutomaticRtCorrection.Execute);
            Assert.AreEqual(5, loaded.AutomaticRtCorrection.ReferenceFileId);
        }

        [TestMethod()]
        public void SettingNullHandsOutTheDefaults() {
            var parameter = new AlignmentBaseParameter { AutomaticRtCorrection = null, };

            Assert.IsNotNull(parameter.AutomaticRtCorrection);
            Assert.IsFalse(parameter.AutomaticRtCorrection.Execute);
        }

        private static TLoad SaveAndLoadAsProjectsDo<TSave, TLoad>(TSave saved) {
            using var stream = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(saved, stream);
            stream.Position = 0;
            return MessagePackDefaultHandler.LoadFromStream<TLoad>(stream);
        }

        /// <summary>The parameter as it was serialised before key 10 (AutomaticRtCorrection) existed.</summary>
        [MessagePackObject]
        public sealed class ParameterWithoutAutomaticRtCorrection
        {
            [Key(0)] public int AlignmentReferenceFileID { get; set; } = 0;
            [Key(1)] public float RetentionTimeAlignmentFactor { get; set; } = 0.5F;
            [Key(2)] public float Ms1AlignmentFactor { get; set; } = 0.5F;
            [Key(3)] public float SpectrumSimilarityAlignmentFactor { get; set; } = 0.5F;
            [Key(4)] public float Ms1AlignmentTolerance { get; set; } = 0.015F;
            [Key(5)] public float RetentionTimeAlignmentTolerance { get; set; } = 0.10F;
            [Key(6)] public float SpectrumSimilarityAlignmentTolerance { get; set; } = 0.8F;
            [Key(7)] public float AlignmentScoreCutOff { get; set; } = 50;
            [Key(8)] public bool TogetherWithAlignment { get; set; } = true;
            [Key(9)] public bool UseRefMatchedPeaksOnly { get; set; } = false;
        }
    }
}
