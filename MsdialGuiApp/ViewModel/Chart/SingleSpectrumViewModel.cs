using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public class SingleSpectrumViewModel : ViewModelBase
    {
        private readonly SingleSpectrumModel model;

        public SingleSpectrumViewModel(SingleSpectrumModel model) {
            this.model = model;
            Spectrum = this.model.Spectrum.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            HorizontalAxis = this.model.HorizontalAxis.Cast<IAxisManager>().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            VerticalAxis = this.model.VerticalAxis.Cast<IAxisManager>().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Brush = this.model.Brush.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            LineThickness = model.LineThickness;
            IsVisible = model.IsVisible;
        }

        public ReadOnlyReactivePropertySlim<List<SpectrumPeak>> Spectrum { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager> HorizontalAxis { get; }
        public GraphLabels Labels => model.Labels;
        public string HorizontalProperty => model.HorizontalPropertySelector.Property;
        public ReadOnlyReactivePropertySlim<IAxisManager> VerticalAxis { get; }
        public string VerticalProperty => model.VerticalPropertySelector.Property;
        public ReadOnlyReactivePropertySlim<IBrushMapper> Brush { get; }
        public string HueProperty => model.HueProperty;
        public ReactivePropertySlim<double> LineThickness { get; }
        public ReactivePropertySlim<bool> IsVisible { get; }
    }
}
