using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export
{
    public abstract class BaseAlignmentExporter
    {
        public virtual void Export(
            Stream stream,
            IReadOnlyList<AlignmentSpotProperty> spots,
            IReadOnlyList<MSDecResult> msdecResults,
            IReadOnlyList<AnalysisFileBean> files,
            IMetadataAccessor metaAccessor,
            IQuantValueAccessor quantAccessor,
            IReadOnlyList<StatsValue> stats) {

            using (var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true)) {

                // Header
                var metaHeaders = metaAccessor.GetHeaders();
                var quantHeaders = quantAccessor.GetQuantHeaders(files);
                var classHeaders = quantAccessor.GetStatHeaders();
                WriteHeader(sw, files, metaHeaders, quantHeaders, classHeaders, stats);

                // Content
                foreach (var spot in spots) {
                    WriteContent(sw, spot, msdecResults[spot.MasterAlignmentID], metaHeaders, quantHeaders, classHeaders, metaAccessor, quantAccessor, stats);

                    foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>()) {
                        WriteContent(sw, driftSpot, msdecResults[driftSpot.MasterAlignmentID], metaHeaders, quantHeaders, classHeaders, metaAccessor, quantAccessor, stats);
                    }
                }
            }
        }

        protected abstract void WriteHeader(
            StreamWriter sw,
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<string> metaHeaders,
            IReadOnlyList<string> quantHeaders,
            IReadOnlyList<string> classHeaders,
            IReadOnlyList<StatsValue> stats);

        protected abstract void WriteContent(
            StreamWriter sw,
            AlignmentSpotProperty spot,
            MSDecResult result,
            IReadOnlyList<string> metaHeaders,
            IReadOnlyList<string> quantHeaders,
            IReadOnlyList<string> classHeaders,
            IMetadataAccessor metaAccessor,
            IQuantValueAccessor quantAccessor,
            IReadOnlyList<StatsValue> stats);
    }
}
