using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart;

/// <summary>A window that draws every MS2 spectrum of the selected peak or alignment spot as tiles.</summary>
internal sealed class Ms2SpectrumListViewModel : ViewModelBase, IDialogPropertiesViewModel
{
    private readonly Ms2SpectrumListModel _model;
    private readonly SerialDisposable _axes = new SerialDisposable();
    private string? _enlargedTitle;

    public Ms2SpectrumListViewModel(Ms2SpectrumListModel model) {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        Disposables.Add(model);
        Disposables.Add(_axes);

        Title = model.Title;
        Header = model.Header;
        Summary = model.Summary;
        IsLoading = model.IsLoading;
        ShowEmpty = model.ShowEmpty;
        ShowReference = model.ShowReference;
        HasReference = model.HasReference;
        ShowEmptyLabel = $"Show {model.Unit} without MS2";
        AxisType = model.AxisType;

        Tiles = model.VisibleItems
            .CombineLatest(model.MassRange, model.Reference, model.ShowReference, model.AxisType, CreateTiles)
            .ToReadOnlyReactivePropertySlim<IReadOnlyList<Ms2SpectrumListTileViewModel>>([])
            .AddTo(Disposables);
        IsEmpty = model.IsLoading.CombineLatest(Tiles, (loading, tiles) => !loading && tiles.Count == 0)
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        EnlargedTile = new ReactivePropertySlim<Ms2SpectrumListTileViewModel?>().AddTo(Disposables);
        // A reloaded list keeps the enlarged spectrum when the same sample or scan is still there.
        // The list is briefly empty while the next spot loads, so the title is forgotten only when a list with spectra lacks it.
        Tiles.Subscribe(tiles => {
            if (_enlargedTitle is null) {
                return;
            }
            var match = tiles.FirstOrDefault(tile => tile.Title == _enlargedTitle);
            EnlargedTile.Value = match;
            if (match is null && tiles.Count > 0) {
                _enlargedTitle = null;
            }
        }).AddTo(Disposables);
        IsEnlarged = EnlargedTile.Select(tile => tile is not null).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        EnlargedPosition = EnlargedTile.Select(tile => tile is null ? string.Empty : $"{IndexOf(tile) + 1} / {Tiles.Value.Count}")
            .ToReadOnlyReactivePropertySlim(string.Empty)
            .AddTo(Disposables);
        EnlargeCommand = new ReactiveCommand<Ms2SpectrumListTileViewModel?>()
            .WithSubscribe(Enlarge)
            .AddTo(Disposables);
        CloseEnlargedCommand = IsEnlarged.ToReactiveCommand()
            .WithSubscribe(() => Enlarge(null))
            .AddTo(Disposables);
        ShowNextCommand = IsEnlarged.ToReactiveCommand()
            .WithSubscribe(() => Step(1))
            .AddTo(Disposables);
        ShowPreviousCommand = IsEnlarged.ToReactiveCommand()
            .WithSubscribe(() => Step(-1))
            .AddTo(Disposables);

        EmptyMessage = model.Error.CombineLatest(model.Items, (error, items) => error ?? (items.Count == 0 ? "No MS2 spectrum to show." : $"None of the {model.Unit} has an MS2 spectrum. Check \"{ShowEmptyLabel}\" to list them all."))
            .ToReadOnlyReactivePropertySlim(string.Empty)
            .AddTo(Disposables);
    }

    public string Title { get; }
    string? IDialogPropertiesViewModel.Title => Title;
    double? IDialogPropertiesViewModel.Width => 1320d;
    double? IDialogPropertiesViewModel.Height => 800d;
    public string ShowEmptyLabel { get; }
    public ReadOnlyReactivePropertySlim<string> Header { get; }
    public ReadOnlyReactivePropertySlim<string> Summary { get; }
    public ReadOnlyReactivePropertySlim<bool> IsLoading { get; }
    public ReactivePropertySlim<bool> ShowEmpty { get; }
    public ReactivePropertySlim<bool> ShowReference { get; }
    public ReadOnlyReactivePropertySlim<bool> HasReference { get; }

    /// <summary>How the vertical axes are drawn; it follows the main MS/MS view until changed here.</summary>
    public ReactivePropertySlim<Ms2SpectrumAxisType> AxisType { get; }
    public IReadOnlyList<Ms2SpectrumAxisType> AxisTypes { get; } = (Ms2SpectrumAxisType[])Enum.GetValues(typeof(Ms2SpectrumAxisType));
    public ReadOnlyReactivePropertySlim<IReadOnlyList<Ms2SpectrumListTileViewModel>> Tiles { get; }
    public ReadOnlyReactivePropertySlim<bool> IsEmpty { get; }
    public ReadOnlyReactivePropertySlim<string> EmptyMessage { get; }

    /// <summary>The spectrum drawn large over the tiles; null while the tiles are shown.</summary>
    public ReactivePropertySlim<Ms2SpectrumListTileViewModel?> EnlargedTile { get; }
    public ReadOnlyReactivePropertySlim<bool> IsEnlarged { get; }
    /// <summary>Where the enlarged spectrum is among the tiles, such as "2 / 12".</summary>
    public ReadOnlyReactivePropertySlim<string> EnlargedPosition { get; }
    public ReactiveCommand<Ms2SpectrumListTileViewModel?> EnlargeCommand { get; }
    public ReactiveCommand CloseEnlargedCommand { get; }
    public ReactiveCommand ShowNextCommand { get; }
    public ReactiveCommand ShowPreviousCommand { get; }

