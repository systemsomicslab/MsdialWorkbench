using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Tests
{
    [TestClass()]
    public class DataProviderExtensionsTests
    {
        [TestMethod()]
        public void LoadMs2SpectraWithRtRangeTest() {
            var provider = new StubDataProvider();
            var spectra = provider.LoadMsNSpectrumsAsync(2, default).Result;
            var expected = spectra.Where(s => 2d <= s.ScanStartTime && s.ScanStartTime < 4d).ToArray();
            var actual = provider.LoadMs2SpectraWithRtRangeAsync(2, 4, default).Result;
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++) {
                Assert.AreEqual(expected[i].ScanStartTime, actual[i].ScanStartTime);
            }
        }

        class StubDataProvider : IDataProvider
        {
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
                throw new NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
                throw new NotImplementedException();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
                throw new NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
                var spectra = new RawSpectrum[]
                {
                    new() { ScanStartTime = 1d, },
                    new() { ScanStartTime = 2d, },
                    new() { ScanStartTime = 3d, },
                    new() { ScanStartTime = 4d, },
                    new() { ScanStartTime = 5d, },
                };
                return Task.FromResult(new ReadOnlyCollection<RawSpectrum>(spectra));
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
                throw new NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
                throw new NotImplementedException();
            }
        }
    }
}