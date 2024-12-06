using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Search;

internal sealed class MsfinderSearcherFactory : DisposableModelBase
{
    private readonly DataBaseMapper _dataBaseMapper;
    private readonly ParameterBase _parameter;
    private readonly DataBaseStorage _dataBases;
    private readonly MoleculeDataBase _molecules;
    private readonly string _tempDir;

    public MsfinderSearcherFactory(DataBaseStorage dataBases, DataBaseMapper dataBaseMapper, ParameterBase parameter, string dataBaseId)
    {
        _parameter = parameter;

        var db = dataBases.MetabolomicsDataBases.Select(db => db.DataBase).FirstOrDefault(db => db.Id == dataBaseId);
        _molecules = db ?? new MoleculeDataBase([], dataBaseId, DataBaseSource.MsFinder, SourceType.None);
        if (db is null)
        {
            dataBases.AddMoleculeDataBase(_molecules, []);
        }
        _dataBases = dataBases;

        _dataBaseMapper = dataBaseMapper;
        _dataBaseMapper.Add(_molecules);

        _tempDir = Path.Combine(parameter.ProjectFolderPath, "MSDIAL_TEMP");
    }

    public InternalMsFinderSingleSpot? CreateModelForAnalysisPeak(MsfinderParameterSetting parameter, ChromatogramPeakFeatureModel peak, MSDecResult msdec, IDataProvider provider, UndoManager undoManager)
    {
        if (!Directory.Exists(_tempDir)) {
            Directory.CreateDirectory(_tempDir);
        }
        var dt = DateTime.Now;
        var nameString = $"PeakID{peak.InnerModel.PeakID}_{dt:yyyy_MM_dd_hh_mm_ss}";
        var dir = Path.Combine(_tempDir, nameString);
        var filePath = Path.Combine(dir, $"{nameString}.{ExportSpectraFileFormat.mat}");
        if (!Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        using (var file = File.Open(filePath, FileMode.Create)) {
            SpectraExport.SaveSpectraTable(
                ExportSpectraFileFormat.mat,
                file,
                peak.InnerModel,
                msdec,
                provider.LoadMs1Spectrums(),
                _dataBaseMapper,
                _parameter);
        }

        return new InternalMsFinderSingleSpot(dir, filePath, _molecules, parameter, peak.AdductType, new SetAnnotationUsecase(peak, peak.MatchResultsModel, undoManager));
    }

    public InternalMsFinderSingleSpot? CreateModelForAlignmentSpot(MsfinderParameterSetting parameter, AlignmentSpotPropertyModel spot, MSDecResult msdec, UndoManager undoManager)
    {
        if (!Directory.Exists(_tempDir)) {
            Directory.CreateDirectory(_tempDir);
        }
        var dt = DateTime.Now;
        var nameString = $"AlignmentID{spot.innerModel.AlignmentID}_{dt:yyyy_MM_dd_hh_mm_ss}";
        var dir = Path.Combine(_tempDir, nameString);
        var filePath = Path.Combine(dir, $"{nameString}.{ExportSpectraFileFormat.mat}");
        if (!Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        using (var file = File.Open(filePath, FileMode.Create)) {
            SpectraExport.SaveSpectraTable(
                ExportSpectraFileFormat.mat,
                file,
                spot.innerModel,
                msdec,
                _dataBaseMapper,
                _parameter);
        }

        return new InternalMsFinderSingleSpot(dir, filePath, _molecules, parameter, spot.AdductType, new SetAnnotationUsecase(spot, spot.MatchResultsModel, undoManager));
    }

    public InternalMsFinderSingleSpot? CreateModelForGcmsAnalysisSpec(MsfinderParameterSetting parameter, SpectrumFeature spectrumFeature, Ms1BasedSpectrumFeature ms1BasedSpectrumFeature, UndoManager undoManager)
    {
        if (!Directory.Exists(_tempDir)) {
            Directory.CreateDirectory(_tempDir);
        }
        var dt = DateTime.Now;
        var nameString = $"SpectrumID{spectrumFeature.AnnotatedMSDecResult.MatchResults.Representative.SpectrumID}_{dt:yyyy_MM_dd_hh_mm_ss}";
        var dir = Path.Combine(_tempDir, nameString);
        var filePath = Path.Combine(dir, $"{nameString}.{ExportSpectraFileFormat.mat}");
        if (!Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        using (var file = File.Open(filePath, FileMode.Create)) {
            SpectraExport.SaveSpectraTableForGcmsAsMatFormat(
                file,
                spectrumFeature.AnnotatedMSDecResult.MSDecResult,
                spectrumFeature.AnnotatedMSDecResult.Molecule,
                spectrumFeature.QuantifiedChromatogramPeak.PeakFeature,
                _parameter.ProjectParam);
        }
        return new InternalMsFinderSingleSpot(dir, filePath, _molecules, parameter, new SetAnnotationUsecase(spectrumFeature.AnnotatedMSDecResult.Molecule, ms1BasedSpectrumFeature.MatchResults, undoManager));
    }

    protected override void Dispose(bool disposing) {
        if (!disposedValue) {
            var dir = new DirectoryInfo(_tempDir);
            if (dir.Exists) {
                RecursiveDelete(dir);
            }
        }
        base.Dispose(disposing);
    }

    private void RecursiveDelete(DirectoryInfo dir) {
        foreach (var d in dir.EnumerateDirectories()) {
            RecursiveDelete(d);
        }
        foreach (var f in dir.EnumerateFiles()) {
            f.Delete();
        }
        dir.Delete();
    }
}
