using CompMs.App.SpectrumViewer.Model;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class SplitSpectrumsViewModel : ViewModelBase
    {
        public SplitSpectrumsViewModel(SplitSpectrumsModel model) {
            Model = model;

            Name = Observable.Return(Model.Name).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            DisplayScans = Model.DisplayScans.ToReadOnlyReactiveCollection().AddTo(Disposables);

            var collectionChanged = new[]
            {
                DisplayScans.ObserveAddChanged().ToUnit(),
                DisplayScans.ObserveRemoveChanged().ToUnit(),
                DisplayScans.ObserveResetChanged().ToUnit(),
            }.Merge();

            HorizontalAxis = collectionChanged
                .Where(_ => DisplayScans.Any())
                .Select(_ => DisplayScans
                    .Select(scan => new Range(scan.Spectrum.DefaultIfEmpty().Min(s => s?.Mass) ?? 0d, scan.Spectrum.DefaultIfEmpty().Max(s => s?.Mass) ?? 0d))
                    .Aggregate((acc, range) => acc.Union(range)))
                .ToReactiveContinuousAxisManager<double>(new ConstantMargin(30), labelType: LabelType.Standard)
                .AddTo(Disposables);
            VerticalAxis = collectionChanged
                .Where(_ => DisplayScans.Any())
                .Select(_ => DisplayScans
                    .Select(scan => new Range(scan.Spectrum.DefaultIfEmpty().Min(s => s?.Intensity) ?? 0d, scan.Spectrum.DefaultIfEmpty().Max(s => s?.Intensity) ?? 0d))
                    .Aggregate((acc, range) => acc.Union(range)))
                .ToReactiveContinuousAxisManager<double>(new ConstantMargin(0, 30), new Range(0, 0), labelType: LabelType.Order)
                .AddTo(Disposables);

            UpperSpectrumsViewModel = new SpectrumViewModel(model.UpperSpectrumModel);
            LowerSpectrumsViewModel = new SpectrumViewModel(model.LowerSpectrumModel);

            ChartBrushes = new[]
            {
                new ConstantBrushMapper<DisplayScan>(Brushes.Black),
                new ConstantBrushMapper<DisplayScan>(Brushes.Red),
                new ConstantBrushMapper<DisplayScan>(Brushes.Blue),
                new ConstantBrushMapper<DisplayScan>(Brushes.Green),
                new ConstantBrushMapper<DisplayScan>(Brushes.Gray),
                new ConstantBrushMapper<DisplayScan>(Brushes.Magenta),
                new ConstantBrushMapper<DisplayScan>(Brushes.Cyan),
                new ConstantBrushMapper<DisplayScan>(Brushes.Yellow),
            };

            DropCommand = new ReactiveCommand<DragEventArgs>().AddTo(Disposables);
            DropCommand
                .Where(e => !e.Handled && e.Data.GetDataPresent(typeof(DisplayScan)))
                .Do(e => e.Handled = true)
                .Select(e => e.Data.GetData(typeof(DisplayScan)))
                .OfType<DisplayScan>()
                .Where(scan => !DisplayScans.Contains(scan))
                .Subscribe(AddScan)
                .AddTo(Disposables);
            CloseCommand = new ReactiveCommand().AddTo(Disposables);
        }

        public SplitSpectrumsModel Model { get; }
        public ReadOnlyReactivePropertySlim<string> Name { get; }
        public ReadOnlyReactiveCollection<DisplayScan> DisplayScans { get; }
        public ReactiveContinuousAxisManager<double> HorizontalAxis { get; }
        public ReactiveContinuousAxisManager<double> VerticalAxis { get; }

        public IBrushMapper[] ChartBrushes { get; }

        public SpectrumViewModel UpperSpectrumsViewModel { get; }
        public SpectrumViewModel LowerSpectrumsViewModel { get; }

        public ReactiveCommand<DragEventArgs> DropCommand { get; }
        public ReactiveCommand CloseCommand { get; }

        public void AddScan(IMSScanProperty scan) {
            Model.AddScan(scan);
        }
    }
}
