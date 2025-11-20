using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export;

internal sealed class AlignmentMztabMExportModel : BindableBase, IAlignmentResultExportModel
{
    private readonly MztabFormatExporter _exporter;
    private readonly AnalysisFileBeanModelCollection _fileModelCollection;
    private readonly AlignmentPeakSpotSupplyer _peakSpotSupplyer;
    private readonly List<ExportType> _types;
    private readonly AccessPeakMetaModel _accessPeakMeta;

    public AlignmentMztabMExportModel(AnalysisFileBeanModelCollection fileModelCollection, AlignmentPeakSpotSupplyer peakSpotSupplyer, DataBaseStorage databaseStorage, IEnumerable<ExportType> types, AccessPeakMetaModel accessPeakMeta) {
        _exporter = new MztabFormatExporter(databaseStorage);
        _fileModelCollection = fileModelCollection;
        _peakSpotSupplyer = peakSpotSupplyer;
        _types = [.. types];
        _accessPeakMeta = accessPeakMeta;
        Types = _types.AsReadOnly();
    }

    public ReadOnlyCollection<ExportType> Types { get; }

    public bool IsSelected {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    private bool _isSelected = false;

    public int CountExportFiles(AlignmentFileBeanModel alignmentFile) {
        return IsSelected ? _types.Count(t => t.ShouldExport) : 0;
    }

    public void Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
        if (!IsSelected) {
            return;
        }
        var filenameTemplate = $"{{0}}_{alignmentFile.FileName}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.mzTab.txt";
        var spots = _peakSpotSupplyer.Supply(alignmentFile, default); // TODO: cancellation
        var msdecs = alignmentFile.LoadMSDecResults();
        var accessor = _accessPeakMeta.GetAccessor();

        foreach (var type in _types.Where(t => t.ShouldExport)) {
            var filename = string.Format(filenameTemplate, type.TargetLabel);
            var filepath = Path.Combine(exportDirectory, filename);

            notification?.Invoke($"Export {filename}");

            using var stream = File.Open(filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            _exporter.MztabFormatExporterCore(
                stream,
                spots,
                msdecs,
                [.. _fileModelCollection.AnalysisFiles.Select(f => f.File)],
                accessor,
                type.QuantValueAccessor,
                type.Stats,
                filename
            );
        }
    }
}
