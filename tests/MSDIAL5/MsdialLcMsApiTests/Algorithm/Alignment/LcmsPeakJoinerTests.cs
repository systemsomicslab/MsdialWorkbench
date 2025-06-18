using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment.Tests
{
    [TestClass()]
    public class LcmsPeakJoinerTests
    {
        [TestMethod]
        public void JoinTest() {
            var rtTol = 1d;
            var mzTol = 0.01;

            var data = new List<List<ChromatogramPeakFeature>>
            {
                new List<ChromatogramPeakFeature>
                {
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(0d), Mass = 100d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(2d + rtTol * 0.9), Mass = 200d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(4d), Mass = 300d + mzTol * 0.9 }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(6d + rtTol * 0.9), Mass = 400d + mzTol * 0.9 }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(8d), Mass = 500d + mzTol * 0.9 }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(8d), Mass = 500d + mzTol * 0.5 }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(10d), Mass = 601d }),
                },
                new List<ChromatogramPeakFeature>
                {
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(0d), Mass = 100d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(2d), Mass = 200d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(4d), Mass = 300d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(6d), Mass = 400d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(8d), Mass = 500d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(10d), Mass = 600d }),
                },
            };
            var files = new List<AnalysisFileBean>
            {
                new AnalysisFileBean { AnalysisFileId = 0 },
                new AnalysisFileBean { AnalysisFileId = 1 },
            };
            var accessor = new StubAccessor(data);
            var evaluator = new FakeEvaluator();

            var joiner = new LcmsPeakJoiner(new() { RetentionTimeAlignmentTolerance = (float)rtTol, Ms1AlignmentTolerance = (float)mzTol }, accessor, evaluator);
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

        [TestMethod]
        public void AlignPeaksToMasterTest() {
            var rtTol = 1d;
            var mzTol = 0.01;

            var spots = new List<AlignmentSpotProperty>
            {
                new AlignmentSpotProperty {
                    MassCenter = 799.991d,
                    TimesCenter = new ChromXs(7),
                    AlignedPeakProperties =
                    [
                        new AlignmentChromPeakFeature(),
                    ]
                },
                new AlignmentSpotProperty {
                    MassCenter = 800d,
                    TimesCenter = new ChromXs(5),
                    AlignedPeakProperties =
                    [
                        new AlignmentChromPeakFeature(),
                    ]
                },
            };
            var masters = new List<ChromatogramPeakFeature>
            {
                new ChromatogramPeakFeature
                {
                    PrecursorMz = 799.991d,
                    ChromXs = new ChromXs(7),
                },
                new ChromatogramPeakFeature
                {
                    PrecursorMz = 800d,
                    ChromXs = new ChromXs(5),
                },
            };
            var target = new ChromatogramPeakFeature
            {
                MasterPeakID = 1,
                Name = "Target",
                PrecursorMz = 800d,
                ChromXs = new ChromXs(5),
            };
            var targets = new List<ChromatogramPeakFeature> { target, };

            var accessor = new StubAccessor([]);
            var evaluator = new FakeEvaluator();
            var joiner = new LcmsPeakJoiner(new() { RetentionTimeAlignmentTolerance = (float)rtTol, Ms1AlignmentTolerance = (float)mzTol }, accessor, evaluator);
            joiner.AlignPeaksToMaster(spots, masters, targets, 0);

            Assert.AreEqual(target.MasterPeakID, spots[1].AlignedPeakProperties[0].MasterPeakID);
            Assert.AreEqual(target.Name, spots[1].AlignedPeakProperties[0].Name);
        }

        [TestMethod]
        public void Join_OnlyRefMatched() {
            var rtTol = 1d;
            var mzTol = 0.01;

            var data = new List<List<ChromatogramPeakFeature>>
            {
                new List<ChromatogramPeakFeature>
                {
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(0d), Mass = 100d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(2d + rtTol * 0.9), Mass = 200d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(4d), Mass = 300d + mzTol * 0.9 }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(6d + rtTol * 0.9), Mass = 400d + mzTol * 0.9 }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(8d), Mass = 500d + mzTol * 0.9 }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(8d), Mass = 500d + mzTol * 0.5 }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(10d), Mass = 601d }),
                },
                new List<ChromatogramPeakFeature>
                {
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(0d), Mass = 100d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(2d), Mass = 200d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(4d), Mass = 300d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(6d), Mass = 400d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(8d), Mass = 500d }),
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { ChromXsTop = new ChromXs(10d), Mass = 600d }),
                },
            };
            var files = new List<AnalysisFileBean>
            {
                new AnalysisFileBean { AnalysisFileId = 0 },
                new AnalysisFileBean { AnalysisFileId = 1 },
            };
            var accessor = new StubAccessor(data);
            var evaluator = new FakeEvaluator();

            var parameter = new MsdialCore.Parameter.AlignmentBaseParameter {
                RetentionTimeAlignmentTolerance = (float)rtTol,
                Ms1AlignmentTolerance = (float)mzTol,
                UseRefMatchedPeaksOnly = true,
            };
            var joiner = new LcmsPeakJoiner(parameter, accessor, evaluator);
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

        [TestMethod()]
        public void AlignPeaksToMasterOverlapTest() {
            var rtTol = 1d;
            var mzTol = 0.01;
            var spots = new List<AlignmentSpotProperty>
            {
                BuildSpotProperty(100.00d, 10),
                BuildSpotProperty(100.01d, 11),
                BuildSpotProperty(100.02d, 12),
                BuildSpotProperty(100.03d, 13),
            };

            var masters = new List<ChromatogramPeakFeature>
            {
                BuildPeakFeature(0, 100.00d, 10),
                BuildPeakFeature(1, 100.01d, 11),
                BuildPeakFeature(2, 100.02d, 12),
                BuildPeakFeature(3, 100.03d, 13),
            };

            var targets = new List<ChromatogramPeakFeature>
            {
                BuildPeakFeature(1, 100.009d, 11),
                BuildPeakFeature(2, 100.011d, 11.2),
            };

            var evaluator = new FakeEvaluator();
            var accessor = new StubAccessor([]);
            var joiner = new LcmsPeakJoiner(new() { RetentionTimeAlignmentTolerance = (float)rtTol, Ms1AlignmentTolerance = (float)mzTol }, accessor, evaluator);
            joiner.AlignPeaksToMaster(spots, masters, targets, 0);

            Assert.AreEqual(-1, spots[0].AlignedPeakProperties[0].PeakID);
            Assert.AreEqual(1, spots[1].AlignedPeakProperties[0].PeakID);
            Assert.AreEqual(2, spots[2].AlignedPeakProperties[0].PeakID);
            Assert.AreEqual(-1, spots[3].AlignedPeakProperties[0].PeakID);
        }

        AlignmentSpotProperty BuildSpotProperty(double mass, double rt) {
            return new AlignmentSpotProperty
            {
                MassCenter = mass,
                TimesCenter = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    new AlignmentChromPeakFeature { PeakID = -1, },
                },
            };
        }

        ChromatogramPeakFeature BuildPeakFeature(int id, double mass, double rt) {
            var peak = new ChromatogramPeakFeature
            {
                PeakID = id,
                PrecursorMz = mass,
                ChromXs = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min), };
            peak.MatchResults.AddMspResult(100, MsScanMatchResultContainer.UnknownResult);
            return peak;
        }
    }

    class FakeEvaluator : IMatchResultEvaluator<MsScanMatchResult>
    {
        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
            throw new NotImplementedException();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result) {
            throw new NotImplementedException();
        }

        public bool IsReferenceMatched(MsScanMatchResult result) {
            return result is not null;
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
            throw new NotImplementedException();
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
            throw new NotImplementedException();
        }
    }

    class StubAccessor : DataAccessor, IFeatureAccessor<ChromatogramPeakFeature>
    {
        private List<List<ChromatogramPeakFeature>> scans;

        public StubAccessor(List<List<ChromatogramPeakFeature>> scans) {
            this.scans = scans;
        }

        public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, IReadOnlyList<RawSpectrum> spectrum, float ms1MassTolerance) {
            throw new NotImplementedException();
        }

        List<ChromatogramPeakFeature> IFeatureAccessor<ChromatogramPeakFeature>.GetMSScanProperties(AnalysisFileBean analysisFile) {
            if (analysisFile.AnalysisFileId < scans.Count) {
                return scans[analysisFile.AnalysisFileId];
            }
            return new List<ChromatogramPeakFeature>(0);
        }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var scans = ((IFeatureAccessor<ChromatogramPeakFeature>)this).GetMSScanProperties(analysisFile);
            return new List<IMSScanProperty>(scans);
        }
    }
}