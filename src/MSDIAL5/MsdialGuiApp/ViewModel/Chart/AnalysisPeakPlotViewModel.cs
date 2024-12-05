using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Chart;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal class AnalysisPeakPlotViewModel : AnalysisPeakPlotViewModel<ChromatogramPeakFeatureModel, ObservableCollection<ChromatogramPeakFeatureModel>>
    {
        public AnalysisPeakPlotViewModel(AnalysisPeakPlotModel model, Action focus, IObservable<bool> isFocused, IMessageBroker broker) :
            base(model, focus, isFocused, broker) {

        }
    }

    internal class AnalysisPeakPlotViewModel<T, U> : ViewModelBase where U: IList, IEnumerable<T>, INotifyCollectionChanged
    {
        private readonly AnalysisPeakPlotModel<T, U> _model;
        private readonly IMessageBroker _broker;

        public AnalysisPeakPlotViewModel(AnalysisPeakPlotModel<T, U> model, Action focus, IObservable<bool> isFocused, IMessageBroker broker) {
            _model = model;

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

            Links = new ReadOnlyObservableCollection<SpotLinker>(model.Links);
            Annotations = new ReadOnlyObservableCollection<SpotAnnotator>(model.Annotations);
            LinkerBrush = model.LinkerBrush;
            SpotLabelBrush = model.SpotLabelBrush;

            Focus = focus;
            _broker = broker;
            IsFocused = isFocused.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public Action Focus { get; }

        public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }

        public IList Spots { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; } 

        public ReadOnlyReactivePropertySlim<IBrushMapper<T>?> Brush { get; }

        public ReadOnlyCollection<BrushMapData<T>> Brushes => _model.Brushes;

        public ReactiveProperty<BrushMapData<T>> SelectedBrush { get; }

        public IReactiveProperty<T?> Target { get; }

        public ReadOnlyReactivePropertySlim<string?> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string?> VerticalProperty { get; }

        public ReadOnlyReactivePropertySlim<string?> LabelProperty { get; }

        public ReadOnlyObservableCollection<SpotLinker> Links { get; }
        public ReadOnlyObservableCollection<SpotAnnotator> Annotations { get; }

        public IBrushMapper<SpotLinker> LinkerBrush { get; }
        public IBrushMapper<SpotAnnotator> SpotLabelBrush { get; }

        public DelegateCommand SaveMrmprobsCommand => _saveMrmprobsCommand ??= new DelegateCommand(() => ExportMrmprobs(false), () => _model.ExportMrmprobs != null);
        private DelegateCommand? _saveMrmprobsCommand;

        public DelegateCommand CopyMrmprobsCommand => _copyMrmprobsCommand ??= new DelegateCommand(() => ExportMrmprobs(true), () => _model.ExportMrmprobs != null);
        private DelegateCommand? _copyMrmprobsCommand;

        private void ExportMrmprobs(bool copy) {
            var m = _model.ExportMrmprobsModel();
            if (m is null) {
                return;
            }
            m.Copy = copy;
            using var vm = new ExportMrmprobsViewModel(m);
            _broker.Publish(vm);
        }
    }
}
