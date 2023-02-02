using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialImmsCore.Algorithm.Alignment.Tests
{
    [TestClass()]
    public class ImmsPeakJoinerTests
    {
        [TestMethod()]
        public void AlignPeaksToMasterTest() {
            var spots = new List<AlignmentSpotProperty>
            {
                BuildSpotProperty(100.000d, 10.00),
                BuildSpotProperty(100.015d, 10.02),
                BuildSpotProperty(100.030d, 10.04),
                BuildSpotProperty(100.045d, 10.06),
            };

            var masters = new List<IMSScanProperty>
            {
                BuildPeakFeature(0, 100.000d, 10.00),
                BuildPeakFeature(1, 100.015d, 10.02),
                BuildPeakFeature(2, 100.030d, 10.04),
                BuildPeakFeature(3, 100.045d, 10.06),
            };

            var targets = new List<IMSScanProperty>
            {
                BuildPeakFeature(1, 100.011d, 10.02),
                BuildPeakFeature(2, 100.019d, 10.03),
            };

            var joiner = new ImmsPeakJoiner(0.015, 0.5, 0.02, 0.05);
            joiner.AlignPeaksToMaster(spots, masters, targets, 0);

            Assert.AreEqual(-1, spots[0].AlignedPeakProperties[0].PeakID);
            Assert.AreEqual(1, spots[1].AlignedPeakProperties[0].PeakID);
            Assert.AreEqual(2, spots[2].AlignedPeakProperties[0].PeakID);
            Assert.AreEqual(-1, spots[3].AlignedPeakProperties[0].PeakID);
        }

        AlignmentSpotProperty BuildSpotProperty(double mass, double drift) {
            return new AlignmentSpotProperty
            {
                MassCenter = mass,
                TimesCenter = new ChromXs(drift, ChromXType.Drift, ChromXUnit.Msec),
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    new AlignmentChromPeakFeature { PeakID = -1, },
                },
            };
        }

        ChromatogramPeakFeature BuildPeakFeature(int id, double mass, double drift) {
            var peak = new ChromatogramPeakFeature
            {
                PeakID = id,
                PrecursorMz = mass,
                ChromXs = new ChromXs(drift, ChromXType.Drift, ChromXUnit.Msec),
            };
            peak.MatchResults.AddMspResult(100, MsScanMatchResultContainer.UnknownResult);
            return peak;
        }
    }
}