using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Export.Tests
{
    [TestClass()]
    public class AlignmentCSVExporterTests
    {
        [TestMethod()]
        public void AlignmentCSVExporterTest() {
            var exporter = new AlignmentCSVExporter(",");
            var spots = new List<AlignmentSpotProperty>
            {
                new AlignmentSpotProperty
                {
                    MasterAlignmentID = 0,
                    Name = "Metabolite 1",
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { FileName = "File 1", PeakHeightTop = 400, },
                        new AlignmentChromPeakFeature { FileName = "File 2", PeakHeightTop = 200, },
                        new AlignmentChromPeakFeature { FileName = "File 3", PeakHeightTop = 500, },
                    },
                },
                new AlignmentSpotProperty
                {
                    MasterAlignmentID = 1,
                    Name = "Metabolite 2",
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { FileName = "File 1", PeakHeightTop = 200, },
                        new AlignmentChromPeakFeature { FileName = "File 2", PeakHeightTop = 800, },
                        new AlignmentChromPeakFeature { FileName = "File 3", PeakHeightTop = 500, },
                    },
                },
                new AlignmentSpotProperty
                {
                    MasterAlignmentID = 2,
                    Name = "Metabolite 3",
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { FileName = "File 1", PeakHeightTop = 100, },
                        new AlignmentChromPeakFeature { FileName = "File 2", PeakHeightTop = 400, },
                        new AlignmentChromPeakFeature { FileName = "File 3", PeakHeightTop = 500, },
                    },
                },
            };
            var msdecs = new List<MSDecResult> { null, null, null, };
            var files = new List<AnalysisFileBean>
            {
                new AnalysisFileBean { AnalysisFileName = "File 1", AnalysisFileClass = "A", AnalysisFileType = AnalysisFileType.Sample, AnalysisFileAnalyticalOrder = 1, AnalysisBatch = 1, },
                new AnalysisFileBean { AnalysisFileName = "File 2", AnalysisFileClass = "A", AnalysisFileType = AnalysisFileType.Sample, AnalysisFileAnalyticalOrder = 2, AnalysisBatch = 1,  },
                new AnalysisFileBean { AnalysisFileName = "File 3", AnalysisFileClass = "B", AnalysisFileType = AnalysisFileType.Sample, AnalysisFileAnalyticalOrder = 3, AnalysisBatch = 1,  },
            };

            var memory = new MemoryStream();
            exporter.Export(memory, spots, msdecs, files, new MulticlassFileMetaAccessor(0), new MockMetaAccessor(), new MockQuantAccessor(), new[] { StatsValue.Average, });

            Assert.AreEqual(
@",Class,A,A,B,NA,NA
,File type,Sample,Sample,Sample,NA,NA
,Injection order,1,2,3,NA,NA
,Batch ID,1,1,1,Average,Average
ID,Name,File 1,File 2,File 3,A,B
0,Metabolite 1,400,200,500,300,500
1,Metabolite 2,200,800,500,500,500
2,Metabolite 3,100,400,500,250,500
",
                System.Text.Encoding.ASCII.GetString(memory.GetBuffer()));
        }

        [TestMethod()]
        public void ContainsDelimiterInFieldTest() {
            var exporter = new AlignmentCSVExporter(",");
            var spots = new List<AlignmentSpotProperty>
            {
                new AlignmentSpotProperty
                {
                    MasterAlignmentID = 0,
                    Name = "Metabolite 1,2",
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { FileName = "File 1", PeakHeightTop = 400, },
                        new AlignmentChromPeakFeature { FileName = "File 2", PeakHeightTop = 200, },
                        new AlignmentChromPeakFeature { FileName = "File 3", PeakHeightTop = 500, },
                    },
                },
            };
            var msdecs = new List<MSDecResult> { null, null, null, };
            var files = new List<AnalysisFileBean>
            {
                new AnalysisFileBean { AnalysisFileName = "File 1", AnalysisFileClass = "A", AnalysisFileType = AnalysisFileType.Sample, AnalysisFileAnalyticalOrder = 1, AnalysisBatch = 1, },
                new AnalysisFileBean { AnalysisFileName = "File 2", AnalysisFileClass = "A", AnalysisFileType = AnalysisFileType.Sample, AnalysisFileAnalyticalOrder = 2, AnalysisBatch = 1,  },
                new AnalysisFileBean { AnalysisFileName = "File 3", AnalysisFileClass = "B", AnalysisFileType = AnalysisFileType.Sample, AnalysisFileAnalyticalOrder = 3, AnalysisBatch = 1,  },
            };

            var memory = new MemoryStream();
            exporter.Export(memory, spots, msdecs, files, new MulticlassFileMetaAccessor(0), new MockMetaAccessor(), new MockQuantAccessor(), new[] { StatsValue.Average, });

            var newline = Environment.NewLine;
            Assert.AreEqual(
                ",Class,A,A,B,NA,NA" + newline +
                ",File type,Sample,Sample,Sample,NA,NA" + newline +
                ",Injection order,1,2,3,NA,NA" + newline +
                ",Batch ID,1,1,1,Average,Average" + newline +
                "ID,Name,File 1,File 2,File 3,A,B" + newline +
                "0,\"Metabolite 1,2\",400,200,500,300,500" + newline,
                System.Text.Encoding.ASCII.GetString(memory.ToArray()));
        }

        class MockMetaAccessor : IMetadataAccessor
        {
            public IReadOnlyDictionary<string, string> GetContent(AlignmentSpotProperty spot, IMSScanProperty msdec) {
                return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    { "ID", spot.MasterAlignmentID.ToString() },
                    { "Name", spot.Name },
                });
            }

            public string[] GetHeaders() {
                return new[] { "ID", "Name", };
            }
        }

        class MockQuantAccessor : IQuantValueAccessor
        {
            public List<string> GetQuantHeaders(IReadOnlyList<AnalysisFileBean> files) {
                return files.Select(file => file.AnalysisFileName).ToList();
            }

            public Dictionary<string, string> GetQuantValues(AlignmentSpotProperty spot) {
                return spot.AlignedPeakProperties.ToDictionary(peak => peak.FileName, peak => peak.PeakHeightTop.ToString("F0"));
            }

            public List<string> GetStatHeaders() {
                return new List<string> { "A", "B" };
            }

            public Dictionary<string, string> GetStatsValues(AlignmentSpotProperty spot, StatsValue stat) {
                return new Dictionary<string, string>
                {
                    { "A", spot.AlignedPeakProperties.Take(2).Average(peak => peak.PeakHeightTop).ToString("F0") },
                    { "B", spot.AlignedPeakProperties.Skip(2).Average(peak => peak.PeakHeightTop).ToString("F0") },
                };
            }
        }
    }
}