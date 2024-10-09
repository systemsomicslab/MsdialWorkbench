using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Export.Tests
{
    [TestClass()]
    public class AnalysisCSVExporterTests
    {
        [TestMethod()]
        public void ContainsDelimiterInFieldTest() {
            var exporterFactory = new AnalysisCSVExporterFactory(",");
            var spots = new ChromatogramPeakFeatureCollection(new List<ChromatogramPeakFeature>
            {
                new ChromatogramPeakFeature(new BaseChromatogramPeakFeature() {
                    PeakHeightTop = 400,
                })
                {
                    MasterPeakID = 0,
                    Name = "Metabolite 1,2",
                },
            });
            var file = new AnalysisFileBean { AnalysisFileName = "File 1", AnalysisFileClass = "A", AnalysisFileType = AnalysisFileType.Sample, AnalysisFileAnalyticalOrder = 1, AnalysisBatch = 1, };

            var memory = new MemoryStream();
            exporterFactory.CreateExporter(new FakeMetaAccessor()).Export(memory, file, spots.Items, new ExportStyle());

            var newline = Environment.NewLine;
            Assert.AreEqual(
                "Id,Name" + newline +
                "0,\"Metabolite 1,2\"" + newline,
                Encoding.ASCII.GetString(memory.ToArray()));
        }

        class FakeProvider : IDataProvider
        {
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
                throw new NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
                throw new NotImplementedException();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
                throw new NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
                throw new NotImplementedException();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
                throw new NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
                throw new NotImplementedException();
            }
        }

        class FakeMetaAccessor : IAnalysisMetadataAccessor, IAnalysisMetadataAccessor<ChromatogramPeakFeature>
        {
            public Dictionary<string, string> GetContent(ChromatogramPeakFeature feature, MSDecResult msdec, IDataProvider provider, AnalysisFileBean analysisFile, ExportStyle exportStyle) {
                return new Dictionary<string, string>
                {
                    ["Id"] = feature.MasterPeakID.ToString(),
                    ["Name"] = feature.Name,
                };
            }

            public Dictionary<string, string> GetContent(ChromatogramPeakFeature feature) {
                return new Dictionary<string, string>
                {
                    ["Id"] = feature.MasterPeakID.ToString(),
                    ["Name"] = feature.Name,
                };
            }

            public string[] GetHeaders() {
                return new[] { "Id", "Name", };
            }
        }
    }
}