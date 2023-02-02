using CompMs.App.SpectrumViewer.Model;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class LipidReferenceCollectionViewModel : ViewModelBase, IScanCollectionViewModel
    {
        public LipidReferenceCollectionViewModel(LipidReferenceCollection model) {
            Model = model;

            LipidStr = new ReactiveProperty<string>(string.Empty).AddTo(Disposables);
            Lipid = Model.ObserveProperty(m => m.Lipid).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Name = Lipid.Where(lipid => lipid != null).Select(lipid => lipid.ToString()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Adduct = Model.ToReactivePropertySlimAsSynchronized(m => m.Adduct).AddTo(Disposables);
            Scan = new ReactivePropertySlim<IMSScanProperty>().AddTo(Disposables);
            Scans = Model.Scans.ToReadOnlyReactiveCollection().AddTo(Disposables);

            ParseLipidCommand = new ReactiveCommand().AddTo(Disposables);
            ParseLipidCommand
                .WithLatestFrom(LipidStr, (_, s) => s)
                .Subscribe(Model.Generate)
                .AddTo(Disposables);

            CloseCommand = new ReactiveCommand().AddTo(Disposables);

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
        }

        public LipidReferenceCollection Model { get; }
        IScanCollection IScanCollectionViewModel.Model => Model;

        public ReadOnlyReactivePropertySlim<string> Name { get; }

        public ReactiveProperty<string> LipidStr { get; }

        public ReadOnlyReactivePropertySlim<ILipid> Lipid { get; }

        public ReadOnlyCollection<AdductIon> Adducts => Model.Adducts;
        public ReactivePropertySlim<AdductIon> Adduct { get; }

        public ReadOnlyReactiveCollection<IMSScanProperty> Scans { get; }

        public ReactivePropertySlim<IMSScanProperty> Scan { get; }

        public IObservable<IMSScanProperty> ScanSource { get; }

        public ReactiveCommand ParseLipidCommand { get; }

        public ReactiveCommand CloseCommand { get; }

        public ReactiveCommand<MouseButtonEventArgs> ClickCommand { get; }
    }
}
