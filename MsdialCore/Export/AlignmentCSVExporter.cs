using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Export
{
    public class AlignmentCSVExporter : BaseAlignmentExporter
    {
        public AlignmentCSVExporter() {
            Separator = "\t";
        }

        public AlignmentCSVExporter(string separator) {
            Separator = separator;
        }

        public string Separator { get; }

        protected override void WriteHeader(
            StreamWriter sw,
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<string> headers) {

            var marginString = MarginSpace(Separator, headers.Count - 1);
            sw.WriteLine(
                string.Join(Separator, new string[]
                {
                    marginString,
                    "Class",
                    string.Join(Separator, files.Select(file => file.AnalysisFileClass)),
                }));
                    
            sw.WriteLine(
                string.Join(Separator, new string[]
                {
                    marginString,
                    "File type",
                    string.Join(Separator, files.Select(n => n.AnalysisFileType)),
                }));
            sw.WriteLine(
                string.Join(Separator, new string[]
                {
                    marginString,
                    "Injection order",
                    string.Join(Separator, files.Select(n => n.AnalysisFileAnalyticalOrder)),
                }));
            sw.WriteLine(
                string.Join(Separator, new string[]
                {
                    marginString,
                    "Batch ID",
                    string.Join(Separator, files.Select(n => n.AnalysisBatch)),
                }));
            sw.WriteLine(
                string.Join(
                    Separator, headers.Concat(files.Select(n => n.AnalysisFileName))
                ) + Separator);
        }

        protected override void WriteContent(
            StreamWriter sw,
            AlignmentSpotProperty spot,
            MSDecResult msdec,
            IReadOnlyList<string> headers,
            IMetadataFormatter metaFormatter,
            IQuantValueAccessor quantAccessor) {
            var metadata = metaFormatter.GetContent(spot, msdec);
            var quantValues = quantAccessor.GetQuantValues(spot);
            sw.WriteLine(string.Join(Separator, headers.Select(header => metadata[header]).Concat(quantValues)) + Separator);
        }

        private static string MarginSpace(string separator, int numColumn) {
            return string.Concat(Enumerable.Repeat(separator, numColumn - 1));
        }
    }
}
