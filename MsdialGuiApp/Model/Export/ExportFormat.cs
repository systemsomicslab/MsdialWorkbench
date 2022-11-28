using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    public sealed class ExportFormat : BindableBase
    {
        private readonly string _fileExtension;

        public ExportFormat(string label, string extension, IAlignmentExporter exporter, AlignmentLongCSVExporter longExporter, IMetadataAccessor metaAccessor) {
            Label = label;
            _fileExtension = extension;
            Exporter = exporter;
            LongExporter = longExporter;
            MetaAccessor = metaAccessor ?? throw new ArgumentNullException(nameof(metaAccessor));
        }

        public string Label { get; }
        public IAlignmentExporter Exporter { get; }
        public AlignmentLongCSVExporter LongExporter { get; }
        public IMetadataAccessor MetaAccessor { get; }

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

        public bool IsLongFormat {
            get => _isLongFormat;
            set => SetProperty(ref _isLongFormat, value);
        }
        private bool _isLongFormat = false;

        public void Export(string outNameTemplate, string exportDirectory, AlignmentResultContainer resultContainer, IReadOnlyList<MSDecResult> msdecResults, Action<string> notification, IEnumerable<ExportType> exportTypes) {
            if (IsLongFormat) {
                ExportLong(outNameTemplate, exportDirectory, resultContainer, msdecResults, notification, exportTypes);
            }
            else {
                ExportWide(outNameTemplate, exportDirectory, resultContainer, msdecResults, notification, exportTypes);
            }
        }

        public void ExportLong(string outNameTemplate, string exportDirectory, AlignmentResultContainer resultContainer, IReadOnlyList<MSDecResult> msdecResults, Action<string> notification, IEnumerable<ExportType> exportTypes) {
            var outMetaName = string.Format(outNameTemplate, "PeakMaster");
            var outMetaFile = Format.WithExtension(outMetaName);
            var outMetaPath = Path.Combine(exportDirectory, outMetaFile);
            notification?.Invoke(outMetaFile);
            using (var outstream = File.Open(outMetaPath, FileMode.Create, FileAccess.Write)) {
                Format.LongExporter.ExportMeta(
                    outstream,
                    resultContainer.AlignmentSpotProperties,
                    msdecResults,
                    Format.MetaAccessor);
            }

            var outValueName = string.Format(outNameTemplate, "Peaks");
            var outValueFile = Format.WithExtension(outValueName);
            var outValuePath = Path.Combine(exportDirectory, outValueFile);
            notification?.Invoke(outValueFile);
            using (var outstream = File.Open(outValuePath, FileMode.Create, FileAccess.Write)) {
                Format.LongExporter.ExportValue(
                    outstream,
                    resultContainer.AlignmentSpotProperties,
                    _analysisFiles,
                    exportTypes.Select(type => (type.TargetLabel, type.QuantValueAccessor)).ToArray());
            }
        }

        public void ExportWide(string outNameTemplate, string exportDirectory, AlignmentResultContainer resultContainer, IReadOnlyList<MSDecResult> msdecResults, Action<string> notification, IEnumerable<ExportType> exportTypes) {
            foreach (var exportType in exportTypes) {
                var outName = string.Format(outNameTemplate, exportType.TargetLabel);
                var outFile = Format.WithExtension(outName);
                var outPath = Path.Combine(exportDirectory, outFile);
                notification?.Invoke(outFile);

                using (var outstream = File.Open(outPath, FileMode.Create, FileAccess.Write)) {
                    Format.Exporter.Export(
                        outstream,
                        resultContainer.AlignmentSpotProperties,
                        msdecResults,
                        _analysisFiles,
                        Format.MetaAccessor,
                        exportType.QuantValueAccessor,
                        exportType.Stats);
                }
            }
        }
    }
}
