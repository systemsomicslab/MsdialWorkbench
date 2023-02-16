using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;

namespace MsdialCoreTests.Parser
{
    public sealed class ChromatogramSerializerTestHelper
    {
        public static void AreEqual(ChromatogramSpotInfo expected, ChromatogramSpotInfo actual) {
            Assert.AreEqual(expected.ChromXs.RT.Value, actual.ChromXs.RT.Value);
            Assert.AreEqual(expected.ChromXs.RI.Value, actual.ChromXs.RI.Value);
            Assert.AreEqual(expected.ChromXs.Mz.Value, actual.ChromXs.Mz.Value);
            Assert.AreEqual(expected.ChromXs.Drift.Value, actual.ChromXs.Drift.Value);
            foreach ((var e, var a) in expected.PeakInfos.Zip(actual.PeakInfos, (e, a) => (e, a))) {
                AreEqual(e, a);
            }
        }

        public static void AreEqual(ChromatogramPeakInfo expected, ChromatogramPeakInfo actual) {
            Assert.AreEqual(expected.FileID, actual.FileID);
            Assert.AreEqual(expected.ChromXsTop.Value, actual.ChromXsTop.Value);
            Assert.AreEqual(expected.ChromXsLeft.Value, actual.ChromXsLeft.Value);
            Assert.AreEqual(expected.ChromXsRight.Value, actual.ChromXsRight.Value);
            foreach ((var aa, var bb) in expected.Chromatogram.Zip(actual.Chromatogram, (e, a) => (e, a))) {
                AreEqual(aa, bb);
            }
        }

        public static void AreEqual(IChromatogramPeak expected, IChromatogramPeak actual) {
            Assert.AreEqual(expected.ChromXs.Value, actual.ChromXs.Value);
            Assert.AreEqual(expected.Intensity, actual.Intensity);
        }

        public static ChromatogramPeak CreateChromatogramPeak(double axis, double intensity, ChromXType mainType) {
            return new ChromatogramPeak(0, 0d, intensity, new ChromXs(axis, mainType));
        }

        public static ChromatogramPeak CreateChromatogramPeak(double axis, double intensity) =>
            CreateChromatogramPeak(axis, intensity, ChromXType.RT);

        public static ChromatogramPeakInfo CreateChromatogramPeakInfo(int id, int num, ChromXType mainType) {
            var chromatogram = new List<ChromatogramPeak>();
            for (int i = 0; i < num; i++) {
                chromatogram.Add(CreateChromatogramPeak(i / 1000f + 100, i * (num - i) + 20, mainType));
            }
            return new ChromatogramPeakInfo(
                id, chromatogram,
                (float)chromatogram[num / 2].ChromXs.Value, (float)chromatogram[num / 4].ChromXs.Value, (float)chromatogram[num / 5 * 3].ChromXs.Value
                );
        }

        public static ChromatogramPeakInfo CreateChromatogramPeakInfo(int id, int num) =>
            CreateChromatogramPeakInfo(id, num, ChromXType.RT);

        public static ChromatogramSpotInfo CreateChromatogramSpotInfo(int num, double rt, double ri, double mz, double drift, ChromXType mainType) {
            var chromatogram = new List<ChromatogramPeakInfo>(num);
            for (int i = 0; i < num; i++) {
                chromatogram.Add(CreateChromatogramPeakInfo(i, num + i, mainType));
            }
            return new ChromatogramSpotInfo(
                chromatogram,
                new ChromXs {
                    RT = new RetentionTime(rt),
                    RI = new RetentionIndex(ri),
                    Mz = new MzValue(mz),
                    Drift = new DriftTime(drift),
                    MainType = mainType,
                }
                );
        }

        public static ChromatogramSpotInfo CreateChromatogramSpotInfo(int num, double value, ChromXType mainType) {
            var chromatogram = new List<ChromatogramPeakInfo>(num);
            for (int i = 0; i < num; i++) {
                chromatogram.Add(CreateChromatogramPeakInfo(i, num + i, mainType));
            }
            return new ChromatogramSpotInfo(
                chromatogram,
                new ChromXs(value, mainType)
                );
        }

        public static ChromatogramSpotInfo CreateCHromatogramSpotInfo(int num, double value) =>
            CreateChromatogramSpotInfo(num, value, ChromXType.RT);
    }
}
