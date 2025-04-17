using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Information;

public sealed class SimilarityMatrixItem(AnalysisFileBeanModel left, AnalysisFileBeanModel right, double similarity, IMSScanProperty? leftScan, IMSScanProperty? rightScan) {
    public AnalysisFileBeanModel Left { get; } = left;
    public AnalysisFileBeanModel Right { get; } = right;
    public double Similarity { get; } = similarity;
    public IMSScanProperty? LeftScan { get; } = leftScan;
    public IMSScanProperty? RightScan { get; } = rightScan;
}

internal sealed class SpectraSimilarityMapModel : BindableBase
{
    private readonly AnalysisFileBeanModelCollection _files;
    private readonly MsRefSearchParameterBase _parameter;
    private readonly Ionization _ionization;
    private IReadOnlyList<IMSScanProperty?>? _scans;

    public SpectraSimilarityMapModel(AnalysisFileBeanModelCollection files, ProjectBaseParameter parameter) {
        _files = files;
        _parameter = new MsRefSearchParameterBase();
        _ionization = parameter.Ionization;
        _mzBin = _ionization switch
        {
            Ionization.ESI => _parameter.Ms2Tolerance,
            Ionization.EI => _parameter.Ms1Tolerance,
            _ => .01,
        };
        _mzBegin = _parameter.MassRangeBegin;
        _mzEnd = _parameter.MassRangeEnd;
    }

    public AnalysisFileBeanModelCollection Files => _files;

    public double MzBin {
        get => _mzBin;
        set {
            if (SetProperty(ref _mzBin, value)) {
                switch (_ionization) {
                    case Ionization.ESI:
                        _parameter.Ms2Tolerance = (float)value;
                        break;
                    case Ionization.EI:
                        _parameter.Ms1Tolerance = (float)value;
                        break;
                }
            }
        }
    }
    private double _mzBin;

    public double MzBegin {
        get => _mzBegin;
        set {
            if (SetProperty(ref _mzBegin, value)) {
                _parameter.MassRangeBegin = (float)value;
            }
        }
    }
    private double _mzBegin;

    public double MzEnd {
        get => _mzEnd;
        set {
            if (SetProperty(ref _mzEnd, value)) {
                _parameter.MassRangeEnd = (float)value;
            }
        }
    }
    private double _mzEnd;

    public SimilarityMatrixItem[] Result {
        get => _result;
        private set => SetProperty(ref _result, value);
    }
    private SimilarityMatrixItem[] _result = [];

    public async Task UpdateSimilaritiesAsync(CancellationToken token = default) {
        var (bin, begin, end, scans) = (MzBin, MzBegin, MzEnd, _scans);
        if (scans is null) {
            return;
        }
        var matrix = await Task.Run(() => MsScanMatching.GetBatchSimpleDotProduct(scans, bin, begin, end), token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        var result = new SimilarityMatrixItem[scans.Count * scans.Count];
        for ( var i = 0; i < scans.Count; i++) {
            for (var j = 0; j < scans.Count; j++) {
                result[i * scans.Count + j] = new SimilarityMatrixItem(_files.AnalysisFiles[i], _files.AnalysisFiles[j], matrix[i][j], scans[i], scans[j]);
            }
        }
        Result = result;
    }

    public async Task UpdateSimilaritiesAsync(IReadOnlyList<IMSScanProperty?> scans, CancellationToken token = default) {
        _scans = scans;
        var (bin, begin, end) = (MzBin, MzBegin, MzEnd);
        var matrix = await Task.Run(() => MsScanMatching.GetBatchSimpleDotProduct(scans, bin, begin, end), token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        var result = new SimilarityMatrixItem[_scans.Count * _scans.Count];
        for ( var i = 0; i < _scans.Count; i++) {
            for (var j = 0; j < _scans.Count; j++) {
                result[i * _scans.Count + j] = new SimilarityMatrixItem(_files.AnalysisFiles[i], _files.AnalysisFiles[j], matrix[i][j], scans[i], scans[j]);
            }
        }
        Result = result;
    }

    public void ClearSimilarities() {
        _scans = null;
        Result = [];
    }
}
