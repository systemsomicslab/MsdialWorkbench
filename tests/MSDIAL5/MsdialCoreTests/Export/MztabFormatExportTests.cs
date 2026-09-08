using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
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
    }

    class StubMetadataAccessor(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter, bool trimSpectrumToExcelLimit = false)
        : BaseMetadataAccessor(refer, parameter, trimSpectrumToExcelLimit) { }
}
