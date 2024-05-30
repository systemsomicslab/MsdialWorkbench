using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Buffers;
using System.Collections.Generic;

namespace CompMs.Common.Components.Tests
{
    [TestClass()]
    public class ChromatogramTests
    {
        [TestMethod()]
        public void SmoothingSimpleTest() {
            var rawdata = new[]
            {
                new ChromatogramPeak(0, 100d, 1000d, new RetentionTime(0d)),
                new ChromatogramPeak(1, 101d, 1001d, new RetentionTime(1d)),
                new ChromatogramPeak(2, 102d, 1002d, new RetentionTime(2d)),
                new ChromatogramPeak(3, 103d, 1003d, new RetentionTime(3d)),
                new ChromatogramPeak(4, 104d, 1004d, new RetentionTime(4d)),
                new ChromatogramPeak(5, 105d, 1005d, new RetentionTime(5d)),
                new ChromatogramPeak(6, 106d, 1006d, new RetentionTime(6d)),
                new ChromatogramPeak(7, 107d, 1007d, new RetentionTime(7d)),
                new ChromatogramPeak(8, 108d, 1008d, new RetentionTime(8d)),
                new ChromatogramPeak(9, 109d, 1009d, new RetentionTime(9d)),
            };
            var chromatogram = new Chromatogram(rawdata, ChromXType.RT, ChromXUnit.Min);
            var actual = chromatogram.ChromatogramSmoothing(SmoothingMethod.SimpleMovingAverage, 2).AsPeakArray();
            Assert.AreEqual(10, actual.Count);
            Assert.AreEqual(5003d / 5, actual[0].Intensity);
            Assert.AreEqual(5007d / 5, actual[1].Intensity);
            Assert.AreEqual(5010d / 5, actual[2].Intensity);
            Assert.AreEqual(5015d / 5, actual[3].Intensity);
            Assert.AreEqual(5020d / 5, actual[4].Intensity);
            Assert.AreEqual(5025d / 5, actual[5].Intensity);
            Assert.AreEqual(5030d / 5, actual[6].Intensity);
            Assert.AreEqual(5035d / 5, actual[7].Intensity);
            Assert.AreEqual(5038d / 5, actual[8].Intensity);
            Assert.AreEqual(5042d / 5, actual[9].Intensity);
            for (int i = 0; i < 10; i++) {
                // Assert.AreEqual(rawdata[i].ID, actual[i].ID);
                Assert.AreEqual(rawdata[i].Mass, actual[i].Mass);
                Assert.AreEqual(rawdata[i].ChromXs.Value, actual[i].ChromXs.Value);
            }
        }

        [TestMethod()]
        public void AsPeakArrayTest() {
            // Arrange
            var peaks = new List<IChromatogramPeak>
            {
                new ChromatogramPeak(0, 100d, 1000d, new ChromXs(1, ChromXType.RT, ChromXUnit.Min)),
                new ChromatogramPeak(1, 200d, 2000d, new ChromXs(2, ChromXType.RT, ChromXUnit.Min)),
            };

            var chromatogram = new Chromatogram(peaks, ChromXType.RT, ChromXUnit.Min);

            // Act
            var resultPeaks = chromatogram.AsPeakArray();

            // Assert
            Assert.AreEqual(peaks.Count, resultPeaks.Count, "The number of peaks should match.");
            for (int i = 0; i < peaks.Count; i++) {
                Assert.AreEqual(peaks[i].ID, resultPeaks[i].ID, $"{nameof(IChromatogramPeak.ID)} of peak at index {i} should match.");
                Assert.AreEqual(peaks[i].Mass, resultPeaks[i].Mass, $"{nameof(IChromatogramPeak.Mass)} of peak at index {i} should match.");
                Assert.AreEqual(peaks[i].Intensity, resultPeaks[i].Intensity, $"{nameof(IChromatogramPeak.Intensity)} of peak at index {i} should match.");
                Assert.That.AreEqual(peaks[i].ChromXs, resultPeaks[i].ChromXs, $"{nameof(IChromatogramPeak.ChromXs)} of peak at index {i} should match.");
            }
        }

