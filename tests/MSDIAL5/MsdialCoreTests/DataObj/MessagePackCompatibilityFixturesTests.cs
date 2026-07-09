using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.Common.MessagePack;
using CompMs.Common.DataObj.Property;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.MSDec.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.IO;
using NCDK;

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
        public void MoleculePropertyInterfaceFixtureMatchesCurrentFormat() {
            IMoleculeProperty sample = new MoleculeProperty(
                "Name",
                new Formula(new System.Collections.Generic.Dictionary<string, int> { ["H"] = 2, ["O"] = 1 }),
                "Ontology",
                "C",
                "ABCDEFGHIJKLMNOQRSTUVWXYZ1");
            var bytes = SerializeToBytes(sample, MoleculePropertyExtension.Formatter);

            var roundTrip = Deserialize<IMoleculeProperty>(bytes, MoleculePropertyExtension.Formatter);

            Assert.AreEqual(sample.Name, roundTrip.Name);
            Assert.AreEqual(sample.Formula.ToString(), roundTrip.Formula.ToString());
            Assert.AreEqual(sample.Ontology, roundTrip.Ontology);
            Assert.AreEqual(sample.SMILES, roundTrip.SMILES);
            Assert.AreEqual(sample.InChIKey, roundTrip.InChIKey);
        }

        [TestMethod]
        public void ChromatogramPeakFeatureInterfaceFixtureMatchesCurrentFormat() {
            IChromatogramPeakFeature sample = new BaseChromatogramPeakFeature {
                ChromScanIdLeft = 1,
                ChromScanIdTop = 2,
                ChromScanIdRight = 3,
                ChromXsLeft = new ChromXs(4),
                ChromXsTop = new ChromXs(5),
                ChromXsRight = new ChromXs(6),
                PeakHeightLeft = 7,
                PeakHeightTop = 8,
                PeakHeightRight = 9,
                PeakAreaAboveZero = 10,
                PeakAreaAboveBaseline = 11,
                Mass = 12,
            };
            var bytes = SerializeToBytes(sample, new ChromatogramPeakFeatureInterfaceFormatter());

            var roundTrip = Deserialize<IChromatogramPeakFeature>(bytes, new ChromatogramPeakFeatureInterfaceFormatter());

            Assert.AreEqual(sample.ChromScanIdLeft, roundTrip.ChromScanIdLeft);
            Assert.AreEqual(sample.ChromScanIdTop, roundTrip.ChromScanIdTop);
            Assert.AreEqual(sample.ChromScanIdRight, roundTrip.ChromScanIdRight);
            Assert.AreEqual(sample.ChromXsLeft.Value, roundTrip.ChromXsLeft.Value);
            Assert.AreEqual(sample.ChromXsTop.Value, roundTrip.ChromXsTop.Value);
            Assert.AreEqual(sample.ChromXsRight.Value, roundTrip.ChromXsRight.Value);
            Assert.AreEqual(sample.PeakHeightLeft, roundTrip.PeakHeightLeft);
            Assert.AreEqual(sample.PeakHeightTop, roundTrip.PeakHeightTop);
            Assert.AreEqual(sample.PeakHeightRight, roundTrip.PeakHeightRight);
            Assert.AreEqual(sample.PeakAreaAboveZero, roundTrip.PeakAreaAboveZero);
            Assert.AreEqual(sample.PeakAreaAboveBaseline, roundTrip.PeakAreaAboveBaseline);
            Assert.AreEqual(sample.Mass, roundTrip.Mass);
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
            var bytes = new byte[256];
            var resolver = formatter is null ? StandardResolver.Instance : new CompositeResolver<T>(formatter);
            var offset = resolver.GetFormatterWithVerify<T>().Serialize(ref bytes, 0, value, resolver);
            Array.Resize(ref bytes, offset);
            return bytes;
        }

        private static T Deserialize<T>(byte[] bytes, IMessagePackFormatter<T> formatter = null) {
            var resolver = formatter is null ? StandardResolver.Instance : new CompositeResolver<T>(formatter);
            return resolver.GetFormatterWithVerify<T>().Deserialize(bytes, 0, resolver, out _);
        }

        private sealed class CompositeResolver<TValue> : IFormatterResolver {
            private readonly IMessagePackFormatter<TValue> formatter;

            public CompositeResolver(IMessagePackFormatter<TValue> formatter) {
                this.formatter = formatter;
            }

            public IMessagePackFormatter<T> GetFormatter<T>() {
                if (formatter is IMessagePackFormatter<T> typed) {
                    return typed;
                }
                return StandardResolver.Instance.GetFormatter<T>();
            }
        }
    }
}
