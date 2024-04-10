using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentSpectraExportFormat : BindableBase {
        private readonly string _extension;
        private readonly IAlignmentSpectraExporter _exporter;

        public AlignmentSpectraExportFormat(string label, string extension, IAlignmentSpectraExporter exporter) {
            if (string.IsNullOrEmpty(label)) {
                throw new ArgumentException($"'{nameof(label)}' cannot be null or empty.", nameof(label));
            }

            if (string.IsNullOrEmpty(extension)) {
                throw new ArgumentException($"'{nameof(extension)}' cannot be null or empty.", nameof(extension));
            }

            Label = label;
            _extension = extension;
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        }

        public string Label { get; }

        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        public void Export(IReadOnlyList<AlignmentSpotProperty> spots, IReadOnlyList<MSDecResult> msdecResults, string fileNameTemplate, string exportDirectory, Action<string>? notification) {
            var outName = string.Format(fileNameTemplate, Label, _extension);
            var outPath = Path.Combine(exportDirectory, outName);
            notification?.Invoke(outName);
            using var stream = File.Open(outPath, FileMode.Create, FileAccess.Write);
            _exporter.BatchExport(stream, spots, msdecResults);
        }
    }
}
