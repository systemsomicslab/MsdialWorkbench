using CompMs.CommonMVVM;
using CompMs.MsdialCore.Export;

namespace CompMs.App.Msdial.Model.Export
{
    public sealed class ExportFormat : BindableBase
    {
        public ExportFormat(string label, string extension, IAlignmentExporter exporter) {
            Label = label;
            FileExtension = extension;
            Exporter = exporter;
        }

        public string Label { get; }
        public string FileExtension { get; }
        public IAlignmentExporter Exporter { get; }
    }
}
