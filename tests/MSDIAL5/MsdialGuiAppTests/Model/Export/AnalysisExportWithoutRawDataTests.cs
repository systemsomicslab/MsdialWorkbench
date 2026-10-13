using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.DataObj.Tests;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export.Tests
{
    [TestClass()]
    public class AnalysisExportWithoutRawDataTests
    {
        private string _folder = string.Empty;
        private string _output = string.Empty;
        private AnalysisFileBeanModel _file = AnalysisFileBeanModelTestHelper.Create();

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-export-without-raw-{Guid.NewGuid():N}");
            _output = Path.Combine(_folder, "export");
            Directory.CreateDirectory(_output);

            _file = AnalysisFileBeanModelTestHelper.Create();
            _file.AnalysisFileName = "sample";
            _file.AnalysisFilePath = Path.Combine(_folder, "sample.mzML"); // not written: the raw data are missing
            _file.File.PeakAreaBeanInformationFilePath = Path.Combine(_folder, "sample.pai");
            _file.File.DeconvolutionFilePath = Path.Combine(_folder, "sample.dcl");
            var feature = new ChromatogramPeakFeature
            {
                PeakID = 0,
                MasterPeakID = 0,
                MS1RawSpectrumIdTop = 12,
                MS2RawSpectrumID = 13,
                PrecursorMz = 300.1,
                ChromXs = new ChromXs(5.0, ChromXType.RT, ChromXUnit.Min),
                IonMode = IonMode.Positive,
            };
            feature.MS2RawSpectrumID2CE[13] = 20d;
            MsdialPeakSerializer.SaveChromatogramPeakFeatures(_file.PeakListFilePath, new List<ChromatogramPeakFeature> { feature, });
            var msdec = new MSDecResult { ScanID = 0, PrecursorMz = 300.1, RawSpectrumID = 13, };
            msdec.Spectrum.Add(new SpectrumPeak(100.05, 1000d));
            msdec.Spectrum.Add(new SpectrumPeak(150.07, 500d));
            MsdecResultsWriter.Write(_file.File.DeconvolutionFilePath, new List<MSDecResult> { msdec, });
        }

        [TestCleanup()]
        public void Cleanup() {
            // AnalysisMspExporter does not dispose the MSDecLoader it opens for the .dcl; its handle goes with the finalizer.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Directory.Delete(_folder, recursive: true);
        }

        [TestMethod()]
        public void ThePeakTableIsWrittenFromTheProjectWithItsIsotopeColumnAsNull() {
            var broker = new MessageBroker();
            var messages = new List<object>();
            broker.Subscribe<ShortMessageRequest>(messages.Add);
            var model = CreateExports(broker).OfType<MsdialAnalysisTableExportModel>().Single();

            model.Export(_output, _file);

            Assert.AreEqual(0, messages.Count);
            var lines = File.ReadAllLines(Path.Combine(_output, "sample.txt"));
            Assert.AreEqual(2, lines.Length);
            var headers = lines[0].Split('\t');
            var row = lines[1].Split('\t');
            Assert.AreEqual("null", row[Array.IndexOf(headers, "MS1 isotopes")]);
            StringAssert.Contains(row[Array.IndexOf(headers, "MSMS spectrum")], "100.05000:1000");
        }

        [TestMethod()]
        public void CentroidSpectraStopAtOnceAndLeaveNoFile() {
            var models = CreateExports(new MessageBroker()).OfType<SpectraTypeSelectableMsdialAnalysisExportModel>().ToArray();
            CollectionAssert.AreEquivalent(new[] { "msp", "mgf", "sdf", }, models.Select(model => model.FileSuffix).ToArray());

            foreach (var model in models) {
                model.SelectedType = ExportspectraType.centroid;
                model.ShouldExport = true;

                var error = Assert.ThrowsException<RawDataNotAvailableException>(() => ((IMsdialAnalysisExport)model).Export(_output, _file), model.FileSuffix);

                Assert.AreEqual(_file.AnalysisFilePath, error.FileName, model.FileSuffix);
            }
            Assert.AreEqual(0, Directory.GetFiles(_output).Length);
        }

        [TestMethod()]
        public void DeconvolutedSpectraAreWrittenFromTheProject() {
            foreach (var model in CreateExports(new MessageBroker()).OfType<SpectraTypeSelectableMsdialAnalysisExportModel>()) {
                model.SelectedType = ExportspectraType.deconvoluted;
                model.ShouldExport = true;

                ((IMsdialAnalysisExport)model).Export(_output, _file);
            }

            StringAssert.Contains(File.ReadAllText(Path.Combine(_output, "Msp_sample.msp")), "100.05");
            StringAssert.Contains(File.ReadAllText(Path.Combine(_output, "Mgf_sample.mgf")), "100.05");
            Assert.IsTrue(File.Exists(Path.Combine(_output, "Sdf_sample.sdf")));
        }

        // The exports Export > Peak list result offers for LC-MS, wired as the method model wires them, over raw data that are missing.
        private static IMsdialAnalysisExport[] CreateExports(IMessageBroker broker) {
            var parameter = new MsdialLcmsParameter();
            return LcmsMethodModel.CreateAnalysisExports(new FailingFactory(), new DataBaseMapper(), parameter, new StudyContextModel(parameter.ProjectParam), broker);
        }

        sealed class FailingFactory : IDataProviderFactory<AnalysisFileBean>
        {
            public IDataProvider Create(AnalysisFileBean source) => throw new AssertFailedException("The raw data were asked for.");
        }
    }
}
