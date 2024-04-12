using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export
{
    public sealed class AlignmentLongCSVExporter
    {
        private readonly string _separator;

        public AlignmentLongCSVExporter(string separator = "\t") {
            _separator = separator;
        }

        public void ExportMeta(Stream stream, IReadOnlyList<AlignmentSpotProperty> spots, IReadOnlyList<MSDecResult> msdecResults, IMetadataAccessor metaAccessor) {
            using var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 4096, leaveOpen: true);
            // Header
            var metaHeaders = metaAccessor.GetHeaders();
            WriteMetaHeader(sw, metaHeaders);

            // Content
            foreach (var spot in spots) {
                WriteMetaContent(sw, spot, msdecResults[spot.MasterAlignmentID], metaHeaders, metaAccessor);
                foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>()) {
                    WriteMetaContent(sw, driftSpot, msdecResults[driftSpot.MasterAlignmentID], metaHeaders, metaAccessor);
                }
            }
        }

        public void ExportFileMeta(Stream stream, IReadOnlyList<AnalysisFileBean> files, MulticlassFileMetaAccessor accessor) {
            using var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 4096, leaveOpen: true);
            // Header
            var headers = accessor.GetHeaders();
            WriteMetaHeader(sw, headers);

            // Content
            var contents = accessor.GetContents(files);
            foreach (var content in contents) {
                sw.WriteLine(string.Join(_separator, content));
            }
        }

        public void ExportValue(Stream stream, IReadOnlyList<AlignmentSpotProperty> spots, IReadOnlyList<AnalysisFileBean> files, params (string label, IQuantValueAccessor accessor)[] quantAccessors) {
            using var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 4096, leaveOpen: true);
            // Header
            WriteValueHeader(sw, quantAccessors.Select(pair => pair.label));

            // Content
            var accessors = quantAccessors.Select(pair => pair.accessor).ToArray();
            foreach (var spot in spots) {
                WriteValueContent(sw, spot, files, accessors);
                foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>()) {
                    WriteValueContent(sw, driftSpot, files, accessors);
                }
            }
        }

        private void WriteMetaHeader(StreamWriter writer, IReadOnlyList<string> header) {
            writer.WriteLine(string.Join(_separator, header));
        }

        private void WriteMetaContent(StreamWriter writer, AlignmentSpotProperty spot, MSDecResult msdecResult, string[] header, IMetadataAccessor meataAccessor) {
            var contents = meataAccessor.GetContent(spot, msdecResult);
            var metaContents = header.Select(col => contents[col]);
            writer.WriteLine(string.Join(_separator, metaContents));
        }

        private void WriteValueHeader(StreamWriter writer, IEnumerable<string> header) {
            writer.WriteLine(string.Join(_separator, new[] { "ID", "File", "Class", }.Concat(header)));
        }

        private void WriteValueContent(StreamWriter writer, AlignmentSpotProperty spot, IReadOnlyList<AnalysisFileBean> files, IQuantValueAccessor[] quantAccessors) {
            var id = spot.MasterAlignmentID.ToString();
            var dicts = quantAccessors.Select(accessor => accessor.GetQuantValues(spot)).ToList();
            foreach (var file in files) {
                IEnumerable<string> row = new[] { id, file.AnalysisFileName, file.AnalysisFileClass }.Concat(dicts.Select(dict => dict[file.AnalysisFileName]));
                writer.WriteLine(string.Join(_separator, row));
            }
        }
    }
}
