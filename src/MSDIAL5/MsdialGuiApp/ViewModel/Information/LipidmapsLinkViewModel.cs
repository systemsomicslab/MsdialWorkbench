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
    }

    public ReadOnlyReactivePropertySlim<LipidmapsLinkItem[]> CurrentItems { get; }
}
