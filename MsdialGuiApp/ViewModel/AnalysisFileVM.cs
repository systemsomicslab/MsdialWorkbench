using CompMs.App.Msdial.Model.Core;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel
{
    public abstract class TempAnalysisFileVM : ResultVM
    {
    }

    // In the future, TempAnalysisFileVM will be replaced by AnalysisFileViewModel.
    abstract class AnalysisFileViewModel : TempAnalysisFileVM
    {
        public AnalysisFileViewModel(AnalysisModelBase model) {
            DisplayLabel = model.ToReactivePropertySlimAsSynchronized(m => m.DisplayLabel).AddTo(Disposables);
        }

        public ReactivePropertySlim<string> DisplayLabel { get; }
    }
}