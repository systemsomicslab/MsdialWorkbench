using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart;

internal class AlignmentPeakPlotViewModel : ViewModelBase
{
    private readonly AlignmentPeakPlotModel _model;
    private readonly IMessageBroker _broker;

    public AlignmentPeakPlotViewModel(AlignmentPeakPlotModel model, Action focus, IObservable<bool> isFocused, IMessageBroker broker) {
        _model = model ?? throw new ArgumentNullException(nameof(model));

        Focus = focus;
        _broker = broker;
        IsFocused = isFocused.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

        Spots = model.Spots;
        HorizontalAxis = model.HorizontalAxis;
        VerticalAxis = model.ObserveProperty(m => m.VerticalAxis).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

        SelectedBrush = model.ToReactivePropertyAsSynchronized(m => m.SelectedBrush).AddTo(Disposables);
        Brush = SelectedBrush.Select(data => data?.Mapper)
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

        Target = model.TargetSource;

        GraphTitle = model.ObserveProperty(m => m.GraphTitle)
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

        HorizontalTitle = model.ObserveProperty(m => m.HorizontalTitle)
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

        VerticalTitle = model.ObserveProperty(m => m.VerticalTitle)
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

        HorizontalProperty = model.ObserveProperty(m => m.HorizontalProperty)
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

        VerticalProperty = model.ObserveProperty(m => m.VerticalProperty)
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

        LabelProperty = model.LabelSource
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

        DuplicatesCommand = model.CanDuplicates.ToAsyncReactiveCommand()
            .WithSubscribe(model.DuplicatesAsync).AddTo(Disposables);
    }

    public Action Focus { get; }

    public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }

    public ReadOnlyObservableCollection<AlignmentSpotPropertyModel> Spots { get; }

    public PeakLinkModel PeakLinks => _model.PeakLinkModel;

    public IAxisManager<double> HorizontalAxis { get; }

    public ReadOnlyReactivePropertySlim<IAxisManager<double>?> VerticalAxis { get; }

    public ReadOnlyReactivePropertySlim<IBrushMapper<AlignmentSpotPropertyModel>?> Brush { get; }

    public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes => _model.Brushes;

    public ReactiveProperty<BrushMapData<AlignmentSpotPropertyModel>> SelectedBrush { get; }

    public IReactiveProperty<AlignmentSpotPropertyModel?> Target { get; }

    public ReadOnlyReactivePropertySlim<string?> GraphTitle { get; }

    public ReadOnlyReactivePropertySlim<string?> HorizontalTitle { get; }

    public ReadOnlyReactivePropertySlim<string?> VerticalTitle { get; }

    public ReadOnlyReactivePropertySlim<string?> HorizontalProperty { get; }

    public ReadOnlyReactivePropertySlim<string?> VerticalProperty { get; }

    public ReadOnlyReactivePropertySlim<string?> LabelProperty { get; }

    public AsyncReactiveCommand DuplicatesCommand { get; }

    public DelegateCommand SaveMrmprobsCommand => _saveMrmprobsCommand ??= new DelegateCommand(() => ExportMrmprobs(false), () => _model.ExportMrmprobs != null);
    private DelegateCommand? _saveMrmprobsCommand;

    public DelegateCommand CopyMrmprobsCommand => _copyMrmprobsCommand ??= new DelegateCommand(() => ExportMrmprobs(true), () => _model.ExportMrmprobs != null);
    private DelegateCommand? _copyMrmprobsCommand;

    private void ExportMrmprobs(bool copy) {
        var m = _model.ExportMrmprobsModel();
        if (m is null) {
            return;
        }
        m.Copy = copy;
        using var vm = new ExportMrmprobsViewModel(m);
        _broker.Publish(vm);
    }
}
