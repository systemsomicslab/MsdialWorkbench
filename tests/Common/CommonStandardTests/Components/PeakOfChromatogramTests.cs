using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Components.Tests
{
    [TestClass()]
    public class PeakOfChromatogramTests
    {
        private PeakOfChromatogram _peakOfChromatogram;
        private ValuePeak[] _peaks;

        [TestInitialize]
        public void SetUp() {
            // Initialize your test peaks and PeakOfChromatogram here
            _peaks = new ValuePeak[]
            {
                new ValuePeak(0, 0.5, 50d, 100d),
                new ValuePeak(1, 1.0, 100d, 200d), // Assume this is the top peak
                new ValuePeak(2, 1.5, 150d, 150d)
            };

            _peakOfChromatogram = new PeakOfChromatogram(_peaks, ChromXType.RT, ChromXUnit.Min, 1, 0, 2); // Top at index 1, left at 0, right at 2
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
            var peaks = new ValuePeak[]
            {
                new ValuePeak(0, 0.2, 50d, 5d),  // Outside left boundary
                new ValuePeak(1, 0.5, 100d, 10d), // Left boundary
                new ValuePeak(2, 0.75, 120d, 30d), // Approach peak
                new ValuePeak(3, 1.0, 150d, 50d),  // Peak
                new ValuePeak(4, 1.25, 180d, 25d), // Depart peak
                new ValuePeak(5, 1.5, 200d, 15d),  // Right boundary
                new ValuePeak(6, 1.8, 250d, 5d)   // Outside right boundary
            };
            var chromatogram = new PeakOfChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 3, 1, 5);

            // Act: Extract the slice of the peak area from the chromatogram.
            var slice = chromatogram.SlicePeakArea();

            // Assert: Verify that the extracted slice matches the expected peak area, excluding outside boundary peaks.
            Assert.AreEqual(5, slice.Length, "The extracted slice should contain exactly 5 peaks, matching the peak and its boundaries.");
            Assert.AreEqual(peaks[1].Id, slice[0].ID, "The first peak in the slice should match the left boundary peak.");
            Assert.AreEqual(peaks[2].Id, slice[1].ID, "The second peak in the slice should be approaching the peak.");
            Assert.AreEqual(peaks[3].Id, slice[2].ID, "The third peak in the slice should be at the peak.");
            Assert.AreEqual(peaks[4].Id, slice[3].ID, "The fourth peak in the slice should be departing the peak.");
            Assert.AreEqual(peaks[5].Id, slice[4].ID, "The fifth peak in the slice should match the right boundary peak.");
        }

        [TestMethod]
        public void GetTop_ReturnsTopPeak()
        {
            var topPeak = _peakOfChromatogram.GetTop();
            Assert.AreEqual(200d, topPeak.Intensity, "The top peak's intensity should match the expected value.");
        }

        [TestMethod]
        public void GetLeft_ReturnsLeftBoundaryPeak()
        {
            var leftPeak = _peakOfChromatogram.GetLeft();
            Assert.AreEqual(100d, leftPeak.Intensity, "The left boundary peak's intensity should match the expected value.");
        }

        [TestMethod]
        public void GetRight_ReturnsRightBoundaryPeak()
        {
            var rightPeak = _peakOfChromatogram.GetRight();
            Assert.AreEqual(150d, rightPeak.Intensity, "The right boundary peak's intensity should match the expected value.");
        }
    }
}