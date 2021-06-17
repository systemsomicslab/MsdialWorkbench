using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using System.IO;
using System.Linq;
using CompMs.MsdialCore.MSDec;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;

namespace CompMs.MsdialImmsCore.Export.Tests
{
    [TestClass()]
    public class ImmsAlignmentExporterTests
    {
        [TestMethod()]
        public void ExportTest() {
            // prepare();
            var datafile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialImmsCoreTests\Resources\input_data1";
            var expectedfile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialImmsCoreTests\Resources\output1.tsv";

            var data = MessagePackHandler.LoadFromFile<DataStorageForTest>(datafile);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(data.MsdecResultFile, out var _, out var _);
            var mapper = new DataBaseMapper();
            mapper.Add(new DataBaseRefer(data.MspDB, "MspDB"));
            mapper.Add(new DataBaseRefer(data.TextDB, "TextDB"));

            var exporter = new AlignmentCSVExporter();
            var stream = new MemoryStream();
            exporter.Export(
                stream,
                data.Spots,
                msdecResults,
                data.Files,
                new ImmsMetadataAccessor(mapper, data.Parameter),
                new LegacyQuantValueAccessor("Height", data.Parameter),
                new List<StatsValue>(0));

            var expected = File.ReadAllText(expectedfile);
            var actual = Encoding.UTF8.GetString(stream.ToArray());
            // Assert.AreEqual(expected, actual);
            // CollectionAssert.AreEqual(
            //     expected.Split(Environment.NewLine).Select(row => row.TrimEnd('\t')).ToArray(),
            //     actual.Split(Environment.NewLine).ToArray());
            foreach ((var ex, var ac) in expected.Split(Environment.NewLine).Select(row => row.TrimEnd('\t')).Zip(actual.Split(Environment.NewLine))) {
                Assert.AreEqual(ex, ac);
            }
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
            var project = @"D:\infusion_project\Bruker_20210521_original\Bruker_20210521\infusion\timsON_neg\2021_06_17_14_34_34.mtd2";
            var newmsdecfile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialImmsCoreTests\Resources\input_dec1";
            var newdatafile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialImmsCoreTests\Resources\input_data1";
            var expected = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialImmsCoreTests\Resources\output1.tsv";

            var storage = new Parser.MsdialImmsSerializer().LoadMsdialDataStorageBase(project);
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
                storage.ParameterBase, MachineCategory.IMMS);

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
}