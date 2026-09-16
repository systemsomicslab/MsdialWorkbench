using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.Common.DataObj.Property;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Utility.Tests;

[TestClass()]
public class DataAccessTests
{
    [TestMethod()]
    public void GetFineIsotopicPeaksTest() {
        var spectrum = new RawSpectrum
        {
            Spectrum = [
                new RawPeakElement { Mz = 50.0, Intensity = 50.0 }, // less than m/z
                new RawPeakElement { Mz = 80.0, Intensity = 80.0 }, // M+0
                new RawPeakElement { Mz = 81.0, Intensity = 81.0 }, // M+1
                new RawPeakElement { Mz = 81.5, Intensity = 81.5 }, // other peak
                new RawPeakElement { Mz = 81.995, Intensity = 81.995 }, // M+2
                new RawPeakElement { Mz = 83.005, Intensity = 83.005 }, // M+3
                new RawPeakElement { Mz = 83.995, Intensity = 83.995 }, // M+3
                new RawPeakElement { Mz = 84.005, Intensity = 84.005 }, // M+4
                new RawPeakElement { Mz = 85.0, Intensity = 85.0 }, // after maxIsotopes

            ],
        };
        var ionFeature = new IonFeatureCharacter
        {
            Charge = 1,
        };
        var mz = 80d;
        var tolerance = .01f;
        var maxIsotopes = 4;
        var actual = DataAccess.GetFineIsotopicPeaks(spectrum, ionFeature, mz, tolerance, maxIsotopes);

        var expected = new[]
        {
            new IsotopicPeak { Mass = 80.0, AbsoluteAbundance = 80.0f, },
            new IsotopicPeak { Mass = 81.0, AbsoluteAbundance = 81.0 },
            new IsotopicPeak { Mass = 81.995, AbsoluteAbundance = 81.995 },
            new IsotopicPeak { Mass = 83.005, AbsoluteAbundance = 83.005 },
            new IsotopicPeak { Mass = 83.995, AbsoluteAbundance = 83.995 },
            new IsotopicPeak { Mass = 84.005, AbsoluteAbundance = 84.005 },
        };

        for (int i = 0; i < actual.Count; i++) {
            System.Diagnostics.Debug.WriteLine($"Actual Isotopic Peak at index {i}: Mass = {actual[i].Mass}, Absolute Abundance = {actual[i].AbsoluteAbundance}");
        }

        Assert.AreEqual(expected.Length, actual.Count, "The count of expected isotopic peaks does not match the count of actual isotopic peaks.");
        for (int i = 0; i < expected.Length; i++) {
            Assert.AreEqual(expected[i].Mass, actual[i].Mass, $"The mass of expected isotopic peak at index {i} does not match the mass of actual isotopic peak at index {i}.");
            Assert.AreEqual(expected[i].AbsoluteAbundance, actual[i].AbsoluteAbundance, $"The absolute abundance of expected isotopic peak at index {i} does not match the absolute abundance of actual isotopic peak at index {i}.");
        }
    }

    [TestMethod()]
    public void GetScanStartIndexByRtReturnsLowerBound() {
        var spectra = new[]
        {
            new RawSpectrum { ScanStartTime = 1f },
            new RawSpectrum { ScanStartTime = 2f },
            new RawSpectrum { ScanStartTime = 3f },
            new RawSpectrum { ScanStartTime = 4f },
        };

        Assert.AreEqual(2, DataAccess.GetScanStartIndexByRt(3.5f, .5f, spectra));
        Assert.AreEqual(0, DataAccess.GetScanStartIndexByRt(.5f, .5f, spectra));
        Assert.AreEqual(spectra.Length, DataAccess.GetScanStartIndexByRt(5f, .5f, spectra));
    }

    [TestMethod()]
    public void GetAverageSpectrumPassIndexTest() {
        var spectra = new[]
        {
            new RawSpectrum { Spectrum = [new() { Mz = 100d, Intensity = 100, }] },
            new RawSpectrum { Spectrum = [new() { Mz = 200d, Intensity = 80, }] },
            new RawSpectrum { Spectrum = [new() { Mz = 300d, Intensity = 100, }] },
            new RawSpectrum { Spectrum = [new() { Mz = 100d, Intensity = 20, }, new() { Mz = 200d, Intensity = 70d }] },
        };

        var actual = DataAccess.GetAverageSpectrum(spectra, [0, 1, 3,], .01);
        var expected = new[]
        {
            new SpectrumPeak { Mass = 100d, Intensity = 40, },
            new SpectrumPeak { Mass = 200d, Intensity = 50, },
        };

        Assert.AreEqual(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++) {
            Assert.AreEqual(expected[i].Mass, actual[i].Mass);
            Assert.AreEqual(expected[i].Intensity, actual[i].Intensity);
        }
    }

