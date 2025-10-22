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

internal sealed class AlignmentGnpsExportModel : BindableBase, IAlignmentResultExportModel {
    private readonly IMetadataAccessor _gnpsMetaDataAccessor;
    private readonly IMetadataAccessor _gnps2MetaDataAccessor;
    private readonly IFileClassMetaAccessor _fileClassMetaAccessor;
    private readonly AnalysisFileBeanModelCollection _files;

    public AlignmentGnpsExportModel(string label, IEnumerable<ExportType> types, IMetadataAccessor gnpsMetaDataAccessor, IMetadataAccessor gnps2MetaDataAccessor, IFileClassMetaAccessor fileClassMetaAccessor, AnalysisFileBeanModelCollection files) {
        Label = label;
        _types = new ObservableCollection<ExportType>(types);
        Types = new ReadOnlyObservableCollection<ExportType>(_types);
        _gnpsMetaDataAccessor = gnpsMetaDataAccessor;
        _gnps2MetaDataAccessor = gnps2MetaDataAccessor;
        _fileClassMetaAccessor = fileClassMetaAccessor;
        _files = files;
    }

    public string Label { get; }

    public ReadOnlyObservableCollection<ExportType> Types { get; }
    private readonly ObservableCollection<ExportType> _types;

    public bool IsSelectedGnps {
        get => _isSelectedGnps;
        set => SetProperty(ref _isSelectedGnps, value);
    }
    private bool _isSelectedGnps = false;

    public bool IsSelectedGnps2 {
        get => _isSelectedGnps2;
        set => SetProperty(ref _isSelectedGnps2, value);
    }
    private bool _isSelectedGnps2 = false;

    public int CountExportFiles(AlignmentFileBeanModel alignmentFile) {
        var count = 0;
        if (IsSelectedGnps) {
           count += Types.Count(type => type.ShouldExport);
        }
        if (IsSelectedGnps2) {
           count += Types.Count(type => type.ShouldExport);
        }
        return count > 0 ? count + 5 : 0;
    }

    public void Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
        if (IsSelectedGnps) {
            ExportForGnps(alignmentFile, exportDirectory, notification);
        }

        if (IsSelectedGnps2) {
            ExportForGnps2(alignmentFile, exportDirectory, notification);
        }
    }

    private void ExportForGnps(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
        var dt = DateTime.Now;
        var outNameTemplate = $"{{0}}_{((IFileBean)alignmentFile).FileID}_{dt:yyyy_MM_dd_HH_mm_ss}";
        var msdecResults = alignmentFile.LoadMSDecResults();
        var container = alignmentFile.LoadAlignmentResultAsync(default).Result;
        var files = _files.AnalysisFiles.Select(f => f.File).ToArray();
        var exporter = new AlignmentGnpsExporter(exportDirectory, alignmentFile.FileName);
        foreach (var type in Types.Where(t => t.ShouldExport)) {
            exporter.Export(container.AlignmentSpotProperties, msdecResults, files, new GnpsFileClassMetaAccessor(), _gnpsMetaDataAccessor, type.QuantValueAccessor);
        }
        exporter.ExportMgf(container.AlignmentSpotProperties, msdecResults);

        var edgeFileName = $"{alignmentFile.FileName}_GNPSEdges_{dt:yyyyMMddHHmm}";
        var edges = AlignmentGnpsExporter.BuildGnpsEdges(container.AlignmentSpotProperties);
        var edge_peakshape = Path.Combine(exportDirectory, edgeFileName + "_peakshape.csv");
        using (var stream = File.Open(edge_peakshape, FileMode.Create, FileAccess.Write, FileShare.Read)) {
            edges.ExportPeakShapeEdges(stream);
        }
        var edge_ioncorrelation = Path.Combine(exportDirectory, edgeFileName + "_ioncorrelation.csv");
        using (var stream = File.Open(edge_ioncorrelation, FileMode.Create, FileAccess.Write, FileShare.Read)) {
            edges.ExportIonCorrelationEdges(stream);
        }
        var edge_insource = Path.Combine(exportDirectory, edgeFileName + "_insource.csv");
        using (var stream = File.Open(edge_insource, FileMode.Create, FileAccess.Write, FileShare.Read)) {
            edges.ExportInsourceEdges(stream);
        }
        var edge_adduct = Path.Combine(exportDirectory, edgeFileName + "_adduct.csv");
        using (var stream = File.Open(edge_adduct, FileMode.Create, FileAccess.Write, FileShare.Read)) {
            edges.ExportAdductEdges(stream);
        }
    }

    private void ExportForGnps2(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
        var dt = DateTime.Now;
        var outNameTemplate = $"{{0}}_{((IFileBean)alignmentFile).FileID}_{dt:yyyy_MM_dd_HH_mm_ss}";
        var msdecResults = alignmentFile.LoadMSDecResults();
        var container = alignmentFile.LoadAlignmentResultAsync(default).Result;
        var files = _files.AnalysisFiles.Select(f => f.File).ToArray();
        var exporter = new AlignmentGnpsExporter(exportDirectory, alignmentFile.FileName);
        foreach (var type in Types.Where(t => t.ShouldExport)) {
            exporter.Export(container.AlignmentSpotProperties, msdecResults, files, _fileClassMetaAccessor, _gnps2MetaDataAccessor, type.QuantValueAccessor);
        }
        exporter.ExportMgf(container.AlignmentSpotProperties, msdecResults);

        var edgeFileName = $"{alignmentFile.FileName}_GNPSEdges_{dt:yyyyMMddHHmm}";
        var edges = AlignmentGnpsExporter.BuildGnpsEdges(container.AlignmentSpotProperties);
        var edge_peakshape = Path.Combine(exportDirectory, edgeFileName + "_peakshape.csv");
        using (var stream = File.Open(edge_peakshape, FileMode.Create, FileAccess.Write, FileShare.Read)) {
            edges.ExportPeakShapeEdges(stream);
        }
        var edge_ioncorrelation = Path.Combine(exportDirectory, edgeFileName + "_ioncorrelation.csv");
        using (var stream = File.Open(edge_ioncorrelation, FileMode.Create, FileAccess.Write, FileShare.Read)) {
            edges.ExportIonCorrelationEdges(stream);
        }
        var edge_insource = Path.Combine(exportDirectory, edgeFileName + "_insource.csv");
        using (var stream = File.Open(edge_insource, FileMode.Create, FileAccess.Write, FileShare.Read)) {
            edges.ExportInsourceEdges(stream);
        }
        var edge_adduct = Path.Combine(exportDirectory, edgeFileName + "_adduct.csv");
        using (var stream = File.Open(edge_adduct, FileMode.Create, FileAccess.Write, FileShare.Read)) {
            edges.ExportAdductEdges(stream);
        }
    }
}
