using CompMs.Common.Enum;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.Parameter.Tests
{
    /// <summary>
    /// SampleMaxOverBlankAverage and SampleAverageOverBlankAverage are obsolete but keep MessagePack
    /// keys 4 and 5, so a project that stored values in them still loads, and the fold change that
    /// blank filtering actually used is what comes back.
    /// </summary>
    [TestClass()]
    public class PostProcessBaseParameterTests
    {
        [TestMethod()]
        public void AProjectWithLegacyBlankRatiosKeepsTheFoldChangeItFilteredWith() {
#pragma warning disable CS0618 // The obsolete members are what an existing project carries.
            var saved = new PostProcessBaseParameter {
                SampleMaxOverBlankAverage = 9,
                SampleAverageOverBlankAverage = 8,
                FoldChangeForBlankFiltering = 3,
                BlankFiltering = BlankFiltering.SampleAveOverBlankAve,
            };

            var loaded = MessagePackSerializer.Deserialize<PostProcessBaseParameter>(MessagePackSerializer.Serialize(saved));

            Assert.AreEqual(3f, loaded.FoldChangeForBlankFiltering);
            Assert.AreEqual(BlankFiltering.SampleAveOverBlankAve, loaded.BlankFiltering);
            Assert.AreEqual(9f, loaded.SampleMaxOverBlankAverage, "key 4 stays reserved and round-trips");
            Assert.AreEqual(8f, loaded.SampleAverageOverBlankAverage, "key 5 stays reserved and round-trips");
#pragma warning restore CS0618
        }
    }
}
