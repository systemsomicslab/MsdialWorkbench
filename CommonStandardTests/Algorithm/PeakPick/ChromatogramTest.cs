using CompMs.Common.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Algorithm.PeakPick.Tests
{
    [TestClass()]
    public class ChromatogramTest
    {
        [TestMethod()]
        public void CreateDataPointTest() {
            var chromatogram = BuildSample();

            var actual = chromatogram.CreateDataPoint(2);
            Assert.AreEqual(2, actual.Id);
            Assert.AreEqual(3d, actual.ChromValue);
            Assert.AreEqual(10100d, actual.Intensity);

            actual = chromatogram.CreateDataPoint(0);
            Assert.AreEqual(0, actual.Id);
            Assert.AreEqual(1d, actual.ChromValue);
            Assert.AreEqual(1100d, actual.Intensity);

            actual = chromatogram.CreateDataPoint(4);
            Assert.AreEqual(4, actual.Id);
            Assert.AreEqual(5d, actual.ChromValue);
            Assert.AreEqual(100d, actual.Intensity);
        }

        [TestMethod()]
        public void IsPeaktopTest() {
            var chromatogram = BuildPeaktopSample();

            Assert.IsFalse(chromatogram.IsPeaktop(1, 2));
            Assert.IsFalse(chromatogram.IsPeaktop(3, 2));
            Assert.IsFalse(chromatogram.IsPeaktop(5, 2));
            Assert.IsTrue(chromatogram.IsPeaktop(6, 2));
            Assert.IsTrue(chromatogram.IsPeaktop(10, 2));
            Assert.IsTrue(chromatogram.IsPeaktop(11, 2));
            Assert.IsFalse(chromatogram.IsPeaktop(15, 2));

            Assert.IsTrue(chromatogram.IsPeaktop(1, 1));
            Assert.IsFalse(chromatogram.IsPeaktop(3, 1));
            Assert.IsFalse(chromatogram.IsPeaktop(5, 1));
            Assert.IsTrue(chromatogram.IsPeaktop(6, 1));
            Assert.IsTrue(chromatogram.IsPeaktop(10, 1));
            Assert.IsTrue(chromatogram.IsPeaktop(11, 1));
            Assert.IsFalse(chromatogram.IsPeaktop(15, 1));
        }

        [TestMethod()]
        public void IsPeakTopTest() {
            var chromatogram = BuildPeaktopSample();

            Assert.IsFalse(chromatogram.IsIntensityPeaktop(1));
            Assert.IsFalse(chromatogram.IsIntensityPeaktop(3));
            Assert.IsFalse(chromatogram.IsIntensityPeaktop(5));
            Assert.IsTrue(chromatogram.IsIntensityPeaktop(6));
            Assert.IsTrue(chromatogram.IsIntensityPeaktop(10));
            Assert.IsTrue(chromatogram.IsIntensityPeaktop(11));
            Assert.IsFalse(chromatogram.IsIntensityPeaktop(15));
        }

        [TestMethod()]
        public void IsOneSideMissingPeakTopTest() {
            var chromatogram = BuildPeaktopSample();

            Assert.IsTrue(chromatogram.IsIntensityOneSideMissingPeaktop(1));
            Assert.IsFalse(chromatogram.IsIntensityOneSideMissingPeaktop(3));
            Assert.IsFalse(chromatogram.IsIntensityOneSideMissingPeaktop(5));
            Assert.IsTrue(chromatogram.IsIntensityOneSideMissingPeaktop(6));
            Assert.IsTrue(chromatogram.IsIntensityOneSideMissingPeaktop(10));
            Assert.IsTrue(chromatogram.IsIntensityOneSideMissingPeaktop(11));
            Assert.IsFalse(chromatogram.IsIntensityOneSideMissingPeaktop(15));
        }

        [TestMethod()]
        public void FindPeakTest() {
            var chromatogram = BuildPeaktopSample();
            var stub = new InfiniteLoopDetector();
            var (_, actual) = chromatogram.FindPeak(start: 8, slopeNoiseFoldCriteria: 1, minimumDatapointCriteria: 0, stub);
            Assert.AreEqual(8, actual.LeftIndex);
            Assert.AreEqual(14, actual.RightIndex);
        }

        internal static Chromatogram BuildSample() {
            return new Chromatogram(new[]
            {
                new ChromatogramPeak(0, 100d,  1100d, new RetentionTime(1d)),
                new ChromatogramPeak(1, 100d,   100d, new RetentionTime(2d)),
                new ChromatogramPeak(2, 100d, 10100d, new RetentionTime(3d)),
                new ChromatogramPeak(3, 100d,  1000d, new RetentionTime(4d)),
                new ChromatogramPeak(4, 100d,   100d, new RetentionTime(5d)),
            });
        }
        private static Chromatogram BuildPeaktopSample() {
            return new Chromatogram(new ChromatogramPeak[]
            {
                new ChromatogramPeak( 0, 100d,   100d, new RetentionTime( 0d)),
                new ChromatogramPeak( 1, 100d, 10000d, new RetentionTime( 1d)),
                new ChromatogramPeak( 2, 100d,  1000d, new RetentionTime( 2d)),
                new ChromatogramPeak( 3, 100d,  1000d, new RetentionTime( 3d)),
                new ChromatogramPeak( 4, 100d,   100d, new RetentionTime( 4d)),
                new ChromatogramPeak( 5, 100d,  1000d, new RetentionTime( 5d)),
                new ChromatogramPeak( 6, 100d, 10000d, new RetentionTime( 6d)),
                new ChromatogramPeak( 7, 100d,  1000d, new RetentionTime( 7d)),
                new ChromatogramPeak( 8, 100d,   100d, new RetentionTime( 8d)),
                new ChromatogramPeak( 9, 100d,  1000d, new RetentionTime( 9d)),
                new ChromatogramPeak(10, 100d, 10000d, new RetentionTime(10d)),
                new ChromatogramPeak(11, 100d, 10000d, new RetentionTime(11d)),
                new ChromatogramPeak(12, 100d,  1000d, new RetentionTime(12d)),
                new ChromatogramPeak(13, 100d,   100d, new RetentionTime(13d)),
                new ChromatogramPeak(14, 100d,   100d, new RetentionTime(14d)),
                new ChromatogramPeak(15, 100d,  1000d, new RetentionTime(15d)),
            });
        }
    }
}
