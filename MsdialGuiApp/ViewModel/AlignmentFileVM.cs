using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel;

namespace CompMs.App.Msdial.ViewModel
{
    public abstract class AlignmentFileViewModel : ViewModelBase, IResultViewModel
    {
        public AlignmentFileViewModel(IAlignmentModel model) {
            Model = model;
            DisplayLabel = model.ToReactivePropertySlimAsSynchronized(m => m.DisplayLabel).AddTo(Disposables);
        }

        public object Model { get; }
        public abstract ICollectionView PeakSpotsView { get; }
        public ReactivePropertySlim<string> DisplayLabel { get; }
    }
}