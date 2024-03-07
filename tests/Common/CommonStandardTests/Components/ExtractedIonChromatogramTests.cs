using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace CompMs.Common.Components.Tests
{
    [TestClass()]
    public class ExtractedIonChromatogramTests
    {
        [TestMethod]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange
            var peaks = new List<ValuePeak>
            {
                new ValuePeak(1, 0.5, 100, 1000),
                new ValuePeak(2, 1.0, 200, 2000)
            };
            var type = ChromXType.RT;
            var unit = ChromXUnit.Min;
            var extractedMz = 150.5;

            // Act
            var eic = new ExtractedIonChromatogram(peaks, type, unit, extractedMz);

            // Assert
            Assert.IsFalse(eic.IsEmpty);
            Assert.AreEqual(2, eic.Length);
            Assert.AreEqual(extractedMz, eic.ExtractedMz);
        }

        [TestMethod]
        public void AsPeakArray_ReturnsCorrectPeaks()
        {
            // Arrange
            var peaks = new ValuePeak[]
            {
                new ValuePeak(1, 0.5, 100, 1000),
                new ValuePeak(2, 1.0, 200, 2000)
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 150.5);

            // Act
            var resultPeaks = eic.AsPeakArray();

            // Assert
            Assert.AreEqual(peaks.Length, resultPeaks.Length);
            for (int i = 0; i < peaks.Length; i++)
            {
                Assert.AreEqual(peaks[i].Time, resultPeaks[i].Time);
                Assert.AreEqual(peaks[i].Intensity, resultPeaks[i].Intensity);
            }
        }

        [TestMethod]
        public void Dispose_ReleasesResources()
        {
            // Arrange
            var pool = ArrayPool<ValuePeak>.Shared;
            var peaks = pool.Rent(2);
            peaks[0] = new ValuePeak(1, 0.5, 100, 1000);
            peaks[1] = new ValuePeak(2, 1.0, 200, 2000);

            var eic = new ExtractedIonChromatogram(peaks, 2, ChromXType.RT, ChromXUnit.Min, 150.5, pool);

            // Act
            eic.Dispose();

            // Assert
            // After calling Dispose, accessing any method or property should throw an ObjectDisposedException.
            Assert.ThrowsException<ObjectDisposedException>(() => eic.AsPeakArray());
        }
    }
}