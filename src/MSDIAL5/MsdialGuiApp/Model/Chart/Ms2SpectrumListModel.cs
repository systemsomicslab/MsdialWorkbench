using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart;

/// <summary>
/// All MS2 spectra of the selected peak or alignment spot, reloaded whenever the selection changes.
/// </summary>
internal sealed class Ms2SpectrumListModel : DisposableModelBase
{
    private Ms2SpectrumListModel(string title, string unit, IObservable<LoadState> states, IObservable<MsSpectrum?> reference, IObservable<string?>? axisTypeLabels) {
        Title = title;
        Unit = unit;

        var initial = LoadState.Empty("No spot is selected.");
        var header = new ReactivePropertySlim<string>(initial.Header).AddTo(Disposables);
        var isLoading = new ReactivePropertySlim<bool>(false).AddTo(Disposables);
        var error = new ReactivePropertySlim<string?>(null).AddTo(Disposables);
        var summary = new ReactivePropertySlim<string>(Ms2SpectrumList.Summarize([], unit)).AddTo(Disposables);
        Header = header.ToReadOnlyReactivePropertySlim(initial.Header).AddTo(Disposables);
        IsLoading = isLoading.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        Error = error.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        Summary = summary.ToReadOnlyReactivePropertySlim(string.Empty).AddTo(Disposables);
        // Everything else is updated before the items, so whoever sees new items also sees whether they are still loading.
        Items = states
            .Do(s => {
                header.Value = s.Header;
                isLoading.Value = s.IsLoading;
                error.Value = s.Error;
                summary.Value = s.IsLoading ? "Loading ..." : s.Error ?? Ms2SpectrumList.Summarize(s.Items.ToList(), unit);
            })
            .Select(s => s.Items)
            .ToReadOnlyReactivePropertySlim<IReadOnlyList<Ms2SpectrumListItem>>(initial.Items)
            .AddTo(Disposables);

        ShowEmpty = new ReactivePropertySlim<bool>(false).AddTo(Disposables);
        ShowReference = new ReactivePropertySlim<bool>(false).AddTo(Disposables);
        // A completed property no longer tells later subscribers its value, so a finite source is kept open.
        Reference = reference.Concat(Observable.Never<MsSpectrum?>()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        HasReference = Reference.Select(r => r?.HasSpectrum ?? false).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

        VisibleItems = Items.CombineLatest(ShowEmpty, Ms2SpectrumList.Arrange)
            .ToReadOnlyReactivePropertySlim<IReadOnlyList<Ms2SpectrumListItem>>([]).AddTo(Disposables);
        // The axis type follows the main MS/MS view, and the window can still change it on its own.
        AxisType = new ReactivePropertySlim<Ms2SpectrumAxisType>(Ms2SpectrumAxisType.Relative).AddTo(Disposables);
        axisTypeLabels?.Subscribe(label => {
            if (Ms2SpectrumList.TryParseAxisType(label, out var type)) {
                AxisType.Value = type;
            }
        }).AddTo(Disposables);

        MassRange = VisibleItems.CombineLatest(Reference, ShowReference, (items, r, show) => Ms2SpectrumList.GetMassRange(items.Select(item => item.Spectrum).Append(show ? r : null)))
            .ToReadOnlyReactivePropertySlim((0d, 1d)).AddTo(Disposables);
    }

    public string Title { get; }
    public string Unit { get; }

    /// <summary>What the list is about, such as the selected spot's ID and m/z.</summary>
    public ReadOnlyReactivePropertySlim<string> Header { get; }
    public ReadOnlyReactivePropertySlim<string> Summary { get; }
    public ReadOnlyReactivePropertySlim<bool> IsLoading { get; }
    public ReadOnlyReactivePropertySlim<string?> Error { get; }

    /// <summary>Every spectrum of the selected spot, in the order the source gives them.</summary>
    public ReadOnlyReactivePropertySlim<IReadOnlyList<Ms2SpectrumListItem>> Items { get; }

    /// <summary>The spectra to draw: the representative first, and empty ones only when <see cref="ShowEmpty"/> is on.</summary>
    public ReadOnlyReactivePropertySlim<IReadOnlyList<Ms2SpectrumListItem>> VisibleItems { get; }

    public ReactivePropertySlim<bool> ShowEmpty { get; }
    public ReactivePropertySlim<bool> ShowReference { get; }
    public ReadOnlyReactivePropertySlim<MsSpectrum?> Reference { get; }
    public ReadOnlyReactivePropertySlim<bool> HasReference { get; }

    /// <summary>How the vertical axes are drawn.</summary>
    public ReactivePropertySlim<Ms2SpectrumAxisType> AxisType { get; }

    /// <summary>The m/z range shared by every tile, so the spectra line up.</summary>
    public ReadOnlyReactivePropertySlim<(double Minimum, double Maximum)> MassRange { get; }

    /// <param name="title">The window title.</param>
    /// <param name="unit">What one spectrum stands for, in the plural: "samples" or "MS2 scans".</param>
    /// <param name="target">The selected peak or spot.</param>
    /// <param name="describe">The header for a target.</param>
    /// <param name="load">Reads every MS2 spectrum of a target.</param>
    /// <param name="reference">The reference spectrum of the current annotation, drawn as a mirror on request.</param>
    /// <param name="axisTypeLabels">The vertical axis type chosen in the main MS/MS view, by its label; the list follows it.</param>
    public static Ms2SpectrumListModel Create<T>(
        string title,
        string unit,
        IObservable<T?> target,
        Func<T, string> describe,
        Func<T, Task<IReadOnlyList<Ms2SpectrumListItem>>> load,
        IObservable<MsSpectrum?> reference,
        IObservable<string?>? axisTypeLabels = null) where T : class {
        if (target is null) {
            throw new ArgumentNullException(nameof(target));
        }
        if (describe is null) {
            throw new ArgumentNullException(nameof(describe));
        }
        if (load is null) {
            throw new ArgumentNullException(nameof(load));
        }

        var states = target.Select(t => {
            if (t is null) {
                return Observable.Return(LoadState.Empty("No spot is selected."));
            }
            var header = describe(t);
            return Observable.FromAsync(async () => {
                try {
                    return LoadState.Loaded(header, await load(t).ConfigureAwait(false) ?? []);
                }
                catch (Exception ex) {
                    return LoadState.Failed(header, $"The MS2 spectra could not be read: {ex.Message}");
                }
            }).StartWith(LoadState.Loading(header));
        }).Switch();
        return new Ms2SpectrumListModel(title, unit, states, reference ?? Observable.Return<MsSpectrum?>(null), axisTypeLabels);
    }

    private sealed class LoadState
    {
        private LoadState(string header, IReadOnlyList<Ms2SpectrumListItem> items, bool isLoading, string? error) {
            Header = header;
            Items = items;
            IsLoading = isLoading;
            Error = error;
        }

        public string Header { get; }
        public IReadOnlyList<Ms2SpectrumListItem> Items { get; }
        public bool IsLoading { get; }
        public string? Error { get; }

        public static LoadState Empty(string header) => new LoadState(header, [], false, null);
        public static LoadState Loading(string header) => new LoadState(header, [], true, null);
        public static LoadState Loaded(string header, IReadOnlyList<Ms2SpectrumListItem> items) => new LoadState(header, items, false, null);
        public static LoadState Failed(string header, string error) => new LoadState(header, [], false, error);
    }
}
