using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Export
{
    public class AnalysisCSVExporter : BaseAnalysisExporter
    {
        public AnalysisCSVExporter() {
            Separator = "\t";
        }

        public AnalysisCSVExporter(string separator) {
            Separator = separator;
        }

        public string Separator { get; }

        protected override void WriteHeader(StreamWriter sw, IReadOnlyList<string> headers) {

            sw.WriteLine(string.Join(Separator, headers));
        }

        protected override void WriteContent(StreamWriter sw, ChromatogramPeakFeature features, MSDecResult msdec, IDataProvider provider, IReadOnlyList<string> headers, IAnalysisMetadataAccessor metaAccessor, AnalysisFileBean analysisFile) {
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
