using CompMs.Common.MessagePack;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.MsdialCore.Parameter.Tests
{
    /// <summary>
    /// LocalSupportRtWindow is MessagePack key 15, added after projects with keys 0-14 were saved.
    /// MessagePack 1.x gives an absent key its type's default, not the member initialiser, so a
    /// plain float would load as 0 and silently turn the local outlier test off.
    /// </summary>
    [TestClass()]
    public class AutomaticAlignmentRetentionTimeCorrectionParameterTests
    {
        [TestMethod()]
        public void AProjectSavedBeforeTheLocalWindowExistedLoadsWithTheDefaultWindow() {
            var saved = new ParameterWithoutLocalSupportRtWindow { Execute = true, OutlierMadThreshold = 4.5F, ReferenceFileId = 7, };

            var loaded = SaveAndLoadAsProjectsDo<ParameterWithoutLocalSupportRtWindow, AutomaticAlignmentRetentionTimeCorrectionParameter>(saved);

            Assert.AreEqual(AutomaticAlignmentRetentionTimeCorrectionParameter.DefaultLocalSupportRtWindow, loaded.LocalSupportRtWindow);
            Assert.AreEqual(1.5F, loaded.LocalSupportRtWindow);
            Assert.IsNull(loaded.SerializedLocalSupportRtWindow, "the absent key is told apart from a stored value");
            Assert.IsTrue(loaded.Execute);
            Assert.AreEqual(4.5F, loaded.OutlierMadThreshold);
            Assert.AreEqual(7, loaded.ReferenceFileId);
        }

        [TestMethod()]
        public void AProjectSavedWithAWindowKeepsIt_ZeroIncluded() {
            foreach (var window in new[] { 0F, 2.5F, AutomaticAlignmentRetentionTimeCorrectionParameter.DefaultLocalSupportRtWindow }) {
                var saved = new AutomaticAlignmentRetentionTimeCorrectionParameter { LocalSupportRtWindow = window, };

                var loaded = SaveAndLoadAsProjectsDo<AutomaticAlignmentRetentionTimeCorrectionParameter, AutomaticAlignmentRetentionTimeCorrectionParameter>(saved);

                Assert.AreEqual(window, loaded.LocalSupportRtWindow);
            }
        }

        [TestMethod()]
        public void ANewProjectCanStillBeReadByTheTypeBeforeTheLocalWindow() {
            var saved = new AutomaticAlignmentRetentionTimeCorrectionParameter { Execute = true, OutlierMadThreshold = 4.5F, LocalSupportRtWindow = 2.5F, };

            var loaded = SaveAndLoadAsProjectsDo<AutomaticAlignmentRetentionTimeCorrectionParameter, ParameterWithoutLocalSupportRtWindow>(saved);

            Assert.IsTrue(loaded.Execute);
            Assert.AreEqual(4.5F, loaded.OutlierMadThreshold);
        }

        [TestMethod()]
        public void TheDefaultWindowIsOneAndAHalfMinutes() {
            var parameter = new AutomaticAlignmentRetentionTimeCorrectionParameter();
            Assert.AreEqual(1.5F, parameter.LocalSupportRtWindow);
            Assert.AreEqual(1.5F, parameter.SerializedLocalSupportRtWindow);
        }

        private static TLoad SaveAndLoadAsProjectsDo<TSave, TLoad>(TSave saved) {
            using var stream = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(saved, stream);
            stream.Position = 0;
            return MessagePackDefaultHandler.LoadFromStream<TLoad>(stream);
        }

        /// <summary>The parameter as it was serialised before key 15 (LocalSupportRtWindow) existed.</summary>
        [MessagePackObject]
        public sealed class ParameterWithoutLocalSupportRtWindow
        {
            [Key(0)] public bool Execute { get; set; } = false;
            [Key(1)] public int ReferenceFileId { get; set; } = -1;
            [Key(2)] public float RtBinWidth { get; set; } = 0.5F;
            [Key(3)] public float MatchRtTolerance { get; set; } = 0.5F;
            [Key(4)] public int MinimumAnchorCount { get; set; } = 3;
            [Key(5)] public int MaximumAnchorCount { get; set; } = 6;
            [Key(6)] public float MinimumSampleCoverage { get; set; } = 0.5F;
            [Key(7)] public float IntensityQuantile { get; set; } = 0.75F;
            [Key(8)] public float MaximumPeakWidthQuantile { get; set; } = 0.5F;
            [Key(9)] public float MinimumSignalToNoise { get; set; } = 3F;
            [Key(10)] public float MinimumGaussianSimilarity { get; set; } = 0F;
            [Key(11)] public float MinimumIdealSlope { get; set; } = 0F;
            [Key(12)] public float OutlierMadThreshold { get; set; } = 3.5F;
            [Key(13)] public float ReferenceCentralityWeight { get; set; } = 0.35F;
            [Key(14)] public bool InterpolateBlankByAnalyticalOrder { get; set; } = true;
        }
    }
}
