using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class ExportMethod : BindableBase {
        private readonly IReadOnlyList<AnalysisFileBean> _analysisFiles;

        public ExportMethod(IReadOnlyList<AnalysisFileBean> analysisFiles, params ExportFormat[] formats) {
            _analysisFiles = analysisFiles ?? throw new ArgumentNullException(nameof(analysisFiles));
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

        public void Export(string outNameTemplate, string exportDirectory, Lazy<IReadOnlyList<AlignmentSpotProperty>> lazySpots, IReadOnlyList<MSDecResult> msdecResults, Action<string>? notification, IEnumerable<ExportType> exportTypes, AccessPeakMetaModel accessPeakMeta, AccessFileMetaModel accessFileMeta) {
            if (IsLongFormat) {
                ExportLong(outNameTemplate, exportDirectory, lazySpots, msdecResults, notification, exportTypes, accessPeakMeta, accessFileMeta);
            }
            else {
                ExportWide(outNameTemplate, exportDirectory, lazySpots, msdecResults, notification, exportTypes, accessPeakMeta, accessFileMeta);
            }
        }

        public void ExportLong(string outNameTemplate, string exportDirectory, Lazy<IReadOnlyList<AlignmentSpotProperty>> lazySpots, IReadOnlyList<MSDecResult> msdecResults, Action<string>? notification, IEnumerable<ExportType> exportTypes, AccessPeakMetaModel accessPeakMeta, AccessFileMetaModel accessFileMeta) {
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
                    accessPeakMeta.GetAccessor());
            }

            var outFileMetaName = string.Format(outNameTemplate, "FileMaster");
            var outFileMetaFile = Format.WithExtension(outFileMetaName);
            var outFileMetaPath = Path.Combine(exportDirectory, outFileMetaFile);
            notification?.Invoke(outFileMetaFile);
            using (var outstream = File.Open(outFileMetaPath, FileMode.Create, FileAccess.Write)) {
                exporter.ExportFileMeta(
                    outstream,
                    _analysisFiles,
                    accessFileMeta.GetAccessor());
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

        public void ExportWide(string outNameTemplate, string exportDirectory, Lazy<IReadOnlyList<AlignmentSpotProperty>> lazySpots, IReadOnlyList<MSDecResult> msdecResults, Action<string>? notification, IEnumerable<ExportType> exportTypes, AccessPeakMetaModel accessPeakMeta, AccessFileMetaModel accessFileMeta) {
            var exporter = Format.CreateWideExporter();
            var accessor = accessPeakMeta.GetAccessor();
            MsdialCore.Export.MulticlassFileMetaAccessor fileMetaAccessor = accessFileMeta.GetAccessor();
            foreach (var exportType in exportTypes) {
                var outName = string.Format(outNameTemplate, exportType.TargetLabel);
                var outFile = Format.WithExtension(outName);
                var outPath = Path.Combine(exportDirectory, outFile);
                notification?.Invoke(outFile);

                using var outstream = File.Open(outPath, FileMode.Create, FileAccess.Write);
                exporter.Export(
                    outstream,
                    lazySpots.Value,
                    msdecResults,
                    _analysisFiles,
                    fileMetaAccessor,
                    accessor,
                    exportType.QuantValueAccessor,
                    exportType.Stats);
            }
        }
    }
}