        [TestMethod()]
        public void AsPeakArray_Trimed() {
            // Arrange
            var arrayPool = ArrayPool<ValuePeak>.Shared;
            var size = 3;
            var peaks = arrayPool.Rent(size);
            peaks[0] = new ValuePeak(0, 1d, 100d, 1000d);
            peaks[1] = new ValuePeak(1, 2d, 200d, 2000d);
            peaks[2] = new ValuePeak(2, 3d, 300d, 3000d);

            var chromatogram = new Chromatogram(peaks, size, ChromXType.RT, ChromXUnit.Min, arrayPool);

            // Act
            var resultPeaks = chromatogram.AsPeakArray();

            // Assert
            Assert.AreEqual(size, resultPeaks.Count, "The number of peaks should match.");
            for (int i = 0; i < resultPeaks.Count; i++) {
                Assert.AreEqual(peaks[i].Id, resultPeaks[i].ID, $"{nameof(IChromatogramPeak.ID)} of peak at index {i} should match.");
                Assert.AreEqual(peaks[i].Mz, resultPeaks[i].Mass, $"{nameof(IChromatogramPeak.Mass)} of peak at index {i} should match.");
                Assert.AreEqual(peaks[i].Intensity, resultPeaks[i].Intensity, $"{nameof(IChromatogramPeak.Intensity)} of peak at index {i} should match.");
                Assert.AreEqual(peaks[i].Time, resultPeaks[i].ChromXs.Value, $"{nameof(IChromatogramPeak.ChromXs)} of peak at index {i} should match.");
            }
        }

        [TestMethod()]
        public void PeakChromXsTest() {
            // Arrange
            var dummyPeaks = new List<IChromatogramPeak>();
            double chromValue = 5.0;
            double mz = 150.0;
            ChromXType expectedType = ChromXType.RT;
            ChromXUnit expectedUnit = ChromXUnit.Min;
            var chromatogram = new Chromatogram(dummyPeaks, expectedType, expectedUnit);

            // Act
            var result = chromatogram.PeakChromXs(chromValue, mz);

            // Assert
            Assert.AreEqual(chromValue, result.Value, "Chromatogram value should match.");
            Assert.AreEqual(mz, result.Mz.Value, "Mass (m/z) value should match.");
            Assert.AreEqual(expectedType, result.Type, "ChromXType should match.");
            Assert.AreEqual(expectedUnit, result.Unit, "ChromXUnit should match.");
        }

        [TestMethod()]
        public void AsPeakTest() {
            // Arrange
            var peaks = new List<IChromatogramPeak>
            {
                new ChromatogramPeak(0, 50d, 500d, new ChromXs(0.5, ChromXType.RT, ChromXUnit.Min)),
                new ChromatogramPeak(1, 100d, 1000d, new ChromXs(1, ChromXType.RT, ChromXUnit.Min)),
                new ChromatogramPeak(2, 150d, 1500d, new ChromXs(1.5, ChromXType.RT, ChromXUnit.Min)),
                new ChromatogramPeak(3, 200d, 2000d, new ChromXs(2, ChromXType.RT, ChromXUnit.Min)),
                new ChromatogramPeak(4, 250d, 2500d, new ChromXs(2.5, ChromXType.RT, ChromXUnit.Min)),
            };

            var chromatogram = new Chromatogram(peaks, ChromXType.RT, ChromXUnit.Min);
            int topIndex = 1;
            int leftIndex = 0;
            int rightIndex = 2;

            // Act
            var resultPeak = chromatogram.AsPeak(topIndex, leftIndex, rightIndex);

            // Assert
            Assert.That.AreEqual(peaks[topIndex], resultPeak.GetTop(), "Top peak should match the peak at the top index.");
            Assert.That.AreEqual(peaks[leftIndex], resultPeak.GetLeft(), "Left peak should match the peak at the left index.");
            Assert.That.AreEqual(peaks[rightIndex], resultPeak.GetRight(), "Right peak should match the peak at the right index.");
        }

        [TestMethod()]
        public void FindPeakTest() {
            // Arrange
            var peaks = new List<IChromatogramPeak>
            {
                new ChromatogramPeak(0, 50d, 500d, new ChromXs(0.5, ChromXType.RT, ChromXUnit.Min)),
                new ChromatogramPeak(1, 100d, 1500d, new ChromXs(1.0, ChromXType.RT, ChromXUnit.Min)), // Expected to identify this peak
                new ChromatogramPeak(2, 150d, 300d, new ChromXs(1.5, ChromXType.RT, ChromXUnit.Min)),
                new ChromatogramPeak(3, 200d, 200d, new ChromXs(2.0, ChromXType.RT, ChromXUnit.Min)),
            };

            var chromatogram = new Chromatogram(peaks, ChromXType.RT, ChromXUnit.Min);
            var peakFeatureStub = new BaseChromatogramPeakFeature
            {
                ChromScanIdLeft = 0,
                ChromScanIdTop = 1,
                ChromScanIdRight = 2,
                ChromXsLeft = new ChromXs(0.5, ChromXType.RT, ChromXUnit.Min),
                ChromXsTop = new ChromXs(1.0, ChromXType.RT, ChromXUnit.Min),
                ChromXsRight = new ChromXs(1.5, ChromXType.RT, ChromXUnit.Min),
                PeakHeightLeft = 500,
                PeakHeightTop = 1500,
                PeakHeightRight = 300,
            };

            // Act
            var foundPeak = chromatogram.FindPeak(3, 1.0, peakFeatureStub);

            // Assert
            Assert.IsNotNull(foundPeak, "Expected to find a peak but none was found.");
            Assert.AreEqual(peakFeatureStub.ChromScanIdTop, foundPeak.GetTop().ID, "The identified top peak does not match the expected peak.");

        }

