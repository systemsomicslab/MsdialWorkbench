using CompMs.App.SpectrumViewer.Model;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class ScanCollectionViewModel : ViewModelBase, IScanCollectionViewModel {
        public ScanCollectionViewModel(ScanCollection model) {
            Model = model;

            Name = Model.ObserveProperty(m => m.Name).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Scans = Model.Scans.ToReadOnlyReactiveCollection().AddTo(Disposables);
            Scan = new ReactivePropertySlim<IMSScanProperty>().AddTo(Disposables);

            ClickCommand = new ReactiveCommand<MouseButtonEventArgs>().AddTo(Disposables);

            ClickCommand
                .Select(e => e.OriginalSource)
                .OfType<FrameworkElement>()
                .Subscribe(element => DragDrop.DoDragDrop(element, DisplayScan.WrapScan(element.DataContext as IMSScanProperty), DragDropEffects.Copy))
                .AddTo(Disposables);

            ScanSource = ClickCommand
                .Where(e => e.ClickCount == 2)
                .WithLatestFrom(Scan)
                .Select(p => p.Second)
                .Where(scan => scan != null);

            CloseCommand = new ReactiveCommand().AddTo(Disposables);
        }

        public ScanCollection Model { get; }
        IScanCollection IScanCollectionViewModel.Model => Model;

        public ReadOnlyReactivePropertySlim<string> Name { get; }

        public ReadOnlyReactiveCollection<IMSScanProperty> Scans { get; }

        public ReactivePropertySlim<IMSScanProperty> Scan { get; }

        public ReactiveCommand<MouseButtonEventArgs> ClickCommand { get; }

        public IObservable<IMSScanProperty> ScanSource { get; }

        public ReactiveCommand CloseCommand { get; }
    }
}
