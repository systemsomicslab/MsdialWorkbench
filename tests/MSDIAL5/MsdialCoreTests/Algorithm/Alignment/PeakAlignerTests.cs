using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Alignment.Tests
{
    [TestClass()]
    public class PeakAlignerTests
    {
        [TestMethod()]
        public void AlignmentTest() {
            var accessor = new MockAccessor(
                new List<List<IMSScanProperty>>
                {
                    new List<IMSScanProperty>
                    {
                        new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 100, ChromXsLeft = new ChromXs(0), ChromXsTop = new ChromXs(0), ChromXsRight = new ChromXs(0) }),
                        new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 200, ChromXsLeft = new ChromXs(0), ChromXsTop = new ChromXs(0), ChromXsRight = new ChromXs(0) }),
                        new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 300, ChromXsLeft = new ChromXs(0), ChromXsTop = new ChromXs(0), ChromXsRight = new ChromXs(0) }),
                    },
                    new List<IMSScanProperty>
                    {
                        new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 100, ChromXsLeft = new ChromXs(0.5), ChromXsTop = new ChromXs(0.5), ChromXsRight = new ChromXs(0.5) }),
                        new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 200, ChromXsLeft = new ChromXs(1.0), ChromXsTop = new ChromXs(1.0), ChromXsRight = new ChromXs(1.0) }),
                        new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 300, ChromXsLeft = new ChromXs(1.5), ChromXsTop = new ChromXs(1.5), ChromXsRight = new ChromXs(1.5) }),
                    },
                    new List<IMSScanProperty>
                    {
                        new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 200, ChromXsLeft = new ChromXs(1.0), ChromXsTop = new ChromXs(1.0), ChromXsRight = new ChromXs(1.0) }),
                        new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 200, ChromXsLeft = new ChromXs(2.0), ChromXsTop = new ChromXs(2.0), ChromXsRight = new ChromXs(2.0) }),
                        new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 200, ChromXsLeft = new ChromXs(3.0), ChromXsTop = new ChromXs(3.0), ChromXsRight = new ChromXs(3.0) }),
                    },
                }
            );

            var analysisFiles = new List<AnalysisFileBean>
            {
                new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileClass = "0" },
                new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileClass = "1" },
                new AnalysisFileBean { AnalysisFileId = 2, AnalysisFileClass = "2" },
                new AnalysisFileBean { AnalysisFileId = 3, AnalysisFileClass = "3" },
            };
            var alignmentFile = new AlignmentFileBean { FileID = 0 };

            var parameter = new MockParameter
            {
                FileID_ClassName = new Dictionary<int, string>
                {
                    {0, "0"}, {1, "1"}, {2, "2"}, {3, "3"},
                },
            };

            var aligner = CreateAligner(accessor, parameter);
            aligner.ProviderFactory = new FakeDataProviderFactory();

            var container = aligner.Alignment(analysisFiles, alignmentFile, null);

            Assert.AreEqual(7, container.TotalAlignmentSpotCount);

            Assert.AreEqual(0.25, container.AlignmentSpotProperties[0].TimesCenter.Value);
            Assert.AreEqual(0, container.AlignmentSpotProperties[1].TimesCenter.Value);
            Assert.AreEqual(0, container.AlignmentSpotProperties[2].TimesCenter.Value);
            Assert.AreEqual(1, container.AlignmentSpotProperties[3].TimesCenter.Value);
            Assert.AreEqual(1.5, container.AlignmentSpotProperties[4].TimesCenter.Value);
            Assert.AreEqual(2.0, container.AlignmentSpotProperties[5].TimesCenter.Value);
            Assert.AreEqual(3.0, container.AlignmentSpotProperties[6].TimesCenter.Value);

            Assert.AreEqual(100, container.AlignmentSpotProperties[0].MassCenter);
            Assert.AreEqual(200, container.AlignmentSpotProperties[1].MassCenter);
            Assert.AreEqual(300, container.AlignmentSpotProperties[2].MassCenter);
            Assert.AreEqual(200, container.AlignmentSpotProperties[3].MassCenter);
            Assert.AreEqual(300, container.AlignmentSpotProperties[4].MassCenter);
            Assert.AreEqual(200, container.AlignmentSpotProperties[5].MassCenter);
            Assert.AreEqual(200, container.AlignmentSpotProperties[6].MassCenter);
        }

        PeakAligner CreateAligner(DataAccessor accessor, ParameterBase parameter) {
            var iupac = new IupacDatabase();
            var joiner = new MockJoiner();
            var filler = new MockFiller(parameter);
            var refiner = new MockRefiner(iupac, new FacadeMatchResultEvaluator());
            var factory = new FakeFactory(parameter, iupac, joiner, filler, refiner, accessor);
            return factory.CreatePeakAligner();
        }
    }

    sealed class FakeFactory : AlignmentProcessFactory
    {
        private readonly MockJoiner _joiner;
        private readonly MockFiller _filler;
        private readonly MockRefiner _refiner;
        private readonly DataAccessor _accessor;

        public FakeFactory(ParameterBase param, IupacDatabase iupac, MockJoiner joiner, MockFiller filler, MockRefiner refiner, DataAccessor accessor) : base(param, iupac) {
            _joiner = joiner;
            _filler = filler;
            _refiner = refiner;
            _accessor = accessor;
        }

        public override IAlignmentRefiner CreateAlignmentRefiner() {
            return _refiner;
        }

        public override DataAccessor CreateDataAccessor() {
            return _accessor;
        }

        public override GapFiller CreateGapFiller() {
            return _filler;
        }

        public override PeakAligner CreatePeakAligner() {
            return new PeakAligner(this, null);
        }

        public override IPeakJoiner CreatePeakJoiner() {
            return _joiner;
        }
    }

    class FakeDataProviderFactory : IDataProviderFactory<AnalysisFileBean>
    {
        public IDataProvider Create(AnalysisFileBean source) {
            return new FakeDataProvider();
        }

        class FakeDataProvider : IDataProvider
        {
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
                return new List<RawSpectrum>().AsReadOnly();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
                throw new NotImplementedException();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
                throw new NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
                throw new NotImplementedException();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
                throw new NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
                throw new NotImplementedException();
            }
        }
    }

    class MockAccessor : DataAccessor
    {
        private List<List<IMSScanProperty>> scans;
        public MockAccessor(List<List<IMSScanProperty>> scans) { this.scans = scans; }

        public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, IReadOnlyList<RawSpectrum> spectrum, float ms1MassTolerance) {
            return new ChromatogramPeakInfo(-1, null, -1, -1, -1);
        }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            if (analysisFile.AnalysisFileId < scans.Count)
                return scans[analysisFile.AnalysisFileId];
            return new List<IMSScanProperty>();
        }
    }

    class MockJoiner : PeakJoiner
    {
        protected override bool IsSimilarTo(IMSScanProperty x, IMSScanProperty y) {
            return Math.Abs(x.ChromXs.Value - y.ChromXs.Value) < 1 && Math.Abs(x.PrecursorMz - y.PrecursorMz) < 1;
        }

        protected override double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            return 1 - Math.Abs(x.ChromXs.Value - y.ChromXs.Value) / 100 / 2 - Math.Abs(x.PrecursorMz - y.PrecursorMz) / 2;
        }
    }

    class MockFiller : GapFiller
    {
        public MockFiller(ParameterBase param) : base(param) {
        }

        protected override double AxTol => 0.5;

        protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return peaks.Average(peak => peak.PeakWidth(ChromXType.RT));
        }

        protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
            return new ChromXs(peaks.Average(peak => peak.ChromXsTop.Value))
            {
                Mz = new MzValue(peaks.Average(peak => peak.Mass)),
            };
        }

        protected override List<ChromatogramPeak> GetPeaks(Ms1Spectra ms1Spectra, RawSpectra rawSpectra, IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
            return new List<ChromatogramPeak> {
                new ChromatogramPeak(0, 50, 100, new RetentionTime(0.5)),
                new ChromatogramPeak(0, 70, 120, new RetentionTime(1)),
            };
        }
    }

    class MockRefiner : AlignmentRefiner
    {
        public MockRefiner(Common.DataObj.Database.IupacDatabase iupac, IMatchResultEvaluator<MsScanMatchResult> evaluator) : base(new ParameterBase(), iupac, evaluator) {
        }

        protected override List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> alignments) {
            return alignments;
        }

        protected override void SetLinks(List<AlignmentSpotProperty> alignments) {
            return;
        }
    }

    class MockParameter : ParameterBase
    {

    }
}