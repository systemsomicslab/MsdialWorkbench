using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Export
{
    public class AlignmentCSVExporter : BaseAlignmentExporter
    {
        private const string DEFAULT_SEPARATOR = "\t";

        private readonly MulticlassFileMetaAccessor _fileMetaAccessor;

        public AlignmentCSVExporter(string separator = DEFAULT_SEPARATOR) {
            Separator = separator;
            _fileMetaAccessor = new(0);
        }

        public string Separator { get; }

        protected override void WriteHeader(
            StreamWriter sw,
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<string> metaHeaders,
            IReadOnlyList<string> quantHeaders,
            IReadOnlyList<string> classHeaders,
            IReadOnlyList<StatsValue> stats) {

            var marginString = RepeatString("", metaHeaders.Count - 1, Separator);
            var naStrig = RepeatString("NA", classHeaders.Count * stats.Count, Separator);

            var header = _fileMetaAccessor.GetHeaders();
            var contents = _fileMetaAccessor.GetContents(files);

            for (int i = 0; i < header.Count; i++) {
                IEnumerable<string> statsFeilds = i == header.Count - 1
                    ? stats.SelectMany(stat => Enumerable.Repeat(stat.ToString(), classHeaders.Count))
                    : [naStrig];
                sw.WriteLine(
                    string.Join(Separator,
                    [
                        marginString,
                        header[i],
                        ..contents.Select(c => c[i]),
                        ..statsFeilds,
                    ]).TrimEnd());
            }

            sw.WriteLine(
                JoinContents(
                    metaHeaders,
                    quantHeaders,
                    stats.SelectMany(_ => classHeaders)
                ).TrimEnd());
        }

        protected override void WriteContent(
            StreamWriter sw,
            AlignmentSpotProperty spot,
            MSDecResult msdec,
            IReadOnlyList<string> metaHeaders,
            IReadOnlyList<string> quantHeaders,
            IReadOnlyList<string> classHeaders,
            IMetadataAccessor metaAccessor,
            IQuantValueAccessor quantAccessor,
            IReadOnlyList<StatsValue> stats) {
            var metadata = metaAccessor.GetContent(spot, msdec);
            var quantValues = quantAccessor.GetQuantValues(spot);
            var statValues = stats.Select(stat => quantAccessor.GetStatsValues(spot, stat))
                .SelectMany(dict => classHeaders.Where(dict.ContainsKey).Select(clss => dict[clss]));
            sw.WriteLine(
                JoinContents(
                    metaHeaders.Select(header => metadata[header]),
                    quantHeaders.Select(header => quantValues[header]),
                    statValues));
        }

        private static string RepeatString(string rep, int numColumn, string separator) {
            return string.Join(separator, Enumerable.Repeat(rep, numColumn));
        }

        private string JoinContents(params IEnumerable<string>[] contentss) {
            var contents = contentss.SelectMany(cs => cs.Select(WrapField));
            return string.Join(Separator, contents);
        }

        private string WrapField(string field) {
            if (field.Contains(Separator)) {
                return $"\"{field}\"";
            }
            return field;
        }
    }
}
