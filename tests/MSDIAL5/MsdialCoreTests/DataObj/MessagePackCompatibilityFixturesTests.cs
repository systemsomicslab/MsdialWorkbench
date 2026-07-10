using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using CompMs.Common.MessagePack;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Buffers;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass]
    public class MessagePackCompatibilityFixturesTests
    {
        [TestMethod]
        public void AnnotatedMsDecResultFixtureMatchesCurrentFormat() {
            var sample = AnnotatedMSDecResultTestHelper.CreateSample();
            var bytes = SerializeToBytes(sample);

            var roundTrip = Deserialize<AnnotatedMSDecResult>(bytes);

            Assert.AreEqual(sample.QuantMass, roundTrip.QuantMass);
        }

        [TestMethod]
        public void QuantifiedChromatogramPeakFixtureMatchesCurrentFormat() {
            var sample = QuantifiedChromatogramPeakTestHelper.CreateSample();
            var bytes = SerializeToBytes(sample);

            var roundTrip = Deserialize<QuantifiedChromatogramPeak>(bytes);

            Assert.IsInstanceOfType(roundTrip.PeakFeature, typeof(BaseChromatogramPeakFeature));
            Assert.AreEqual(sample.PeakFeature.ChromScanIdLeft, roundTrip.PeakFeature.ChromScanIdLeft);
            Assert.AreEqual(sample.PeakFeature.ChromScanIdTop, roundTrip.PeakFeature.ChromScanIdTop);
            Assert.AreEqual(sample.PeakFeature.ChromScanIdRight, roundTrip.PeakFeature.ChromScanIdRight);
            Assert.AreEqual(sample.PeakFeature.ChromXsLeft.Value, roundTrip.PeakFeature.ChromXsLeft.Value);
            Assert.AreEqual(sample.PeakFeature.ChromXsTop.Value, roundTrip.PeakFeature.ChromXsTop.Value);
            Assert.AreEqual(sample.PeakFeature.ChromXsRight.Value, roundTrip.PeakFeature.ChromXsRight.Value);
            Assert.AreEqual(sample.PeakFeature.PeakHeightLeft, roundTrip.PeakFeature.PeakHeightLeft);
            Assert.AreEqual(sample.PeakFeature.PeakHeightTop, roundTrip.PeakFeature.PeakHeightTop);
            Assert.AreEqual(sample.PeakFeature.PeakHeightRight, roundTrip.PeakFeature.PeakHeightRight);
            Assert.AreEqual(sample.PeakFeature.PeakAreaAboveZero, roundTrip.PeakFeature.PeakAreaAboveZero);
            Assert.AreEqual(sample.PeakFeature.PeakAreaAboveBaseline, roundTrip.PeakFeature.PeakAreaAboveBaseline);
            Assert.AreEqual(sample.PeakFeature.Mass, roundTrip.PeakFeature.Mass);
            Assert.IsInstanceOfType(roundTrip.PeakShape, typeof(ChromatogramPeakShape));
            Assert.AreEqual(sample.PeakShape.GetType(), roundTrip.PeakShape.GetType());
            Assert.AreEqual(sample.MS1RawSpectrumIdTop, roundTrip.MS1RawSpectrumIdTop);
            Assert.AreEqual(sample.MS1RawSpectrumIdLeft, roundTrip.MS1RawSpectrumIdLeft);
            Assert.AreEqual(sample.MS1RawSpectrumIdRight, roundTrip.MS1RawSpectrumIdRight);
        }

        [TestMethod]
        public void MoleculePropertyInterfaceDeserializesLegacyBytes() {
            var bytes = new byte[] { 0x95, 0xA4, 0x4E, 0x61, 0x6D, 0x65, 0xDC, 0x00, 0x1B, 0xA3, 0x48, 0x32, 0x4F, 0xCB, 0x40, 0x32, 0x02, 0xB4, 0x5D, 0xFA, 0xFB, 0xED, 0xCB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC3, 0x00, 0x82, 0xA1, 0x48, 0x02, 0xA1, 0x4F, 0x01, 0xA8, 0x4F, 0x6E, 0x74, 0x6F, 0x6C, 0x6F, 0x67, 0x79, 0xA1, 0x43, 0xBA, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x31 };

            var roundTrip = Deserialize<IMoleculeProperty>(bytes, MoleculePropertyExtension.Formatter);

            Assert.AreEqual("Name", roundTrip.Name);
            Assert.AreEqual("H2O", roundTrip.Formula.ToString());
            Assert.AreEqual("Ontology", roundTrip.Ontology);
            Assert.AreEqual("C", roundTrip.SMILES);
            Assert.AreEqual("ABCDEFGHIJKLMNOQRSTUVWXYZ1", roundTrip.InChIKey);
        }

        [TestMethod]
        public void ChromatogramPeakFeatureInterfaceDeserializesLegacyBytes() {
            var bytes = new byte[] { 0x9C, 0x01, 0x02, 0x03, 0x98, 0x92, 0x01, 0x93, 0xCB, 0x40, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x92, 0x02, 0x93, 0xCB, 0xBF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x04, 0x92, 0x04, 0x93, 0xCB, 0xBF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x02, 0x92, 0x03, 0x93, 0xCB, 0xBF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x03, 0x00, 0xCB, 0x40, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x98, 0x92, 0x01, 0x93, 0xCB, 0x40, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x92, 0x02, 0x93, 0xCB, 0xBF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x04, 0x92, 0x04, 0x93, 0xCB, 0xBF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x02, 0x92, 0x03, 0x93, 0xCB, 0xBF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x03, 0x00, 0xCB, 0x40, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x98, 0x92, 0x01, 0x93, 0xCB, 0x40, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x92, 0x02, 0x93, 0xCB, 0xBF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x04, 0x92, 0x04, 0x93, 0xCB, 0xBF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x02, 0x92, 0x03, 0x93, 0xCB, 0xBF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x03, 0x00, 0xCB, 0x40, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCB, 0x40, 0x1C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCB, 0x40, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCB, 0x40, 0x22, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCB, 0x40, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCB, 0x40, 0x26, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCB, 0x40, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            var roundTrip = Deserialize<IChromatogramPeakFeature>(bytes, new ChromatogramPeakFeatureInterfaceFormatter());

            Assert.AreEqual(1, roundTrip.ChromScanIdLeft);
            Assert.AreEqual(2, roundTrip.ChromScanIdTop);
            Assert.AreEqual(3, roundTrip.ChromScanIdRight);
            Assert.AreEqual(4, roundTrip.ChromXsLeft.Value);
            Assert.AreEqual(5, roundTrip.ChromXsTop.Value);
            Assert.AreEqual(6, roundTrip.ChromXsRight.Value);
            Assert.AreEqual(7, roundTrip.PeakHeightLeft);
            Assert.AreEqual(8, roundTrip.PeakHeightTop);
            Assert.AreEqual(9, roundTrip.PeakHeightRight);
            Assert.AreEqual(10, roundTrip.PeakAreaAboveZero);
            Assert.AreEqual(11, roundTrip.PeakAreaAboveBaseline);
            Assert.AreEqual(12, roundTrip.Mass);
        }

        [TestMethod]
        public void LargeListMessagePackRoundTripsFixtureList() {
            var samples = new[] { AnnotatedMSDecResultTestHelper.CreateSample(), AnnotatedMSDecResultTestHelper.CreateSample() };
            using var stream = new MemoryStream();
            LargeListMessagePack.Serialize(stream, samples);
            stream.Position = 0;

            var actual = LargeListMessagePack.Deserialize<AnnotatedMSDecResult>(stream);

            Assert.AreEqual(samples.Length, actual.Count);
            Assert.AreEqual(samples[0].QuantMass, actual[0].QuantMass);
            Assert.AreEqual(samples[1].QuantMass, actual[1].QuantMass);
        }

        private static byte[] SerializeToBytes<T>(T value, IMessagePackFormatter<T> formatter = null) {
            var option = MessagePackSerializerOptions.Standard;
            if (formatter is not null) {
                var resolver = CompositeResolver.Create([formatter], [StandardResolver.Instance]);
                option = option.WithResolver(resolver);
            }
            var arraywriter = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(arraywriter);
            MessagePackSerializer.Serialize(ref writer, value, option);
            writer.Flush();
            var bytes = new byte[arraywriter.WrittenCount];
            arraywriter.WrittenMemory.CopyTo(bytes);
            return bytes;
        }

        private static T Deserialize<T>(byte[] bytes, IMessagePackFormatter<T> formatter = null) {
            var option = MessagePackSerializerOptions.Standard;
            if (formatter is not null) {
                var resolver = CompositeResolver.Create([formatter], [StandardResolver.Instance]);
                option = option.WithResolver(resolver);
            }
            return MessagePackSerializer.Deserialize<T>(bytes, option);
        }
    }
}
