using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export;

internal sealed class AlignmentGnpsExportModel : BindableBase, IAlignmentResultExportModel {
    public AlignmentGnpsExportModel(string label, IEnumerable<ExportType> types, AccessPeakMetaModel accessPeakMeta, AccessFileMetaModel accessFileMeta, AnalysisFileBeanModelCollection files) {
        Label = label;
        _types = new ObservableCollection<ExportType>(types);
        Types = new ReadOnlyObservableCollection<ExportType>(_types);
        AccessPeakMetaModel = accessPeakMeta;
        AccessFileMeta = accessFileMeta;
        _files = files;
    }

    public string Label { get; }

    public AccessPeakMetaModel AccessPeakMetaModel { get; }
    public AccessFileMetaModel AccessFileMeta { get; }
    public ReadOnlyObservableCollection<ExportType> Types { get; }
    private readonly ObservableCollection<ExportType> _types;
    private readonly AnalysisFileBeanModelCollection _files;

    public bool IsSelected {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    private bool _isSelected = false;

    public int CountExportFiles(AlignmentFileBeanModel alignmentFile) {
        return IsSelected ? Types.Count(type => type.ShouldExport) + 5 : 0;
    }

    public void Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
        if (!IsSelected) {
            return;
        }
        var outNameTemplate = $"{{0}}_{((IFileBean)alignmentFile).FileID}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
        var msdecResults = alignmentFile.LoadMSDecResults();
        var container = alignmentFile.LoadAlignmentResultAsync(default).Result;
        var files = _files.AnalysisFiles.Select(f => f.File).ToArray();
        var exporter = new AlignmentGnpsExporter(exportDirectory, alignmentFile.FileName);
        foreach (var type in Types.Where(t => t.ShouldExport)) {
            exporter.Export(container.AlignmentSpotProperties, msdecResults, files, AccessFileMeta.GetAccessor(), AccessPeakMetaModel.GetAccessor(), type.QuantValueAccessor, type.Stats);
        }
    }
}
