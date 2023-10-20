using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.Export
{
    public interface IAlignmentSpectraExporter
    {
        void Export(Stream stream, AlignmentSpotProperty spot, MSDecResult msdecResult);
    }

    public static class AlignmentSpectraExporterExtension {
        public static void BatchExport(this IAlignmentSpectraExporter exporter, Stream stream, IReadOnlyList<AlignmentSpotProperty> spots, IReadOnlyList<MSDecResult> msdecResults) {
            foreach (var spot in spots) {
                exporter.Export(stream, spot, msdecResults[spot.MasterAlignmentID]);
            }
        }
    }
}
