using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Export.Tests
{
    [TestClass]
    public class MztabFormatExportTests
    {
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\test_mztab.mzTab.txt", @"Resources\Export")]
        public async Task MztabFormatExporterTest() {
            IMsdialDataStorage<ParameterBase> storage = default!;
            using (var streamManager = new DirectoryTreeStreamManager("./Resources/Export")) {
                storage = await MsdialDataStorage.Serializer.LoadAsync(streamManager, "Dataset_2025_07_31_12_31_11.mddata", "", "");
                storage.FixDatasetFolder("./Resources/Export");
            }
            var alignmentFile = storage.AlignmentFiles.Last();
            var container = AlignmentResultContainer.Load(alignmentFile);
            var loader = new MSDec.MSDecLoader(alignmentFile.SpectraFilePath, []);

            var exporter = new MztabFormatExporter(storage.DataBases);
            using var stream = new MemoryStream();
            //using var stream = File.Open("../../test_mztab.mzTab.txt", FileMode.Create);
            exporter.MztabFormatExporterCore(
                stream,
                [.. container.AlignmentSpotProperties],
                loader.LoadMSDecResults(),
                storage.AnalysisFiles,
                new StubMetadataAccessor(storage.DataBaseMapper, storage.Parameter),
                new LegacyQuantValueAccessor("Height", storage.Parameter),
                [ StatsValue.Average, StatsValue.Stdev, ],
                "mztab_test");

            using var exp_stream = File.Open("Resources/Export/test_mztab.mzTab.txt", FileMode.Open);
            var buffer = new byte[stream.GetBuffer().Length];
            await exp_stream.ReadAsync(buffer);

            CollectionAssert.AreEqual(buffer, stream.GetBuffer());
        }

        /// <summary>
        /// A spot whose search kept more than one candidate publishes them all as ranked evidence rows of
        /// one input spectrum, which is how mzTab-M says "A or B".
        /// </summary>
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task AnIndistinguishableAlternativeIsPublishedAsASecondRankedEvidenceRow() {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();

            var spot = container.AlignmentSpotProperties.First(s => s.Name == "Quercetin");
            var alternative = spot.MatchResults.Representative.Clone();
            alternative.Name = "Morin";
            alternative.LibraryID = spot.MatchResults.Representative.LibraryID + 1000;
            // Strictly lower on the last term of the ordering only, so the representative does not change
            // and the alternative is unambiguously rank 2.
            alternative.TotalScore = spot.MatchResults.Representative.TotalScore - 0.1f;
            spot.MatchResults.AddResult(alternative);

            var lines = ExportToLines(storage, container, msdecs);

            var evidence = lines.Where(line => line.StartsWith("SME\t")).Select(line => line.Split('\t')).ToArray();
            var forThisSpot = evidence.Where(f => f[2] == spot.MasterAlignmentID.ToString()).ToArray();
            Assert.AreEqual(2, forThisSpot.Length, "both candidates should be published");
            CollectionAssert.AreEqual(new[] { "Quercetin", "Morin" }, forThisSpot.Select(f => f[7]).ToArray());
            CollectionAssert.AreEqual(new[] { "1", "2" }, forThisSpot.Select(f => f[f.Length - 1]).ToArray());
            Assert.AreEqual("small_test_msp:Morin", forThisSpot[1][3]);

            // The rows carry consecutive, file-unique identifiers and the feature row references both.
            var smeIds = forThisSpot.Select(f => f[1]).ToArray();
            Assert.AreEqual(int.Parse(smeIds[0]) + 1, int.Parse(smeIds[1]));
            var feature = lines.Select(line => line.Split('\t'))
                .Single(f => f[0] == "SMF" && f[1] == spot.MasterAlignmentID.ToString());
            Assert.AreEqual(string.Join("|", smeIds), feature[2]);
            Assert.AreEqual("1", feature[3], "two alternatives for one feature is ambiguity code 1");
        }

        /// <summary>
        /// One candidate is not an ambiguity, so the feature row leaves the code null.
        /// </summary>
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task ASingleCandidateLeavesTheAmbiguityCodeNull() {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var spot = container.AlignmentSpotProperties.First(s => s.Name == "Quercetin");

            var lines = ExportToLines(storage, container, msdecs);

            var feature = lines.Select(line => line.Split('\t'))
                .Single(f => f[0] == "SMF" && f[1] == spot.MasterAlignmentID.ToString());
            Assert.AreEqual("null", feature[3]);
            Assert.IsFalse(feature[2].Contains("|"), "a single reference needs no separator");
        }

        /// <summary>
        /// A feature row must not reference an evidence row that was never written. The reference set and
        /// the rows come from the same layout pass, so this holds for every row of the file.
        /// </summary>
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task EveryFeatureReferenceResolvesToAnEvidenceRow() {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();

            var lines = ExportToLines(storage, container, msdecs);
            var rows = lines.Select(line => line.Split('\t')).ToArray();

            var written = rows.Where(f => f[0] == "SME").Select(f => f[1]).ToArray();
            var referenced = rows.Where(f => f[0] == "SMF" && f[2] != "null").SelectMany(f => f[2].Split('|')).ToArray();
            CollectionAssert.AreEquivalent(written, referenced);
            CollectionAssert.AllItemsAreUnique(written);
        }

        private static async Task<(IMsdialDataStorage<ParameterBase>, AlignmentResultContainer, List<MSDec.MSDecResult>)> LoadExportFixtureAsync() {
            IMsdialDataStorage<ParameterBase> storage;
            using (var streamManager = new DirectoryTreeStreamManager("./Resources/Export")) {
                storage = await MsdialDataStorage.Serializer.LoadAsync(streamManager, "Dataset_2025_07_31_12_31_11.mddata", "", "");
                storage.FixDatasetFolder("./Resources/Export");
            }
            var alignmentFile = storage.AlignmentFiles.Last();
            var container = AlignmentResultContainer.Load(alignmentFile);
            var loader = new MSDec.MSDecLoader(alignmentFile.SpectraFilePath, []);
            return (storage, container, loader.LoadMSDecResults());
        }

        private static string[] ExportToLines(
            IMsdialDataStorage<ParameterBase> storage,
            AlignmentResultContainer container,
            List<MSDec.MSDecResult> msdecs) {
            var exporter = new MztabFormatExporter(storage.DataBases);
            using var stream = new MemoryStream();
            exporter.MztabFormatExporterCore(
                stream,
                [.. container.AlignmentSpotProperties],
                msdecs,
                storage.AnalysisFiles,
                new StubMetadataAccessor(storage.DataBaseMapper, storage.Parameter),
                new LegacyQuantValueAccessor("Height", storage.Parameter),
                [StatsValue.Average, StatsValue.Stdev,],
                "mztab_test");
            return System.Text.Encoding.ASCII.GetString(stream.ToArray()).Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        }

        /// <summary>
        /// Agilent and Bruker both write a ".d" folder, and ms_run-format is told apart by what the folder holds.
        /// When no format was recorded for it, a folder that is not on disk when the file is exported (moved, archived,
        /// or a project opened on another machine) gets a neutral user term, not a vendor it may not be.
        /// </summary>
        /// <remarks>
        /// ms_run-id_format is not checked: SetRawFileMetadataDic overwrites it with "MS-DIAL set Datapoint Number"
        /// for every format after the switch.
        /// </remarks>
        [DataTestMethod()]
        [DataRow("AcqData/", "[MS, MS:1001509, Agilent MassHunter format, ]")]
        [DataRow("analysis.baf", "[MS, MS:1000815, Bruker BAF format, ]")]
        [DataRow("analysis.tdf", "[MS, MS:1002817, Bruker TDF format, ]")]
        [DataRow("analysis.tsf", "[MS, MS:1003282, Bruker TSF format, ]")]
        [DataRow("", "[,, Unknown .d folder format, ]")]
        [DataRow(null, "[,, Unknown .d folder format, ]")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task ADotDFolderIsDescribedByWhatItHolds(string content, string expectedFormat) {
            // content: a file name, a folder name ending in "/", "" for an empty .d folder, null for no folder at all.
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var scratch = Path.Combine(Path.GetTempPath(), $"msdial-mztab-dotd-{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try {
                foreach (var file in storage.AnalysisFiles) {
                    var folder = Path.Combine(scratch, file.AnalysisFileName + ".d");
                    file.AnalysisFilePath = folder;
                    if (content is null) {
                        continue;
                    }
                    Directory.CreateDirectory(folder);
                    if (content.EndsWith("/")) {
                        Directory.CreateDirectory(Path.Combine(folder, content.TrimEnd('/')));
                    }
                    else if (content.Length > 0) {
                        File.WriteAllBytes(Path.Combine(folder, content), new byte[0]);
                    }
                }

                var lines = ExportToLines(storage, container, msdecs);

                var formats = lines.Select(line => line.Split('\t'))
                    .Where(f => f[0] == "MTD" && f.Length > 2 && f[1].StartsWith("ms_run[") && f[1].EndsWith("]-format"))
                    .Select(f => f[2])
                    .ToArray();
                Assert.AreEqual(storage.AnalysisFiles.Count, formats.Length);
                Assert.IsTrue(formats.All(format => format == expectedFormat), string.Join(" | ", formats));
            }
            finally {
                Directory.Delete(scratch, recursive: true);
            }
        }

        /// <summary>
        /// A .raw is a Waters folder or a Thermo file. When no format was recorded for it, one that is not on disk when
        /// the file is exported (which used to throw from File.GetAttributes) gets a neutral user term, as an
        /// unrecognisable .d folder does.
        /// </summary>
        [DataTestMethod()]
        [DataRow("folder", "[MS, MS:1000526, Waters raw format, ]")]
        [DataRow("file", "[MS, MS:1000563, Thermo RAW format, ]")]
        [DataRow("missing", "[,, Unknown .raw format, ]")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task ADotRawPathIsDescribedByWhatItIs(string kind, string expectedFormat) {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var scratch = Path.Combine(Path.GetTempPath(), $"msdial-mztab-dotraw-{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try {
                foreach (var file in storage.AnalysisFiles) {
                    var path = Path.Combine(scratch, file.AnalysisFileName + ".raw");
                    file.AnalysisFilePath = path;
                    if (kind == "folder") {
                        Directory.CreateDirectory(path);
                    }
                    else if (kind == "file") {
                        File.WriteAllBytes(path, new byte[0]);
                    }
                }

                var lines = ExportToLines(storage, container, msdecs);

                var formats = lines.Select(line => line.Split('\t'))
                    .Where(f => f[0] == "MTD" && f.Length > 2 && f[1].StartsWith("ms_run[") && f[1].EndsWith("]-format"))
                    .Select(f => f[2])
                    .ToArray();
                Assert.AreEqual(storage.AnalysisFiles.Count, formats.Length);
                Assert.IsTrue(formats.All(format => format == expectedFormat), string.Join(" | ", formats));
            }
            finally {
                Directory.Delete(scratch, recursive: true);
            }
        }

        /// <summary>
        /// A .d folder is read as RawDataHandler reads it, AcqData first, so one that holds both an AcqData folder and a
        /// Bruker analysis file is Agilent's. This is the one case whose ms_run-format changed when formats began to be
        /// recorded: it used to be described by the Bruker file.
        /// </summary>
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task ADotDFolderHoldingAcqDataIsAgilentsWhateverElseItHolds() {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var scratch = Path.Combine(Path.GetTempPath(), $"msdial-mztab-dotd-both-{System.Guid.NewGuid():N}");
            try {
                foreach (var file in storage.AnalysisFiles) {
                    var folder = Path.Combine(scratch, file.AnalysisFileName + ".d");
                    Directory.CreateDirectory(Path.Combine(folder, "AcqData"));
                    File.WriteAllBytes(Path.Combine(folder, "analysis.tdf"), new byte[0]);
                    file.AnalysisFilePath = folder;
                }

                var formats = MsRunFormats(ExportToLines(storage, container, msdecs));

                Assert.AreEqual(storage.AnalysisFiles.Count, formats.Length);
                Assert.IsTrue(formats.All(format => format == "[MS, MS:1001509, Agilent MassHunter format, ]"), string.Join(" | ", formats));
            }
            finally {
                Directory.Delete(scratch, recursive: true);
            }
        }

        /// <summary>
        /// A format recorded while the raw data were present is what ms_run-format says after they are gone, with the
        /// term the exporter writes for that format on disk.
        /// </summary>
        [DataTestMethod()]
        [DataRow(RawFileFormat.ThermoRaw, ".raw", "[MS, MS:1000563, Thermo RAW format, ]")]
        [DataRow(RawFileFormat.WatersRaw, ".raw", "[MS, MS:1000526, Waters raw format, ]")]
        [DataRow(RawFileFormat.AgilentMassHunterD, ".d", "[MS, MS:1001509, Agilent MassHunter format, ]")]
        [DataRow(RawFileFormat.BrukerTdf, ".d", "[MS, MS:1002817, Bruker TDF format, ]")]
        [DataRow(RawFileFormat.BrukerTsf, ".d", "[MS, MS:1003282, Bruker TSF format, ]")]
        [DataRow(RawFileFormat.BrukerBaf, ".d", "[MS, MS:1000815, Bruker BAF format, ]")]
        [DataRow(RawFileFormat.SciexWiff, ".wiff", "[MS, MS:1000562, ABI WIFF format, ]")]
        [DataRow(RawFileFormat.ShimadzuLcd, ".lcd", "[MS, MS:1003009, Shimadzu Biotech LCD format, ]")]
        [DataRow(RawFileFormat.MzML, ".mzML", "[MS, MS:1000584, mzML format, ]")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task ARecordedFormatIsWrittenWhenTheRawDataAreGone(RawFileFormat recorded, string extension, string expectedFormat) {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var gone = Path.Combine(Path.GetTempPath(), $"msdial-mztab-gone-{System.Guid.NewGuid():N}");
            foreach (var file in storage.AnalysisFiles) {
                file.AnalysisFilePath = Path.Combine(gone, file.AnalysisFileName + extension);
                file.RawFileFormat = recorded;
            }

            var formats = MsRunFormats(ExportToLines(storage, container, msdecs));

            Assert.AreEqual(storage.AnalysisFiles.Count, formats.Length);
            Assert.IsTrue(formats.All(format => format == expectedFormat), string.Join(" | ", formats));
            foreach (var format in formats) {
                AssertIsAPsiMsFileFormat(format);
            }
            Assert.IsFalse(Directory.Exists(gone), "exporting must not create the raw data folder");
        }

        /// <summary>
        /// A recorded format that cannot be the file at the path (a Thermo format on a .d) is not trusted; the file is
        /// described by what the disk shows, as when nothing is recorded.
        /// </summary>
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task ARecordedFormatThatDoesNotMatchThePathIsIgnored() {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var scratch = Path.Combine(Path.GetTempPath(), $"msdial-mztab-mismatch-{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try {
                Assert.AreEqual(2, storage.AnalysisFiles.Count);
                // Present: a Bruker TDF folder recorded as Thermo. Gone: a .d recorded as Thermo.
                var present = Path.Combine(scratch, "present.d");
                Directory.CreateDirectory(present);
                File.WriteAllBytes(Path.Combine(present, "analysis.tdf"), new byte[0]);
                storage.AnalysisFiles[0].AnalysisFilePath = present;
                storage.AnalysisFiles[0].RawFileFormat = RawFileFormat.ThermoRaw;
                storage.AnalysisFiles[1].AnalysisFilePath = Path.Combine(scratch, "gone.d");
                storage.AnalysisFiles[1].RawFileFormat = RawFileFormat.ThermoRaw;

                var formats = MsRunFormats(ExportToLines(storage, container, msdecs));

                CollectionAssert.AreEqual(new[] { "[MS, MS:1002817, Bruker TDF format, ]", "[,, Unknown .d folder format, ]" }, formats);
            }
            finally {
                Directory.Delete(scratch, recursive: true);
            }
        }

        /// <summary>
        /// A recorded format is never replaced, so it can outlive the file it was recorded for. Raw data on disk are
        /// described by what is there, exactly as before formats were recorded: a Waters folder where a Thermo file was
        /// recorded is Waters, and a Thermo file where a Waters folder was recorded is Thermo.
        /// </summary>
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task RawDataOnDiskAreDescribedByWhatIsThereWhateverWasRecorded() {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var scratch = Path.Combine(Path.GetTempPath(), $"msdial-mztab-replaced-{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try {
                Assert.AreEqual(2, storage.AnalysisFiles.Count);
                var waters = Path.Combine(scratch, "was_thermo.raw");
                Directory.CreateDirectory(waters);
                storage.AnalysisFiles[0].AnalysisFilePath = waters;
                storage.AnalysisFiles[0].RawFileFormat = RawFileFormat.ThermoRaw;
                var thermo = Path.Combine(scratch, "was_waters.RAW");
                File.WriteAllBytes(thermo, new byte[0]);
                storage.AnalysisFiles[1].AnalysisFilePath = thermo;
                storage.AnalysisFiles[1].RawFileFormat = RawFileFormat.WatersRaw;

                var formats = MsRunFormats(ExportToLines(storage, container, msdecs));

                CollectionAssert.AreEqual(new[] { "[MS, MS:1000526, Waters raw format, ]", "[MS, MS:1000563, Thermo RAW format, ]" }, formats);
                Assert.AreEqual(RawFileFormat.ThermoRaw, storage.AnalysisFiles[0].RawFileFormat, "exporting does not change what is recorded");
                Assert.AreEqual(RawFileFormat.WatersRaw, storage.AnalysisFiles[1].RawFileFormat, "exporting does not change what is recorded");
            }
            finally {
                Directory.Delete(scratch, recursive: true);
            }
        }

        /// <summary>
        /// Registration to export: the format is recorded while the raw data are present, the project is saved, the raw
        /// data are deleted, and the project reloaded from what was saved still names each format with its PSI-MS term.
        /// </summary>
        [DataTestMethod()]
        [DataRow("thermo", "waters")]
        [DataRow("bruker-tdf", "agilent")]
        [DataRow("bruker-tsf", "bruker-baf")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task AFormatRecordedAtRegistrationOutlivesTheRawData(string firstKind, string secondKind) {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            Assert.AreEqual(2, storage.AnalysisFiles.Count);
            var kinds = new[] { firstKind, secondKind };
            var scratch = Path.Combine(Path.GetTempPath(), $"msdial-mztab-e2e-{System.Guid.NewGuid():N}");
            var expected = new string[kinds.Length];
            try {
                for (int i = 0; i < kinds.Length; i++) {
                    var file = storage.AnalysisFiles[i];
                    (file.AnalysisFilePath, expected[i]) = CreateRawData(scratch, file.AnalysisFileName, kinds[i]);
                    Assert.AreEqual(RawFileFormat.Unknown, file.RawFileFormat, "the fixture predates recorded formats");
                    Assert.IsTrue(RawFileFormatClassifier.RecordIfUnknown(file), kinds[i]);
                }
            }
            finally {
                // The project is saved and exported only after the raw data are gone.
                Directory.Delete(scratch, recursive: true);
            }

            var memory = new MemoryStream();
            using (IStreamManager manager = ZipStreamManager.OpenCreate(memory)) {
                await storage.SaveAsync(manager, "Test", "TestFolder");
                manager.Complete();
            }
            IMsdialDataStorage<ParameterBase> reloaded;
            using (var manager = ZipStreamManager.OpenGet(memory)) {
                reloaded = await MsdialDataStorage.Serializer.LoadAsync(manager, "Test", null, "TestFolder");
            }

            var formats = MsRunFormats(ExportToLines(reloaded, container, msdecs));

            CollectionAssert.AreEqual(expected, formats);
            foreach (var format in formats) {
                AssertIsAPsiMsFileFormat(format);
            }
            Assert.IsFalse(Directory.Exists(scratch), "exporting must not create the raw data folder");
        }

        /// <summary>
        /// Every PSI-MS term the exporter can write as ms_run-format, for any recorded format, is a file format term.
        /// </summary>
        [TestMethod()]
        public void EveryPsiMsFormatTermIsAFileFormat() {
            var written = 0;
            foreach (var format in System.Enum.GetValues(typeof(RawFileFormat)).Cast<RawFileFormat>()) {
                foreach (var extension in new[] { ".RAW", ".D", ".MZML", ".TXT" }) {
                    var term = MztabFormatExporter.MsRunFormatTerms(format, extension).Format;
                    if (term.StartsWith("[MS,")) {
                        AssertIsAPsiMsFileFormat(term);
                        written++;
                    }
                }
            }
            Assert.IsTrue(written > 0);
        }

        /// <summary>
        /// The children of MS:1000560 (mass spectrometer file format) that the exporter writes, with their names as in
        /// psi-ms.obo (checked against data-version 4.2.2, where none of them is obsolete).
        /// </summary>
        private static readonly Dictionary<string, string> PsiMsFileFormats = new Dictionary<string, string>
        {
            ["MS:1000526"] = "Waters raw format",
            ["MS:1000562"] = "ABI WIFF format",
            ["MS:1000563"] = "Thermo RAW format",
            ["MS:1000584"] = "mzML format",
            ["MS:1000815"] = "Bruker BAF format",
            ["MS:1001509"] = "Agilent MassHunter format",
            ["MS:1002817"] = "Bruker TDF format",
            ["MS:1003009"] = "Shimadzu Biotech LCD format",
            ["MS:1003282"] = "Bruker TSF format",
        };

        private static void AssertIsAPsiMsFileFormat(string term) {
            // An mzTab-M parameter: [CV label, accession, name, value].
            var fields = term.TrimStart('[').TrimEnd(']').Split(',').Select(field => field.Trim()).ToArray();
            Assert.AreEqual(4, fields.Length, term);
            Assert.AreEqual("MS", fields[0], term);
            Assert.IsTrue(PsiMsFileFormats.TryGetValue(fields[1], out var name), $"{fields[1]} is not a known file format term: {term}");
            Assert.AreEqual(name, fields[2], term);
            Assert.AreEqual(string.Empty, fields[3], term);
        }

        private static (string Path, string ExpectedFormat) CreateRawData(string folder, string name, string kind) {
            switch (kind) {
                case "thermo": {
                    var path = Path.Combine(folder, name + ".raw");
                    Directory.CreateDirectory(folder);
                    File.WriteAllBytes(path, new byte[0]);
                    return (path, "[MS, MS:1000563, Thermo RAW format, ]");
                }
                case "waters": {
                    var path = Path.Combine(folder, name + ".raw");
                    Directory.CreateDirectory(path);
                    return (path, "[MS, MS:1000526, Waters raw format, ]");
                }
                case "agilent": {
                    var path = Path.Combine(folder, name + ".d");
                    Directory.CreateDirectory(Path.Combine(path, "AcqData"));
                    return (path, "[MS, MS:1001509, Agilent MassHunter format, ]");
                }
                case "bruker-tdf":
                case "bruker-tsf":
                case "bruker-baf": {
                    var path = Path.Combine(folder, name + ".d");
                    Directory.CreateDirectory(path);
                    File.WriteAllBytes(Path.Combine(path, "analysis." + kind.Substring("bruker-".Length)), new byte[0]);
                    var expected = kind == "bruker-tdf" ? "[MS, MS:1002817, Bruker TDF format, ]"
                        : kind == "bruker-tsf" ? "[MS, MS:1003282, Bruker TSF format, ]"
                        : "[MS, MS:1000815, Bruker BAF format, ]";
                    return (path, expected);
                }
                default:
                    throw new System.ArgumentException(kind);
            }
        }

        private static string[] MsRunFormats(string[] lines) {
            return lines.Select(line => line.Split('\t'))
                .Where(f => f[0] == "MTD" && f.Length > 2 && f[1].StartsWith("ms_run[") && f[1].EndsWith("]-format"))
                .Select(f => f[2])
                .ToArray();
        }


        [TestMethod()]
        //[DeploymentItem(@"Resources\Export\small_test_project.mdproject", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        //[DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        //[DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_DriftSpots.arf", @"Resources\Export")]
        public async Task MztabWriteMtdSectionTest()
        {
            IMsdialDataStorage<ParameterBase> storage = default!;
            using (var streamManager = new DirectoryTreeStreamManager("./Resources/Export")) {
                storage = await MsdialDataStorage.Serializer.LoadAsync(streamManager, "Dataset_2025_07_31_12_31_11.mddata", "", "");
                storage.FixDatasetFolder("./Resources/Export");
            }

            var dbs = storage.DataBases.MetabolomicsDataBases.Select(db => new MztabFormatExporter.Database
            {
                AnnotatorID = db.DataBase.Id,
                Metadata = "[,, User-defined MSP library file, ]",
                Type = "null",
                Filename = Path.GetFileName(db.DataBase.DataBaseSourceFilePath),
                Uri = "file://" + db.DataBase.DataBaseSourceFilePath.Replace("\\", "/").Replace(" ", "%20") ?? "null"
            }).ToArray();
            var file2class = storage.AnalysisFiles.ToDictionary(f => f.AnalysisFileId, f => f.AnalysisFileClass);
            var idConfidenceMeasure = new Dictionary<int, string> {
                [1] = "[,, MS-DIAL algorithm matching score, ]",
                [2] = "[,, Retention time similarity, ]",
                [3] = "[,, m/z similarity, ]",
                [4] = "[,, Simple dot product, ]",
                [5] = "[,, Weighted dot product, ]",
                [6] = "[,, Reverse dot product, ]",
                [7] = "[,, Matched peaks count, ]",
                [8] = "[,, Matched peaks percentage, ]",
            };
            var msRunFormat = "[,, ABF(Analysis Base File) file, ]";
            var msRunIDFormat = "[,, ABF file Datapoint Number, ]";
            var rawFileMetadata = storage.AnalysisFiles.Select(f => new MztabFormatExporter.RawFileMetadata
            {
                Id = f.AnalysisFileId + 1,
                Assay = "assay[" + (f.AnalysisFileId + 1) + "]",
                Assay_ref = f.AnalysisFileName,
                Run = "ms_run[" + (f.AnalysisFileId + 1) + "]",
                FileLocation = "file://" + f.AnalysisFilePath.Replace("\\", "/").Replace(" ", "%20"),
                Format_cv = msRunFormat,
                Id_format_cv = msRunIDFormat,
                Scan_polarity = storage.Parameter.IonMode.ToString(),
                Scan_polarity_cv = storage.Parameter.IonMode.ToString() == "Positive" ? "[MS,MS:1000130,positive scan,]" : "[MS, MS:1000129, negative scan, ]",
                AnalysisFileExtention = ".ABF",
                AnalysisClass = f.AnalysisFileClass,
                AnalysisFileId = f.AnalysisFileId
            }).ToDictionary(d => d.Id, d => d);

            var alignmentFile = storage.AlignmentFiles.Last();
            //var container = AlignmentResultContainer.Load(new AlignmentFileBean { FilePath = Path.Combine("./Resources/Export", Path.GetFileName(alignmentFile.FilePath)), });
            var container = AlignmentResultContainer.Load(alignmentFile);

            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, leaveOpen: true))
            {
                var exporter = new MztabFormatExporter(storage.DataBases);
                exporter.WriteMtdSection(writer, "test_mztab", storage.Parameter, [.. container.AlignmentSpotProperties], rawFileMetadata, file2class, idConfidenceMeasure, dbs);
            }
        }

        private static ParameterBase parameter = new ParameterBase
        {
            Authors = "Authors",
            Comment = "Comment",
            TargetOmics = TargetOmics.Metabolomics,
            MS2DataType = MSDataType.Centroid,
            IsHeightMatrixExport = true,
            IsIdentifiedImportedInStatistics = true,
            IsKeepRemovableFeaturesAndAssignedTagForChecking = true,
            IsKeepSuggestedMetaboliteFeatures = false,
            IsMassMatrixExport = false,
            IsNormalizeIS = false,
            IsNormalizeIsLowess = false,
            IsNormalizeLowess = false,
            IsNormalizeMTic = false,
            IsNormalizeNone = true,
            IsNormalizeSplash = false,
            IsNormalizeTic = false,
            IsNormalizedMatrixExport = false,
            IsPeakAreaMatrixExport = false,
            IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = false,
        };
        private const string DEFAULT_SEPARATOR = "\t";

        private const string mztabVersion = "2.0.0-M";
        private const string mtdPrefix = "MTD";
        private const string smlPrefix = "SML";
        private const string commentPrefix = "COM";
        private const string mztabId ="mzTab-M-Test";

        private static readonly List<string> cvItem1 = new() { "MS", "PSI-MS controlled vocabulary", "4.1.192", "https://www.ebi.ac.uk/ols/ontologies/ms" };
        private static readonly List<string> cvItem2 = new() { "UO", "Units of Measurement Ontology", "2023-05-25", "http://purl.obolibrary.org/obo/uo.owl" };
        private readonly DataBaseStorage _dataBaseStorage;
        private const string idConfidenceDefault = "[,, MS-DIAL algorithm matching score, ]";
        private const string idConfidenceManual = "[MS, MS:1001058, quality estimation by manual validation, ]";
        private const string quantificationMethod = "[MS, MS:1002019, Label-free raw feature quantitation, ]";

        private const string smallMoleculeIdentificationReliability = "[MS, MS:1003032, compound identification confidence code in MS-DIAL, ]"; // new define on psi-ms.obo

        public string Separator { get; }

        private readonly Dictionary<string, string> _annotatorID2DataBaseID;

        /// <summary>
        /// The name column holds a compound name, and the processing status is a column of its own.
        /// </summary>
        /// <remarks>
        /// These exercise what the checked-in fixture cannot: it contains no prefixed name, no
        /// dual lipid name and no evidence record, so reverting the normalisation leaves the
        /// byte-compared golden entirely green. The golden pins that nothing ELSE moved; these pin
        /// the change itself.
        /// </remarks>
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task APrefixedNameIsPublishedAsACompoundNameAndAnEvidenceColumn() {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var spot = container.AlignmentSpotProperties.First(s => s.Name == "Quercetin");
            spot.Name = AnnotationName.AsNoMs2("Quercetin");
            spot.MatchResults.Representative.EvidenceSource = AnnotationEvidenceSource.PrecursorOnly;

            var fields = SmallMoleculeRow(ExportToLines(storage, container, msdecs), spot);

            Assert.AreEqual("Quercetin", fields.Value("chemical_name"),
                "the status no longer travels inside the name");
            Assert.AreEqual("PrecursorOnly", fields.Value("opt_global_evidence_source"),
                "it travels here instead, where it says more than the prefix did");
        }

        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task ALipidWhoseChainsWereNotResolvedIsPublishedAtClassLevel() {
            // The author's rule of 2026-09-10: precursor mass alone does not support an sn-chain
            // composition, so the class-level form is what may be published.
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var spot = container.AlignmentSpotProperties.First(s => s.Name == "Quercetin");
            spot.Name = "PC 34:1|PC 16:0_18:1";
            spot.MatchResults.Representative.IsLipidChainsMatch = false;

            var fields = SmallMoleculeRow(ExportToLines(storage, container, msdecs), spot);

            Assert.AreEqual("PC 34:1", fields.Value("chemical_name"));
        }

        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task ALipidWhoseChainsWereResolvedKeepsThem() {
            // The control, and the reason the level cannot be decided from the text: the identical
            // string means "chains resolved" when GetRefinedLipidAnnotationLevel wrote it.
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var spot = container.AlignmentSpotProperties.First(s => s.Name == "Quercetin");
            spot.Name = "PC 34:1|PC 16:0_18:1";
            spot.MatchResults.Representative.IsLipidChainsMatch = true;

            var fields = SmallMoleculeRow(ExportToLines(storage, container, msdecs), spot);

            Assert.AreEqual("PC 16:0_18:1", fields.Value("chemical_name"));
        }

        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task TheEvidenceColumnsCarryTheRecordAndItsAbsence() {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var recorded = container.AlignmentSpotProperties.First(s => s.Name == "Quercetin");
            recorded.MatchResults.Representative.EvidenceSource = AnnotationEvidenceSource.WeakSpectrumMatch;
            recorded.MatchResults.Representative.MeasuredTerms = MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass;
            var unrecorded = container.AlignmentSpotProperties.First(s => s.Name == "Isodemethylwedelolactone");

            var lines = ExportToLines(storage, container, msdecs);

            Assert.AreEqual("WeakSpectrumMatch", SmallMoleculeRow(lines, recorded).Value("opt_global_evidence_source"));
            Assert.AreEqual("Spectrum|AccurateMass", SmallMoleculeRow(lines, recorded).Value("opt_global_measured_terms"));
            // A project written before the evidence record existed says so, rather than claiming
            // that nothing was compared.
            Assert.AreEqual("null", SmallMoleculeRow(lines, unrecorded).Value("opt_global_evidence_source"));
        }

        /// <summary>
        /// The evidence section cites comparisons that happened, and only those.
        /// </summary>
        /// <remarks>
        /// The gate used to test the substring "no MS2" on the name, so a "low score" spot -- a
        /// spectrum acquired, compared and found wanting -- received a full rank-1 evidence row
        /// with nothing in it saying so. A weak match still earns its row, because a comparison
        /// that partly agreed is exactly what the evidence section is for; a comparison that
        /// explained nothing has nothing to cite.
        /// </remarks>
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task AWeakMatchKeepsItsEvidenceRowAndAnUnmatchedSpectrumDoesNot() {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var weak = container.AlignmentSpotProperties.First(s => s.Name == "Quercetin");
            weak.MatchResults.Representative.EvidenceSource = AnnotationEvidenceSource.WeakSpectrumMatch;
            var unmatched = container.AlignmentSpotProperties.First(s => s.Name == "Isodemethylwedelolactone");
            unmatched.MatchResults.Representative.EvidenceSource = AnnotationEvidenceSource.UnmatchedSpectrum;

            var lines = ExportToLines(storage, container, msdecs);
            var evidence = lines.Where(line => line.StartsWith("SME\t")).Select(line => line.Split('\t')).ToArray();

            Assert.IsTrue(evidence.Any(f => f[2] == weak.MasterAlignmentID.ToString()),
                "a spectrum was compared and partly agreed: that is citable evidence");
            Assert.IsFalse(evidence.Any(f => f[2] == unmatched.MasterAlignmentID.ToString()),
                "a comparison that explained nothing supports no identification");
        }

        /// <summary>
        /// Reads one SML row by column NAME, from the SMH line, so that adding a column does not
        /// silently shift what a test asserts.
        /// </summary>
        private static SmallMoleculeFields SmallMoleculeRow(string[] lines, AlignmentSpotProperty spot) {
            var header = lines.Single(line => line.StartsWith("SMH\t")).Split('\t');
            var row = lines.Select(line => line.Split('\t'))
                .Single(f => f[0] == "SML" && f[1] == spot.MasterAlignmentID.ToString());
            return new SmallMoleculeFields(header, row);
        }

        private sealed class SmallMoleculeFields
        {
            private readonly string[] _header;
            private readonly string[] _row;

            public SmallMoleculeFields(string[] header, string[] row) {
                _header = header;
                _row = row;
            }

            public string Value(string column) {
                var index = System.Array.IndexOf(_header, column);
                Assert.AreNotEqual(-1, index, $"no column named \"{column}\"");
                return _row[index];
            }
        }
    }

    class StubMetadataAccessor(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter, bool trimSpectrumToExcelLimit = false)
        : BaseMetadataAccessor(refer, parameter, trimSpectrumToExcelLimit) { }
}
