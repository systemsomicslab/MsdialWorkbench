using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    class RawDecSpectrumsViewModel : ViewModelBase
    {
        public RawDecSpectrumsViewModel(
            RawDecSpectrumsModel model,
            IObservable<IAxisManager<double>> horizontalAxisSource = null,
            IObservable<IAxisManager<double>> upperVerticalAxisSource = null,
            IObservable<IAxisManager<double>> lowerVerticalAxisSource = null,
            IObservable<IBrushMapper<SpectrumComment>> upperSpectrumBrushSource = null,
            IObservable<IBrushMapper<SpectrumComment>> lowerSpectrumBrushSource = null) {

            this.model = model;

            var upperVerticalAxis = this.model
                .RawRefSpectrumModels
                .HorizontalRangeSource
                .ToReactiveAxisManager<double>(new ConstantMargin(0, 30), new Range(0d, 0d), LabelType.Percent)
                .AddTo(Disposables);
            var upperLogVerticalAxis = this.model
                .RawRefSpectrumModels
                .HorizontalRangeSource
                .Select(range => (range.Minimum.Value, range.Maximum.Value))
                .ToReactiveLogScaleAxisManager(new ConstantMargin(0, 30), 1d, 1d)
                .AddTo(Disposables);

            var axis = new AxisItemViewModel(upperVerticalAxis, "Normal");
            UpperVerticalAxiss = new ObservableCollection<AxisItemViewModel>(new[]
            {
                axis,
                new AxisItemViewModel(upperLogVerticalAxis, "Log10"),
            });
            UpperVerticalAxis = new ReactivePropertySlim<AxisItemViewModel>(axis).AddTo(Disposables);

            RawRefSpectrumViewModels = new MsSpectrumViewModel(
                this.model.RawRefSpectrumModels,
                horizontalAxisSource,
                UpperVerticalAxis.Select(item => item.AxisManager),
                lowerVerticalAxisSource,
                upperSpectrumBrushSource,
                lowerSpectrumBrushSource);

            DecRefSpectrumViewModels = new MsSpectrumViewModel(
                this.model.DecRefSpectrumModels,
                horizontalAxisSource,
                UpperVerticalAxis.Select(item => item.AxisManager),
                lowerVerticalAxisSource,
                upperSpectrumBrushSource,
                lowerSpectrumBrushSource);
        }

        private readonly RawDecSpectrumsModel model;

        public MsSpectrumViewModel RawRefSpectrumViewModels { get; }

        public MsSpectrumViewModel DecRefSpectrumViewModels { get; }

        public ObservableCollection<AxisItemViewModel> UpperVerticalAxiss { get; }

        public ReactivePropertySlim<AxisItemViewModel> UpperVerticalAxis { get; }

    }

    public class AxisItemViewModel : BindableBase
    {
        public AxisItemViewModel(IAxisManager<double> axisManager, string label) {
            AxisManager = axisManager;
            Label = label;
        }

        public IAxisManager<double> AxisManager { get; }

        public string Label { get; }
    }
}
