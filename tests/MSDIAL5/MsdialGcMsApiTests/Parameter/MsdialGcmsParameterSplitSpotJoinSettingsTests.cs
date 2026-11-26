using CompMs.MsdialGcMsApi.DataObj;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Buffers;
using System.IO;

namespace CompMs.MsdialGcMsApi.Parameter.Tests
{
    /// <summary>
    /// The alignment joins two spots that retention drift split apart when <see cref="MsdialGcmsParameter.JoinSpotsSplitByRetentionDrift"/>
    /// is on and their centres are within <see cref="MsdialGcmsParameter.SplitSpotJoinWindow"/> retention tolerances.
    /// </summary>
    [TestClass]
    public class MsdialGcmsParameterSplitSpotJoinSettingsTests
    {
        private const int FirstSplitSpotJoinKey = 160;

        private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);

        [TestMethod]
        public void ANewGcmsParameterJoinsWithinTwoRetentionTolerances() {
            var parameter = new MsdialGcmsParameter(isLabUseOnly: false);

            Assert.IsTrue(parameter.JoinSpotsSplitByRetentionDrift);
            Assert.AreEqual(2f, parameter.SplitSpotJoinWindow);
        }

        [TestMethod]
        public void TheSplitSpotJoinSettingsAreSavedAndLoaded() {
            var parameter = new MsdialGcmsParameter { JoinSpotsSplitByRetentionDrift = false, SplitSpotJoinWindow = 1.5f, };
            var memory = new MemoryStream();
            new MsdialGcmsDataStorage { MsdialGcmsParameter = parameter, }.SaveParameterAsync(memory).Wait();

            var actual = new MsdialGcmsDataStorage().LoadParameter(new MemoryStream(memory.ToArray()));

            Assert.IsFalse(actual.JoinSpotsSplitByRetentionDrift);
            Assert.AreEqual(1.5f, actual.SplitSpotJoinWindow);
        }

        [TestMethod]
        public void AParameterFileSavedBeforeTheSplitSpotJoinSettingsJoinsWithTheDefaults() {
            var saved = MessagePackSerializer.Serialize(new MsdialGcmsParameter { JoinSpotsSplitByRetentionDrift = false, SplitSpotJoinWindow = 3f, }, Options);
            Assert.AreEqual(FirstSplitSpotJoinKey + 2, new MessagePackReader(saved).ReadArrayHeader(), "the join settings must be the last keys for the fixture to predate them");

            var actual = new MsdialGcmsDataStorage().LoadParameter(new MemoryStream(Truncate(saved, FirstSplitSpotJoinKey)));

            Assert.IsTrue(actual.JoinSpotsSplitByRetentionDrift);
            Assert.AreEqual(MsdialGcmsParameter.DefaultSplitSpotJoinWindow, actual.SplitSpotJoinWindow);
        }

        /// <summary>The first <paramref name="length"/> elements of an int-keyed object, as an older version wrote it.</summary>
        private static byte[] Truncate(byte[] array, int length) {
            var reader = new MessagePackReader(array);
            var count = reader.ReadArrayHeader();
            Assert.IsTrue(length <= count);
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(buffer);
            writer.WriteArrayHeader(length);
            for (var i = 0; i < length; i++) {
                writer.WriteRaw(reader.ReadRaw());
            }
            writer.Flush();
            return buffer.WrittenSpan.ToArray();
        }
    }
}
