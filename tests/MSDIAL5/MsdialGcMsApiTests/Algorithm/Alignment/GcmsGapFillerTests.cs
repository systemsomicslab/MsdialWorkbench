using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment.Tests {
    [TestClass()]
    public class GcmsGapFillerTests {
        [TestMethod()]
        public void UpdateQuantMassSetsTargetMassToSpotQuantMass() {
            var filler = new StubGcmsGapFiller(new MsdialGcmsParameter());

            var target = new AlignmentChromPeakFeature {
                FileID = 0,
                MasterPeakID = -1,
                PeakID = -1,
                Mass = 999d,
            };
            var detected = new AlignmentChromPeakFeature {
                FileID = 1,
                MasterPeakID = 1,
                PeakID = 1,
                ChromXsLeft = new ChromXs(0.9d, ChromXType.RT, ChromXUnit.Min),
                ChromXsTop = new ChromXs(1.0d, ChromXType.RT, ChromXUnit.Min),
                ChromXsRight = new ChromXs(1.1d, ChromXType.RT, ChromXUnit.Min),
            };
            var spot = new AlignmentSpotProperty {
                QuantMass = 123.456d,
                AlignedPeakProperties = new List<AlignmentChromPeakFeature> { target, detected },
            };

            filler.UpdateQuantMass(rawSpectra: null, spot, fileID: target.FileID);

            Assert.AreEqual(spot.QuantMass, target.Mass);
        }

        private sealed class StubGcmsGapFiller : GcmsGapFiller {
            public StubGcmsGapFiller(MsdialGcmsParameter parameter) : base(parameter) {
            }

            protected override double AxTol => 0.05d;

            protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
                return new ChromXs(1.0d, ChromXType.RT, ChromXUnit.Min) {
                    Mz = new MzValue(spot.QuantMass),
                    RI = new RetentionIndex(1000d),
                };
            }

            protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
                return 0.1d;
            }

            protected override List<ChromatogramPeak> GetPeaks(RawSpectra rawSpectra, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
                return new List<ChromatogramPeak>();
            }
        }
    }
}
