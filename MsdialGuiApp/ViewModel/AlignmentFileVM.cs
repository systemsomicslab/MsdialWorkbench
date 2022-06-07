using CompMs.App.Msdial.Model.Core;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel
{
    public abstract class AlignmentFileViewModel : ResultVM
    {
        public AlignmentFileViewModel(IAlignmentModel model) : base(model) {
            DisplayLabel = model.ToReactivePropertySlimAsSynchronized(m => m.DisplayLabel).AddTo(Disposables);
        }

        public ReactivePropertySlim<string> DisplayLabel { get; }
    }
}