using CompMs.App.Msdial.Model.Information;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Information;

public sealed class LipidmapsLinkViewModel : ViewModelBase
{
    private readonly LipidmapsLinksModel _model;

    public LipidmapsLinkViewModel(LipidmapsLinksModel model) {
        _model = model;

        CurrentItems = model.CurrentItems.ToReadOnlyReactivePropertySlim([]).AddTo(Disposables);
        Retrieving = model.ObserveProperty(m => m.Retrieving).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        HasItems = model.ObserveProperty(m => m.HasItems).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
    }

    public ReadOnlyReactivePropertySlim<LipidmapsLinkItem[]> CurrentItems { get; }

    public ReadOnlyReactivePropertySlim<bool> Retrieving { get; }
    public ReadOnlyReactivePropertySlim<bool> HasItems { get; }
}
