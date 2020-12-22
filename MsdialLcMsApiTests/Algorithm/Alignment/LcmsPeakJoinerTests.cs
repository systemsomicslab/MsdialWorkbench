using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using System;
using System.Collections.Generic;
using System.Text;
using CompMs.MsdialCore.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using System.Diagnostics;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment.Tests
{
    [TestClass()]
    public class LcmsPeakJoinerTests
    {
        [TestMethod()]
        public void MergeChromatogramPeaksTest() {
            var rtTol = 1d;
            var mzTol = 0.01;

            var master = new List<IMSScanProperty>
            {
                new ChromatogramPeakFeature { ChromXs = new ChromXs(1d), Mass = 100d },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(3d), Mass = 100d },
            };
            var sub = new List<IMSScanProperty>
            {
                new ChromatogramPeakFeature { ChromXs = new ChromXs(1d), Mass = 100d }, // merge if completely same
                new ChromatogramPeakFeature { ChromXs = new ChromXs(1d+rtTol), Mass = 100d + mzTol*0.999 }, // merge if difference less than or equal to tolerance
                new ChromatogramPeakFeature { ChromXs = new ChromXs(1d), Mass = 100d + mzTol + 0.001 }, // don't merge if difference larger than tolerance
                new ChromatogramPeakFeature { ChromXs = new ChromXs(2d), Mass = 100d },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(0d), Mass = 90d }, // smallest rt and m/z is ok
                new ChromatogramPeakFeature { ChromXs = new ChromXs(4d), Mass = 110d }, // largest rt and m/z is ok
            };

            var joiner = new LcmsPeakJoiner(rtTol, mzTol);
            var result = joiner.MergeChromatogramPeaks(master, sub);
            var expected = new List<IMSScanProperty>
            {
                master[0], master[1],
                sub[2], sub[4], sub[5],
            };

            Debug.WriteLine("Expected:");
            expected.ForEach(feature => Debug.WriteLine($"\tChrom: {feature.ChromXs.Value}, Mass: {feature.PrecursorMz}"));
            Debug.WriteLine("Result:");
            result.ForEach(feature => Debug.WriteLine($"\tChrom: {feature.ChromXs.Value}, Mass: {feature.PrecursorMz}"));
            Assert.AreEqual(5, result.Count);
            CollectionAssert.AreEquivalent(expected, result);
        }

        [TestMethod()]
        public void AlignPeaksToMasterTest() {
            var rtTol = 1d;
            var mzTol = 0.01;
            var joiner = new LcmsPeakJoiner(rtTol, mzTol);

            var master = new List<IMSScanProperty>
            {
                new ChromatogramPeakFeature { ChromXs = new ChromXs(0d), Mass = 100d },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(2d), Mass = 200d },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(4d), Mass = 300d },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(6d), Mass = 400d },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(8d), Mass = 500d },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(10d), Mass = 600d },
            };
            var sub = new List<IMSScanProperty>
            {
                new ChromatogramPeakFeature { ChromXs = new ChromXs(0d), Mass = 100d },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(2d + rtTol * 0.9), Mass = 200d },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(4d), Mass = 300d + mzTol * 0.9 },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(6d + rtTol * 0.9), Mass = 400d + mzTol * 0.9 },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(8d), Mass = 500d + mzTol * 0.9 },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(8d), Mass = 500d + mzTol * 0.5 },
                new ChromatogramPeakFeature { ChromXs = new ChromXs(10d), Mass = 601d },
            };
            var spots = new List<AlignmentSpotProperty>
            {
                new AlignmentSpotProperty { AlignedPeakProperties = new List<AlignmentChromPeakFeature> { new AlignmentChromPeakFeature() } },
                new AlignmentSpotProperty { AlignedPeakProperties = new List<AlignmentChromPeakFeature> { new AlignmentChromPeakFeature() } },
                new AlignmentSpotProperty { AlignedPeakProperties = new List<AlignmentChromPeakFeature> { new AlignmentChromPeakFeature() } },
                new AlignmentSpotProperty { AlignedPeakProperties = new List<AlignmentChromPeakFeature> { new AlignmentChromPeakFeature() } },
                new AlignmentSpotProperty { AlignedPeakProperties = new List<AlignmentChromPeakFeature> { new AlignmentChromPeakFeature() } },
                new AlignmentSpotProperty { AlignedPeakProperties = new List<AlignmentChromPeakFeature> { new AlignmentChromPeakFeature() } },
            };
            var masses = new List<double>
            {
                sub[0].PrecursorMz,
                sub[1].PrecursorMz,
                sub[2].PrecursorMz,
                sub[3].PrecursorMz,
                sub[5].PrecursorMz,
                0d,
            };

            joiner.AlignPeaksToMaster(spots, master, sub, 0);

            Debug.WriteLine("Expected:");
            masses.ForEach(mass => Debug.WriteLine($"\tMass: {mass}"));
            Debug.WriteLine("Result:");
            spots.ForEach(spot => Debug.WriteLine($"\tMass: {spot.AlignedPeakProperties[0].Mass}"));
            CollectionAssert.AreEqual(masses, spots.Select(spot => spot.AlignedPeakProperties[0].Mass).ToArray());
        }
    }
}