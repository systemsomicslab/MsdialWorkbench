using CompMs.App.SpectrumViewer.Model;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class SpectrumViewModel : ViewModelBase {
        public SpectrumViewModel(SpectrumModel model) {
            Model = model;

            Name = Observable.Return(Model.Name).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            DisplayScans = Model.DisplayScans.ToReadOnlyReactiveCollection().AddTo(Disposables);
            DisplayScan = new ReactivePropertySlim<DisplayScan>().AddTo(Disposables);

            var collectionChanged = new[]
            {
                DisplayScans.ObserveAddChanged().ToUnit(),
                DisplayScans.ObserveRemoveChanged().ToUnit(),
                DisplayScans.ObserveResetChanged().ToUnit(),
            }.Merge();
            HorizontalAxis = collectionChanged
                .Select(_ => DisplayScans
                    .Select(scan => new Range(scan.Spectrum.Min(s => s.Mass), scan.Spectrum.Max(s => s.Mass)))
                    .Aggregate((acc, range) => acc.Union(range)))
                .ToReactiveContinuousAxisManager<double>(new ConstantMargin(30), labelType: LabelType.Standard)
                .AddTo(Disposables);
            VerticalAxis = collectionChanged
                .Select(_ => DisplayScans
                    .Select(scan => new Range(scan.Spectrum.Min(s => s.Intensity), scan.Spectrum.Max(s => s.Intensity)))
                    .Aggregate((acc, range) => acc.Union(range)))
                .ToReactiveContinuousAxisManager<double>(new ConstantMargin(0, 30), new Range(0, 0), labelType: LabelType.Order)
                .AddTo(Disposables);

            DropCommand = new ReactiveCommand<DragEventArgs>().AddTo(Disposables);
            DropCommand
                .Where(e => e.Data.GetDataPresent(typeof(DisplayScan)))
                .Select(e => e.Data.GetData(typeof(DisplayScan)))
                .OfType<DisplayScan>()
                .Where(scan => !DisplayScans.Contains(scan))
                .Subscribe(AddScan)
                .AddTo(Disposables);

            CloseCommand = new ReactiveCommand().AddTo(Disposables);
        }

        public SpectrumModel Model { get; }

        public ReadOnlyReactivePropertySlim<string> Name { get; }

        public ReadOnlyReactiveCollection<DisplayScan> DisplayScans { get; }

        public ReactivePropertySlim<DisplayScan> DisplayScan { get; }

        public ReactiveContinuousAxisManager<double> HorizontalAxis { get; }

        public ReactiveContinuousAxisManager<double> VerticalAxis { get; }

        public ReactiveCommand<DragEventArgs> DropCommand { get; }

        public ReactiveCommand CloseCommand { get; }

        public void AddScan(IObservable<DisplayScan> scan) {
            scan.Subscribe(Model.AddScan).AddTo(Disposables);
        }
        
        public void AddScan(IMSScanProperty scan) {
            Model.AddScan(scan);
        }
    }
}
