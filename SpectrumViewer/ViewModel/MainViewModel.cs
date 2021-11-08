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

            SpectrumViewModels = model.SpectrumModels.ToReadOnlyReactiveCollection(sm => new SpectrumViewModel(sm)).AddTo(Disposables);
            SpectrumViewModel = SpectrumViewModels.ObserveAddChanged().ToReactiveProperty().AddTo(Disposables);
            var spectrumViewModels = CollectionViewSource.GetDefaultView(SpectrumViewModels) as IEditableCollectionView;
            spectrumViewModels.NewItemPlaceholderPosition = NewItemPlaceholderPosition.AtEnd;

            NewSpectrumCommand = new ReactiveCommand()
                .WithSubscribe(model.AddSpectrumModel)
                .AddTo(Disposables);

            AddLipidReferenceCollectionCommand = new ReactiveCommand()
                .WithSubscribe(model.AddLipidReferenceGeneration)
                .AddTo(Disposables);

            ScanCollections.ObserveElementObservableProperty(sc => sc.CloseCommand)
                .Select(prop => prop.Instance.Model)
                .Subscribe(model.RemoveScanCollection)
                .AddTo(Disposables);
            ScanCollections.ObserveElementObservableProperty(sc => sc.ScanSource)
                .Select(prop => prop.Value)
                .WithLatestFrom(SpectrumViewModel)
                .Where(p => p.First != null && p.Second != null)
                .Subscribe(p => p.Second.AddScan(p.First))
                .AddTo(Disposables);

            SpectrumViewModels.ObserveElementObservableProperty(sv => sv.CloseCommand)
                .Select(prop => prop.Instance.Model)
                .Subscribe(model.RemoveSpectrumModel)
                .AddTo(Disposables);


            broker.ToObservable<FileOpenRequest>()
                .Subscribe(model.FileOpen)
                .AddTo(Disposables);
        }

        public ReactivePropertySlim<bool> IsExpanded { get; }

        public LipidQueryBeanViewModel LipidQueries { get; }

        public ReadOnlyReactiveCollection<IScanCollectionViewModel> ScanCollections { get; }

        public ReactiveProperty<IScanCollectionViewModel> ScanCollection { get; }

        public ReadOnlyReactiveCollection<SpectrumViewModel> SpectrumViewModels { get; }

        public ReactiveProperty<SpectrumViewModel> SpectrumViewModel { get; }

        public ReactiveCommand NewSpectrumCommand { get; }

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