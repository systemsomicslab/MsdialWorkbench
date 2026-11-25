using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
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

        Tiles = model.VisibleItems
            .CombineLatest(model.MassRange, model.Reference, model.ShowReference, CreateTiles)
            .ToReadOnlyReactivePropertySlim<IReadOnlyList<Ms2SpectrumListTileViewModel>>([])
            .AddTo(Disposables);
        IsEmpty = model.IsLoading.CombineLatest(Tiles, (loading, tiles) => !loading && tiles.Count == 0)
            .ToReadOnlyReactivePropertySlim()
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
    public ReadOnlyReactivePropertySlim<IReadOnlyList<Ms2SpectrumListTileViewModel>> Tiles { get; }
    public ReadOnlyReactivePropertySlim<bool> IsEmpty { get; }
    public ReadOnlyReactivePropertySlim<string> EmptyMessage { get; }

    private IReadOnlyList<Ms2SpectrumListTileViewModel> CreateTiles(IReadOnlyList<Ms2SpectrumListItem> items, (double Minimum, double Maximum) massRange, MsSpectrum? reference, bool showReference) {
        // One horizontal axis for every tile: zooming one tile zooms them all, so peaks stay in line.
        var horizontalAxis = new ContinuousAxisManager<double>(massRange.Minimum, massRange.Maximum, new ConstantMargin(10d));
        var mirror = showReference && (reference?.HasSpectrum ?? false);
        var verticalAxis = mirror
            ? new ContinuousAxisManager<double>(-Ms2SpectrumList.RelativeIntensityMaximum, Ms2SpectrumList.RelativeIntensityMaximum, new RelativeMargin(.05d))
            : new ContinuousAxisManager<double>(0d, Ms2SpectrumList.RelativeIntensityMaximum, new RelativeMargin(0d, .1d), new AxisRange(0d, 0d));
        _axes.Disposable = new CompositeDisposable(horizontalAxis, verticalAxis);
        var referencePeaks = mirror ? Ms2SpectrumList.ToRelativeSpectrum(reference, mirrored: true) : [];
        return items.Select(item => new Ms2SpectrumListTileViewModel(item, horizontalAxis, verticalAxis, referencePeaks)).ToList();
    }
}

internal sealed class Ms2SpectrumListTileViewModel
{
    public Ms2SpectrumListTileViewModel(Ms2SpectrumListItem item, IAxisManager<double> horizontalAxis, IAxisManager<double> verticalAxis, List<SpectrumPeak> referencePeaks) {
        Title = item.Title;
        Detail = item.Detail;
        IsRepresentative = item.IsRepresentative;
        HasSpectrum = item.HasSpectrum;
        Peaks = Ms2SpectrumList.ToRelativeSpectrum(item.Spectrum);
        ReferencePeaks = referencePeaks;
        HorizontalAxis = horizontalAxis;
        VerticalAxis = verticalAxis;
    }

    public string Title { get; }
    public string Detail { get; }
    public bool IsRepresentative { get; }
    public bool HasSpectrum { get; }
    public List<SpectrumPeak> Peaks { get; }
    public List<SpectrumPeak> ReferencePeaks { get; }
    public IAxisManager<double> HorizontalAxis { get; }
    public IAxisManager<double> VerticalAxis { get; }
}
