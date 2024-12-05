using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Tests;

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

    [DataTestMethod()]
    [DataRow(3.0d)]
    [DataRow(2.8d)]
    [DataRow(3.4d)]
    public void LoadMs2SpectraByNearestMs1WithRtTest(double targetRt) {
        var provider = new StubDataProvider
        {
            MsSpectra = new ReadOnlyCollection<RawSpectrum>([
                new() { ScanStartTime = 1d, MsLevel = 1, ExperimentID = 1, },
                new() { ScanStartTime = 1d, MsLevel = 2, ExperimentID = 2, },
                new() { ScanStartTime = 2d, MsLevel = 1, ExperimentID = 1, },
                new() { ScanStartTime = 2d, MsLevel = 2, ExperimentID = 2, },
                new() { ScanStartTime = 3d, MsLevel = 1, ExperimentID = 1, },
                new() { ScanStartTime = 3d, MsLevel = 2, ExperimentID = 2, },
                new() { ScanStartTime = 3d, MsLevel = 2, ExperimentID = 3, },
                new() { ScanStartTime = 4d, MsLevel = 1, ExperimentID = 1, },
                new() { ScanStartTime = 4d, MsLevel = 2, ExperimentID = 2, },
                new() { ScanStartTime = 5d, MsLevel = 1, ExperimentID = 1, },
                new() { ScanStartTime = 5d, MsLevel = 2, ExperimentID = 2, },
            ]),
        };

        // Call the method being tested
        var actual = provider.LoadMs2SpectraByNearestMs1WithRtAsync(targetRt, default).Result;
        var expected = new[]{ provider.MsSpectra[5], provider.MsSpectra[6], };

        // Ensure MS2 spectra are found
        Assert.IsTrue(actual.Length > 0, "Expected MS2 spectra, but none were found.");

        // Assertions
        Assert.AreEqual(expected.Length, actual.Length, "The number of MS2 spectra does not match.");
        CollectionAssert.AreEqual(expected, actual, "The returned MS2 spectra do not match the expected spectra.");
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

        public ReadOnlyCollection<RawSpectrum> MsSpectra { get; set; }

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return MsSpectra;
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
            return Task.FromResult(MsSpectra);
        }
    }
}