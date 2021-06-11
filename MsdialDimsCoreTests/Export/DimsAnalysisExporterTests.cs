using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CompMs.MsdialCore.Algorithm.Annotation;

namespace CompMs.MsdialDimsCore.Export.Tests
{
    [TestClass()]
    public class DimsAnalysisExporterTests
    {
        [TestMethod()]
        public void ExportTest() {
            // prepare();
            var datafile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialDimsCoreTests\Resources\input_data2";
            var expectedfile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialDimsCoreTests\Resources\output2.tsv";

            var data = MessagePackHandler.LoadFromFile<DataStorageForTest>(datafile);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(data.MsdecResultFile, out var _, out var _);
            var mapper = new DataBaseMapper();
            mapper.Add(new DataBaseRefer(data.MspDB, "MspDB"));
            mapper.Add(new DataBaseRefer(data.TextDB, "TextDB"));
            var provider = new StandardDataProvider(data.Files[0], false, 5);

            var stream = new MemoryStream();
            var exporter = new AnalysisCSVExporter();
            var metaAccessor = new DimsAnalysisMetadataAccessor(mapper, data.Parameter);

            exporter.Export(stream, data.Features, msdecResults, provider, metaAccessor);

            var expected = File.ReadAllText(expectedfile);
            var actual = Encoding.UTF8.GetString(stream.ToArray());
            Assert.AreEqual(expected, actual);
        }

        private static void OriginalExportChromPeakFeature(
            Stream outstream,
            IReadOnlyList<ChromatogramPeakFeature> features, List<MSDecResult> msdecResults,
            IDataProvider provider,
            List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB,
            ParameterBase param, MachineCategory category) {
            var spectrums = provider.LoadMsSpectrums();
            using (var sw = new StreamWriter(outstream, Encoding.ASCII, 1024, true)) {
                // Header
                ResultExport.WriteChromPeakFeatureExportHeader(sw, category);

                // From the second
                foreach (var feature in features) {
                    var msdecID = feature.MasterPeakID;
                    var msdec = msdecResults[msdecID];
                    ResultExport.WriteChromPeakFeatureMetadata(sw, feature, msdec, spectrums.ToList(), param, mspDB, textDB);
                }
            }
        }

        private static void prepare() {
            var project = @"D:\msdial_test\Msdial\out\MSMSALL_Positive\2021_04_21_13_43_04.mtd2";
            var newmsdecfile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialDimsCoreTests\Resources\input_dec2";
            var newdatafile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialDimsCoreTests\Resources\input_data2";
            var expected = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialDimsCoreTests\Resources\output2.tsv";

            var storage = new Parser.MsdialDimsSerializer().LoadMsdialDataStorageBase(project);
            var analysisFile = storage.AnalysisFiles[0];

            var features = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(analysisFile.DeconvolutionFilePath, out _, out _);
            var rand = new Random();

            var idcs = Enumerable.Range(0, 20).Select(_ => rand.Next(features.Count)).ToList();
            var newFeatures = idcs.Select(idc => features[idc]).ToList();
            var newDecs = idcs.Select(idc => msdecResults[idc]).ToList();

            var counter = 0;
            foreach (var feature in newFeatures) {
                feature.MasterPeakID = counter++;
            }
            var msp = storage.MspDB;
            var text = storage.TextDB;
            using (var stream = File.Open(expected, FileMode.Create)) {
                OriginalExportChromPeakFeature(
                    stream,
                    newFeatures, newDecs,
                    new StandardDataProvider(analysisFile, isGuiProcess: false, retry: 5),
                    msp, text,
                    storage.ParameterBase, MachineCategory.IFMS);
            }

            var newMsp = new List<MoleculeMsReference>();
            var newText = new List<MoleculeMsReference>();
            foreach (var feature in newFeatures) {
                if (feature.MspBasedMatchResult != null && feature.MspBasedMatchResult.LibraryIDWhenOrdered >= 0 && feature.MspBasedMatchResult.LibraryIDWhenOrdered < msp.Count) {
                    newMsp.Add(msp[feature.MspBasedMatchResult.LibraryIDWhenOrdered]);
                    feature.MspBasedMatchResult.LibraryIDWhenOrdered = newMsp.Count - 1;
                }
                if (feature.TextDbBasedMatchResult != null && feature.TextDbBasedMatchResult.LibraryIDWhenOrdered >= 0 && feature.TextDbBasedMatchResult.LibraryIDWhenOrdered < text.Count) {
                    newText.Add(text[feature.TextDbBasedMatchResult.LibraryIDWhenOrdered]);
                    feature.TextDbBasedMatchResult.LibraryIDWhenOrdered = newText.Count - 1;
                }
            }

            MsdecResultsWriter.Write(newmsdecfile, newDecs);

            var data = new DataStorageForTest
            {
                Features = newFeatures,
                Parameter = storage.ParameterBase,
                MsdecResultFile = newmsdecfile,
                Files = storage.AnalysisFiles,
                MspDB = msp,
                TextDB = text,
            };

            MessagePackHandler.SaveToFile(data, newdatafile);
        }
    }
}