    private void Enlarge(Ms2SpectrumListTileViewModel? tile) {
        _enlargedTitle = tile?.Title;
        EnlargedTile.Value = tile;
    }

    private int IndexOf(Ms2SpectrumListTileViewModel tile) {
        var tiles = Tiles.Value;
        for (int i = 0; i < tiles.Count; i++) {
            if (ReferenceEquals(tiles[i], tile)) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>Moves the enlarged spectrum to the next or previous tile, wrapping around at either end.</summary>
    private void Step(int delta) {
        var tiles = Tiles.Value;
        if (EnlargedTile.Value is not { } current || tiles.Count == 0) {
            return;
        }
        var index = IndexOf(current);
        var next = ((index < 0 ? 0 : index + delta) % tiles.Count + tiles.Count) % tiles.Count;
        Enlarge(tiles[next]);
    }

    private IReadOnlyList<Ms2SpectrumListTileViewModel> CreateTiles(IReadOnlyList<Ms2SpectrumListItem> items, (double Minimum, double Maximum) massRange, MsSpectrum? reference, bool showReference, Ms2SpectrumAxisType axisType) {
        // One horizontal axis for every tile: zooming one tile zooms them all, so peaks stay in line.
        var horizontalAxis = new ContinuousAxisManager<double>(massRange.Minimum, massRange.Maximum, new ConstantMargin(10d));
        var disposables = new CompositeDisposable { horizontalAxis };
        var mirror = showReference && (reference?.HasSpectrum ?? false);
        // Like the main MS/MS view, each spectrum gets its own vertical axis over its own intensities.
        // With the reference shown, it gets a lower pane that meets the spectrum at the m/z axis.
        var referenceAxis = CreateVerticalAxis(axisType, mirror ? reference : null);
        disposables.Add((IDisposable)referenceAxis);
        var referencePeaks = mirror ? reference!.Spectrum : [];
        var title = Ms2SpectrumList.GetAxisTitle(axisType);
        var tiles = items.Select(item => {
            var verticalAxis = CreateVerticalAxis(axisType, item.Spectrum);
            disposables.Add((IDisposable)verticalAxis);
            return new Ms2SpectrumListTileViewModel(item, horizontalAxis, verticalAxis, referenceAxis, mirror, referencePeaks, axisType, title);
        }).ToList();
        _axes.Disposable = disposables;
        return tiles;
    }

    /// <summary>The same kinds of vertical axis as the main MS/MS view offers, over the intensities of one spectrum.</summary>
    internal static IAxisManager<double> CreateVerticalAxis(Ms2SpectrumAxisType type, MsSpectrum? spectrum) {
        var (min, max) = Ms2SpectrumList.GetIntensityRange(spectrum);
        var margin = new RelativeMargin(0d, .1d);
        switch (type) {
            case Ms2SpectrumAxisType.Absolute:
                return new ContinuousAxisManager<double>(0d, max, margin, new AxisRange(0d, 0d)) { LabelType = LabelType.Standard };
            case Ms2SpectrumAxisType.Log10:
                // Drawn from intensity 1 up, as in the main view, so that the weakest peaks still show.
                return new LogScaleAxisManager<double>(Math.Max(min, 1d), Math.Max(max, 10d), margin, 1d, 1d) { LabelType = LabelType.Percent };
            case Ms2SpectrumAxisType.Sqrt:
                return new SqrtAxisManager(0d, max, margin, new AxisRange(0d, 0d)) { LabelType = LabelType.Percent };
            case Ms2SpectrumAxisType.Relative:
            default:
                return new ContinuousAxisManager<double>(0d, max, margin, new AxisRange(0d, 0d)) { LabelType = LabelType.Percent };
        }
    }
}

internal sealed class Ms2SpectrumListTileViewModel
{
    public Ms2SpectrumListTileViewModel(Ms2SpectrumListItem item, IAxisManager<double> horizontalAxis, IAxisManager<double> verticalAxis, IAxisManager<double> referenceVerticalAxis, bool isMirrored, List<SpectrumPeak> referencePeaks, Ms2SpectrumAxisType axisType, string verticalTitle) {
        Title = item.Title;
        Detail = item.Detail;
        IsRepresentative = item.IsRepresentative;
        HasSpectrum = item.HasSpectrum;
        Peaks = item.Spectrum.Spectrum;
        ReferencePeaks = referencePeaks;
        HorizontalAxis = horizontalAxis;
        VerticalAxis = verticalAxis;
        ReferenceVerticalAxis = referenceVerticalAxis;
        IsMirrored = isMirrored;
        AxisType = axisType;
        VerticalTitle = verticalTitle;
    }

    public string Title { get; }
    public string Detail { get; }
    public bool IsRepresentative { get; }
    public bool HasSpectrum { get; }
    public List<SpectrumPeak> Peaks { get; }
    /// <summary>The reference spectrum, drawn downward in the lower pane when <see cref="IsMirrored"/>.</summary>
    public List<SpectrumPeak> ReferencePeaks { get; }
    /// <summary>Whether the reference is drawn under the spectrum.</summary>
    public bool IsMirrored { get; }
    public IAxisManager<double> ReferenceVerticalAxis { get; }
    public IAxisManager<double> HorizontalAxis { get; }
    public IAxisManager<double> VerticalAxis { get; }
    public Ms2SpectrumAxisType AxisType { get; }
    public string VerticalTitle { get; }
}
