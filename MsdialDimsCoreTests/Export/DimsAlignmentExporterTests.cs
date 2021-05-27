using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.Common.Components;
using CompMs.MsdialCore.Parameter;
using CompMs.Common.Enum;
using System.Linq;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Parser;
using System.IO;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Export;

namespace CompMs.MsdialDimsCore.Export.Tests
{
    [TestClass()]
    public class DimsAlignmentExporterTests
    {
        [TestMethod()]
        public void ExportTest() {
            // prepare();
            var datafile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialDimsCoreTests\Resources\input_data1";
            var expectedfile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialDimsCoreTests\Resources\output1.tsv";

            var data = MessagePackHandler.LoadFromFile<DataStorageForTest>(datafile);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(data.MsdecResultFile, out var _, out var _);
            var mapper = new DataBaseMapper();
            mapper.Add("MspDB", new MockKey(data.MspDB));
            mapper.Add("TextDB", new MockKey(data.TextDB));
            mapper.Restore(null);

            var exporter = new AlignmentCSVExporter();
            var stream = new MemoryStream();
            exporter.Export(
                stream,
                data.Spots,
                msdecResults,
                data.Files,
                new DimsMetadataAccessor(mapper, data.Parameter),
                new LegacyQuantValueAccessor("Height", data.Parameter));

            var expected = File.ReadAllText(expectedfile);
            var actual = Encoding.UTF8.GetString(stream.ToArray());
            Assert.AreEqual(expected, actual);
        }

        private static void OriginalExportAlignmentResult(
            string outfile, string exportType,
            IReadOnlyList<AlignmentSpotProperty> spots, List<MSDecResult> msdecResults,
            List<AnalysisFileBean> files,
            List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB,
            ParameterBase param, MachineCategory category) {
            using (var sw = new StreamWriter(outfile, false, Encoding.ASCII)) {
                // Header
                MsdialCore.Export.ResultExport.WriteAlignmentResultHeader(sw, category, files);

                // From the second
                foreach (var spot in spots) {
                    var msdecID = spot.MasterAlignmentID;
                    var msdec = msdecResults[msdecID];
                    MsdialCore.Export.ResultExport.WriteAlignmentSpotFeature(sw, spot, msdec, param, mspDB, textDB, exportType);

                    foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>()) {
                        msdecID = driftSpot.MasterAlignmentID;
                        msdec = msdecResults[msdecID];
                        MsdialCore.Export.ResultExport.WriteAlignmentSpotFeature(sw, driftSpot, msdec, param, mspDB, textDB, exportType);
                    }
                }
            }
        }

        private static void prepare() {
            var project = @"D:\msdial_test\Msdial\out\MSMSALL_Positive\2021_04_21_13_43_04.mtd2";
            var newmsdecfile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialDimsCoreTests\Resources\input_dec1";
            var newdatafile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialDimsCoreTests\Resources\input_data1";
            var expected = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialDimsCoreTests\Resources\output1.tsv";

            var storage = new Parser.MsdialDimsSerializer().LoadMsdialDataStorageBase(project);
            var alignmentFile = storage.AlignmentFiles[0];
            var container = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(alignmentFile.FilePath);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _);
            var rand = new Random();

            var idcs = Enumerable.Range(0, 20).Select(_ => rand.Next(container.AlignmentSpotProperties.Count)).ToList();
            var newSpots = idcs.Select(idc => container.AlignmentSpotProperties[idc]).ToList();
            var newDecs = idcs.Select(idc => msdecResults[idc]).ToList();

            var counter = 0;
            foreach (var spot in newSpots) {
                spot.MasterAlignmentID = counter++;
            }
            var msp = storage.MspDB;
            var text = storage.TextDB;
            OriginalExportAlignmentResult(
                expected, "Height",
                newSpots, newDecs,
                storage.AnalysisFiles,
                msp, text,
                storage.ParameterBase, MachineCategory.IFMS);

            var newMsp = new List<MoleculeMsReference>();
            var newText = new List<MoleculeMsReference>();
            foreach (var spot in newSpots) {
                if (spot.MspBasedMatchResult != null && spot.MspBasedMatchResult.LibraryIDWhenOrdered >= 0 && spot.MspBasedMatchResult.LibraryIDWhenOrdered < msp.Count) {
                    newMsp.Add(msp[spot.MspBasedMatchResult.LibraryIDWhenOrdered]);
                    spot.MspBasedMatchResult.LibraryIDWhenOrdered = newMsp.Count - 1;
                }
                if (spot.TextDbBasedMatchResult != null && spot.TextDbBasedMatchResult.LibraryIDWhenOrdered >= 0 && spot.TextDbBasedMatchResult.LibraryIDWhenOrdered < text.Count) {
                    newText.Add(text[spot.TextDbBasedMatchResult.LibraryIDWhenOrdered]);
                    spot.TextDbBasedMatchResult.LibraryIDWhenOrdered = newText.Count - 1;
                }
            }

            MsdecResultsWriter.Write(newmsdecfile, newDecs);

            var data = new DataStorageForTest
            {
                Spots = newSpots,
                Parameter = storage.ParameterBase,
                MsdecResultFile = newmsdecfile,
                Files = storage.AnalysisFiles,
                MspDB = msp,
                TextDB = text,
            };

            MessagePackHandler.SaveToFile(data, newdatafile);
        }
    }

    [MessagePack.MessagePackObject]
    public class DataStorageForTest
    {
        [MessagePack.Key(0)]
        public List<AlignmentSpotProperty> Spots { get; set; }
        [MessagePack.Key(1)]
        public ParameterBase Parameter { get; set; }
        [MessagePack.Key(2)]
        public string MsdecResultFile { get; set; }
        [MessagePack.Key(3)]
        public List<AnalysisFileBean> Files { get; set; }
        [MessagePack.Key(4)]
        public List<MoleculeMsReference> MspDB { get; set; }
        [MessagePack.Key(5)]
        public List<MoleculeMsReference> TextDB { get; set; }
        [MessagePack.Key(6)]
        public List<ChromatogramPeakFeature> Features { get; set; }
    }

    class MockKey : IReferRestorationKey
    {
        public MockKey(List<MoleculeMsReference> db) {
            this.db = db;
        }

        private List<MoleculeMsReference> db;

        public IMatchResultRefer Accept(IRestorationVisitor visitor) {
            return new DataBaseRefer(db);
        }
    }
}