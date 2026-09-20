using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
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
    /// <summary>
    /// Every library a run searched is declared in the mzTab-M metadata section.
    /// </summary>
    /// <remarks>
    /// The annotation rows cite a library by its database[n] prefix, so a library that the MTD
    /// section never declares leaves the file referring to something it does not define. That is
    /// exactly what happened when DataBaseSource.PredictedMsp was added: the switch in
    /// SetDatabaseList had a case for every kind that existed when it was written, and adding a kind
    /// did not fail anything -- the new library simply vanished from the metadata while its
    /// annotations kept citing it.
    ///
    /// Found by a Peak ID traceability audit on 2026-09-15, in code added two commits earlier.
    /// </remarks>
    [TestClass()]
    public class MztabDatabaseSectionTests
    {
        /// <summary>
        /// EVERY LIBRARY KIND THAT CAN CARRY AN MSP-LIKE LIBRARY IS DECLARED.
        /// </summary>
        /// <remarks>
        /// The list is written out rather than derived from the enum, because not every
        /// DataBaseSource member reaches a MoleculeDataBase -- the EAD lipid kinds arrive as
        /// EadLipidDatabase and None never names a library. Adding a kind the Console can build means
        /// adding it here, and the failure message says so.
        /// </remarks>
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task EveryMoleculeLibraryKindIsDeclaredInTheMetadataSection() {
            var kinds = new[] {
                DataBaseSource.Msp,
                DataBaseSource.PredictedMsp,
                DataBaseSource.Lbm,
                DataBaseSource.Text,
            };

            foreach (var kind in kinds) {
                var lines = await ExportWithSingleDatabaseAsync(kind);
                var declared = lines.Where(l => l.StartsWith("MTD\tdatabase[1]")).ToArray();

                Assert.IsTrue(declared.Any(),
                    $"{kind} produced no database[1] block; add a case to MztabFormatExport.SetDatabaseList");
                Assert.IsTrue(declared.Any(l => l.StartsWith("MTD\tdatabase[1]-prefix\tTestDB")),
                    $"{kind} did not publish the prefix its annotation rows cite");
            }
        }

        /// <summary>
        /// And a generated library says it is generated.
        /// </summary>
        /// <remarks>
        /// Declaring it as a user-defined MSP would satisfy the reference but throw away the only
        /// thing the kind exists to record -- that these spectra were computed, not acquired.
        /// </remarks>
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.arf2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06_PeakProperties.arf", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\AlignmentResult_2025_07_31_12_33_06.dcl", @"Resources\Export")]
        public async Task AnInSilicoLibraryIsDeclaredAsInSilico() {
            var acquired = await ExportWithSingleDatabaseAsync(DataBaseSource.Msp);
            var predicted = await ExportWithSingleDatabaseAsync(DataBaseSource.PredictedMsp);

            var acquiredLine = acquired.Single(l => l.StartsWith("MTD\tdatabase[1]\t"));
            var predictedLine = predicted.Single(l => l.StartsWith("MTD\tdatabase[1]\t"));

            Assert.AreNotEqual(acquiredLine, predictedLine,
                "the two kinds must be distinguishable in the published metadata");
            Assert.IsTrue(predictedLine.Contains("in-silico"), predictedLine);
        }

        private static async Task<string[]> ExportWithSingleDatabaseAsync(DataBaseSource kind) {
            var (storage, container, msdecs) = await LoadExportFixtureAsync();
            var database = new MoleculeDataBase(
                new List<MoleculeMsReference>(), "TestDB", kind, SourceType.MspDB, @"C:\libraries\test_library.msp");
            var databases = new DataBaseStorage(
                new List<DataBaseItem<MoleculeDataBase>> {
                    new DataBaseItem<MoleculeDataBase>(database, new List<IAnnotatorParameterPair<MoleculeDataBase>>()),
                },
                new List<DataBaseItem<ShotgunProteomicsDB>>(),
                new List<DataBaseItem<EadLipidDatabase>>());

            var exporter = new MztabFormatExporter(databases);
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
            return System.Text.Encoding.ASCII.GetString(stream.ToArray())
                .Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
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
    }
}
