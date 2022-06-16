using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class AnalysisPeakPlotViewModel : ViewModelBase
    {
        private readonly AnalysisPeakPlotModel _model;

        public AnalysisPeakPlotViewModel(AnalysisPeakPlotModel model, Action focus, IObservable<bool> isFocused) {
            Spots = model.Spots;

            HorizontalAxis = model.HorizontalAxis;
            VerticalAxis = model.VerticalAxis;

            SelectedBrush = model.ToReactivePropertyAsSynchronized(m => m.SelectedBrush).AddTo(Disposables);
            Brush = SelectedBrush.Select(data => data?.Mapper)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            Target = model.TargetSource;

            GraphTitle = model.ObserveProperty(m => m.GraphTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalTitle = model.ObserveProperty(m => m.HorizontalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalTitle = model.ObserveProperty(m => m.VerticalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalProperty = model.ObserveProperty(m => m.HorizontalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalProperty = model.ObserveProperty(m => m.VerticalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LabelProperty = model.LabelSource
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            _model = model;
            Focus = focus;
            IsFocused = isFocused.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public Action Focus { get; }

        public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }

        public ObservableCollection<ChromatogramPeakFeatureModel> Spots { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; } 

        public ReadOnlyReactivePropertySlim<IBrushMapper<ChromatogramPeakFeatureModel>> Brush { get; }

        public ReadOnlyCollection<BrushMapData<ChromatogramPeakFeatureModel>> Brushes => _model.Brushes;

        public ReactiveProperty<BrushMapData<ChromatogramPeakFeatureModel>> SelectedBrush { get; }

        public IReactiveProperty<ChromatogramPeakFeatureModel> Target { get; }

        public ReadOnlyReactivePropertySlim<string> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> LabelProperty { get; }
    }
}
