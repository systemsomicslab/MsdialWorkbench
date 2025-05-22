using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Mathematics.Statistics;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    private readonly Subject<MsSpectrum?> _upper, _lower;
    private readonly ObservableMsSpectrum _upperSpectrum, _lowerSpectrum;

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

        Disposables.Add(_serialDisposable);

        _upper = new Subject<MsSpectrum?>().AddTo(Disposables);
        _upperSpectrum = new ObservableMsSpectrum(_upper, null, null).AddTo(Disposables);

        _lower = new Subject<MsSpectrum?>().AddTo(Disposables);
        _lowerSpectrum = new ObservableMsSpectrum(_lower, null, null).AddTo(Disposables);

        HorizontalAxis = new[] {
            _upperSpectrum.GetRange(s => s.Mass),
            _lowerSpectrum.GetRange(s => s.Mass),
        }.CombineLatest(ab => (Math.Min(ab[0].Item1, ab[1].Item1), Math.Max(ab[0].Item2, ab[1].Item2)))
        .ToReactiveContinuousAxisManager(new ConstantMargin(10))
        .AddTo(Disposables);
        UpperVerticalAxis = _upperSpectrum.GetRange(s => s.Intensity)
            .ToReactiveContinuousAxisManager(new ConstantMargin(0, 30))
            .AddTo(Disposables);
        LowerVerticalAxis = _lowerSpectrum.GetRange(s => s.Intensity)
            .ToReactiveContinuousAxisManager(new ConstantMargin(0, 30))
            .AddTo(Disposables);
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

    public SimilarityMatrixItem? SelectedMatrixItem {
        get => _selectedMatrixItem;
        set {
            if (SetProperty(ref _selectedMatrixItem, value)) {
                if (_selectedMatrixItem?.Right.Scan is { } rscan) {
                    _lower.OnNext(new MsSpectrum(rscan.Spectrum));
                }
                if (_selectedMatrixItem?.Left.Scan is { } lscan) {
                    _upper.OnNext(new MsSpectrum(lscan.Spectrum));
                }
            }
        }
    }
    private SimilarityMatrixItem? _selectedMatrixItem;

    public List<AnalysisFileBeanModel> OrderedFiles {
        get => _orderedFiles;
        set => SetProperty(ref _orderedFiles, value);
    }
    private List<AnalysisFileBeanModel> _orderedFiles = [];

    public IAxisManager<double> HorizontalAxis { get; }
    public IAxisManager<double> UpperVerticalAxis { get; }
    public IAxisManager<double> LowerVerticalAxis { get; }

    public async Task UpdateSimilaritiesAsync(CancellationToken token = default) {
        var (bin, begin, end, scans, spot) = (MzBin, MzBegin, MzEnd, _scans, _spot);
        if (scans is null || spot is null) {
            return;
        }
        var matrix = await Task.Run(() => MsScanMatching.GetBatchSimpleDotProduct(scans, bin, begin, end), token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();

        SelectedMatrixItem = null;

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

        var tree = StatisticsMathematics.ClusteringWard2Distance(matrix, StatisticsMathematics.CalculateSpearmanCorrelationDistance);
        OrderedFiles = [];
        tree.NodePreOrder(i => {
            if (tree[i].Count() == 0) {
                OrderedFiles.Add(Files.AnalysisFiles[i]);
            }
        });
    }

    public async Task UpdateSimilaritiesAsync(AlignmentSpotPropertyModel spot, IReadOnlyList<IMSScanProperty?> scans, CancellationToken token = default) {
        _scans = scans;
        _spot = spot;
        var (bin, begin, end) = (MzBin, MzBegin, MzEnd);
        var matrix = await Task.Run(() => MsScanMatching.GetBatchSimpleDotProduct(scans, bin, begin, end), token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();

        SelectedMatrixItem = null;

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

        var tree = StatisticsMathematics.ClusteringWard2Distance(matrix, StatisticsMathematics.CalculateSpearmanCorrelationDistance);
        OrderedFiles = [];
        tree.NodePreOrder(i => {
            if (tree[i].Count() == 0) {
                OrderedFiles.Add(Files.AnalysisFiles[i]);
            }
        });
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
