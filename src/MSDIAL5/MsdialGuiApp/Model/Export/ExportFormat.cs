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
        public IAlignmentExporter CreateWideExporter() => new AlignmentCSVExporter(_separator);
        public AlignmentLongCSVExporter CreateLongExporter() =>  new AlignmentLongCSVExporter(_separator);

        public string WithExtension(string filename) {
            return filename + "." + _extension;
        }

        public static ExportFormat Tsv { get; } = new ExportFormat("txt", "txt", "\t");
        public static ExportFormat Csv { get; } = new ExportFormat("csv", "csv", ",");
    }

    internal sealed class ExportMethod : BindableBase {
        private readonly IReadOnlyList<AnalysisFileBean> _analysisFiles;
        private readonly IAlignmentMetadataAccessorFactory _accessorFactory;

        public ExportMethod(IReadOnlyList<AnalysisFileBean> analysisFiles, IAlignmentMetadataAccessorFactory accessorFactory, params ExportFormat[] formats) {
            _analysisFiles = analysisFiles ?? throw new ArgumentNullException(nameof(analysisFiles));
            _accessorFactory = accessorFactory ?? throw new ArgumentNullException(nameof(accessorFactory));
            Formats = formats;
            _format = formats.First();
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

        public bool TrimToExcelLimit {
            get => _trimToExcelLimit;
            set => SetProperty(ref _trimToExcelLimit, value);
        }
        private bool _trimToExcelLimit = true;

        public void Export(string outNameTemplate, string exportDirectory, Lazy<IReadOnlyList<AlignmentSpotProperty>> lazySpots, IReadOnlyList<MSDecResult> msdecResults, Action<string> notification, IEnumerable<ExportType> exportTypes) {
            if (IsLongFormat) {
                ExportLong(outNameTemplate, exportDirectory, lazySpots, msdecResults, notification, exportTypes);
            }
            else {
                ExportWide(outNameTemplate, exportDirectory, lazySpots, msdecResults, notification, exportTypes);
            }
        }

        public void ExportLong(string outNameTemplate, string exportDirectory, Lazy<IReadOnlyList<AlignmentSpotProperty>> lazySpots, IReadOnlyList<MSDecResult> msdecResults, Action<string> notification, IEnumerable<ExportType> exportTypes) {
            var outMetaName = string.Format(outNameTemplate, "PeakMaster");
            var outMetaFile = Format.WithExtension(outMetaName);
            var outMetaPath = Path.Combine(exportDirectory, outMetaFile);
            notification?.Invoke(outMetaFile);
            var exporter = Format.CreateLongExporter();
            using (var outstream = File.Open(outMetaPath, FileMode.Create, FileAccess.Write)) {
                exporter.ExportMeta(
                    outstream,
                    lazySpots.Value,
                    msdecResults,
                    _accessorFactory.CreateAccessor(TrimToExcelLimit));
            }

            var outValueName = string.Format(outNameTemplate, "PeakValues");
            var outValueFile = Format.WithExtension(outValueName);
            var outValuePath = Path.Combine(exportDirectory, outValueFile);
            notification?.Invoke(outValueFile);
            using (var outstream = File.Open(outValuePath, FileMode.Create, FileAccess.Write)) {
                exporter.ExportValue(
                    outstream,
                    lazySpots.Value,
                    _analysisFiles,
                    exportTypes.Select(type => (type.TargetLabel, type.QuantValueAccessor)).ToArray());
            }
        }

        public void ExportWide(string outNameTemplate, string exportDirectory, Lazy<IReadOnlyList<AlignmentSpotProperty>> lazySpots, IReadOnlyList<MSDecResult> msdecResults, Action<string> notification, IEnumerable<ExportType> exportTypes) {
            var exporter = Format.CreateWideExporter();
            var accessor = _accessorFactory.CreateAccessor(TrimToExcelLimit);
            foreach (var exportType in exportTypes) {
                var outName = string.Format(outNameTemplate, exportType.TargetLabel);
                var outFile = Format.WithExtension(outName);
                var outPath = Path.Combine(exportDirectory, outFile);
                notification?.Invoke(outFile);

                using (var outstream = File.Open(outPath, FileMode.Create, FileAccess.Write)) {
                    exporter.Export(
                        outstream,
                        lazySpots.Value,
                        msdecResults,
                        _analysisFiles,
                        accessor,
                        exportType.QuantValueAccessor,
                        exportType.Stats);
                }
            }
        }
    }
}
