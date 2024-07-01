using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace CompMs.App.Msdial.Model.Search;

internal sealed class MsScanCompoundSearchUsecase : BindableBase, ICompoundSearchUsecase<ICompoundResult, IMSScanProperty>
{
    private readonly ObservableCollection<MoleculeDataBase> _dataBases;
    private MoleculeDataBase? _selectedDatabase;

    public MsScanCompoundSearchUsecase()
    {
        _dataBases = [];
        SearchParameter = new MsRefSearchParameterBase();
    }

    public IList SearchMethods {
        get {
            return _dataBases;
        }
    }

    public object? SearchMethod {
        get => _searchMethod;
        set {
            if (value is MoleculeDataBase db && _dataBases.Contains(db)) {
                if (SetProperty(ref _searchMethod, value)) {
                    _selectedDatabase = db;
                }
            }
        }
    }
    private object? _searchMethod;

    public MsRefSearchParameterBase SearchParameter { get; }

    public IReadOnlyList<ICompoundResult> Search(IMSScanProperty target) {
        if (_selectedDatabase is null) {
            return [];
        }
        var results = new List<ICompoundResult>(_selectedDatabase.Database.Count);
        var scorer = new MsScanMatchResultScorer<IMSScanProperty, MoleculeMsReference>(new SpectrumMatchCalculatorWrapper(SearchParameter));
        foreach (var reference in _selectedDatabase.Database) {
            var result = scorer.Score(target, reference);
            results.Add(new CompoundResult(reference, result));
        }
        results.Sort((x, y) => y.MatchResult.TotalScore.CompareTo(x.MatchResult.TotalScore));
        return results;
    }

    public void AddDataBase(string path) {
        var references = LibraryHandler.ReadMspLibrary(path);
        var filename = Path.GetFileName(path);
        var db = new MoleculeDataBase(references, filename, DataBaseSource.Msp, SourceType.MspDB);
        _dataBases.Add(db);
    }

    public void AddDataBase(MoleculeDataBase db) {
        _dataBases.Add(db);
    }

    private class SpectrumMatchCalculatorWrapper(MsRefSearchParameterBase parameter) : IMatchScoreCalculator<IMSScanProperty, MoleculeMsReference, Ms2MatchResult>
    {
        private readonly Ms2MatchCalculator _calculator = new();

        public Ms2MatchResult Calculate(IMSScanProperty query, MoleculeMsReference reference) {
            var normScan = DataAccess.GetNormalizedMSScanProperty(query, parameter);
            var query_ = new MSScanMatchQuery(normScan, parameter);
            return _calculator.Calculate(query_, reference);
        }
    }
}
