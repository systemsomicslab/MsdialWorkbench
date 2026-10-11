using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.DataObj.Tests;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Export;
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
            var parameter = new MsdialLcmsParameter();
            var spectraType = new SpectraType(
                ExportspectraType.deconvoluted,
                new ChromatogramShapeMetadataAccessorDecorator(new LcmsAnalysisMetadataAccessor(new DataBaseMapper(), parameter, ExportspectraType.deconvoluted)),
                new RawDataOptionalProviderFactory(new FailingFactory()));
            var broker = new MessageBroker();
            var messages = new List<object>();
            broker.Subscribe<ShortMessageRequest>(messages.Add);
            var model = new MsdialAnalysisTableExportModel(new[] { spectraType, }, new[] { new SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporterFactory(separator: "\t")), }, broker);

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
            var parameter = new MsdialLcmsParameter();
            var centroid = new RawDataRequiredProviderFactory(new FailingFactory());
            IMsdialAnalysisExport model = new SpectraTypeSelectableMsdialAnalysisExportModel(new Dictionary<ExportspectraType, IAnalysisExporter<ChromatogramPeakFeatureCollection>>
            {
                [ExportspectraType.deconvoluted] = new AnalysisMspExporter(new DataBaseMapper(), parameter),
                [ExportspectraType.centroid] = new AnalysisMspExporter(new DataBaseMapper(), parameter, file => new CentroidMsScanPropertyLoader(centroid.Create(file), parameter.MS2DataType)),
            })
            {
                FilePrefix = "Msp",
                FileSuffix = "msp",
                SelectedType = ExportspectraType.centroid,
                ShouldExport = true,
            };

            var error = Assert.ThrowsException<RawDataNotAvailableException>(() => model.Export(_output, _file));

            Assert.AreEqual(_file.AnalysisFilePath, error.FileName);
            Assert.AreEqual(0, Directory.GetFiles(_output).Length);
        }

        [TestMethod()]
        public void DeconvolutedSpectraAreWrittenFromTheProject() {
            var parameter = new MsdialLcmsParameter();
            IMsdialAnalysisExport model = new SpectraTypeSelectableMsdialAnalysisExportModel(new Dictionary<ExportspectraType, IAnalysisExporter<ChromatogramPeakFeatureCollection>>
            {
                [ExportspectraType.deconvoluted] = new AnalysisMspExporter(new DataBaseMapper(), parameter),
            })
            {
                FilePrefix = "Msp",
                FileSuffix = "msp",
                ShouldExport = true,
            };

            model.Export(_output, _file);

            StringAssert.Contains(File.ReadAllText(Path.Combine(_output, "Msp_sample.msp")), "100.05");
        }

        sealed class FailingFactory : IDataProviderFactory<AnalysisFileBean>
        {
            public IDataProvider Create(AnalysisFileBean source) => throw new AssertFailedException("The raw data were asked for.");
        }
    }
}
