using CompMs.Common.MessagePack;
using CompMs.MsdialGcMsApi.DataObj;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers;
using System.IO;

namespace CompMs.MsdialGcMsApi.Parameter.Tests
{
    /// <summary>
    /// GcmsPeakJoiner takes the EI similarity threshold and weight from the spectrum similarity settings. A project or
    /// parameter file saved while it took them from Ms1AlignmentTolerance / Ms1AlignmentFactor (the GC-MS dialog wrote
    /// its "EI similarity tolerance / factor" there) must keep aligning with what it aligned with, and the dialog must
    /// show those values: such a file has no key 157 in MsdialGcmsParameter.
    /// </summary>
    [TestClass]
    public class MsdialGcmsParameterEiSimilaritySettingsTests
    {
        private const int EiSimilarityMarkerKey = 157;
        private const int GcmsParameterKeyInStorage = 6;

        private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);

        [TestMethod]
        public void ANewGcmsParameterStartsAtTheGcmsThreshold() {
            var parameter = new MsdialGcmsParameter(isLabUseOnly: false);

            // 0.5 on the integrated EI similarity: two spectra of one compound in two files commonly score 0.6-0.75.
            Assert.AreEqual(.5f, parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance);
            Assert.AreEqual(.5f, parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor);
        }

        [TestMethod]
        public void AParameterFileSavedBeforeTheSpectrumSimilaritySettingsTakesTheGcmsDefaultAndKeepsItsEiWeight() {
            var saved = MessagePackSerializer.Serialize(SavedByAnOlderGui(), Options);
            Assert.IsTrue(new MessagePackReader(saved).ReadArrayHeader() > EiSimilarityMarkerKey, "the truncation below must drop the marker, and every key after it");

            // MsdialGcmsDataStorage.LoadParameter is what the method setting dialog's "Load parameter" reads.
            var actual = new MsdialGcmsDataStorage().LoadParameter(new MemoryStream(Truncate(saved, EiSimilarityMarkerKey)));

            // The GC-MS default, neither the 0.7 the dialog wrote into the MS1 alignment tolerance nor the unseen 0.8.
            Assert.AreEqual(MsdialGcmsParameter.DefaultSpectrumSimilarityAlignmentTolerance, actual.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance);
            Assert.AreEqual(.5f, actual.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance);
            Assert.AreEqual(.6f, actual.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor);
            Assert.IsTrue(actual.AlignmentEiSimilarityIsSpectrumSimilarity);
        }

        [TestMethod]
        public void AProjectSavedBeforeTheSpectrumSimilaritySettingsTakesTheGcmsDefaultAndKeepsItsEiWeight() {
            var storage = new MsdialGcmsDataStorage { MsdialGcmsParameter = SavedByAnOlderGui(), };
            var saved = MessagePackSerializer.Serialize(storage, Options);
            var old = ReplaceElement(saved, GcmsParameterKeyInStorage, parameter => Truncate(parameter, EiSimilarityMarkerKey));

            // The call MsdialGcmsDataStorage.Serializer makes for the project container.
            var actual = MessagePackDefaultHandler.LoadFromStream<MsdialGcmsDataStorage>(new MemoryStream(old)).MsdialGcmsParameter;

            Assert.AreEqual(.5f, actual.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance);
            Assert.AreEqual(.6f, actual.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor);
        }

        [TestMethod]
        [DataRow(.015f)]
        [DataRow(.5f)]
        public void AConsoleProjectSavedBeforeTheSpectrumSimilaritySettingsTakesTheGcmsDefault(float ms1AlignmentTolerance) {
            // A project the Console wrote from a method file with "MS1 tolerance for alignment: 0.015" (LC-MS style)
            // or 0.5 (the earlier GC-MS template), which as a similarity threshold admitted any pair.
            var parameter = SavedByAnOlderGui();
            parameter.AlignmentBaseParam.Ms1AlignmentTolerance = ms1AlignmentTolerance;
            parameter.AlignmentBaseParam.Ms1AlignmentFactor = .5f;
            var storage = new MsdialGcmsDataStorage { MsdialGcmsParameter = parameter, };
            var saved = MessagePackSerializer.Serialize(storage, Options);
            var old = ReplaceElement(saved, GcmsParameterKeyInStorage, element => Truncate(element, EiSimilarityMarkerKey));

            var actual = MessagePackDefaultHandler.LoadFromStream<MsdialGcmsDataStorage>(new MemoryStream(old)).MsdialGcmsParameter;

            Assert.AreEqual(.5f, actual.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance);
            Assert.AreEqual(.5f, actual.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor);
            Assert.AreEqual(ms1AlignmentTolerance, actual.AlignmentBaseParam.Ms1AlignmentTolerance, "the MS1 alignment settings are left as they were");
        }

        [TestMethod]
        public void AParameterSavedWithTheSpectrumSimilaritySettingsKeepsThem() {
            var parameter = SavedByAnOlderGui();
            parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = .55f;
            parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor = .4f;
            var memory = new MemoryStream();
            new MsdialGcmsDataStorage { MsdialGcmsParameter = parameter, }.SaveParameterAsync(memory).Wait();

            var actual = new MsdialGcmsDataStorage().LoadParameter(new MemoryStream(memory.ToArray()));

            Assert.AreEqual(.55f, actual.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance);
            Assert.AreEqual(.4f, actual.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor);
            Assert.AreEqual(.7f, actual.AlignmentBaseParam.Ms1AlignmentTolerance, "the MS1 alignment settings are left as they were");
            Assert.AreEqual(.6f, actual.AlignmentBaseParam.Ms1AlignmentFactor);
        }

        private static MsdialGcmsParameter SavedByAnOlderGui() {
            var parameter = new MsdialGcmsParameter();
            // What the GC-MS alignment dialog wrote for "EI similarity tolerance 70 %, factor 0.6".
            parameter.AlignmentBaseParam.Ms1AlignmentTolerance = .7f;
            parameter.AlignmentBaseParam.Ms1AlignmentFactor = .6f;
            // A value nobody saw: the dialog never showed it and the joiner never read it.
            parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = .8f;
            parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor = .5f;
            return parameter;
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

        private static byte[] ReplaceElement(byte[] array, int index, Func<byte[], byte[]> replace) {
            var reader = new MessagePackReader(array);
            var count = reader.ReadArrayHeader();
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(buffer);
            writer.WriteArrayHeader(count);
            for (var i = 0; i < count; i++) {
                var element = reader.ReadRaw();
                if (i == index) {
                    writer.WriteRaw(replace(element.ToArray()));
                }
                else {
                    writer.WriteRaw(element);
                }
            }
            writer.Flush();
            return buffer.WrittenSpan.ToArray();
        }
    }
}
