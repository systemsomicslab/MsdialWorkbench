using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Tests;

[TestClass]
public class PeakSpottingCoreTests
{
    // Nominal-mass MS1 data whose observed range is 100-600, with one chromatographic peak at
    // m/z 300 and one at m/z 600, the very end of the range.
    private const double LastSliceMz = 600d;

    [TestMethod]
    public void GetMassSlices_NominalMass_IncludesBothEndsOfTheMs1Range() {
        var parameter = CreateNominalParameter();
        var core = new PeakSpottingCore(parameter);

        var (targetMasses, massStep) = core.GetMassSlices(new FakeProvider(CreateSpectra()));

        Assert.AreEqual(1f, massStep);
        Assert.AreEqual(100d, targetMasses.First());
        Assert.AreEqual(LastSliceMz, targetMasses.Last());
        Assert.AreEqual(501, targetMasses.Count);
    }

    [DataTestMethod]
    [DataRow(2)]
    [DataRow(4)]
    public void Execute3DFeatureDetection_SingleThread_GivesTheMultiThreadPeakList(int numThreads) {
        var parameter = CreateNominalParameter();
        var file = new AnalysisFileBean { AcquisitionType = AcquisitionType.DDA, };
        var range = new ChromatogramRange(0d, 100d, ChromXType.RT, ChromXUnit.Min);

        var single = new PeakSpottingCore(parameter).Execute3DFeatureDetection(file, new FakeProvider(CreateSpectra()), 1, CancellationToken.None, null, range);
        var multi = new PeakSpottingCore(parameter).Execute3DFeatureDetection(file, new FakeProvider(CreateSpectra()), numThreads, CancellationToken.None, null, range);

        // The single-thread path used to stop before the last mass slice and so lost the m/z 600 peak.
        Assert.IsTrue(single.Any(f => Math.Abs(f.PeakFeature.Mass - LastSliceMz) < 0.5), "The peak in the last mass slice was not detected with one thread.");
        Assert.IsTrue(single.Any(f => Math.Abs(f.PeakFeature.Mass - 300d) < 0.5));
        Assert.AreEqual(multi.Count, single.Count);
        for (int i = 0; i < single.Count; i++) {
            Assert.AreEqual(multi[i].PeakFeature.Mass, single[i].PeakFeature.Mass);
            Assert.AreEqual(multi[i].PeakFeature.ChromXsTop.Value, single[i].PeakFeature.ChromXsTop.Value);
            Assert.AreEqual(multi[i].PeakFeature.ChromXsLeft.Value, single[i].PeakFeature.ChromXsLeft.Value);
            Assert.AreEqual(multi[i].PeakFeature.ChromXsRight.Value, single[i].PeakFeature.ChromXsRight.Value);
            Assert.AreEqual(multi[i].PeakFeature.PeakHeightTop, single[i].PeakFeature.PeakHeightTop);
            Assert.AreEqual(multi[i].PeakFeature.PeakAreaAboveZero, single[i].PeakFeature.PeakAreaAboveZero);
            Assert.AreEqual(multi[i].MS1RawSpectrumIdTop, single[i].MS1RawSpectrumIdTop);
            Assert.AreEqual(multi[i].PeakID, single[i].PeakID);
        }
    }

    private static ParameterBase CreateNominalParameter() {
        return new ParameterBase
        {
            IonMode = IonMode.Positive,
            AccuracyType = AccuracyType.IsNominal,
            MassSliceWidth = .5f,
            MassRangeBegin = 0f,
            MassRangeEnd = 1000f,
            CentroidMs1Tolerance = .5f,
            MinimumAmplitude = 1000d,
            MinimumDatapoints = 5d,
        };
    }

    private static List<RawSpectrum> CreateSpectra() {
        var spectra = new List<RawSpectrum>();
        for (int i = 0; i < 200; i++) {
            var rt = i * .05d;
            var elements = new[]
            {
                new RawPeakElement { Mz = 100d, Intensity = 10d, },
                new RawPeakElement { Mz = 300d, Intensity = Gaussian(rt, 3d), },
                new RawPeakElement { Mz = LastSliceMz, Intensity = Gaussian(rt, 6d), },
            };
            spectra.Add(new RawSpectrum
            {
                Index = i,
                ScanNumber = i,
                OriginalIndex = i,
                MsLevel = 1,
                ScanPolarity = ScanPolarity.Positive,
                ScanStartTime = rt,
                ScanStartTimeUnit = Units.Minute,
                Spectrum = elements,
                DefaultArrayLength = elements.Length,
                LowestObservedMz = elements.First().Mz,
                HighestObservedMz = elements.Last().Mz,
                BasePeakMz = elements.OrderByDescending(e => e.Intensity).First().Mz,
                BasePeakIntensity = elements.Max(e => e.Intensity),
                TotalIonCurrent = elements.Sum(e => e.Intensity),
            });
        }
        return spectra;
    }

    private static double Gaussian(double rt, double center) {
        const double height = 1e6, sigma = .1d;
        return 10d + height * Math.Exp(-(rt - center) * (rt - center) / (2 * sigma * sigma));
    }

    private sealed class FakeProvider : IDataProvider
    {
        private readonly ReadOnlyCollection<RawSpectrum> _spectra;

        public FakeProvider(List<RawSpectrum> spectra) {
            _spectra = spectra.AsReadOnly();
        }

        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => _spectra;
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => Task.FromResult(_spectra);
        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => level == 1 ? _spectra : new List<RawSpectrum>().AsReadOnly();
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => Task.FromResult(LoadMsNSpectrums(level));
        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => _spectra;
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => Task.FromResult(_spectra);
    }
}
