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
        public void SetUp()
        {
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
        public void CalculateArea_ReturnsExpectedResult()
        {
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
        public void CalculateBaseLineArea_ReturnsExpectedResult()
        {
            // Act
            double baselineArea = _peakOfChromatogram.CalculateBaseLineArea();

            // Assert
            // Calculation based on the base intensity of left and right peaks and their chromXs values
            double expectedBaselineArea = (100d + 150d) * 1 / 2; 
            Assert.AreEqual(expectedBaselineArea, baselineArea, "Calculated baseline area does not match the expected result.");
        }

        [TestMethod]
        public void CalculatePeakAmplitude_ReturnsExpectedResult()
        {
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
        public void IsValid_GivenMinimumAmplitude_ReturnsCorrectValidity(double minimumAmplitude, bool expectedValidity)
        {
            // Arrange
            // Set the minimum amplitude to test against

            // Act
            bool isValid = _peakOfChromatogram.IsValid(minimumAmplitude);

            // Assert
            // Determine if the peak should be valid based on the test setup and assert accordingly
            Assert.AreEqual(expectedValidity, isValid, "Peak validity does not match the expected result.");
        }
    }
}