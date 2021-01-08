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
using CompMs.MsdialCore.Algorithm;
using CompMs.Common.DataObj;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment.Tests
{
    [TestClass()]
    public class LcmsPeakJoinerTests
    {
        [TestMethod]
        public void JoinTest() {
            var rtTol = 1d;
            var mzTol = 0.01;

            var data = new List<List<IMSScanProperty>>
            {
                new List<IMSScanProperty>
                {
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(0d), Mass = 100d },
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(2d + rtTol * 0.9), Mass = 200d },
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(4d), Mass = 300d + mzTol * 0.9 },
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(6d + rtTol * 0.9), Mass = 400d + mzTol * 0.9 },
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(8d), Mass = 500d + mzTol * 0.9 },
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(8d), Mass = 500d + mzTol * 0.5 },
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(10d), Mass = 601d },
                },
                new List<IMSScanProperty>
                {
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(0d), Mass = 100d },
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(2d), Mass = 200d },
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(4d), Mass = 300d },
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(6d), Mass = 400d },
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(8d), Mass = 500d },
                    new ChromatogramPeakFeature { ChromXs = new ChromXs(10d), Mass = 600d },
                },
            };
            var files = new List<AnalysisFileBean>
            {
                new AnalysisFileBean { AnalysisFileId = 0 },
                new AnalysisFileBean { AnalysisFileId = 1 },
            };
            var accessor = new MockAccessor(data);

            var joiner = new LcmsPeakJoiner(rtTol, mzTol);
            var actual = joiner.Join(files, 1, accessor);

            Debug.WriteLine("data [0]:");
            data[0].ForEach(d => Debug.WriteLine($"\tMass: {d.PrecursorMz}\tRT: {d.ChromXs.RT.Value}"));
            Debug.WriteLine("Result [0]:");
            actual.ForEach(spot => Debug.WriteLine($"\tMass: {spot.AlignedPeakProperties[0]?.Mass}\tRT: {spot.AlignedPeakProperties[0]?.ChromXsTop?.RT.Value}"));
            Debug.WriteLine("data [1]:");
            data[1].ForEach(d => Debug.WriteLine($"\tMass: {d.PrecursorMz}\tRT: {d.ChromXs.RT.Value}"));
            Debug.WriteLine("Result [1]:");
            actual.ForEach(spot => Debug.WriteLine($"\tMass: {spot.AlignedPeakProperties[1]?.Mass}\tRT: {spot.AlignedPeakProperties[1]?.ChromXsTop?.RT.Value}"));

            Assert.AreEqual(7, actual.Count);
            Assert.AreEqual(data[0][0].PrecursorMz, actual[0].AlignedPeakProperties[0].Mass);
            Assert.AreEqual(data[0][1].PrecursorMz, actual[1].AlignedPeakProperties[0].Mass);
            Assert.AreEqual(data[0][2].PrecursorMz, actual[2].AlignedPeakProperties[0].Mass);
            Assert.AreEqual(data[0][3].PrecursorMz, actual[3].AlignedPeakProperties[0].Mass);
            Assert.AreEqual(data[0][5].PrecursorMz, actual[4].AlignedPeakProperties[0].Mass);
            Assert.AreEqual(data[0][6].PrecursorMz, actual[6].AlignedPeakProperties[0].Mass);
            Assert.AreEqual(data[0][0].ChromXs.RT.Value, actual[0].AlignedPeakProperties[0].ChromXsTop.RT.Value);
            Assert.AreEqual(data[0][1].ChromXs.RT.Value, actual[1].AlignedPeakProperties[0].ChromXsTop.RT.Value);
            Assert.AreEqual(data[0][2].ChromXs.RT.Value, actual[2].AlignedPeakProperties[0].ChromXsTop.RT.Value);
            Assert.AreEqual(data[0][3].ChromXs.RT.Value, actual[3].AlignedPeakProperties[0].ChromXsTop.RT.Value);
            Assert.AreEqual(data[0][5].ChromXs.RT.Value, actual[4].AlignedPeakProperties[0].ChromXsTop.RT.Value);
            Assert.AreEqual(data[0][6].ChromXs.RT.Value, actual[6].AlignedPeakProperties[0].ChromXsTop.RT.Value);

            Assert.AreEqual(data[1][0].PrecursorMz, actual[0].AlignedPeakProperties[1].Mass);
            Assert.AreEqual(data[1][1].PrecursorMz, actual[1].AlignedPeakProperties[1].Mass);
            Assert.AreEqual(data[1][2].PrecursorMz, actual[2].AlignedPeakProperties[1].Mass);
            Assert.AreEqual(data[1][3].PrecursorMz, actual[3].AlignedPeakProperties[1].Mass);
            Assert.AreEqual(data[1][4].PrecursorMz, actual[4].AlignedPeakProperties[1].Mass);
            Assert.AreEqual(data[1][5].PrecursorMz, actual[5].AlignedPeakProperties[1].Mass);
            Assert.AreEqual(data[1][0].ChromXs.RT.Value, actual[0].AlignedPeakProperties[1].ChromXsTop.RT.Value);
            Assert.AreEqual(data[1][1].ChromXs.RT.Value, actual[1].AlignedPeakProperties[1].ChromXsTop.RT.Value);
            Assert.AreEqual(data[1][2].ChromXs.RT.Value, actual[2].AlignedPeakProperties[1].ChromXsTop.RT.Value);
            Assert.AreEqual(data[1][3].ChromXs.RT.Value, actual[3].AlignedPeakProperties[1].ChromXsTop.RT.Value);
            Assert.AreEqual(data[1][4].ChromXs.RT.Value, actual[4].AlignedPeakProperties[1].ChromXsTop.RT.Value);
            Assert.AreEqual(data[1][5].ChromXs.RT.Value, actual[5].AlignedPeakProperties[1].ChromXsTop.RT.Value);
        }
    }

    class MockAccessor : DataAccessor
    {
        private List<List<IMSScanProperty>> scans;

        public MockAccessor(List<List<IMSScanProperty>> scans) {
            this.scans = scans;
        }

        public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, List<RawSpectrum> spectrum) {
            throw new NotImplementedException();
        }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            if (analysisFile.AnalysisFileId < scans.Count) {
                return scans[analysisFile.AnalysisFileId];
            }
            return new List<IMSScanProperty>();
        }
    }
}