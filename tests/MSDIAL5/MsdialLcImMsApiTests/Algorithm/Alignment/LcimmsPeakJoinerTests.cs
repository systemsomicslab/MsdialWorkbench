using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialLcImMsApi.Algorithm.Alignment.Tests
{
    [TestClass()]
    public class LcimmsPeakJoinerTests
    {
        [TestMethod()]
        public void AlignPeaksToMasterTest() {
            var spots = new List<AlignmentSpotProperty>
            {
                BuildSpotProperty(100.000d, 1d, 10.00),
                BuildSpotProperty(100.020d, 1d, 10.10),
            };

            var id = 0;
            var masters = new List<IMSScanProperty>
            {
                BuildPeakFeature(ref id, 0, 100.000d, 1d, 10.00),
                BuildPeakFeature(ref id, 1, 100.020d, 1d, 10.10),
            };

            id = 0;
            var target = BuildPeakFeature(ref id, 0, 100.015d, 1d, 10.01);
            var targets = new List<IMSScanProperty>
            {
                target,
            };

            var joiner = new LcimmsPeakJoiner(.2, .01, .1);
            joiner.AlignPeaksToMaster(spots, masters, targets, 0);

            Assert.AreEqual(-1, spots[0].AlignmentDriftSpotFeatures[0].AlignedPeakProperties[0].PeakID);
            Assert.AreEqual(target.DriftChromFeatures[0].PeakID, spots[1].AlignmentDriftSpotFeatures[0].AlignedPeakProperties[0].PeakID);
        }

        AlignmentSpotProperty BuildSpotProperty(double mass, double rt, double drift) {
            return new AlignmentSpotProperty
            {
                MassCenter = mass,
                TimesCenter = new ChromXs(new RetentionTime(rt, ChromXUnit.Min)),
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    new AlignmentChromPeakFeature { MasterPeakID = -1, PeakID = -1, },
                },

                AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>
                {
                    new AlignmentSpotProperty
                    {
                        MassCenter = mass,
                        TimesCenter = new ChromXs(new DriftTime(drift, ChromXUnit.Msec))
                        {
                            RT = new RetentionTime(rt, ChromXUnit.Min),
                        },
                        AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                        {
                            new AlignmentChromPeakFeature { MasterPeakID = -1, PeakID = -1, },
                        },
                    }
                },
            };
        }

        ChromatogramPeakFeature BuildPeakFeature(ref int masterId, int id, double mass, double rt, double drift) {
            var peak = new ChromatogramPeakFeature
            {
                MasterPeakID = masterId++,
                PeakID = id,
                PrecursorMz = mass,
                ChromXs = new ChromXs(new RetentionTime(rt, ChromXUnit.Min)),

                DriftChromFeatures = new List<ChromatogramPeakFeature> {
                    new ChromatogramPeakFeature
                    {
                        MasterPeakID = masterId++,
                        PeakID = 0,
                        PrecursorMz = mass,
                        ChromXs = new ChromXs(new DriftTime(drift, ChromXUnit.Msec))
                        {
                            RT = new RetentionTime(rt, ChromXUnit.Min),
                        },
                    }
                }
            };
            peak.MatchResults.AddMspResult(100, MsScanMatchResultContainer.UnknownResult);
            return peak;
        }
    }
}