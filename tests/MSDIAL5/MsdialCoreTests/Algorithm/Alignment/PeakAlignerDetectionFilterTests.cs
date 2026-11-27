using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Alignment.Tests
{
    /// <summary>
    /// Gap filling can count a peak joined from its file as not detected: the GC-MS gap filler keeps its MasterPeakID and
    /// sets its PeakID to -2 when it has no signal at the spot's quant mass. The detection filters (peak count, QC at
    /// least, N% detected in one group) are applied again after gap filling, so no spot is kept that fails them.
    /// </summary>
    [TestClass()]
    public class PeakAlignerDetectionFilterTests
    {
        private string _folder = string.Empty;

        [TestInitialize()]
        public void Initialize() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-aligner-detection-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup()]
        public void Cleanup() {
            if (Directory.Exists(_folder)) {
                Directory.Delete(_folder, recursive: true);
            }
        }

        [TestMethod()]
        public void ASpotThatFailsThePeakCountFilterAfterGapFillingIsRemoved() {
            // Peak count filter 50% of 4 files: 2 detected peaks. Every spot is joined in 2 or 3 files and passes the
            // filter before gap filling.
            var parameter = CreateParameter(peakCountFilter: 50f);
            var spots = new[]
            {
                CreateSpot(id: 0, joinedFiles: new[] { 0, 1, }),     // file 1 demoted: 1 detected, removed
                CreateSpot(id: 1, joinedFiles: new[] { 2, 3, }),     // nothing demoted: kept
                CreateSpot(id: 2, joinedFiles: new[] { 0, 1, 2, }),  // file 1 demoted: 2 detected, kept
            };
            var aligner = CreateAligner(parameter, spots, demoted: new[] { (0, 1), (2, 1), });

            var container = aligner.Alignment(AnalysisFiles(), new AlignmentFileBean { FileID = 0 }, null);

            CollectionAssert.AreEqual(new[] { "spot 1", "spot 2", }, container.AlignmentSpotProperties.Select(spot => spot.Name).ToArray());
        }

        [TestMethod()]
        public void ASpotWhoseQcPeakIsCountedAsNotDetectedFailsTheQcFilter() {
            var parameter = CreateParameter(peakCountFilter: 0f);
            parameter.FileID_AnalysisFileType[1] = AnalysisFileType.QC;
            parameter.QcAtLeastFilter = true;
            var spots = new[]
            {
                CreateSpot(id: 0, joinedFiles: new[] { 0, 1, }),
                CreateSpot(id: 1, joinedFiles: new[] { 0, 1, }),
            };
            var aligner = CreateAligner(parameter, spots, demoted: new[] { (0, 1), });

            var container = aligner.Alignment(AnalysisFiles(), new AlignmentFileBean { FileID = 0 }, null);

            CollectionAssert.AreEqual(new[] { "spot 1", }, container.AlignmentSpotProperties.Select(spot => spot.Name).ToArray());
        }

        [TestMethod()]
        public void ASpotThatFailsTheDetectedNumberFilterAfterGapFillingIsRemoved() {
            var parameter = CreateParameter(peakCountFilter: 0f);
            parameter.NPercentDetectedInOneGroup = 50f;
            var spots = new[]
            {
                CreateSpot(id: 0, joinedFiles: new[] { 0, 1, }),
                CreateSpot(id: 1, joinedFiles: new[] { 0, 1, }),
            };
            var aligner = CreateAligner(parameter, spots, demoted: new[] { (0, 1), });

            var container = aligner.Alignment(AnalysisFiles(), new AlignmentFileBean { FileID = 0 }, null);

            CollectionAssert.AreEqual(new[] { "spot 1", }, container.AlignmentSpotProperties.Select(spot => spot.Name).ToArray());
        }

        [TestMethod()]
        public void TheEicFileHoldsTheChromatogramsOfTheSpotsThatRemain() {
            var parameter = CreateParameter(peakCountFilter: 50f);
            var spots = new[]
            {
                CreateSpot(id: 0, joinedFiles: new[] { 0, 1, }),
                CreateSpot(id: 1, joinedFiles: new[] { 0, 1, }),
                CreateSpot(id: 2, joinedFiles: new[] { 2, 3, }),
                CreateSpot(id: 3, joinedFiles: new[] { 1, 2, }),
            };
            var aligner = CreateAligner(parameter, spots, demoted: new[] { (1, 1), });
            var alignmentFile = new AlignmentFileBean { FileID = 0, EicFilePath = Path.Combine(_folder, "AlignResult-test.EIC.aef") };
            var spotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");

            var container = aligner.Alignment(AnalysisFiles(), alignmentFile, spotSerializer);

            CollectionAssert.AreEqual(new[] { "spot 0", "spot 2", "spot 3", }, container.AlignmentSpotProperties.Select(spot => spot.Name).ToArray());
            // Each chromatogram records the id its spot had when it was collected (see SpotMarkingAccessor), so the EIC
            // entry at each position must belong to the spot at that position.
            var expected = new[] { 0f, 2f, 3f, };
            for (int i = 0; i < expected.Length; i++) {
                var info = spotSerializer.DeserializeAtFromFile(alignmentFile.EicFilePath, i);
                Assert.AreEqual(4, info.PeakInfos.Count);
                foreach (var peakInfo in info.PeakInfos) {
                    Assert.AreEqual(expected[i], peakInfo.ChromXsTop.Value, 1e-6);
                }
            }
        }

        [TestMethod()]
        public void NothingIsRemovedWhenGapFillingChangesNoDetection() {
            // As in LC-MS, IM-MS and DI-MS: only cells that were not detected are filled.
            var parameter = CreateParameter(peakCountFilter: 50f);
            var spots = new[]
            {
                CreateSpot(id: 0, joinedFiles: new[] { 0, 1, }),
                CreateSpot(id: 1, joinedFiles: new[] { 2, 3, }),
                CreateSpot(id: 2, joinedFiles: new[] { 0, 1, 2, }),
            };
            var aligner = CreateAligner(parameter, spots, demoted: Array.Empty<(int, int)>());

            var container = aligner.Alignment(AnalysisFiles(), new AlignmentFileBean { FileID = 0 }, null);

            CollectionAssert.AreEqual(new[] { "spot 0", "spot 1", "spot 2", }, container.AlignmentSpotProperties.Select(spot => spot.Name).ToArray());
        }

        private static List<AnalysisFileBean> AnalysisFiles() {
            return Enumerable.Range(0, 4)
                .Select(id => new AnalysisFileBean { AnalysisFileId = id, AnalysisFileName = $"file{id}", AnalysisFileClass = "A", AnalysisFileType = AnalysisFileType.Sample, })
                .ToList();
        }

        private static MockParameter CreateParameter(float peakCountFilter) {
            return new MockParameter
            {
                FileID_ClassName = Enumerable.Range(0, 4).ToDictionary(id => id, _ => "A"),
                FileID_AnalysisFileType = Enumerable.Range(0, 4).ToDictionary(id => id, _ => AnalysisFileType.Sample),
                PeakCountFilter = peakCountFilter,
            };
        }

        private static AlignmentSpotProperty CreateSpot(int id, int[] joinedFiles) {
            var peaks = Enumerable.Range(0, 4)
                .Select(fileID => {
                    var joined = joinedFiles.Contains(fileID);
                    return new AlignmentChromPeakFeature
                    {
                        FileID = fileID,
                        FileName = $"file{fileID}",
                        MasterPeakID = joined ? 10 * id + fileID : -1,
                        PeakID = joined ? 10 * id + fileID : -1,
                        Name = $"spot {id}",
                        Mass = 100d + id,
                        // A lower file id has the taller peak, so the first joined file represents the spot.
                        PeakHeightTop = joined ? 1000d - fileID : 0d,
                        ChromXsLeft = new ChromXs(id + 0.9d),
                        ChromXsTop = new ChromXs(id + 1d),
                        ChromXsRight = new ChromXs(id + 1.1d),
                    };
                })
                .ToList();
            return new AlignmentSpotProperty
            {
                MasterAlignmentID = id,
                AlignmentID = id,
                QuantMass = 100d + id,
                AlignedPeakProperties = peaks,
                AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>(),
            };
        }

        private static PeakAligner CreateAligner(ParameterBase parameter, AlignmentSpotProperty[] spots, (int SpotID, int FileID)[] demoted) {
            var iupac = new IupacDatabase();
            var factory = new DetectionFilterFactory(
                parameter,
                iupac,
                new FixedJoiner(spots),
                new DemotingGapFiller(demoted),
                new MockRefiner(iupac, new FacadeMatchResultEvaluator()),
                new SpotMarkingAccessor());
            var aligner = factory.CreatePeakAligner();
            aligner.ProviderFactory = new FakeDataProviderFactory();
            return aligner;
        }

        private sealed class DetectionFilterFactory : AlignmentProcessFactory
        {
            private readonly IPeakJoiner _joiner;
            private readonly IGapFiller _filler;
            private readonly IAlignmentRefiner _refiner;
            private readonly DataAccessor _accessor;

            public DetectionFilterFactory(ParameterBase param, IupacDatabase iupac, IPeakJoiner joiner, IGapFiller filler, IAlignmentRefiner refiner, DataAccessor accessor) : base(param, iupac) {
                _joiner = joiner;
                _filler = filler;
                _refiner = refiner;
                _accessor = accessor;
            }

            public override IAlignmentRefiner CreateAlignmentRefiner() => _refiner;
            public override DataAccessor CreateDataAccessor() => _accessor;
            public override IGapFiller CreateGapFiller() => _filler;
            public override PeakAligner CreatePeakAligner() => new PeakAligner(this, null);
            public override IPeakJoiner CreatePeakJoiner() => _joiner;
        }

        private sealed class FixedJoiner : IPeakJoiner
        {
            private readonly AlignmentSpotProperty[] _spots;

            public FixedJoiner(AlignmentSpotProperty[] spots) {
                _spots = spots;
            }

            public List<AlignmentSpotProperty> Join(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {
                return _spots.ToList();
            }
        }

        /// <summary>
        /// Fills every cell that was not joined, as every gap filler does, and counts the listed joined peaks as not
        /// detected, as the GC-MS gap filler does for a peak with no signal at the spot's quant mass.
        /// </summary>
        private sealed class DemotingGapFiller : IGapFiller
        {
            private readonly HashSet<(int SpotID, int FileID)> _demoted;

            public DemotingGapFiller((int SpotID, int FileID)[] demoted) {
                _demoted = new HashSet<(int SpotID, int FileID)>(demoted);
            }

            public bool NeedsGapFill(AlignmentSpotProperty spot, AnalysisFileBean analysisFile) => true;

            public void GapFill(Ms1Spectra ms1Spectra, RawSpectra rawSpectra, IReadOnlyList<RawSpectrum> spectra, AlignmentSpotProperty spot, int fileID) {
                var target = spot.AlignedPeakProperties.First(peak => peak.FileID == fileID);
                if (target.MasterPeakID < 0) {
                    target.MasterPeakID = -2;
                    target.PeakID = -2;
                }
                else if (spot.RepresentativeFileID != fileID && _demoted.Contains((spot.MasterAlignmentID, fileID))) {
                    target.PeakID = -2;
                    target.PeakHeightTop = 0d;
                }
            }
        }

        /// <summary>Records in each chromatogram the id its spot had when the chromatogram was collected.</summary>
        private sealed class SpotMarkingAccessor : DataAccessor
        {
            public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, float ms1MassTolerance) {
                return new ChromatogramPeakInfo(peak.FileID, null, spot.MasterAlignmentID, -1f, -1f);
            }

            public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
                throw new NotSupportedException();
            }
        }
    }
}
