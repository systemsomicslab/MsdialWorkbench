using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Internal.Tests
{
    [TestClass()]
    public class PrecursorMzSelectedDataProviderTests
    {
        [TestMethod()]
        public void LoadMsNSpectrumsAsyncTest() {
            var provider = new StubDataProvider();
            var filtered = new PrecursorMzSelectedDataProvider(provider, 300d, 100d);
            var actual = filtered.LoadMsNSpectrums(2);
            var expected = provider.LoadMsNSpectrums(2).Where(s => Math.Abs(s.Precursor.SelectedIonMz - 300d) <= 100d).ToArray();

            Assert.AreEqual(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; i++) {
                Assert.AreEqual(expected[i].Precursor.SelectedIonMz, actual[i].Precursor.SelectedIonMz);
            }
        }

        class StubDataProvider : IDataProvider
        {
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
                throw new System.NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
                throw new System.NotImplementedException();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
                return new List<RawSpectrum>
                {
                    new() { Precursor = new RawPrecursorIon { SelectedIonMz = 100d, } },
                    new() { Precursor = new RawPrecursorIon { SelectedIonMz = 200d, } },
                    new() { Precursor = new RawPrecursorIon { SelectedIonMz = 300d, } },
                    new() { Precursor = new RawPrecursorIon { SelectedIonMz = 400d, } },
                    new() { Precursor = new RawPrecursorIon { SelectedIonMz = 500d, } },
                }.AsReadOnly();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
                throw new System.NotImplementedException();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
                throw new System.NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
                throw new System.NotImplementedException();
            }
        }
    }
}