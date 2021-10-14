using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CompMs.Common.DataObj.Result;
using System.Threading.Tasks;
using CompMs.MsdialImmsCore.DataObj;

namespace CompMs.MsdialImmsCore.Export.Tests
{
    [TestClass()]
    public class ImmsAnalysisExportTests
    {
        [TestMethod()]
        public void ExportTest() {
            // prepare();
            var datafile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialImmsCoreTests\Resources\input_data2.cache";
            var expectedfile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialImmsCoreTests\Resources\output2.tsv.cache";

            using (var datastream = File.Open(datafile, FileMode.Open)) {
                var data = MessagePackDefaultHandler.LoadFromStream<DataStorageForTest>(datastream);
                var msdecResults = MsdecResultsReader.ReadMSDecResults(data.MsdecResultFile, out var _, out var _);
                var mapper = new DataBaseMapper();
                var msp = new MoleculeDataBase(data.MspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB);
                var text = new MoleculeDataBase(data.TextDB, "TextDB", DataBaseSource.Text, SourceType.TextDB);
                mapper.Add(new MassAnnotator(msp, data.Parameter.MspSearchParam, TargetOmics.Lipidomics, SourceType.MspDB, "MspDB", -1), msp);
                mapper.Add(new MassAnnotator(text, data.Parameter.TextDbSearchParam, TargetOmics.Lipidomics, SourceType.TextDB, "TextDB", -1), text);
                var provider = new ImmsAverageDataProvider(data.Files[0], false, 5);

                var stream = new MemoryStream();
                var exporter = new AnalysisCSVExporter();
                var metaAccessor = new ImmsAnalysisMetadataAccessor(mapper, data.Parameter);

                exporter.Export(stream, data.Features, msdecResults, provider, metaAccessor);

                var expected = File.ReadAllText(expectedfile);
                var actual = Encoding.UTF8.GetString(stream.ToArray());
                Assert.AreEqual(expected, actual);
            }
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

        private static async Task prepare() {
            var project = @"D:\infusion_project\Bruker_20210521_original\Bruker_20210521\infusion\timsON_pos\2021_08_12_10_36_16.mtd2";
            var newmsdecfile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialImmsCoreTests\Resources\input_dec2.cache";
            var newdatafile = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialImmsCoreTests\Resources\input_data2.cache";
            var expected = @"C:\Users\YUKI MATSUZAWA\works\msdialworkbench\MsdialImmsCoreTests\Resources\output2.tsv.cache";

            var streamManager = new DirectoryTreeStreamManager(Path.GetDirectoryName(project));
            var storage = await MsdialImmsDataStorage.Serializer.LoadAsync(streamManager, Path.GetFileName(project), string.Empty);
            var analysisFile = storage.AnalysisFiles[0];

            var features = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(analysisFile.DeconvolutionFilePath, out _, out _);
            var rand = new Random();


            var newFeatures = features.Where(feature => feature.IsReferenceMatched(storage.DataBaseMapper)).OrderBy(_ => rand.Next()).Take(10).Concat(
                features.Where(feature => feature.IsAnnotationSuggested(storage.DataBaseMapper)).OrderBy(_ => rand.Next()).Take(5)).Concat(
                features.Where(feature => feature.IsUnknown).OrderBy(_ => rand.Next()).Take(5)).ToList();
            var newDecs = newFeatures.Select(feature => msdecResults[feature.MasterPeakID]).ToList();

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
                    new ImmsAverageDataProvider(analysisFile, isGuiProcess: false, retry: 5),
                    msp, text,
                    storage.Parameter, MachineCategory.IMMS);
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
                Parameter = storage.Parameter,
                MsdecResultFile = newmsdecfile,
                Files = storage.AnalysisFiles,
                MspDB = msp,
                TextDB = text,
            };

            using (var stream = File.Open(newdatafile, FileMode.Create)) {
                MessagePackDefaultHandler.SaveToStream(data, stream);
            }
        }
    }
}
