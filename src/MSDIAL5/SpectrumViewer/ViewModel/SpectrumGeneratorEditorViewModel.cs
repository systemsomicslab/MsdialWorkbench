using CompMs.App.SpectrumViewer.Model;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class SpectrumGeneratorEditorViewModel : ViewModelBase
    {
        public SpectrumGeneratorEditorViewModel(SpectrumGeneratorEditorModel model) {
            Model = model;
            SpectrumViewModel = new SpectrumViewModel(Model.SpectrumModel);

            Name = Observable.Return("Editor view").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            LipidClass = Model.ToReactivePropertySlimAsSynchronized(m => m.LipidClass).AddTo(Disposables);
            Adduct = Model.ToReactivePropertySlimAsSynchronized(m => m.Adduct).AddTo(Disposables);

            Variables = Model.Variables.ToReadOnlyReactiveCollection(m => new MzVariableViewModel(m)).AddTo(Disposables);
            AddVariableCommand = new ReactiveCommand().WithSubscribe(Model.AddVariable).AddTo(Disposables);
            Variables.ObserveElementObservableProperty(vm => vm.RemoveCommand)
                .Select(p => p.Instance.Model)
                .Subscribe(Model.RemoveVariable)
                .AddTo(Disposables);

            Rules = Model.Rules.ToReadOnlyReactiveCollection(m => new SpectrumGenerationRuleViewModel(m)).AddTo(Disposables);
            AddRuleCommand = new ReactiveCommand().WithSubscribe(Model.AddRule).AddTo(Disposables);
            Rules.ObserveElementObservableProperty(vm => vm.RemoveCommand)
                .Select(p => p.Instance.Model)
                .Subscribe(Model.RemoveRule)
                .AddTo(Disposables);

            PreviewLipidViewModel = new LipidSelectionViewModel(Model.PreviewLipidModel).AddTo(Disposables);
            PreviewAdduct = Model.ToReactivePropertySlimAsSynchronized(m => m.PreviewAdduct).AddTo(Disposables);

            CloseCommand = new ReactiveCommand().AddTo(Disposables);
            PreviewSpectrumCommand = new ReactiveCommand()
                .WithSubscribe(Model.PreviewSpectrum)
                .AddTo(Disposables);
        }

        public SpectrumGeneratorEditorModel Model { get; }

        public SpectrumViewModel SpectrumViewModel { get; }

        public ReadOnlyReactivePropertySlim<string> Name { get; }

        public ReadOnlyCollection<LbmClass> LipidClasses => Model.LipidClasses;

        public ReactivePropertySlim<LbmClass> LipidClass { get; }

        public ReadOnlyCollection<AdductIon> Adducts => Model.Adducts;

        public ReactivePropertySlim<AdductIon> Adduct { get; }

        public ReadOnlyReactiveCollection<MzVariableViewModel> Variables { get; }

        public ReactiveCommand AddVariableCommand { get; }

        public ReadOnlyReactiveCollection<SpectrumGenerationRuleViewModel> Rules { get; }

        public ReactiveCommand AddRuleCommand { get; }

        public LipidSelectionViewModel PreviewLipidViewModel { get; }

        public ReactivePropertySlim<AdductIon> PreviewAdduct { get; }

        public ReactiveCommand CloseCommand { get; }

        public ReactiveCommand PreviewSpectrumCommand { get; }

        public ReactiveCommand<DragEventArgs> DropCommand => SpectrumViewModel?.DropCommand;
    }
}
