using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Information;

public sealed class SamplePeakScan(AnalysisFileBeanModel file, IMSScanProperty? scan, ReadOnlyReactivePropertySlim<AlignmentChromPeakFeatureModel?> peak) : IDisposable
{
    public AnalysisFileBeanModel File { get; } = file;
    public IMSScanProperty? Scan { get; } = scan;
    public ReadOnlyReactivePropertySlim<AlignmentChromPeakFeatureModel?> Peak { get; } = peak;

    public void Dispose() {
        Peak.Dispose();
    }
}

public sealed class SimilarityMatrixItem(SamplePeakScan left, SamplePeakScan right, double similarity) {
    public SamplePeakScan Left { get; } = left;
    public SamplePeakScan Right { get; } = right;
    public double Similarity { get; } = similarity;
}

internal sealed class SpectraSimilarityMapModel : DisposableModelBase
{
    private readonly AnalysisFileBeanModelCollection _files;
    private readonly MsRefSearchParameterBase _parameter;
    private readonly Ionization _ionization;
    private IReadOnlyList<IMSScanProperty?>? _scans;
    private AlignmentSpotPropertyModel? _spot;
    private readonly SerialDisposable _serialDisposable = new();

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
        var (bin, begin, end, scans, spot) = (MzBin, MzBegin, MzEnd, _scans, _spot);
        if (scans is null || spot is null) {
            return;
        }
        var matrix = await Task.Run(() => MsScanMatching.GetBatchSimpleDotProduct(scans, bin, begin, end), token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        var samplePeakScans = new SamplePeakScan[scans.Count];
        var disposables = new CompositeDisposable();
        for (int i = 0; i < samplePeakScans.Length; i++) {
            samplePeakScans[i] = CreateSamplePeakScan(i, scans, spot).AddTo(disposables);
        }
        _serialDisposable.Disposable = disposables;

        var result = new SimilarityMatrixItem[scans.Count * scans.Count];
        for ( var i = 0; i < scans.Count; i++) {
            for (var j = 0; j < scans.Count; j++) {
                result[i * scans.Count + j] = new SimilarityMatrixItem(samplePeakScans[i], samplePeakScans[j], matrix[i][j]);
            }
        }
        Result = result;
    }

    public async Task UpdateSimilaritiesAsync(AlignmentSpotPropertyModel spot, IReadOnlyList<IMSScanProperty?> scans, CancellationToken token = default) {
        _scans = scans;
        _spot = spot;
        var (bin, begin, end) = (MzBin, MzBegin, MzEnd);
        var matrix = await Task.Run(() => MsScanMatching.GetBatchSimpleDotProduct(scans, bin, begin, end), token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        var samplePeakScans = new SamplePeakScan[scans.Count];
        var disposables = new CompositeDisposable();
        for (int i = 0; i < samplePeakScans.Length; i++) {
            samplePeakScans[i] = CreateSamplePeakScan(i, scans, spot).AddTo(Disposables);
        }
        _serialDisposable.Disposable = disposables;

        var result = new SimilarityMatrixItem[scans.Count * scans.Count];
        for ( var i = 0; i < scans.Count; i++) {
            for (var j = 0; j < scans.Count; j++) {
                result[i * scans.Count + j] = new SimilarityMatrixItem(samplePeakScans[i], samplePeakScans[j], matrix[i][j]);
            }
        }
        Result = result;
    }

    private SamplePeakScan CreateSamplePeakScan(int i, IReadOnlyList<IMSScanProperty?> scans, AlignmentSpotPropertyModel spot) {
        return new SamplePeakScan(_files.AnalysisFiles[i], scans[i], spot.AlignedPeakPropertiesModelProperty.DefaultIfNull(m => m[i]).ToReadOnlyReactivePropertySlim());
    }

    public void ClearSimilarities() {
        _scans = null;
        _spot = null;
        Result = [];
        _serialDisposable.Disposable = null;
    }
}
