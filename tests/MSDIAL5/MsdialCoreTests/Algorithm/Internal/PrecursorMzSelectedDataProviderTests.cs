using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsdialCoreTestHelper.DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Internal.Tests
{
    [TestClass()]
    public class PrecursorMzSelectedDataProviderTests
    {
        [TestMethod()]
        public void LoadMsNSpectrumsAsyncTest() {
            var provider = new StubDataProvider()
            {
                Spectra = new List<RawSpectrum>
                {
                    new() { MsLevel = 2, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, } },
                    new() { MsLevel = 2, Precursor = new RawPrecursorIon { SelectedIonMz = 200d, } },
                    new() { MsLevel = 2, Precursor = new RawPrecursorIon { SelectedIonMz = 300d, } },
                    new() { MsLevel = 2, Precursor = new RawPrecursorIon { SelectedIonMz = 400d, } },
                    new() { MsLevel = 2, Precursor = new RawPrecursorIon { SelectedIonMz = 500d, } },
                }
            };
            var filtered = new PrecursorMzSelectedDataProvider(provider, 300d, 100d, Common.Enum.AcquisitionType.DDA);
            var actual = filtered.LoadMsNSpectrums(2);
            var expected = provider.LoadMsNSpectrums(2).Where(s => Math.Abs(s.Precursor.SelectedIonMz - 300d) <= 100d).ToArray();

            Assert.AreEqual(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; i++) {
                Assert.AreEqual(expected[i].Precursor.SelectedIonMz, actual[i].Precursor.SelectedIonMz);
            }
        }
    }
}