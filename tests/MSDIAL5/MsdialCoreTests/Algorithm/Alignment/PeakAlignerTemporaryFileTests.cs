using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
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
    /// The alignment spills one chromatogram file per analysis file before it writes the EIC file. Those spills are
    /// created beside the EIC file, not in %TEMP% on the system drive, and are deleted afterwards and on failure.
    /// </summary>
    [TestClass()]
    public class PeakAlignerTemporaryFileTests
    {
        private string _folder = string.Empty;

        [TestInitialize()]
        public void Initialize() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-aligner-spill-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup()]
        public void Cleanup() {
            if (Directory.Exists(_folder)) {
                Directory.Delete(_folder, recursive: true);
            }
        }

        [TestMethod()]
        public void ChromatogramSpillsAreCreatedBesideTheEicFileAndDeletedAfterwards() {
            var alignmentFile = new AlignmentFileBean { FileID = 0, EicFilePath = Path.Combine(_folder, "AlignResult-test.EIC.aef") };
            var aligner = CreateAligner(failOnFileId: null);

            var container = aligner.Alignment(AnalysisFiles(), alignmentFile, ChromatogramSerializerFactory.CreateSpotSerializer("CSS1"));

            Assert.AreEqual(7, container.TotalAlignmentSpotCount);
            Assert.AreEqual(4, aligner.SpillFiles.Count, "One spill per analysis file.");
            Assert.AreEqual(4, aligner.SpillFiles.Distinct().Count());
            foreach (var spill in aligner.SpillFiles) {
                Assert.AreEqual(_folder, Path.GetDirectoryName(spill));
                StringAssert.StartsWith(Path.GetFileName(spill), "AlignResult-test.EIC.aef.");
                StringAssert.EndsWith(spill, ".tmp");
            }
            Assert.IsTrue(aligner.SpillExistedWhenCollected.All(exists => exists));
            // Only the EIC file is left: every spill was deleted.
            CollectionAssert.AreEqual(new[] { alignmentFile.EicFilePath }, Directory.GetFiles(_folder));
            Assert.IsTrue(new FileInfo(alignmentFile.EicFilePath).Length > 0);
        }

        [TestMethod()]
        public void ChromatogramSpillsAreDeletedWhenTheAlignmentFails() {
            var alignmentFile = new AlignmentFileBean { FileID = 0, EicFilePath = Path.Combine(_folder, "AlignResult-test.EIC.aef") };
            var aligner = CreateAligner(failOnFileId: 2);

            Assert.ThrowsException<InvalidOperationException>(
                () => aligner.Alignment(AnalysisFiles(), alignmentFile, ChromatogramSerializerFactory.CreateSpotSerializer("CSS1")));

            Assert.AreEqual(3, aligner.SpillFiles.Count, "Files 0, 1 and 2 were reached before the failure.");
            Assert.AreEqual(_folder, Path.GetDirectoryName(aligner.SpillFiles[0]));
            // All four spills, including the one for the file never reached, are gone, and no EIC file was written.
            Assert.AreEqual(0, Directory.GetFiles(_folder).Length);
        }

        private static List<AnalysisFileBean> AnalysisFiles() {
            return new List<AnalysisFileBean>
            {
                new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileClass = "0" },
                new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileClass = "1" },
                new AnalysisFileBean { AnalysisFileId = 2, AnalysisFileClass = "2" },
                new AnalysisFileBean { AnalysisFileId = 3, AnalysisFileClass = "3" },
            };
        }

        private static RecordingPeakAligner CreateAligner(int? failOnFileId) {
            static ChromatogramPeakFeature Peak(double mass, double time) {
                return new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = mass, ChromXsLeft = new ChromXs(time), ChromXsTop = new ChromXs(time), ChromXsRight = new ChromXs(time) });
            }
            var accessor = new MockAccessor(new List<List<IMSScanProperty>>
            {
                new List<IMSScanProperty> { Peak(100, 0), Peak(200, 0), Peak(300, 0), },
                new List<IMSScanProperty> { Peak(100, 0.5), Peak(200, 1.0), Peak(300, 1.5), },
                new List<IMSScanProperty> { Peak(200, 1.0), Peak(200, 2.0), Peak(200, 3.0), },
            });
            var parameter = new MockParameter
            {
                FileID_ClassName = new Dictionary<int, string> { { 0, "0" }, { 1, "1" }, { 2, "2" }, { 3, "3" }, },
            };
            var iupac = new IupacDatabase();
            var factory = new FakeFactory(parameter, iupac, new MockJoiner(), new MockFiller(parameter), new MockRefiner(iupac, new FacadeMatchResultEvaluator()), accessor);
            return new RecordingPeakAligner(factory, failOnFileId) { ProviderFactory = new FakeDataProviderFactory() };
        }

        private sealed class RecordingPeakAligner : PeakAligner
        {
            private readonly int? _failOnFileId;

            public RecordingPeakAligner(AlignmentProcessFactory factory, int? failOnFileId) : base(factory, null) {
                _failOnFileId = failOnFileId;
            }

            public List<string> SpillFiles { get; } = new List<string>();
            public List<bool> SpillExistedWhenCollected { get; } = new List<bool>();

            protected override string CollectAlignmentPeaks(
                AnalysisFileBean analysisFile, List<AlignmentChromPeakFeature> peaks, List<AlignmentSpotProperty> spots,
                string tempFile, ChromatogramSerializer<ChromatogramPeakInfo> serializer = null) {
                SpillFiles.Add(tempFile);
                SpillExistedWhenCollected.Add(File.Exists(tempFile));
                if (analysisFile.AnalysisFileId == _failOnFileId) {
                    throw new InvalidOperationException("Simulated failure while collecting the peaks of one file.");
                }
                return base.CollectAlignmentPeaks(analysisFile, peaks, spots, tempFile, serializer);
            }
        }
    }
}
