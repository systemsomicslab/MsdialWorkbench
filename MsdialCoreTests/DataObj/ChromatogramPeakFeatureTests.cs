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
            var basePeak = new BaseChromatogramPeakFeature
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
            var peak = new ChromatogramPeakFeature(basePeak);
            Assert.AreEqual(1, peak.ChromScanIdLeft);
            Assert.AreEqual(2, peak.ChromScanIdTop);
            Assert.AreEqual(3, peak.ChromScanIdRight);
            Assert.AreEqual(4, peak.ChromXsLeft.Value);
            Assert.AreEqual(5, peak.ChromXsTop.Value);
            Assert.AreEqual(6, peak.ChromXsRight.Value);
            Assert.AreEqual(7, peak.PeakHeightLeft);
            Assert.AreEqual(8, peak.PeakHeightTop);
            Assert.AreEqual(9, peak.PeakHeightRight);
            Assert.AreEqual(10, peak.PeakAreaAboveZero);
            Assert.AreEqual(11, peak.PeakAreaAboveBaseline);
            Assert.AreEqual(12, peak.Mass);
            Assert.AreEqual(peak.Mass, peak.PrecursorMz);

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

        public double PeakWidth() {
            throw new NotImplementedException();
        }

        public double PeakWidth(ChromXType type) {
            throw new NotImplementedException();
        }
    }
}