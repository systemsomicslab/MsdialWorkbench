using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Components.Tests
{
    [TestClass()]
    public class PeakOfChromatogramTests
    {
        private PeakOfChromatogram _peakOfChromatogram;
        private List<IChromatogramPeak> _peaks;

        [TestInitialize]
        public void SetUp() {
            // Initialize your test peaks and PeakOfChromatogram here
            _peaks = new List<IChromatogramPeak>
            {
                new ChromatogramPeak(0, 50d, 100d, new ChromXs(0.5, ChromXType.RT, ChromXUnit.Min)),
                new ChromatogramPeak(1, 100d, 200d, new ChromXs(1.0, ChromXType.RT, ChromXUnit.Min)), // Assume this is the top peak
                new ChromatogramPeak(2, 150d, 150d, new ChromXs(1.5, ChromXType.RT, ChromXUnit.Min))
            };

            _peakOfChromatogram = new PeakOfChromatogram(_peaks, ChromXType.RT, 1, 0, 2); // Top at index 1, left at 0, right at 2
        }

        [TestMethod]
        public void CalculateArea_ReturnsExpectedResult() {
            // Arrange
            // Expected area calculation based on the trapezoidal rule

            // Act
            double area = _peakOfChromatogram.CalculateArea();

            // Assert
            // Replace the expectedArea with your calculated expected result based on your test data
            double expectedArea = (100d + 200d) * .5 / 2 + (200 + 150d) * .5 / 2;
            Assert.AreEqual(expectedArea, area, "Calculated area does not match the expected result.");
        }

        [TestMethod]
        public void CalculateBaseLineArea_ReturnsExpectedResult() {
            // Act
            double baselineArea = _peakOfChromatogram.CalculateBaseLineArea();

            // Assert
            // Calculation based on the base intensity of left and right peaks and their chromXs values
            double expectedBaselineArea = (100d + 150d) * 1 / 2;
            Assert.AreEqual(expectedBaselineArea, baselineArea, "Calculated baseline area does not match the expected result.");
        }

        [TestMethod]
        public void CalculatePeakAmplitude_ReturnsExpectedResult() {
            // Act
            double amplitude = _peakOfChromatogram.CalculatePeakAmplitude();

            // Assert
            // Expected amplitude is the top peak's intensity minus the minimum of left and right peak intensities
            double expectedAmplitude = 200d - 100d;
            Assert.AreEqual(expectedAmplitude, amplitude, "Calculated amplitude does not match the expected result.");
        }

        [DataTestMethod]
        [DataRow(50d, true)]
        [DataRow(150d, false)]
        public void IsValid_GivenMinimumAmplitude_ReturnsCorrectValidity(double minimumAmplitude, bool expectedValidity) {
            // Arrange
            // Set the minimum amplitude to test against

            // Act
            bool isValid = _peakOfChromatogram.IsValid(minimumAmplitude);

            // Assert
            // Determine if the peak should be valid based on the test setup and assert accordingly
            Assert.AreEqual(expectedValidity, isValid, "Peak validity does not match the expected result.");
        }

        [TestMethod()]
        public void SlicePeakAreaTest() {
            // Arrange: Setup a longer chromatogram with a more defined peak and specify broader peak boundaries.
            var peaks = new List<IChromatogramPeak>
            {
                new ChromatogramPeak(0, 50d, 5d, new ChromXs(0.2, ChromXType.RT, ChromXUnit.Min)),  // Outside left boundary
                new ChromatogramPeak(1, 100d, 10d, new ChromXs(0.5, ChromXType.RT, ChromXUnit.Min)), // Left boundary
                new ChromatogramPeak(2, 120d, 30d, new ChromXs(0.75, ChromXType.RT, ChromXUnit.Min)), // Approach peak
                new ChromatogramPeak(3, 150d, 50d, new ChromXs(1.0, ChromXType.RT, ChromXUnit.Min)),  // Peak
                new ChromatogramPeak(4, 180d, 25d, new ChromXs(1.25, ChromXType.RT, ChromXUnit.Min)), // Depart peak
                new ChromatogramPeak(5, 200d, 15d, new ChromXs(1.5, ChromXType.RT, ChromXUnit.Min)),  // Right boundary
                new ChromatogramPeak(6, 250d, 5d, new ChromXs(1.8, ChromXType.RT, ChromXUnit.Min))   // Outside right boundary
            };
            var chromatogram = new PeakOfChromatogram(peaks, ChromXType.RT, 3, 1, 5);

            // Act: Extract the slice of the peak area from the chromatogram.
            var slice = chromatogram.SlicePeakArea();

            // Assert: Verify that the extracted slice matches the expected peak area, excluding outside boundary peaks.
            Assert.AreEqual(5, slice.Length, "The extracted slice should contain exactly 5 peaks, matching the peak and its boundaries.");
            Assert.AreEqual(peaks[1].ID, slice[0].ID, "The first peak in the slice should match the left boundary peak.");
            Assert.AreEqual(peaks[2].ID, slice[1].ID, "The second peak in the slice should be approaching the peak.");
            Assert.AreEqual(peaks[3].ID, slice[2].ID, "The third peak in the slice should be at the peak.");
            Assert.AreEqual(peaks[4].ID, slice[3].ID, "The fourth peak in the slice should be departing the peak.");
            Assert.AreEqual(peaks[5].ID, slice[4].ID, "The fifth peak in the slice should match the right boundary peak.");
        }
    }
}