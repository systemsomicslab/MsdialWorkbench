using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Loader;
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class BarChartViewModel : ViewModelBase
    {
        private readonly BarChartModel _model;

        public BarChartViewModel(BarChartModel model, Action focusAction, IObservable<bool> isFocused) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            FocusAction = focusAction;
            IsFocused = isFocused.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            BarItems = model.BarItemsSource
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            // HorizontalAxis = model
            //     .BarItemsSource
            //     .Select(items => items.Select(item => item.Class).ToArray())
            //     .ToReactiveCategoryAxisManager()
            //     .AddTo(Disposables);
            HorizontalAxis = model.OrderedClasses.ToReactiveCategoryAxisManager().AddTo(Disposables);

            VerticalAxis = model
                .VerticalRangeAsObservable
                .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0, 0.05), new AxisRange(0, 0), LabelType.Order)
                .AddTo(Disposables);

            var brushSource = model.ClassBrush;
            if (brushSource is null) {
                brushSource = BarItems.Select(
                    items => new KeyBrushMapper<BarItem>(
                        items.Zip(ChartBrushes.SolidColorBrushList, (item, brush) => (item, brush))
                            .ToDictionary(p => p.item, p => (Brush)p.brush),
                        Brushes.Blue
                    ));
            }
            BrushSource = brushSource.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            Errors = BarItems
                .Where(items => items is not null)
                .Select(items => items.Select(item => item.Error).ToArray())
                .ToReadOnlyReactivePropertySlim(Array.Empty<double>())
                .AddTo(Disposables);

            HorizontalTitle = model.Elements
                .ObserveProperty(m => m.HorizontalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalTitle = model.Elements
                .ObserveProperty(m => m.VerticalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalProperty = model.Elements
                .ObserveProperty(m => m.HorizontalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalProperty = model.Elements
                .ObserveProperty(m => m.VerticalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            BarItemsLoaderDataViewModels = new ReadOnlyObservableCollection<BarItemsLoaderDataViewModel>(new ObservableCollection<BarItemsLoaderDataViewModel>(model.BarItemsLoaderDatas.Select(data => new BarItemsLoaderDataViewModel(data, model.BarItemsLoaderData))));
            BarItemsLoaderDataViewModel = model.BarItemsLoaderData.ToReactivePropertyAsSynchronized(
                m => m.Value,
                m => m.Select(m_ => BarItemsLoaderDataViewModels.FirstOrDefault(vm => vm.Model == m_)),
                vm => vm.Where(vm_ => !(vm_ is null)).Select(vm_ => vm_.Model)).AddTo(Disposables);

            var cv = CollectionViewSource.GetDefaultView(BarItemsLoaderDataViewModels);
            cv.Filter += o => ((BarItemsLoaderDataViewModel)o).IsEnabled.Value;
            BarItemsLoaderDataViewModels.ObserveElementObservableProperty(vm => vm.IsEnabled)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .Subscribe(_ => Application.Current.Dispatcher.Invoke(() => cv?.Refresh()))
                .AddTo(Disposables);

            IsLoading = model.IsLoading.ToReadOnlyReactivePropertySlim(true).AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<List<BarItem>> BarItems { get; }

        public ReadOnlyReactivePropertySlim<double[]> Errors { get; }

        public IAxisManager<string> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public ReadOnlyReactivePropertySlim<string?> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string?> VerticalProperty { get; }

        public ReadOnlyReactivePropertySlim<IBrushMapper<BarItem>?> BrushSource { get; }

        public Action FocusAction { get; }
        public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }

        public IReactiveProperty<BarItemsLoaderDataViewModel> BarItemsLoaderDataViewModel { get; }
        public ReadOnlyObservableCollection<BarItemsLoaderDataViewModel> BarItemsLoaderDataViewModels { get; }

        public ReadOnlyReactivePropertySlim<bool> IsLoading { get; }
    }
}
