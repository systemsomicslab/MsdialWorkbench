using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace MsdialCoreTestAppTests.Process;

[TestClass]
public sealed class LcmsProcessTests
{
    [TestMethod]
    public void LightModeHeightMatrixContainsAValueForEveryAnalysisFile()
    {
        var scratch = Path.Combine(Path.GetTempPath(), $"msdial-light-matrix-{Guid.NewGuid():N}");
        Directory.CreateDirectory(scratch);
        try {
            var files = new[] {
                new AnalysisFileBean {
                    AnalysisFileId = 0,
                    AnalysisFileName = "sample-a",
                    AnalysisFileClass = "sample",
                    AnalysisFileAnalyticalOrder = 1,
                    AnalysisBatch = 1,
                },
                new AnalysisFileBean {
                    AnalysisFileId = 1,
                    AnalysisFileName = "sample-b",
                    AnalysisFileClass = "sample",
                    AnalysisFileAnalyticalOrder = 2,
                    AnalysisBatch = 1,
                },
            };
            using var store = AlignmentLightPeakStore.CreateTemp();
            store.Initialize(1, files);
            store.WriteSpotPeaks(0, [
                new AlignmentChromPeakFeature { FileID = 0, FileName = "sample-a", PeakHeightTop = 125d },
                new AlignmentChromPeakFeature { FileID = 1, FileName = "sample-b", PeakHeightTop = 250d },
            ]);
            var spot = new AlignmentSpotProperty { MasterAlignmentID = 0 };
            var output = Path.Combine(scratch, "height.txt");
            var parameter = new ParameterBase {
                FileID_ClassName = new Dictionary<int, string> {
                    [0] = "sample",
                    [1] = "sample",
                },
            };

            using (var stream = File.Create(output)) {
                LcmsProcess.ExportAlignmentMatrix(
                    stream,
                    [spot],
                    [new MSDecResult()],
                    files,
                    new MinimalMetadataAccessor(),
                    "Height",
                    parameter,
                    store,
                    [StatsValue.Average]);
            }

            var text = File.ReadAllText(output);
            StringAssert.Contains(text, "sample-a\tsample-b");
            StringAssert.Contains(text, "125\t250");
        }
        finally {
            Directory.Delete(scratch, recursive: true);
        }
    }

    private sealed class MinimalMetadataAccessor : IMetadataAccessor
    {
        public IReadOnlyDictionary<string, string> GetContent(AlignmentSpotProperty spot, IMSScanProperty msdec) {
            return new ReadOnlyDictionary<string, string>(new Dictionary<string, string> {
                ["ID"] = spot.MasterAlignmentID.ToString(),
            });
        }

        public string[] GetHeaders() {
            return ["ID"];
        }
    }
}
