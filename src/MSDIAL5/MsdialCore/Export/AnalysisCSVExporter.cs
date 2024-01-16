using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export
{
    public sealed class AnalysisCSVExporter : ILegacyAnalysisExporter
    {
        public AnalysisCSVExporter(string separator) {
            Separator = separator;
        }

        public string Separator { get; }

        public void Export(Stream stream, IReadOnlyList<ChromatogramPeakFeature> features, IReadOnlyList<MSDecResult> msdecResults, IDataProvider provider, IAnalysisMetadataAccessor metaAccessor, AnalysisFileBean analysisFile) {
            using (var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true)) {
                // Header
                var headers = metaAccessor.GetHeaders();
                WriteHeader(sw, headers);

                // Content
                foreach (var feature in features) {
                    WriteContent(sw, feature, msdecResults[feature.MasterPeakID], provider, headers, metaAccessor, analysisFile);
                }
            }
        }

        private void WriteHeader(StreamWriter sw, IReadOnlyList<string> headers) {
            sw.WriteLine(string.Join(Separator, headers));
        }

        private void WriteContent(StreamWriter sw, ChromatogramPeakFeature features, MSDecResult msdec, IDataProvider provider, IReadOnlyList<string> headers, IAnalysisMetadataAccessor metaAccessor, AnalysisFileBean analysisFile) {
            var metadata = metaAccessor.GetContent(features, msdec, provider, analysisFile);
            sw.WriteLine(string.Join(Separator, headers.Select(header => WrapField(metadata[header]))));
        }

        private string WrapField(string field) {
            if (field.Contains(Separator)) {
                return $"\"{field}\"";
            }
            return field;
        }
    }
}
