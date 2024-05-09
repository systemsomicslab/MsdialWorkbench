using CompMs.App.SpectrumViewer.Model;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class SplitSpectrumsViewModel : ViewModelBase
    {
        private static readonly double CH2 = (MassDiffDictionary.HydrogenMass * 2) + MassDiffDictionary.CarbonMass;

        public SplitSpectrumsViewModel(SplitSpectrumsModel model) {
            Model = model;

            Name = Observable.Return(Model.Name).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var collectionChanged = new[]
            {
                Model.DisplayScans.ObserveAddChanged().ToUnit(),
                Model.DisplayScans.ObserveRemoveChanged().ToUnit(),
                Model.DisplayScans.ObserveResetChanged().ToUnit(),
            }.Merge();

            HorizontalAxis = collectionChanged
                .Where(_ => Model.DisplayScans.Any())
                .Select(_ => Model.DisplayScans
                    .Select(scan => new AxisRange(scan.Spectrum.DefaultIfEmpty().Min(s => s?.Mass) ?? 0d, scan.Spectrum.DefaultIfEmpty().Max(s => s?.Mass) ?? 0d))
                    .Aggregate((acc, range) => acc.Union(range)))
                .ToReactiveContinuousAxisManager<double>(new ConstantMargin(30), labelType: LabelType.Standard)
                .AddTo(Disposables);
            VerticalAxis = RelativeAxisManager.CreateBaseAxis(new ConstantMargin(0, 30), new AxisRange(0, 0));
            var intensityAxis = RelativeAxisManager.CreateBaseAxis();
            IntensityGradientAxis = intensityAxis;
            DefectVerticalAxis = new DefectAxisManager(CH2, new ConstantMargin(10)).AddTo(Disposables);

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

            var defectChartColors = new[]
            {
                Colors.Black,
                Colors.Red,
                Colors.Blue,
                Colors.Green,
                Colors.Gray,
                Colors.Magenta,
                Colors.Cyan,
                Colors.Yellow,
            };

            IEnumerable<Color> cycle() {
                while(defectChartColors.Any()) {
                    foreach (var color in defectChartColors)
                        yield return color;
                }
            }

            var colors = cycle().GetEnumerator();
            DisplayScans = Model.DisplayScans.ToReadOnlyReactiveCollection(scan => new DisplayScanViewModel(scan, VerticalAxis, intensityAxis, colors.MoveNext() ? colors.Current : defectChartColors[0])).AddTo(Disposables);

            DropCommand = new ReactiveCommand<DragEventArgs>().AddTo(Disposables);
            DropCommand
                .Where(e => !e.Handled && e.Data.GetDataPresent(typeof(DisplayScan)))
                .Do(e => e.Handled = true)
                .Select(e => e.Data.GetData(typeof(DisplayScan)))
                .OfType<DisplayScan>()
                .Where(scan => !Model.DisplayScans.Contains(scan))
                .Subscribe(AddScan)
                .AddTo(Disposables);
            CloseCommand = new ReactiveCommand().AddTo(Disposables);

            ShiftMz = model.ToReactivePropertyAsSynchronized(m => m.ShiftMz).AddTo(Disposables);
            ShiftScanCommand = ShiftMz.Select(mz => mz != 0d)
                .ToReactiveCommand()
                .WithSubscribe(model.ShifScan)
                .AddTo(Disposables);

            RemoveScanCommand = new ReactiveCommand()
                .WithSubscribe(model.RemoveScan)
                .AddTo(Disposables);
        }

        public SplitSpectrumsModel Model { get; }
        public ReadOnlyReactivePropertySlim<string> Name { get; }
        public ReadOnlyReactiveCollection<DisplayScanViewModel> DisplayScans { get; }
        public IAxisManager<double> HorizontalAxis { get; }
        public IAxisManager<double> VerticalAxis { get; }
        public IAxisManager<double> DefectVerticalAxis { get; }
        public IAxisManager<double> IntensityGradientAxis { get; }

        public IBrushMapper[] ChartBrushes { get; }

        public SpectrumViewModel UpperSpectrumsViewModel { get; }
        public SpectrumViewModel LowerSpectrumsViewModel { get; }

        public ReactiveCommand<DragEventArgs> DropCommand { get; }
        public ReactiveCommand CloseCommand { get; }

        public void AddScan(IMSScanProperty scan) {
            Model.AddScan(scan);
        }

        [RegularExpression(@"[-+]?\d*\.?\d+")]
        public ReactiveProperty<double> ShiftMz { get; }
        public ReactiveCommand ShiftScanCommand { get; }

        public ReactiveCommand RemoveScanCommand { get; }
    }
}