    [TestMethod()]
    public void GetAverageSpectrumTest() {
        var spectra = new[]
        {
            new RawSpectrum { Spectrum = [new() { Mz = 100d, Intensity = 100, }] },
            new RawSpectrum { Spectrum = [new() { Mz = 200d, Intensity = 80, }] },
            new RawSpectrum { Spectrum = [new() { Mz = 100d, Intensity = 20, }, new() { Mz = 200d, Intensity = 70d }] },
        };

        var actual = DataAccess.GetAverageSpectrum(spectra, .01);
        var expected = new[]
        {
            new SpectrumPeak { Mass = 100d, Intensity = 40, },
            new SpectrumPeak { Mass = 200d, Intensity = 50, },
        };

        Assert.AreEqual(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++) {
            Assert.AreEqual(expected[i].Mass, actual[i].Mass);
            Assert.AreEqual(expected[i].Intensity, actual[i].Intensity);
        }
    }

    [TestMethod()]
    public void GetMs2ValuePeaksReturnsMatchingPeaksInScanOrder() {
        var provider = new StubDataProvider([
            new RawSpectrum { MsLevel = 1 },
            CreateMs2Spectrum(10, 500, 0, 3, 7),
            CreateMs2Spectrum(11, 500, 2, 100, 200),
            CreateMs2Spectrum(12, 500, 0, 5, 11),
            new RawSpectrum { MsLevel = 2 },
        ]);
        var parameter = new ParameterBase {
            CentroidMs1Tolerance = 0.1f,
            CentroidMs2Tolerance = 0.05f,
        };

        var actual = DataAccess.GetMs2ValuePeaks(
            provider, 500, 0, 4, [100, 200], parameter, AcquisitionType.DDA, targetCE: 0);

        Assert.AreEqual(2, actual.Count);
        Assert.AreEqual(2, actual[0].Length);
        Assert.AreEqual(2, actual[1].Length);
        CollectionAssert.AreEqual(new[] { 10, 12 }, actual[0].Select(peak => peak.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 10, 12 }, actual[1].Select(peak => peak.Id).ToArray());
        Assert.AreEqual(3d, actual[0][0].Intensity);
        Assert.AreEqual(5d, actual[0][1].Intensity);
        Assert.AreEqual(7d, actual[1][0].Intensity);
        Assert.AreEqual(11d, actual[1][1].Intensity);
    }

    private static RawSpectrum CreateMs2Spectrum(int index, double precursorMz, double collisionEnergy, double firstIntensity, double secondIntensity) {
        return new RawSpectrum {
            Index = index,
            MsLevel = 2,
            CollisionEnergy = collisionEnergy,
            ScanStartTime = index,
            Precursor = new RawPrecursorIon { IsolationTargetMz = precursorMz },
            Spectrum = [
                new RawPeakElement { Mz = 100, Intensity = firstIntensity },
                new RawPeakElement { Mz = 150, Intensity = 0 },
                new RawPeakElement { Mz = 200, Intensity = secondIntensity },
                new RawPeakElement { Mz = 250, Intensity = 0 },
            ],
        };
    }

    private sealed class StubDataProvider : IDataProvider {
        private readonly ReadOnlyCollection<RawSpectrum> _spectra;

        public StubDataProvider(IList<RawSpectrum> spectra) {
            _spectra = new ReadOnlyCollection<RawSpectrum>(spectra);
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return _spectra;
        }

        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => throw new System.NotImplementedException();

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => throw new System.NotImplementedException();

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => throw new System.NotImplementedException();

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => throw new System.NotImplementedException();

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => throw new System.NotImplementedException();
    }

    [TestMethod()]
    public void ReferenceMatchedExportUsesAnnotationName() {
        // "no MS2: " and "low score: " are the two shapes SetMoleculeMsPropertyAsSuggested
        // writes in MS-DIAL 5; "w/o MS2: " is still written by the peptide path.
        foreach (var name in new[] { "", " ", "Unknown", "unknown feature", "null", "empty", "no MS2: compound", "low score: compound", "w/o MS2: compound", "RIKEN MS/MS" }) {
            var peak = new AlignmentChromPeakFeature { Name = name };
            Assert.AreEqual("FALSE", DataAccess.GetSpotValueAsString(peak, "Reference matched"), name);
            Assert.AreEqual(0d, DataAccess.GetSpotValue(peak, "Reference matched"), name);
        }

        foreach (var name in new[] { "FA 16:0", "PC 34:1", "Reference compound" }) {
            var peak = new AlignmentChromPeakFeature { Name = name };
            Assert.AreEqual("TRUE", DataAccess.GetSpotValueAsString(peak, "Reference matched"), name);
            Assert.AreEqual(1d, DataAccess.GetSpotValue(peak, "Reference matched"), name);
        }
    }
}