        [TestMethod()]
        public void AsPeak_IdentifiesCorrectPeakWithinTimeRange() {
            // Arrange
            var peaks = new List<IChromatogramPeak>
            {
                new ChromatogramPeak(0, 50d, 5d, new ChromXs(0.5, ChromXType.RT, ChromXUnit.Min)),  // Before time range
                new ChromatogramPeak(1, 100d, 10d, new ChromXs(1.0, ChromXType.RT, ChromXUnit.Min)), // Start of time range
                new ChromatogramPeak(2, 150d, 20d, new ChromXs(1.5, ChromXType.RT, ChromXUnit.Min)), // Highest intensity within range
                new ChromatogramPeak(3, 200d, 15d, new ChromXs(2.0, ChromXType.RT, ChromXUnit.Min)), // Within time range
                new ChromatogramPeak(4, 250d, 10d, new ChromXs(2.5, ChromXType.RT, ChromXUnit.Min)), // End of time range
                new ChromatogramPeak(5, 300d, 5d, new ChromXs(3.0, ChromXType.RT, ChromXUnit.Min))   // After time range
            };
            var chromatogram = new Chromatogram(peaks, ChromXType.RT, ChromXUnit.Min);

            double timeLeft = 1.0, timeRight = 2.5;

            // Act
            var peak = chromatogram.AsPeak(timeLeft, timeRight);

            // Assert
            Assert.IsNotNull(peak, "A peak should be identified within the specified time range.");
            Assert.AreEqual(1, peak.GetLeft().ID, "The left boundary of the peak should match the start of the time range.");
            Assert.AreEqual(2, peak.GetTop().ID, "The top of the peak should have the highest intensity within the time range.");
            Assert.AreEqual(4, peak.GetRight().ID, "The right boundary of the peak should match the end of the time range.");
        }

        [TestMethod]
        public void AsPeak_ReturnsNullWhenNoDataPointsWithinTimeRange() {
            // Arrange
            var peaks = new List<IChromatogramPeak>
            {
                new ChromatogramPeak(0, 50d, 5d, new ChromXs(0.5, ChromXType.RT, ChromXUnit.Min)),
                new ChromatogramPeak(1, 100d, 10d, new ChromXs(2.5, ChromXType.RT, ChromXUnit.Min))
            };
            var chromatogram = new Chromatogram(peaks, ChromXType.RT, ChromXUnit.Min);

            // Specifying a time range that doesn't include any of the defined peaks
            double timeLeft = 3.0, timeRight = 4.0;

            // Act
            var peak = chromatogram.AsPeak(timeLeft, timeRight);

            // Assert
            // Assuming that AsPeak method is designed to return null when no peaks are found within the specified time range.
            Assert.IsNull(peak, "AsPeak should return null when no data points are found within the specified time range.");
        }

        [TestMethod]
        public void AsPeak_IdentifiesCorrectPeakWithinTimeBoundaries() {
            // Arrange
            var peaks = new List<IChromatogramPeak>
            {
                new ChromatogramPeak(1, 100d, 10d, new ChromXs(0.8, ChromXType.RT, ChromXUnit.Min)),
                new ChromatogramPeak(2, 150d, 20d, new ChromXs(1.0, ChromXType.RT, ChromXUnit.Min)), // Closest to top time
                new ChromatogramPeak(3, 200d, 15d, new ChromXs(1.2, ChromXType.RT, ChromXUnit.Min)),
            };
            var chromatogram = new Chromatogram(peaks, ChromXType.RT, ChromXUnit.Min);

            double timeLeft = 0.5, timeTop = 1.0, timeRight = 1.5;

            // Act
            var peak = chromatogram.AsPeak(timeLeft, timeTop, timeRight);

            // Assert
            Assert.IsNotNull(peak, "A peak should be identified within the specified time boundaries.");
            Assert.AreEqual(2, peak.GetTop().ID, "The top of the peak should be closest to the specified top time.");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void AsPeak_ThrowsArgumentExceptionForInvalidTimeBoundaries() {
            // Arrange
            var peaks = new List<IChromatogramPeak>(); // Empty peaks for this test case
            var chromatogram = new Chromatogram(peaks, ChromXType.RT, ChromXUnit.Min);

            // Invalid time boundaries
            double timeLeft = 1.0, timeTop = 0.5, timeRight = 0.8;

            // Act & Assert
            var peak = chromatogram.AsPeak(timeLeft, timeTop, timeRight);
            Assert.IsNull(peak);
        }
    }
}