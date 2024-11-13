using CompMs.App.Msdial.Model.DataObj;
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
        if (db is null) {
            dataBases.AddMoleculeDataBase(_molecules, []);
        }
        _dataBases = dataBases;

        _dataBaseMapper = dataBaseMapper;
        _dataBaseMapper.Add(_molecules);

        _tempDir = Path.Combine(parameter.ProjectFolderPath, "MSDIAL_TEMP");
    }

    public InternalMsFinderSingleSpot? CreateModel(ChromatogramPeakFeatureModel peak, MSDecResult msdec, IDataProvider provider) {
        if (!Directory.Exists(_tempDir)) {
            Directory.CreateDirectory(_tempDir);
        }
        var dt = DateTime.Now;
        var nameString = $"PeakID{peak.InnerModel.PeakID}_{dt:yyyy_MM_dd_hh_mm}";
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
        return new InternalMsFinderSingleSpot(dir, filePath, peak, _molecules);
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
