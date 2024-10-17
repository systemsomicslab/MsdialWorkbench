using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Components.Tests
{
    [TestClass()]
    public class ExtractedIonChromatogramTests
    {
        private ExtractedIonChromatogram _chromatogram;
        private ChromXType _type;
        private ChromXUnit _unit;
        private int _size;

        [TestInitialize]
        public void SetUp() {
            var pool = ArrayPool<ValuePeak>.Shared;
            _size = 3;
            var peaks = pool.Rent(_size);
            peaks[0] = new ValuePeak(1, 0.5, 100, 1000); // Lower intensity
            peaks[1] = new ValuePeak(2, 1.0, 200, 2000); // Highest intensity
            peaks[2] = new ValuePeak(3, 1.5, 300, 1500); // Medium intensity
            _type = ChromXType.RT;
            _unit = ChromXUnit.Min;
            _chromatogram = new ExtractedIonChromatogram(peaks, _size, ChromXType.RT, ChromXUnit.Min, extractedMz: 150, pool);
        }

        [TestMethod]
        public void Constructor_InitializesCorrectly() {
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

        [DataTestMethod]
        [DataRow(0, true)]
        [DataRow(1, false)]
        public void IsEmptyTest(int size, bool expected) {
            // Arrange
            var chromatogram = new ExtractedIonChromatogram(new ValuePeak[4], size, ChromXType.RT, ChromXUnit.Min, 100.0, null);

            // Act
            var result = chromatogram.IsEmpty;

            // Assert
            Assert.AreEqual(expected, result);
        }


        [TestMethod]
        public void AsPeakArray_ReturnsCorrectCopyOfPeaks() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100, 1000),
                new ValuePeak(2, 2.0, 200, 2000)
            };
            var chromatogram = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 150);

            // Act
            var returnedPeaks = chromatogram.AsPeakArray();

            // Assert
            Assert.IsNotNull(returnedPeaks);
            Assert.AreEqual(peaks.Length, returnedPeaks.Length, "The number of peaks should match.");
            for (int i = 0; i < peaks.Length; i++) {
                Assert.AreEqual(peaks[i].Id, returnedPeaks[i].Id, $"Peak {i} Id mismatch.");
                Assert.AreEqual(peaks[i].Time, returnedPeaks[i].Time, $"Peak {i} Time mismatch.");
                Assert.AreEqual(peaks[i].Intensity, returnedPeaks[i].Intensity, $"Peak {i} Intensity mismatch.");
                Assert.AreEqual(peaks[i].Mz, returnedPeaks[i].Mz, $"Peak {i} Mz mismatch.");
            }

            // Ensure the returned array is a copy, not a reference to the original array
            Assert.AreNotSame(peaks, returnedPeaks, "The method should return a copy of the peak array, not a reference to the original array.");
        }

        [TestMethod]
        public void Dispose_ReleasesResources() {
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

        [TestMethod]
        public void Time_ReturnsCorrectValue() {
            var time = _chromatogram.Time(1);
            Assert.AreEqual(1.0, time);
        }

        [TestMethod]
        public void Intensity_ReturnsCorrectValue() {
            var intensity = _chromatogram.Intensity(1);
            Assert.AreEqual(2000, intensity);
        }

        [TestMethod]
        public void Mz_ReturnsCorrectValue() {
            var mz = _chromatogram.Mz(1);
            Assert.AreEqual(200, mz);
        }

        [TestMethod]
        public void Id_ReturnsCorrectValue() {
            var id = _chromatogram.Id(1);
            Assert.AreEqual(2, id);
        }

        [TestMethod]
        public void Methods_ThrowObjectDisposedException_AfterDispose() {
            _chromatogram.Dispose();

            // Attempt to access a property to check for the exception.
            Assert.ThrowsException<ObjectDisposedException>(() => _chromatogram.Time(0));
            Assert.ThrowsException<ObjectDisposedException>(() => _chromatogram.Intensity(0));
            Assert.ThrowsException<ObjectDisposedException>(() => _chromatogram.Mz(0));
            Assert.ThrowsException<ObjectDisposedException>(() => _chromatogram.Id(0));
        }

        [TestMethod]
        public void PeakChromXs_CreatesCorrectChromXsObject() {
            // Arrange
            double chromValue = 2.5;
            double mz = 250;

            var peaks = new[]
            {
                new ValuePeak(1, 0.5, 100, 1000), // Lower intensity
                new ValuePeak(2, 1.0, 200, 2000), // Highest intensity
                new ValuePeak(3, 1.5, 300, 1500)  // Medium intensity
            };
            var chromatogram = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 150.5);

            // Act
            var result = chromatogram.PeakChromXs(chromValue, mz);

            // Assert
            Assert.AreEqual(chromValue, result.Value);
            Assert.AreEqual(mz, result.Mz.Value);
            Assert.AreEqual(ChromXType.RT, result.Type);
            Assert.AreEqual(ChromXUnit.Min, result.Unit);
        }

        [TestMethod]
        public void GetPeakTopId_ReturnsIndexWithHighestIntensity() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 0.5, 100, 1000), // Lower intensity
                new ValuePeak(2, 1.0, 200, 2000), // Highest intensity
                new ValuePeak(3, 1.5, 300, 1500)  // Medium intensity
            };
            var chromatogram = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 150.5);

            // Act
            var peakTopId = chromatogram.GetPeakTopId(0, 3);

            // Assert
            Assert.AreEqual(1, peakTopId, "The method should return the index of the peak with the highest intensity.");
        }

        [TestMethod]
        public void ShrinkPeakRange_AdjustsPeakRangeCorrectly() {
            // Arrange
            var peaks = new[]
            {
                    new ValuePeak(0, 1.0, 150, 400),  // Initial start
                    new ValuePeak(1, 2.0, 150, 600),
                    new ValuePeak(2, 3.0, 150, 500),  // Start
                    new ValuePeak(3, 4.0, 150, 2000), // Peak top
                    new ValuePeak(4, 5.0, 150, 800),
                    new ValuePeak(5, 6.0, 150, 300),
                    new ValuePeak(6, 7.0, 150, 200),  // End
                    new ValuePeak(7, 8.0, 150, 300),  // Initial end
                };
            var chromatogram = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 150.5);
            int start = 0, end = peaks.Length, averagePeakWidth = 1;

            // Act
            var (newStart, peakTopId, newEnd) = chromatogram.ShrinkPeakRange(start, end, averagePeakWidth);

            // Assert
            Assert.AreEqual(2, newStart);
            Assert.AreEqual(3, peakTopId);
            Assert.AreEqual(7, newEnd);
        }

        [TestMethod]
        public void PeakHeightFromBounds_CalculatesCorrectHeights() {
            // Arrange
            var peaks = new[]
            {
                    new ValuePeak(1, 1.0, 100, 500), // Start
                    new ValuePeak(2, 2.0, 200, 2000), // Peak top
                    new ValuePeak(3, 3.0, 300, 800)  // End
                };
            var chromatogram = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 150.5);
            int start = 0, end = peaks.Length, top = 1;

            // Act
            var (minHeight, maxHeight) = chromatogram.PeakHeightFromBounds(start, end, top);

            // Assert
            Assert.AreEqual(1200, minHeight, "The minimum height calculation is incorrect.");
            Assert.AreEqual(1500, maxHeight, "The maximum height calculation is incorrect.");
        }

        [TestMethod]
        public void HasBoundaryBelowThreshold_ReturnsCorrectly() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 0.5, 100, 200), // Start peak with low intensity
                new ValuePeak(2, 1.0, 200, 1000), // Middle peak with high intensity
                new ValuePeak(3, 1.5, 300, 150), // End peak with low intensity
            };
            var chromatogram = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 150.5);
            double threshold = 300;

            // Act & Assert
            Assert.IsTrue(chromatogram.HasBoundaryBelowThreshold(0, peaks.Length, threshold), "Should return true as the start and end boundaries are below the threshold.");

            // Testing with a threshold lower than the start and end peaks' intensity
            Assert.IsFalse(chromatogram.HasBoundaryBelowThreshold(0, peaks.Length, 100), "Should return false as neither boundary is below the lower threshold.");
        }

        [TestMethod]
        public void IntensityDifference_ReturnsCorrectDifference() {
            // Arrange
            var chromatogram = _chromatogram;

            // Indices of the peaks to compare
            int firstPeakIndex = 1;
            int secondPeakIndex = 0;

            // Expected difference based on predefined peaks in CreateTestChromatogram()
            double expectedDifference = 1000;

            // Act
            double difference = chromatogram.IntensityDifference(firstPeakIndex, secondPeakIndex);

            // Assert
            Assert.AreEqual(expectedDifference, difference, "The calculated intensity difference does not match the expected value.");
        }

        [TestMethod]
        public void TimeDifference_ReturnsCorrectDifference() {
            // Arrange
            var chromatogram = _chromatogram;

            // Indices of the peaks to compare
            int firstPeakIndex = 1;
            int secondPeakIndex = 0;

            // Expected difference based on
            double expectedDifference = 0.5;

            // Act
            double difference = chromatogram.TimeDifference(firstPeakIndex, secondPeakIndex);

            // Assert
            Assert.AreEqual(expectedDifference, difference, "The calculated time difference does not match the expected value.");
        }

        [TestMethod]
        public void CalculateArea_ReturnsCorrectValue() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100, 500),
                new ValuePeak(2, 2.0, 200, 1000)
            };
            var chromatogram = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 150);
            double expectedArea = 750; // Calculated manually for test purposes

            // Act
            double area = chromatogram.CalculateArea(0, 1);

            // Assert
            Assert.AreEqual(expectedArea, area, "The calculated area does not match the expected value.");
        }

        [TestMethod]
        public void IsValidPeakTop_ValidatesCorrectly() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(0, 0.5, 50, 10), // Lower intensity (invalid peak top due to intensity)
                new ValuePeak(1, 1.0, 100, 500), // Valid peak top
                new ValuePeak(2, 1.5, 150, 200)  // Lower intensity (valid peak top due to position)
            };
            var chromatogram = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 150);
            // Act & Assert for a valid peak top
            bool isValid = chromatogram.IsValidPeakTop(1);
            Assert.IsTrue(isValid, "Peak at index 1 should be a valid peak top.");

            // Act & Assert for an invalid peak top because it's the first point (boundary condition)
            isValid = chromatogram.IsValidPeakTop(0);
            Assert.IsFalse(isValid, "Peak at index 0 should not be considered a valid peak top due to it being at the boundary.");

            // Act & Assert for an invalid peak top due to adjacent points' intensities
            // Adding additional peaks to demonstrate an invalid case based on intensities
            peaks = new[]
            {
                new ValuePeak(0, 0.5, 50, 0), // Zero intensity (boundary condition)
                new ValuePeak(1, 1.0, 100, 500), // Peak top
                new ValuePeak(2, 1.5, 150, 0)  // Zero intensity
            };
            chromatogram = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 150);
            isValid = chromatogram.IsValidPeakTop(1);
            Assert.IsFalse(isValid, "Peak at index 1 should not be considered a valid peak top due to adjacent points having zero intensity.");
        }

        [TestMethod]
        public void CountSpikes_NoSignificantSpikes_ReturnsZero() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 12.0),
                new ValuePeak(3, 3.0, 100.0, 11.0),
                // No significant spikes in intensity; differences are below the threshold
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            double threshold = 5.0;

            // Act
            int spikeCount = eic.CountSpikes(0, peaks.Length, threshold);

            // Assert
            Assert.AreEqual(0, spikeCount);
        }

        [TestMethod]
        public void CountSpikes_SingleSignificantSpike_ReturnsOne() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 20.0), // Significant spike here
                new ValuePeak(3, 3.0, 100.0, 5.0),  // Significant spike here
                new ValuePeak(4, 4.0, 100.0, 10.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            double threshold = 5.0;

            // Act
            int spikeCount = eic.CountSpikes(0, peaks.Length, threshold);

            // Assert
            Assert.AreEqual(1, spikeCount);
        }

        [TestMethod]
        public void CountSpikes_MultipleSignificantSpikes_ReturnsCorrectCount() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 21.0), // Significant spike
                new ValuePeak(3, 3.0, 100.0, 10.0),
                new ValuePeak(4, 4.0, 100.0, 20.0), // Another significant spike
                new ValuePeak(5, 5.0, 100.0, 9.0),
                new ValuePeak(6, 6.0, 100.0, 10.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            double threshold = 5.0;

            // Act
            int spikeCount = eic.CountSpikes(0, peaks.Length, threshold);

            // Assert
            Assert.AreEqual(2, spikeCount);
        }

        [TestMethod]
        public void IsPeakTop_WithHigherIntensityThanNeighbors_ReturnsTrue() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 20.0), // This peak is higher than its neighbors
                new ValuePeak(3, 3.0, 100.0, 10.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isPeakTop = eic.IsPeakTop(1); // Index of the peak in question

            // Assert
            Assert.IsTrue(isPeakTop);
        }

        [TestMethod]
        public void IsPeakTop_AtArrayEdges_ReturnsFalse() {
            // Testing the behavior for peaks at the array edges might depend on the implementation details.
            // If peaks at edges cannot be peak tops due to lack of neighbors, this test should expect false.
            // However, if edge behavior is handled differently, adjust the test accordingly.

            // Arrange (example scenario where the first and last peaks cannot be peak tops)
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0), // Edge peak
                new ValuePeak(2, 2.0, 100.0, 10.0),
                new ValuePeak(3, 3.0, 100.0, 20.0), // Edge peak
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act & Assert
            Assert.IsFalse(eic.IsPeakTop(0), "First peak should not be considered a peak top.");
            Assert.IsFalse(eic.IsPeakTop(peaks.Length - 1), "Last peak should not be considered a peak top.");
        }

        [TestMethod]
        public void IsPeakTop_InAFlatRegion_ReturnsFalse() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0),
                new ValuePeak(2, 2.0, 100.0, 20.0), // Part of a flat region
                new ValuePeak(3, 3.0, 100.0, 20.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isFlatTopPeak = eic.IsPeakTop(1); // Index of the peak in question

            // Assert
            Assert.IsTrue(isFlatTopPeak, "Peak in a flat region should be considered a peak top candidate.");
        }

        [TestMethod]
        public void IsLargePeakTop_WithProperIntensityGradient_ReturnsTrue() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 15.0), // Immediate neighbor
                new ValuePeak(3, 3.0, 100.0, 20.0), // Large peak top
                new ValuePeak(4, 4.0, 100.0, 15.0), // Immediate neighbor
                new ValuePeak(5, 5.0, 100.0, 10.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isLargePeakTop = eic.IsLargePeakTop(2); // Index (not ID) of the peak in question

            // Assert
            Assert.IsTrue(isLargePeakTop);
        }

        [TestMethod]
        public void IsLargePeakTop_WithoutSufficientIntensityGradient_ReturnsFalse() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 15.0),
                new ValuePeak(2, 2.0, 100.0, 20.0), // This is not a large peak top because the next-outer neighbors do not have a lower intensity
                new ValuePeak(3, 3.0, 100.0, 20.0),
                new ValuePeak(4, 4.0, 100.0, 20.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isNotLargePeakTop = eic.IsLargePeakTop(2); // Index of the peak in question

            // Assert
            Assert.IsFalse(isNotLargePeakTop);
        }

        [TestMethod]
        public void IsLargePeakTop_NearArrayEdges_ReturnsFalse() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0), // Edge case
                new ValuePeak(2, 2.0, 100.0, 15.0),
                new ValuePeak(3, 3.0, 100.0, 20.0), // Potential large peak top but too close to the edge
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act & Assert
            Assert.IsFalse(eic.IsLargePeakTop(0), "First peak cannot be a large peak top due to edge location.");
            Assert.IsFalse(eic.IsLargePeakTop(2), "Last peak cannot be a large peak top due to insufficient neighbors.");
        }

        [TestMethod]
        public void IsLargePeakTop_InAFlatRegion_ReturnsFalse() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0),
                new ValuePeak(2, 2.0, 100.0, 20.0), // Part of a flat region, not a distinct large peak top
                new ValuePeak(3, 3.0, 100.0, 20.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isInFlatRegion = eic.IsLargePeakTop(1); // Index of the peak in question

            // Assert
            Assert.IsFalse(isInFlatRegion, "Peak in a flat region should not be considered a large peak top.");
        }

        [TestMethod]
        public void IsBroadPeakTop_WithCorrectIntensityGradient_ReturnsTrue() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 15.0), // Outer neighbor
                new ValuePeak(2, 2.0, 100.0, 18.0), // Immediate neighbor
                new ValuePeak(3, 3.0, 100.0, 20.0), // Broad peak top
                new ValuePeak(4, 4.0, 100.0, 18.0), // Immediate neighbor
                new ValuePeak(5, 5.0, 100.0, 16.0), // Outer neighbor with slightly less intensity, still considered a broad peak due to one side's criteria being met
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isBroadPeakTop = eic.IsBroadPeakTop(2); // Index of the peak in question

            // Assert
            Assert.IsTrue(isBroadPeakTop);
        }

        [TestMethod]
        public void IsBroadPeakTop_WithoutSufficientIntensityGradient_ReturnsFalse() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(0, 0.0, 100.0, 30.0),
                new ValuePeak(1, 1.0, 100.0, 20.0),
                new ValuePeak(2, 2.0, 100.0, 20.0), // This peak does not qualify as a broad peak top
                new ValuePeak(3, 3.0, 100.0, 20.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isNotBroadPeakTop = eic.IsBroadPeakTop(2); // Index of the peak in question

            // Assert
            Assert.IsFalse(isNotBroadPeakTop);
        }

        [TestMethod]
        public void IsBroadPeakTop_NearArrayEdges_ReturnsFalse() {
            // Testing behavior for peaks near the array edges may vary based on how broadness is defined at edges.

            // Arrange (assuming edge peaks cannot qualify due to lack of neighbors)
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0), // Potential edge case
                new ValuePeak(2, 2.0, 100.0, 22.0),
                new ValuePeak(3, 3.0, 100.0, 20.0), // Potential edge case
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act & Assert
            Assert.IsFalse(eic.IsBroadPeakTop(0), "First peak should not be considered a broad peak top due to edge location.");
            Assert.IsFalse(eic.IsBroadPeakTop(2), "Last peak should not be considered a broad peak top due to edge location.");
        }

        [TestMethod]
        public void IsBroadPeakTop_InAFlatRegion_ReturnsTrue() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0),
                new ValuePeak(2, 2.0, 100.0, 20.0), // Part of a flat region, should be a distinct broad peak top
                new ValuePeak(3, 3.0, 100.0, 20.0),
                new ValuePeak(4, 4.0, 100.0, 20.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isFlatRegion = eic.IsBroadPeakTop(1); // Index of the peak in question

            // Assert
            Assert.IsTrue(isFlatRegion, "Peak in a flat region should be considered a broad peak top candidate.");
        }

        [TestMethod]
        public void IsBottom_WithLowerIntensityThanNeighbors_ReturnsTrue() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0),
                new ValuePeak(2, 2.0, 100.0, 10.0), // This peak is a bottom
                new ValuePeak(3, 3.0, 100.0, 20.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isBottom = eic.IsBottom(1); // Index of the peak in question

            // Assert
            Assert.IsTrue(isBottom);
        }

        [TestMethod]
        public void IsBottom_AtArrayEdges_ReturnsFalse() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0), // Edge case
                new ValuePeak(2, 2.0, 100.0, 20.0),
                new ValuePeak(3, 3.0, 100.0, 10.0), // Potential bottom but too close to the edge
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act & Assert
            Assert.IsFalse(eic.IsBottom(0), "First peak cannot be a bottom due to edge location.");
            Assert.IsFalse(eic.IsBottom(peaks.Length - 1), "Last peak cannot be a bottom due to insufficient neighbors.");
        }

        [TestMethod]
        public void IsBottom_InAFlatRegion_ReturnsTrue() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 10.0), // Part of a flat region, a distinct bottom
                new ValuePeak(3, 3.0, 100.0, 10.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isInFlatRegion = eic.IsBottom(1); // Index of the peak in question

            // Assert
            Assert.IsTrue(isInFlatRegion, "Peak in a flat region should be considered a bottom.");
        }

        [TestMethod]
        public void IsLargeBottom_WithProperIntensityGradient_ReturnsTrue() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 30.0),
                new ValuePeak(2, 2.0, 100.0, 20.0), // Immediate neighbor
                new ValuePeak(3, 3.0, 100.0, 10.0), // Large bottom
                new ValuePeak(4, 4.0, 100.0, 20.0), // Immediate neighbor
                new ValuePeak(5, 5.0, 100.0, 30.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isLargeBottom = eic.IsLargeBottom(2); // Index of the peak in question

            // Assert
            Assert.IsTrue(isLargeBottom);
        }

        [TestMethod]
        public void IsLargeBottom_WithoutSufficientIntensityGradient_ReturnsFalse() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0),
                new ValuePeak(2, 2.0, 100.0, 10.0), // This is not a large bottom because the next-outer neighbors do not have higher intensity
                new ValuePeak(3, 3.0, 100.0, 20.0),
                new ValuePeak(4, 4.0, 100.0, 20.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isNotLargeBottom = eic.IsLargeBottom(2); // Index of the peak in question

            // Assert
            Assert.IsFalse(isNotLargeBottom);
        }

        [TestMethod]
        public void IsLargeBottom_NearArrayEdges_ReturnsFalse() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 30.0), // Edge case
                new ValuePeak(2, 2.0, 100.0, 20.0),
                new ValuePeak(3, 3.0, 100.0, 10.0), // Potential large bottom but too close to the edge
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            // Act & Assert
            Assert.IsFalse(eic.IsLargeBottom(0), "First peak cannot be a large bottom due to edge location.");
            Assert.IsFalse(eic.IsLargeBottom(peaks.Length - 1), "Last peak cannot be a large bottom due to insufficient neighbors.");
        }

        [TestMethod]
        public void IsLargeBottom_InAFlatRegion_ReturnsFalse() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 10.0), // Part of a flat region, not a distinct large bottom
                new ValuePeak(3, 3.0, 100.0, 10.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isInFlatRegion = eic.IsLargeBottom(1); // Index of the peak in question

            // Assert
            Assert.IsFalse(isInFlatRegion, "Peak in a flat region should not be considered a large bottom.");
        }

        [TestMethod]
        public void IsBroadBottom_WithValleyCharacteristics_ReturnsTrue() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 25.0),
                new ValuePeak(2, 2.0, 100.0, 20.0), // Immediate neighbor
                new ValuePeak(3, 3.0, 100.0, 15.0), // Broad bottom
                new ValuePeak(4, 4.0, 100.0, 20.0), // Immediate neighbor
                new ValuePeak(5, 5.0, 100.0, 25.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isBroadBottom = eic.IsBroadBottom(2); // Index of the peak in question

            // Assert
            Assert.IsTrue(isBroadBottom);
        }

        [TestMethod]
        public void IsBroadBottom_WithoutBroadValleyCharacteristics_ReturnsFalse() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 15.0),
                new ValuePeak(2, 2.0, 100.0, 15.0), // Not a broad bottom due to lack of a wider base
                new ValuePeak(3, 3.0, 100.0, 15.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isNotBroadBottom = eic.IsBroadBottom(2); // Index of the peak in question

            // Assert
            Assert.IsFalse(isNotBroadBottom);
        }

        [TestMethod]
        public void IsBroadBottom_AtArrayEdges_ReturnsFalse() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0), // Potential broad bottom but at the edge
                new ValuePeak(2, 2.0, 100.0, 25.0),
                new ValuePeak(3, 3.0, 100.0, 20.0), // Another potential broad bottom but at the other edge
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act & Assert
            Assert.IsFalse(eic.IsBroadBottom(0), "Edge peak cannot be a broad bottom due to insufficient comparison points.");
            Assert.IsFalse(eic.IsBroadBottom(peaks.Length - 1), "Edge peak cannot be a broad bottom due to insufficient comparison points.");
        }

        [TestMethod]
        public void IsBroadBottom_InAFlatRegion_ReturnsTrue() {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 15.0),
                new ValuePeak(2, 2.0, 100.0, 15.0), // Part of a flat region, potentially a broad bottom
                new ValuePeak(3, 3.0, 100.0, 15.0),
                new ValuePeak(4, 4.0, 100.0, 15.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            bool isBroadBottomInFlatRegion = eic.IsBroadBottom(1); // Index of the peak in question

            // Assert
            Assert.IsTrue(isBroadBottomInFlatRegion, "Peak in a flat region should be considered a broad bottom due to its broader valley characteristics.");
        }

        [TestMethod]
        public void IsFlat_WithinAmplitudeNoiseThreshold_ReturnsTrue()
        {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 10.5), // Center peak
                new ValuePeak(3, 3.0, 100.0, 10.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            double amplitudeNoise = 0.5 + 1e-9;

            // Act
            bool isFlat = eic.IsFlat(1, amplitudeNoise); // Index of the center peak in question

            // Assert
            Assert.IsTrue(isFlat);
        }

        [TestMethod]
        public void IsFlat_ExceedingAmplitudeNoiseThreshold_ReturnsFalse()
        {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 9.0),
                new ValuePeak(2, 2.0, 100.0, 11.0), // Center peak
                new ValuePeak(3, 3.0, 100.0, 9.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            double amplitudeNoise = 0.5;

            // Act
            bool isNotFlat = eic.IsFlat(2, amplitudeNoise); // Index of the center peak in question

            // Assert
            Assert.IsFalse(isNotFlat);
        }

        [TestMethod]
        public void IsFlat_AtArrayEdges_WithSufficientData_ReturnsAppropriateResult()
        {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0), // Edge peak, testing if considered flat
                new ValuePeak(2, 2.0, 100.0, 10.5),
                new ValuePeak(3, 3.0, 100.0, 10.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            double amplitudeNoise = 0.5;

            // Act & Assert for both edges
            bool isFlatStart = eic.IsFlat(0, amplitudeNoise); // Checking if the first peak is considered flat
            bool isFlatEnd = eic.IsFlat(peaks.Length - 1, amplitudeNoise); // Checking if the last peak is considered flat

            // Assert
            Assert.IsFalse(isFlatStart, "Edge peak should not be considered flat due to insufficient neighbors.");
            Assert.IsFalse(isFlatEnd, "Edge peak should not be considered flat due to insufficient neighbors.");
        }

        [TestMethod]
        public void IsFlat_WithVeryLowAmplitudeNoiseThreshold_ReturnsFalseForNormallyFlatSegment()
        {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 10.1), // Center peak, normally considered flat
                new ValuePeak(3, 3.0, 100.0, 10.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            double amplitudeNoise = 0.05; // Very low threshold

            // Act
            bool isNotFlat = eic.IsFlat(1, amplitudeNoise); // Index of the center peak in question

            // Assert
            Assert.IsFalse(isNotFlat, "Segment normally considered flat does not meet criteria with a very low amplitude noise threshold.");
        }

        [TestMethod]
        public void TrimPeaks_ExtractsCorrectSubsetOfPeaks()
        {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 20.0),
                new ValuePeak(3, 3.0, 100.0, 30.0),
                new ValuePeak(4, 4.0, 100.0, 40.0),
                new ValuePeak(5, 5.0, 100.0, 50.0)
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            int left = 1; // Second element
            int right = 3; // Inclusive fourth element

            // Act
            var trimmedPeaks = eic.TrimPeaks(left, right);

            // Assert
            Assert.AreEqual(3, trimmedPeaks.Length, "Trimmed peaks array should contain exactly three peaks.");
            Assert.AreEqual(peaks[1].Intensity, trimmedPeaks[0].Intensity, "First peak in trimmed array should match the second peak in the original array.");
            Assert.AreEqual(peaks[2].Intensity, trimmedPeaks[1].Intensity, "Second peak in trimmed array should match the third peak in the original array.");
            Assert.AreEqual(peaks[3].Intensity, trimmedPeaks[2].Intensity, "Third peak in trimmed array should match the fourth peak in the original array.");
        }

        [TestMethod]
        public void TrimPeaks_WithSinglePeak_ReturnsSingleElementArray()
        {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 20.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            int left = 1; // Second element
            int right = 1; // Inclusive second element

            // Act
            var trimmedPeaks = eic.TrimPeaks(left, right);

            // Assert
            Assert.AreEqual(1, trimmedPeaks.Length, "Trimmed peaks array should contain exactly one peak.");
            Assert.AreEqual(peaks[1].Intensity, trimmedPeaks[0].Intensity, "Trimmed array should contain the second peak from the original array.");
        }

        [TestMethod]
        public void TrimPeaks_OutsideBounds_ThrowsException()
        {
            // This test assumes that the method should check bounds and throw an exception if the indices are out of bounds.
            // If the method does not perform bounds checking and this behavior is acceptable, this test should be omitted or adjusted accordingly.

            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 20.0)
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            int left = -1; // Invalid index
            int right = 3; // Beyond array bounds

            // Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => eic.TrimPeaks(left, right), "Trimming with indices outside of bounds should throw ArgumentOutOfRangeException.");
        }

        [TestMethod]
        public void GetIntensityMedian_WithOddNumberOfPeaks_ReturnsMiddleValue()
        {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 20.0),
                new ValuePeak(3, 3.0, 100.0, 30.0), // Median value
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            double medianIntensity = eic.GetIntensityMedian();

            // Assert
            Assert.AreEqual(20.0, medianIntensity);
        }

        [TestMethod]
        public void GetIntensityMedian_WithSinglePeak_ReturnsPeakIntensity()
        {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0), // Only peak
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            double medianIntensity = eic.GetIntensityMedian();

            // Assert
            Assert.AreEqual(10.0, medianIntensity);
        }

        [TestMethod]
        public void GetIntensityMedian_WithNoPeaks_ReturnsZero()
        {
            // Arrange
            var peaks = Array.Empty<ValuePeak>();
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            double medianIntensity = eic.GetIntensityMedian();

            // Assert
            Assert.AreEqual(0.0, medianIntensity, "Median intensity of an empty peak set should be zero.");
        }

        [TestMethod]
        public void GetMaximumIntensity_WithMultiplePeaks_ReturnsHighestIntensity()
        {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 50.0), // Highest intensity
                new ValuePeak(3, 3.0, 100.0, 30.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            double maxIntensity = eic.GetMaximumIntensity();

            // Assert
            Assert.AreEqual(50.0, maxIntensity);
        }

        [TestMethod]
        public void GetMaximumIntensity_WithSinglePeak_ReturnsThatIntensity()
        {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0), // Only peak
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            double maxIntensity = eic.GetMaximumIntensity();

            // Assert
            Assert.AreEqual(20.0, maxIntensity);
        }

        [TestMethod]
        public void GetMaximumIntensity_WithNoPeaks_ReturnsZero()
        {
            // Arrange
            var peaks = Array.Empty<ValuePeak>();
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            double maxIntensity = eic.GetMaximumIntensity();

            // Assert
            Assert.AreEqual(0.0, maxIntensity, "Maximum intensity of an empty peak set should be zero.");
        }

        [TestMethod]
        public void GetMinimumIntensity_WithMultiplePeaks_ReturnsLowestIntensity()
        {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0),
                new ValuePeak(2, 2.0, 100.0, 5.0),  // Lowest intensity
                new ValuePeak(3, 3.0, 100.0, 30.0),
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            double minIntensity = eic.GetMinimumIntensity();

            // Assert
            Assert.AreEqual(5.0, minIntensity);
        }

        [TestMethod]
        public void GetMinimumIntensity_WithSinglePeak_ReturnsThatIntensity()
        {
            // Arrange
            var peaks = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 25.0), // Only peak
            };
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            double minIntensity = eic.GetMinimumIntensity();

            // Assert
            Assert.AreEqual(25.0, minIntensity);
        }

        [TestMethod]
        public void GetMinimumIntensity_WithNoPeaks_ReturnsZero()
        {
            // Arrange
            var peaks = Array.Empty<ValuePeak>();
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            double minIntensity = eic.GetMinimumIntensity();

            // Assert
            Assert.AreEqual(0.0, minIntensity, "Minimum intensity of an empty peak set should be zero.");
        }

        [TestMethod]
        public void GetMinimumNoiseLevel_AccurateEstimation_ReturnsEstimatedNoise() {
            // Arrange
            var peaks = Enumerable.Range(1, 100).Select(i =>
                new ValuePeak(i, i, 100.0, 10.0 + (i % 10))).ToArray(); // Simulated data with a patterned variation
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            var parameter = new NoiseEstimateParameter
            {
                NoiseEstimateBin = 10, // Grouping size for noise estimation
                MinimumNoiseWindowSize = 5, // Minimum window size to perform estimation
                MinimumNoiseLevel = 0.5, // Default minimum noise level
            };

            // Act
            double noiseLevel = eic.GetMinimumNoiseLevel(parameter);

            // Assert
            Assert.IsTrue(noiseLevel > parameter.MinimumNoiseLevel, "Estimated noise should be greater than the minimum specified noise level.");
        }

        [TestMethod]
        public void GetMinimumNoiseLevel_NotEnoughData_DefaultsToMinNoiseLevel() {
            // Arrange
            var peaks = Enumerable.Range(1, 10).Select(i =>
                new ValuePeak(i, i, 100.0, 10.0 + (i % 2))).ToArray(); // Limited data points
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            var parameter = new NoiseEstimateParameter
            {
                NoiseEstimateBin = 10, // Large bin size considering the data
                MinimumNoiseWindowSize = 10, // Unattainable window size given the data
                MinimumNoiseLevel = 1.0, // Specified minimum noise level
            };

            // Act
            double noiseLevel = eic.GetMinimumNoiseLevel(parameter);

            // Assert
            Assert.AreEqual(parameter.MinimumNoiseLevel, noiseLevel, "With insufficient data, should default to the specified minimum noise level.");
        }

        [TestMethod]
        public void Difference_CorrectlyCalculatesIntensityDifferences_ReturnsNewChromatogram()
        {
            // Arrange
            var peaks1 = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0),
                new ValuePeak(2, 2.0, 100.0, 30.0),
            };
            var peaks2 = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
                new ValuePeak(2, 2.0, 100.0, 20.0),
            };
            var eic1 = new ExtractedIonChromatogram(peaks1, ChromXType.RT, ChromXUnit.Min, 100.0);
            var eic2 = new ExtractedIonChromatogram(peaks2, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            var diffChromatogram = eic1.Difference(eic2);

            // Assert
            Assert.IsNotNull(diffChromatogram, "Difference chromatogram should not be null.");
            Assert.AreEqual(10.0, diffChromatogram.AsPeakArray()[0].Intensity, "First peak intensity difference should be 10.");
            Assert.AreEqual(10.0, diffChromatogram.AsPeakArray()[1].Intensity, "Second peak intensity difference should be 10.");
        }

        [TestMethod]
        public void Difference_WithNegativeIntensityDifferences_FloorsAtZero()
        {
            // Arrange
            var peaks1 = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 10.0),
            };
            var peaks2 = new[]
            {
                new ValuePeak(1, 1.0, 100.0, 20.0),
            };
            var eic1 = new ExtractedIonChromatogram(peaks1, ChromXType.RT, ChromXUnit.Min, 100.0);
            var eic2 = new ExtractedIonChromatogram(peaks2, ChromXType.RT, ChromXUnit.Min, 100.0);

            // Act
            var diffChromatogram = eic1.Difference(eic2);

            // Assert
            Assert.IsNotNull(diffChromatogram, "Difference chromatogram should not be null.");
            Assert.AreEqual(0.0, diffChromatogram.AsPeakArray()[0].Intensity, "Intensity differences resulting in negative values should be floored at zero.");
        }

        [TestMethod]
        public void ChromatogramSmoothing_ApplySimpleMovingAverage_ReducesNoise()
        {
            // Arrange
            var peaks = Enumerable.Range(1, 100).Select(i =>
                new ValuePeak(i, i, 100.0, 10.0 + (i % 10))).ToArray();
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            var smoothingMethod = SmoothingMethod.SimpleMovingAverage;
            var level = 5; // Level of smoothing for the test

            // Act
            var smoothedEic = eic.ChromatogramSmoothing(smoothingMethod, level);
            var originalMedian = eic.GetIntensityMedian();
            var smoothedMedian = smoothedEic.GetIntensityMedian();

            // Assert
            Assert.IsNotNull(smoothedEic);
            Assert.IsTrue(smoothedMedian < originalMedian, "Smoothing should generally reduce the median intensity due to noise reduction.");
        }

        [TestMethod]
        public void GetPeakDetectionResultFromRange_WithinValidRange_DetectsPeaksAccurately()
        {
            // Arrange
            var peaks = Enumerable.Range(5, 20).Select(i =>
                new ValuePeak(i, i, 100.0, 10.0 + (i % 10))).ToArray();
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            int startID = 10; // Start index of the range
            int endID = 20; // End index of the range

            // Act
            var detectionResult = eic.GetPeakDetectionResultFromRange(startID, endID);

            // Assert
            Assert.IsNotNull(detectionResult);
            Assert.AreEqual(10 - 5, detectionResult.ScanNumAtLeftPeakEdge);
            Assert.AreEqual(20 - 5, detectionResult.ScanNumAtRightPeakEdge);
        }

        [TestMethod]
        public void GetPeakDetectionResultFromRange_OutsideValidRange_ReturnsEmptyResult()
        {
            // Arrange
            var peaks = Enumerable.Range(1, 20).Select(i =>
                new ValuePeak(i, i, 100.0, 10.0 + (i % 10))).ToArray();
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            int startID = -5; // Invalid start index
            int endID = peaks.Length + 5; // Invalid end index

            // Act
            var detectionResult = eic.GetPeakDetectionResultFromRange(startID, endID);

            // Assert
            Assert.IsNotNull(detectionResult);
            Assert.AreEqual(0, detectionResult.ScanNumAtLeftPeakEdge);
            Assert.AreEqual(19, detectionResult.ScanNumAtRightPeakEdge);
        }

        [TestMethod]
        public void GetPeakDetectionResultFromRange_NoPeaksInRange_ReturnsEmptyResult()
        {
            // Arrange
            var peaks = Enumerable.Range(1, 20).Select(i =>
                new ValuePeak(i, i, 100.0, 10.0 + (i % 10))).ToArray();
            var eic = new ExtractedIonChromatogram(peaks, ChromXType.RT, ChromXUnit.Min, 100.0);
            int startID = 50; // Assuming this range has no peaks
            int endID = 60;

            // Act
            var detectionResult = eic.GetPeakDetectionResultFromRange(startID, endID);

            // Assert
            Assert.IsNull(detectionResult);
        }
    }
}