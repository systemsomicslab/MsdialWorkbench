using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.Common.MessagePack;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class ChromatogramPeakFeatureTests
    {
        [TestMethod]
        public void ChromatogramPeakFeatureInterfaceSerializeTest() {
            var peak = new ChromatogramPeakFeature
            {
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
                Mass = 12
            };

            var memory = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(peak, memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = MessagePackDefaultHandler.LoadFromStream<FakeChromatogramPeakFeature>(memory);
            Assert.AreEqual(peak.ChromScanIdLeft, actual.ChromScanIdLeft);
            Assert.AreEqual(peak.ChromScanIdTop, actual.ChromScanIdTop);
            Assert.AreEqual(peak.ChromScanIdRight, actual.ChromScanIdRight);
            Assert.AreEqual(peak.ChromXsLeft.Value, actual.ChromXsLeft.Value);
            Assert.AreEqual(peak.ChromXsTop.Value, actual.ChromXsTop.Value);
            Assert.AreEqual(peak.ChromXsRight.Value, actual.ChromXsRight.Value);
            Assert.AreEqual(peak.PeakHeightLeft, actual.PeakHeightLeft);
            Assert.AreEqual(peak.PeakHeightTop, actual.PeakHeightTop);
            Assert.AreEqual(peak.PeakHeightRight, actual.PeakHeightRight);
            Assert.AreEqual(peak.PeakAreaAboveZero, actual.PeakAreaAboveZero);
            Assert.AreEqual(peak.PeakAreaAboveBaseline, actual.PeakAreaAboveBaseline);
            Assert.AreEqual(peak.Mass, actual.Mass);
        }
    }

    [MessagePackObject]
    public class FakeChromatogramPeakFeature : IChromatogramPeakFeature {
        [Key(0)]
        public int ChromScanIdLeft { get; set; }
        [Key(1)]
        public int ChromScanIdTop { get; set; }
        [Key(2)]
        public int ChromScanIdRight { get; set; }
        [Key(3)]
        public ChromXs ChromXsLeft { get; set; }
        [Key(4)]
        public ChromXs ChromXsTop { get; set; }
        [Key(5)]
        public ChromXs ChromXsRight { get; set; }
        [Key(6)]
        public double PeakHeightLeft { get; set; }
        [Key(7)]
        public double PeakHeightTop { get; set; }
        [Key(8)]
        public double PeakHeightRight { get; set; }
        [Key(9)]
        public double PeakAreaAboveZero { get; set; }
        [Key(10)]
        public double PeakAreaAboveBaseline { get; set; }
        [Key(43)]
        public double Mass { get; set; }

        public double PeakWidth(ChromXType type) {
            throw new NotImplementedException();
        }
    }
}