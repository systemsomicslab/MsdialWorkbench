using CompMs.App.SpectrumViewer.Model;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class LipidSelectionViewModel : ViewModelBase
    {
        public LipidSelectionViewModel(LipidSelectionModel model) {
            Model = model;
            LipidClass = Model.ToReactivePropertySlimAsSynchronized(m => m.LipidClass).AddTo(Disposables);
            Mass = Model.ToReactivePropertySlimAsSynchronized(m => m.Mass).AddTo(Disposables);
            ChainsType = Model.ToReactivePropertySlimAsSynchronized(m => m.ChainsType).AddTo(Disposables);
            ChainCount = Model.ToReactivePropertySlimAsSynchronized(m => m.ChainCount).AddTo(Disposables);
            Chains = Model.Chains.ToReadOnlyReactiveCollection(cm => new ChainSelectionViewModel(cm)).AddTo(Disposables);
            ChainsStr = Model.ToReactivePropertySlimAsSynchronized(m => m.ChainsStr).AddTo(Disposables);
            IsSubMolecularLevel = ChainsType.Select(t => t == "SubMolecularLevel").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsNotSubMolecularLevel = IsSubMolecularLevel.Inverse().ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            AddChainCommand = new ReactiveCommand()
                .WithSubscribe(Model.AddChain)
                .AddTo(Disposables);
            RemoveChainCommand = new ReactiveCommand()
                .WithSubscribe(Model.RemoveChain)
                .AddTo(Disposables);
        }

        public LipidSelectionModel Model { get; }

        public ReactivePropertySlim<LbmClass> LipidClass { get; }

        public ReadOnlyCollection<LbmClass> LipidClasses => Model.LipidClasses;

        public ReactivePropertySlim<double> Mass { get; }

        public ReactivePropertySlim<string> ChainsType { get; }

        public ReactivePropertySlim<string> ChainsStr { get; }

        public ReadOnlyCollection<string> ChainsTypes => Model.ChainsTypes;

        public ReactivePropertySlim<int> ChainCount { get; }

        public ReadOnlyReactiveCollection<ChainSelectionViewModel> Chains { get; }

        public ReadOnlyReactivePropertySlim<bool> IsSubMolecularLevel { get; }

        public ReadOnlyReactivePropertySlim<bool> IsNotSubMolecularLevel { get; }

        public ReactiveCommand AddChainCommand { get; }

        public ReactiveCommand RemoveChainCommand { get; }
    }

    public class ChainSelectionViewModel : ViewModelBase
    {
        public ChainSelectionViewModel(ChainSelectionModel model) {
            Model = model;

            ChainType = Model.ToReactivePropertySlimAsSynchronized(m => m.ChainType).AddTo(Disposables);
            CarbonCount = Model.ToReactivePropertySlimAsSynchronized(m => m.CarbonCount).AddTo(Disposables);
            DoubleBondCount = Model.ToReactivePropertySlimAsSynchronized(m => m.DoubleBondCount).AddTo(Disposables);
            DoubleBonds = Model.DoubleBonds.ToReadOnlyReactiveCollection(db => new DoubleBondSetViewModel(db)).AddTo(Disposables);
            OxidizedCount = Model.ToReactivePropertySlimAsSynchronized(m => m.OxidizedCount).AddTo(Disposables);
            Oxidises = Model.Oxidises.ToReadOnlyReactiveCollection(ox => new OxidizedSetViewModel(ox)).AddTo(Disposables);

            AddDoubleBondCommand = new ReactiveCommand()
                .WithSubscribe(Model.AddDoubleBond)
                .AddTo(Disposables);
            AddOxidizedCommand = new ReactiveCommand()
                .WithSubscribe(Model.AddOxidized)
                .AddTo(Disposables);
            DoubleBonds.ObserveElementObservableProperty(vm => vm.RemoveCommand)
                .Select(p => p.Instance.Model)
                .Subscribe(Model.RemoveDoubleBond)
                .AddTo(Disposables);
            Oxidises.ObserveElementObservableProperty(vm => vm.RemoveCommand)
                .Select(p => p.Instance.Model)
                .Subscribe(Model.RemoveOxidized)
                .AddTo(Disposables);
        }

        public ChainSelectionModel Model { get; }

        public ReactivePropertySlim<string> ChainType { get; }

        public ReadOnlyCollection<string> ChainTypes => Model.ChainTypes;

        public ReactivePropertySlim<int> CarbonCount { get; }

        public ReactivePropertySlim<int> DoubleBondCount { get; }

        public ReadOnlyReactiveCollection<DoubleBondSetViewModel> DoubleBonds { get; }

        public ReactivePropertySlim<int> OxidizedCount { get; }

        public ReadOnlyReactiveCollection<OxidizedSetViewModel> Oxidises { get; }

        public ReactiveCommand AddDoubleBondCommand { get; }

        public ReactiveCommand AddOxidizedCommand { get; }
    }

    public class DoubleBondSetViewModel : ViewModelBase
    {
        public DoubleBondSetViewModel(DoubleBondSetModel model) {
            Model = model;

            Position = Model.ToReactivePropertySlimAsSynchronized(m => m.Position).AddTo(Disposables);
            BondType = Model.ToReactivePropertySlimAsSynchronized(m => m.BondType).AddTo(Disposables);

            RemoveCommand = new ReactiveCommand().AddTo(Disposables);
        }

        public DoubleBondSetModel Model { get; }

        public ReactivePropertySlim<int> Position { get; }

        public ReactivePropertySlim<string> BondType { get; }

        public ReadOnlyCollection<string> BondTypes => Model.BondTypes;

        public ReactiveCommand RemoveCommand { get; }
    }

    public class OxidizedSetViewModel : ViewModelBase
    {
        public OxidizedSetViewModel(OxidizedSetModel model) {
            Model = model;

            Position = Model.ToReactivePropertySlimAsSynchronized(m => m.Position).AddTo(Disposables);

            RemoveCommand = new ReactiveCommand().AddTo(Disposables);
        }

        public OxidizedSetModel Model { get; }

        public ReactivePropertySlim<int> Position { get; }

        public ReactiveCommand RemoveCommand { get; }
    }
}
