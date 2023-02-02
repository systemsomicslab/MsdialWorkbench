using CompMs.App.SpectrumViewer.Model;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class MzVariableViewModel : ViewModelBase
    {
        public MzVariableViewModel(MzVariableModel model) {
            Model = model;

            VariableType = Model.ToReactivePropertySlimAsSynchronized(m => m.VariableType).AddTo(Disposables);
            PlaceholderType = Model.ToReactivePropertySlimAsSynchronized(m => m.PlaceholderType).AddTo(Disposables);
            IsConstant = VariableType.Select(t => t == "Constant").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsPlaceholder = VariableType.Select(t => t == "Placeholder").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            ShouldSelectChain = VariableType.CombineLatest(PlaceholderType,
                (v, p) => v == "EAD chain desorption" || v == "Placeholder" && p == "Specific chain")
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            ShouldSelectReference = VariableType.Select(v => v == "Loss")
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            ExactMass = Model.ToReactivePropertySlimAsSynchronized(m => m.ExactMass).AddTo(Disposables);
            ChainPosition = Model.ToReactivePropertySlimAsSynchronized(m => m.ChainPosition).AddTo(Disposables);
            Mzs = Model.Mzs.ToReadOnlyReactiveCollection().AddTo(Disposables);
            Left = Model.ToReactivePropertySlimAsSynchronized(m => m.Left).AddTo(Disposables);
            Right = Model.ToReactivePropertySlimAsSynchronized(m => m.Right).AddTo(Disposables);

            RemoveCommand = new ReactiveCommand().AddTo(Disposables);
        }

        public MzVariableModel Model { get; }

        public ReactivePropertySlim<string> VariableType { get; }
        public ReactivePropertySlim<string> PlaceholderType { get; }
        public ReactivePropertySlim<double> ExactMass { get; }
        public ReactivePropertySlim<int> ChainPosition { get; }
        public ReadOnlyReactiveCollection<MzVariableModel> Mzs { get; }
        public ReactivePropertySlim<MzVariableModel> Left { get; }
        public ReactivePropertySlim<MzVariableModel> Right { get; }
        public ReadOnlyReactivePropertySlim<bool> IsConstant { get; }
        public ReadOnlyReactivePropertySlim<bool> IsPlaceholder { get; }
        public ReadOnlyReactivePropertySlim<bool> ShouldSelectChain { get; }
        public ReadOnlyReactivePropertySlim<bool> ShouldSelectReference { get; }
        public ReactiveCommand RemoveCommand { get; }
    }
}
