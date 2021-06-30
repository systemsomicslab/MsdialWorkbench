using CompMs.App.Msdial.Model.Core;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel
{
    public abstract class TempAlignmentFileVM : ResultVM
    {
    }

    // In the future, TempAlignmentFileVM will be replaced by AlignmentFileViewModel.
    abstract class AlignmentFileViewModel : TempAnalysisFileVM
    {
        public AlignmentFileViewModel(AlignmentModelBase model) {
            DisplayLabel = model.ToReactivePropertySlimAsSynchronized(m => m.DisplayLabel).AddTo(Disposables);
        }

        public ReactivePropertySlim<string> DisplayLabel { get; }
    }
}