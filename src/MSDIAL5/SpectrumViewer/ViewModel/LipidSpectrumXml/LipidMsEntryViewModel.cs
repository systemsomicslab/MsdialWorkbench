using CompMs.App.SpectrumViewer.Model.LipidSpectrumXml;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.SpectrumViewer.ViewModel.LipidSpectrumXml
{
    public class LipidMsEntryViewModel : ViewModelBase
    {
        public LipidMsEntryViewModel(LipidMsEntryModel model) {
            Model = model;

            LipidClass = Model.ToReactivePropertySlimAsSynchronized(m => m.LipidClass).AddTo(Disposables);
            LSILevel = Model.ToReactivePropertySlimAsSynchronized(m => m.LSILevel).AddTo(Disposables);
            Adduct = Model.ToReactivePropertySlimAsSynchronized(m => m.Adduct).AddTo(Disposables);
            DisplayName = new[] { LipidClass.ToUnit(), Adduct.ToUnit(), LSILevel.ToUnit(), }
                .Merge()
                .Select(_ => Model.DisplayName)
                .ToReadOnlyReactivePropertySlim(Model.DisplayName)
                .AddTo(Disposables);

            Precursors = Wire(Model.Precursors, "Precursors", IonRowKind.Precursor);
            ClassIons = Wire(Model.ClassIons, "ClassIons", IonRowKind.ClassIon);
            ChainIons = Wire(Model.ChainIons, "ChainIons", IonRowKind.ChainIon);
            InvisibleIons = Wire(Model.InvisibleIons, "InvisibleIons", IonRowKind.InvisibleIon);
            ProhibitedIons = Wire(Model.ProhibitedIons, "ProhibitedIons", IonRowKind.ProhibitedIon);
        }

        public LipidMsEntryModel Model { get; }

        public ReactivePropertySlim<string> LipidClass { get; }
        public ReactivePropertySlim<string> LSILevel { get; }
        public ReactivePropertySlim<string> Adduct { get; }
        public ReadOnlyReactivePropertySlim<string> DisplayName { get; }

        public ReadOnlyReactiveCollection<IonRowViewModel> Precursors { get; }
        public ReactiveCommand AddPrecursorCommand { get; private set; }

        public ReadOnlyReactiveCollection<IonRowViewModel> ClassIons { get; }
        public ReactiveCommand AddClassIonCommand { get; private set; }

        public ReadOnlyReactiveCollection<IonRowViewModel> ChainIons { get; }
        public ReactiveCommand AddChainIonCommand { get; private set; }

        public ReadOnlyReactiveCollection<IonRowViewModel> InvisibleIons { get; }
        public ReactiveCommand AddInvisibleIonCommand { get; private set; }

        public ReadOnlyReactiveCollection<IonRowViewModel> ProhibitedIons { get; }
        public ReactiveCommand AddProhibitedIonCommand { get; private set; }

        private ReadOnlyReactiveCollection<IonRowViewModel> Wire(
                System.Collections.ObjectModel.ObservableCollection<IonRowModel> rows,
                string wrapperElementName,
                IonRowKind kind) {
            var collection = rows.ToReadOnlyReactiveCollection(m => new IonRowViewModel(m)).AddTo(Disposables);

            var addCommand = new ReactiveCommand()
                .WithSubscribe(() => Model.AddRow(rows, wrapperElementName, kind))
                .AddTo(Disposables);
            collection.ObserveElementObservableProperty(vm => vm.RemoveCommand)
                .Select(p => p.Instance.Model)
                .Subscribe(row => Model.RemoveRow(rows, row))
                .AddTo(Disposables);

            switch (kind) {
                case IonRowKind.Precursor: AddPrecursorCommand = addCommand; break;
                case IonRowKind.ClassIon: AddClassIonCommand = addCommand; break;
                case IonRowKind.ChainIon: AddChainIonCommand = addCommand; break;
                case IonRowKind.InvisibleIon: AddInvisibleIonCommand = addCommand; break;
                case IonRowKind.ProhibitedIon: AddProhibitedIonCommand = addCommand; break;
            }
            return collection;
        }
    }
}
