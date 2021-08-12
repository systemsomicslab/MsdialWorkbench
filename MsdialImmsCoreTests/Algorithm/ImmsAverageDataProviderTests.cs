using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using CompMs.Common.DataObj;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm.Tests
{
    [TestClass()]
    public class ImmsAverageDataProviderTests
    {
        [TestMethod()]
        public void IndexTest() {
            var provider = new ImmsAverageDataProvider(
                new[]
                {
                    new RawSpectrum { Index = 0, OriginalIndex = 0, MsLevel = 1,
                        ScanStartTime = 1, DriftTime = 1, },
                    new RawSpectrum { Index = 1, OriginalIndex = 1, MsLevel = 2,
                        ScanStartTime = 1, DriftTime = 1, },
                    new RawSpectrum { Index = 2, OriginalIndex = 2, MsLevel = 2,
                        ScanStartTime = 1, DriftTime = 1, },
                    new RawSpectrum { Index = 3, OriginalIndex = 3, MsLevel = 1,
                        ScanStartTime = 2, DriftTime = 1, },
                    new RawSpectrum { Index = 4, OriginalIndex = 4, MsLevel = 2,
                        ScanStartTime = 2, DriftTime = 1, },
                    new RawSpectrum { Index = 5, OriginalIndex = 5, MsLevel = 2,
                        ScanStartTime = 2, DriftTime = 1, },
                    new RawSpectrum { Index = 6, OriginalIndex = 6, MsLevel = 1,
                        ScanStartTime = 3, DriftTime = 2, },
                    new RawSpectrum { Index = 7, OriginalIndex = 7, MsLevel = 2,
                        ScanStartTime = 3, DriftTime = 2, },
                },
                0.01, 0.02);

            var actuals = provider.LoadMsSpectrums();

            var expects = new[] {
                new RawSpectrum { Index = 0, OriginalIndex = 0, MsLevel = 1,
                    ScanStartTime = 1, DriftTime = 1, },
                new RawSpectrum { Index = 1, OriginalIndex = 1, MsLevel = 2,
                    ScanStartTime = 1, DriftTime = 1, },
                new RawSpectrum { Index = 2, OriginalIndex = 2, MsLevel = 2,
                    ScanStartTime = 1, DriftTime = 1, },
                new RawSpectrum { Index = 3, OriginalIndex = 4, MsLevel = 2,
                    ScanStartTime = 2, DriftTime = 1, },
                new RawSpectrum { Index = 4, OriginalIndex = 5, MsLevel = 2,
                    ScanStartTime = 2, DriftTime = 1, },
                new RawSpectrum { Index = 5, OriginalIndex = 6, MsLevel = 1,
                    ScanStartTime = 3, DriftTime = 2, },
                new RawSpectrum { Index = 6, OriginalIndex = 7, MsLevel = 2,
                    ScanStartTime = 3, DriftTime = 2, },
            };

            foreach ((var expect, var actual) in expects.Zip(actuals)) {
                Assert.AreEqual(expect.Index, actual.Index);
                Assert.AreEqual(expect.OriginalIndex, actual.OriginalIndex);
                Assert.AreEqual(expect.MsLevel, actual.MsLevel);
                Assert.AreEqual(expect.ScanStartTime, actual.ScanStartTime);
                Assert.AreEqual(expect.DriftTime, actual.DriftTime);
            }
        }
    }
}