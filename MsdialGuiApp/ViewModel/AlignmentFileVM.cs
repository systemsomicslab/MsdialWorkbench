using CompMs.App.Msdial.Model.Core;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel
{
    abstract class AlignmentFileViewModel : ResultVM
    {
        public AlignmentFileViewModel(AlignmentModelBase model) {
            DisplayLabel = model.ToReactivePropertySlimAsSynchronized(m => m.DisplayLabel).AddTo(Disposables);
        }

        public ReactivePropertySlim<string> DisplayLabel { get; }
    }
}