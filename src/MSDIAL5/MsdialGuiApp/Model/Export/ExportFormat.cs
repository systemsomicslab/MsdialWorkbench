using CompMs.CommonMVVM;
using CompMs.MsdialCore.Export;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class ExportFormat : BindableBase
    {
        private readonly string _label;
        private readonly string _extension;
        private readonly string _separator;

        private ExportFormat(string label, string extension, string separator) {
            _label = label;
            _extension = extension;
            _separator = separator;
        }

        public string Label => _label;
        public AlignmentCSVExporter CreateWideExporter() => new AlignmentCSVExporter(_separator);
        public AlignmentLongCSVExporter CreateLongExporter() =>  new AlignmentLongCSVExporter(_separator);

        public string WithExtension(string filename) {
            return filename + "." + _extension;
        }

        public static ExportFormat Tsv { get; } = new ExportFormat("txt", "txt", "\t");
        public static ExportFormat Csv { get; } = new ExportFormat("csv", "csv", ",");
    }
}
