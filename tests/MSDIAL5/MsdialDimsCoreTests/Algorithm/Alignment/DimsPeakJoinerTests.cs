using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm.Alignment.Tests
{
    [TestClass()]
    public class DimsPeakJoinerTests
    {
        [TestMethod()]
        public void JoinGivesAMasterTheFilesClosestPeak() {
            // Both peaks of file 1 lie within the m/z tolerance of the one master. The closer one must
            // join it, not the farther one.
            var data = new List<List<IMSScanProperty>>
            {
                new List<IMSScanProperty> { BuildPeak(0, 100.000d), },
                new List<IMSScanProperty> { BuildPeak(0, 100.0005d), BuildPeak(1, 100.009d), },
            };
            var files = data.Select((_, i) => new AnalysisFileBean { AnalysisFileId = i, AnalysisFileName = $"file{i}", }).ToList();

            var joiner = new DimsPeakJoiner(mztol: 0.01d);
            var spots = joiner.Join(files, referenceId: 0, new StubAccessor(data));

            Assert.AreEqual(1, spots.Count);
            Assert.AreEqual(0, spots[0].AlignedPeakProperties[1].PeakID);
            Assert.AreEqual(100.0005d, spots[0].AlignedPeakProperties[1].Mass, 1e-9);
        }

        private static ChromatogramPeakFeature BuildPeak(int id, double mz) {
            var peak = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = mz, ChromXsTop = new ChromXs(mz, ChromXType.Mz, ChromXUnit.Mz), })
            {
                MasterPeakID = id,
                PeakID = id,
                PrecursorMz = mz,
            };
            return peak;
        }

        private sealed class StubAccessor : DataAccessor
        {
            private readonly List<List<IMSScanProperty>> _data;

            public StubAccessor(List<List<IMSScanProperty>> data) {
                _data = data;
            }

            public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
                return _data[analysisFile.AnalysisFileId].ToList();
            }

            public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, float ms1MassTolerance) {
                throw new NotSupportedException();
            }
        }
    }
}
