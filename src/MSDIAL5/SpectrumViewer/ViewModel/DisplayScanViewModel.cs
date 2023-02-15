using CompMs.App.SpectrumViewer.Model;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public sealed class DisplayScanViewModel : ViewModelBase
    {
        private readonly DisplayScan _scan;

        public DisplayScanViewModel(DisplayScan scan, IAxisManager<double> shareIntensityAxis, IAxisManager<double> shareGradientAxis, Color color) {
            _scan = scan;
            Brush = new SolidColorBrush(color);
            Brush.Freeze();
            var gradientAxis = new RelativeAxisManager(scan.Spectrum.Select(p => p.Intensity).DefaultIfEmpty().Min(), scan.Spectrum.Select(p => p.Intensity).DefaultIfEmpty().Max(), shareGradientAxis).AddTo(Disposables);
            GradientAxis = gradientAxis;
            GradientBrush = new GradientBrushMapper<double>(gradientAxis, new[] { new GradientStop(Colors.LightGray, 0d), new GradientStop(color, 1d) });
            ConstantBrush = new ConstantBrushMapper(Brush);
            IntensityAxis = new RelativeAxisManager(scan.Spectrum.Select(p => p.Intensity).DefaultIfEmpty().Min(), scan.Spectrum.Select(p => p.Intensity).DefaultIfEmpty().Max(), shareIntensityAxis).AddTo(Disposables);
            Visible = scan.ToReactivePropertyAsSynchronized(s => s.Visible).AddTo(Disposables);
            IsSelected = scan.ToReactivePropertySlimAsSynchronized(s => s.IsSelected).AddTo(Disposables);
        }

        public string Name => _scan.Name;

        public ReactiveProperty<bool> Visible { get; }

        public ReactivePropertySlim<bool> IsSelected { get; }

        public ReadOnlyCollection<SpectrumPeak> Spectrum => _scan.Spectrum.AsReadOnly();

        public Brush Brush { get; }
        public IAxisManager<double> IntensityAxis { get; }
        public IAxisManager<double> GradientAxis { get; }
        public GradientBrushMapper<double> GradientBrush { get; }
        public ConstantBrushMapper ConstantBrush { get; }
    }
}
