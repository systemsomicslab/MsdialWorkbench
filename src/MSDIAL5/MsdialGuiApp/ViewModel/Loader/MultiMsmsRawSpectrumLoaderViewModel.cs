using CompMs.App.Msdial.Model.Loader;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Loader
{
    internal sealed class MultiMsmsRawSpectrumLoaderViewModel : ViewModelBase
    {
        public MultiMsmsRawSpectrumLoaderViewModel(MultiMsmsRawSpectrumLoader? loader) {
            if (loader is null) {
                Ms2IdList = Observable.Return(new List<MsSelectionItem>(0)).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
                SelectedMs2Id = new ReactivePropertySlim<MsSelectionItem?>().AddTo(Disposables);
            }
            else {
                Ms2IdList = loader.Ms2List.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
                SelectedMs2Id = loader.Ms2IdSelector;
            }
        }

        public ReadOnlyReactivePropertySlim<List<MsSelectionItem>> Ms2IdList { get; }
        public ReactivePropertySlim<MsSelectionItem?> SelectedMs2Id { get; }
    }
}
