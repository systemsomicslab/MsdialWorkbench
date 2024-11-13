using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment.Tests
{
    [TestClass()]
    public class LcmsGapFillerTests
    {
        [TestMethod()]
        public void LcmsGapFillerTest() {
            var rtTol = 1d;
            var mzTol = 0.01;

            var filler = new LcmsGapFiller(
                rtTol, mzTol,
                Common.Enum.IonMode.Positive,
                Common.Enum.SmoothingMethod.LinearWeightedMovingAverage, 0,
                true);

            var spectra = new List<RawSpectrum>
            {
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 1.00, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 10, Mz = 100 - mzTol * 0.6 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 1.05, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 15, Mz = 100 - mzTol * 0.5 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 1.25, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 20, Mz = 100 - mzTol * 0.4 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 1.40, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 30, Mz = 100 - mzTol * 0.3 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 1.55, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 40, Mz = 100 - mzTol * 0.2 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 1.80, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 50, Mz = 100 - mzTol * 0.1 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 1.95, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 45, Mz = 100 + mzTol * 0.0 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 2.00, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 40, Mz = 100 + mzTol * 0.1 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 2.10, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 60, Mz = 100 + mzTol * 0.2 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 2.35, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 70, Mz = 100 + mzTol * 0.3 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 2.50, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 60, Mz = 100 + mzTol * 0.4 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 2.65, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 30, Mz = 100 + mzTol * 0.5 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 2.85, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 15, Mz = 100 + mzTol * 0.6 } } },
                new RawSpectrum { MsLevel = 1, ScanPolarity = ScanPolarity.Positive, ScanStartTime = 2.90, Spectrum = new RawPeakElement[] { new RawPeakElement { Intensity = 10, Mz = 100 + mzTol * 0.7 } } },
            };

            var spot = new AlignmentSpotProperty
            {
                // center 2.05, width 0.6
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    new AlignmentChromPeakFeature { MasterPeakID = -1, PeakID = -1 },
                    new AlignmentChromPeakFeature {
                        MasterPeakID = 1, PeakID = 1, IonMode = Common.Enum.IonMode.Positive,
                        ChromXsLeft = new ChromXs(1.8), ChromXsTop = new ChromXs(2.0), ChromXsRight = new ChromXs(2.2),
                        PeakHeightTop = 100d, Mass = 100d + mzTol * 0.9, },
                    new AlignmentChromPeakFeature {
                        MasterPeakID = 2, PeakID = 2, IonMode = Common.Enum.IonMode.Positive ,
                        ChromXsLeft = new ChromXs(1.9), ChromXsTop = new ChromXs(1.95), ChromXsRight = new ChromXs(2.1),
                        PeakHeightTop = 200d, Mass = 100d, },
                    new AlignmentChromPeakFeature {
                        MasterPeakID = -1, PeakID = -1 },
                    new AlignmentChromPeakFeature {
                        MasterPeakID = -1, PeakID = -1 },
                    new AlignmentChromPeakFeature {
                        MasterPeakID = 4, PeakID = 4, IonMode = Common.Enum.IonMode.Positive,
                        ChromXsLeft = new ChromXs(1.7), ChromXsTop = new ChromXs(2.2), ChromXsRight = new ChromXs(2.3),
                        PeakHeightTop = 150d, Mass = 100d + mzTol * 0.9, },
                },
            };

            var ms1Spectra = new Ms1Spectra(spectra, Common.Enum.IonMode.Positive, Common.Enum.AcquisitionType.DDA);
            var rawSpectra = new RawSpectra(spectra, Common.Enum.IonMode.Positive, Common.Enum.AcquisitionType.DDA);
            filler.GapFill(ms1Spectra, rawSpectra, spectra, spot, 0);
            Debug.WriteLine($"Mass: {spot.AlignedPeakProperties[0].Mass}");
            Debug.WriteLine($"ChromScanIdTop: {spot.AlignedPeakProperties[0].ChromScanIdTop}");
            Debug.WriteLine($"ChromScanIdLeft: {spot.AlignedPeakProperties[0].ChromScanIdLeft}");
            Debug.WriteLine($"ChromScanIdRight: {spot.AlignedPeakProperties[0].ChromScanIdRight}");
            Debug.WriteLine($"ChromXsTop: {spot.AlignedPeakProperties[0].ChromXsTop.Value}");
            Debug.WriteLine($"ChromXsLeft: {spot.AlignedPeakProperties[0].ChromXsLeft.Value}");
            Debug.WriteLine($"ChromXsRight: {spot.AlignedPeakProperties[0].ChromXsRight.Value}");
            Debug.WriteLine($"PeakHeightTop: {spot.AlignedPeakProperties[0].PeakHeightTop}");
            Debug.WriteLine($"PeakHeightLeft: {spot.AlignedPeakProperties[0].PeakHeightLeft}");
            Debug.WriteLine($"PeakHeightRight: {spot.AlignedPeakProperties[0].PeakHeightRight}");
            Debug.WriteLine($"PeakAreaAboveZero: {spot.AlignedPeakProperties[0].PeakAreaAboveZero}");

            Assert.AreEqual(100d, spot.AlignedPeakProperties[0].Mass);
            Assert.AreEqual(0, spot.AlignedPeakProperties[0].ChromScanIdLeft);
            Assert.AreEqual(3, spot.AlignedPeakProperties[0].ChromScanIdTop);
            Assert.AreEqual(5, spot.AlignedPeakProperties[0].ChromScanIdRight);
            Assert.AreEqual(1.25, spot.AlignedPeakProperties[0].ChromXsLeft.Value);
            Assert.AreEqual(1.80, spot.AlignedPeakProperties[0].ChromXsTop.Value);
            Assert.AreEqual(2.00, spot.AlignedPeakProperties[0].ChromXsRight.Value);
            Assert.AreEqual(20, spot.AlignedPeakProperties[0].PeakHeightLeft);
            Assert.AreEqual(50, spot.AlignedPeakProperties[0].PeakHeightTop);
            Assert.AreEqual(40, spot.AlignedPeakProperties[0].PeakHeightRight);
        }
    }
}