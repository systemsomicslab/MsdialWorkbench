using CompMs.App.SpectrumViewer.Model;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly MessageBroker broker;
        private readonly MainModel model;

        public MainViewModel(MessageBroker broker) {
            this.broker = broker;
            model = new MainModel();

            IsExpanded = new ReactivePropertySlim<bool>().AddTo(Disposables);

            LipidQueries = new LipidQueryBeanViewModel(model.LipidQueries).AddTo(Disposables);

            ScanCollections = model.ScanCollections.ToReadOnlyReactiveCollection(MapScanModelToViewModel).AddTo(Disposables);
            ScanCollection = ScanCollections.ObserveAddChanged().ToReactiveProperty().AddTo(Disposables);

            SplitSpectrumViewModels = model.SplitSpectrumModels.ToReadOnlyReactiveCollection(ssm => new SplitSpectrumsViewModel(ssm)).AddTo(Disposables);
            SplitSpectrumViewModel = SplitSpectrumViewModels.ObserveAddChanged().ToReactiveProperty().AddTo(Disposables);
            var splitSpectrumViewModels = CollectionViewSource.GetDefaultView(SplitSpectrumViewModels) as IEditableCollectionView;
            splitSpectrumViewModels.NewItemPlaceholderPosition = NewItemPlaceholderPosition.AtEnd;

            GeneratorEditorViewModels = model.GeneratorEditorModels.ToReadOnlyReactiveCollection(gm => new SpectrumGeneratorEditorViewModel(gm)).AddTo(Disposables);
            GeneratorEditorViewModel = GeneratorEditorViewModels.ObserveAddChanged().ToReactiveProperty().AddTo(Disposables);

            ViewModels = new IObservable<ViewModelBase>[]
            {
                SplitSpectrumViewModels.ToObservable(),
                SplitSpectrumViewModels.ObserveAddChanged(),
                GeneratorEditorViewModels.ToObservable(),
                GeneratorEditorViewModels.ObserveAddChanged(),
            }.Merge().ToReactiveCollection().AddTo(Disposables);
            SplitSpectrumViewModels.ObserveRemoveChanged().Subscribe(ViewModels.RemoveOnScheduler);
            GeneratorEditorViewModels.ObserveRemoveChanged().Subscribe(ViewModels.RemoveOnScheduler);
            ViewModel = ViewModels.ObserveAddChanged().ToReactiveProperty().AddTo(Disposables);
            var viewModels = CollectionViewSource.GetDefaultView(ViewModels) as IEditableCollectionView;
            viewModels.NewItemPlaceholderPosition = NewItemPlaceholderPosition.AtEnd;

            AddLipidReferenceCollectionCommand = new ReactiveCommand()
                .WithSubscribe(model.AddLipidReferenceGeneration)
                .AddTo(Disposables);

            ScanCollections.ObserveElementObservableProperty(sc => sc.CloseCommand)
                .Select(prop => prop.Instance.Model)
                .Subscribe(model.RemoveScanCollection)
                .AddTo(Disposables);
            ScanCollections.ObserveElementObservableProperty(sc => sc.ScanSource)
                .Select(prop => prop.Value)
                .WithLatestFrom(SplitSpectrumViewModel)
                .Where(p => p.First != null && p.Second != null)
                .Subscribe(p => p.Second.AddScan(p.First))
                .AddTo(Disposables);

            NewSpectrumCommand = new ReactiveCommand()
                .WithSubscribe(model.AddSpectrumModel)
                .AddTo(Disposables);

            SplitSpectrumViewModels.ObserveElementObservableProperty(sv => sv.CloseCommand)
                .Select(prop => prop.Instance.Model)
                .Subscribe(model.RemoveSpectrumModel)
                .AddTo(Disposables);

            NewGeneratorEditorCommand = new ReactiveCommand()
                .WithSubscribe(model.AddSpectrumGeneratorEditorModel)
                .AddTo(Disposables);

            GeneratorEditorViewModels.ObserveElementObservableProperty(gv => gv.CloseCommand)
                .Select(prop => prop.Instance.Model)
                .Subscribe(model.RemoveSpectrumGeneratorEditorModel)
                .AddTo(Disposables);

            broker.ToObservable<FileOpenRequest>()
                .Subscribe(model.FileOpen)
                .AddTo(Disposables);
        }

        public ReactivePropertySlim<bool> IsExpanded { get; }

        public LipidQueryBeanViewModel LipidQueries { get; }

        public ReadOnlyReactiveCollection<IScanCollectionViewModel> ScanCollections { get; }

        public ReactiveProperty<IScanCollectionViewModel> ScanCollection { get; }

        public ReadOnlyReactiveCollection<SplitSpectrumsViewModel> SplitSpectrumViewModels { get; }

        public ReactiveProperty<SplitSpectrumsViewModel> SplitSpectrumViewModel { get; }

        public ReadOnlyReactiveCollection<SpectrumGeneratorEditorViewModel> GeneratorEditorViewModels { get; }

        public ReactiveProperty<SpectrumGeneratorEditorViewModel> GeneratorEditorViewModel { get; }

        public ReactiveCollection<ViewModelBase> ViewModels { get; }

        public ReactiveProperty<ViewModelBase> ViewModel { get; }

        public ReactiveCommand NewSpectrumCommand { get; }

        public ReactiveCommand NewGeneratorEditorCommand { get; }

        public ReactiveCommand AddLipidReferenceCollectionCommand { get; }

        private static IScanCollectionViewModel MapScanModelToViewModel(IScanCollection scanCollection) {
            switch (scanCollection) {
                case ScanCollection sc:
                    return new ScanCollectionViewModel(sc);
                case LipidReferenceCollection lc:
                    return new LipidReferenceCollectionViewModel(lc);
                default:
                    return null;
            }
        }
    }
}