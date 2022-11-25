using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.App.Msdial.Model.Export
{
    public sealed class ExportFormat : BindableBase
    {
        private readonly string _fileExtension;

        public ExportFormat(string label, string extension, IAlignmentExporter exporter) {
            Label = label;
            _fileExtension = extension;
            Exporter = exporter;
        }

        public string Label { get; }
        public IAlignmentExporter Exporter { get; }

        public string WithExtension(string filename) {
            return filename + "." + _fileExtension;
        }
    }

    public sealed class ExportMethod : BindableBase {
        private readonly IReadOnlyList<AnalysisFileBean> _analysisFiles;

        public ExportMethod(IReadOnlyList<AnalysisFileBean> analysisFiles, params ExportFormat[] formats) {
            _analysisFiles = analysisFiles ?? throw new ArgumentNullException(nameof(analysisFiles));
            Formats = formats;
        }

        public ExportFormat[] Formats { get; }

        public ExportFormat Format {
            get => _format;
            set => SetProperty(ref _format, value);
        }
        private ExportFormat _format;

        public void Export(string outName, ExportType exportType, string exportDirectory, AlignmentResultContainer resultContainer, IReadOnlyList<MSDecResult> msdecResults, Action<string> notification) {
            var outFile = Format.WithExtension(outName);
            var outPath = Path.Combine(exportDirectory, outFile);
            notification?.Invoke(outFile);

            using (var outstream = File.Open(outPath, FileMode.Create, FileAccess.Write)) {
                Format.Exporter.Export(
                    outstream,
                    resultContainer.AlignmentSpotProperties,
                    msdecResults,
                    _analysisFiles,
                    exportType.MetadataAccessor,
                    exportType.QuantValueAccessor,
                    exportType.Stats);
            }
        }
    }
}
