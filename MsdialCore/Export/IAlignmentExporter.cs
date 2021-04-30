using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export
{
    public interface IAlignmentExporter
    {
        void Export(
            Stream stream,
            IReadOnlyList<AlignmentSpotProperty> spots,
            IReadOnlyList<MSDecResult> msdecResults,
            IReadOnlyList<AnalysisFileBean> files,
            IMetadataFormatter metaFormatter,
            IQuantValueAccessor quantAccessor);
    }

    public abstract class BaseAlignmentExporter : IAlignmentExporter
    {
        public virtual void Export(
            Stream stream,
            IReadOnlyList<AlignmentSpotProperty> spots,
            IReadOnlyList<MSDecResult> msdecResults,
            IReadOnlyList<AnalysisFileBean> files,
            IMetadataFormatter metaFormatter,
            IQuantValueAccessor quantAccessor) {

            using (var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true)) {

                // Header
                var headers = metaFormatter.GetHeaders();
                WriteHeader(sw, files, headers);

                // Content
                foreach (var spot in spots) {
                    WriteContent(sw, spot, msdecResults[spot.MasterAlignmentID], headers, metaFormatter, quantAccessor);

                    foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>()) {
                        WriteContent(sw, driftSpot, msdecResults[driftSpot.MasterAlignmentID], headers, metaFormatter, quantAccessor);
                    }
                }
            }
        }

        protected abstract void WriteHeader(
            StreamWriter sw,
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<string> headers);

        protected abstract void WriteContent(
            StreamWriter sw,
            AlignmentSpotProperty spot,
            MSDecResult result,
            IReadOnlyList<string> headers,
            IMetadataFormatter metaFormatter,
            IQuantValueAccessor quantAccessor);
    }
}
