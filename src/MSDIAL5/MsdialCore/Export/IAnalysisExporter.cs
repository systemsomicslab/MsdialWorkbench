using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialCore.Export
{
    public interface IAnalysisExporter
    {
        void Export(
            Stream stream,
            IReadOnlyList<ChromatogramPeakFeature> features,
            IReadOnlyList<MSDecResult> msdecResults,
            IDataProvider provider,
            IAnalysisMetadataAccessor metaAccessor,
            AnalysisFileBean analysisFile);
    }

    public abstract class BaseAnalysisExporter : IAnalysisExporter
    {
        public virtual void Export(Stream stream, IReadOnlyList<ChromatogramPeakFeature> features, IReadOnlyList<MSDecResult> msdecResults, IDataProvider provider, IAnalysisMetadataAccessor metaAccessor, AnalysisFileBean analysisFile) {
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

        protected abstract void WriteHeader(StreamWriter sw, IReadOnlyList<string> headers);
        protected abstract void WriteContent(StreamWriter sw, ChromatogramPeakFeature features, MSDecResult result, IDataProvider provider, IReadOnlyList<string> headers, IAnalysisMetadataAccessor metaAccessor, AnalysisFileBean analysisFile);
    }
}
