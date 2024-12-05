using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Chart;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class AnalysisPeakPlotModel : AnalysisPeakPlotModel<ChromatogramPeakFeatureModel, ObservableCollection<ChromatogramPeakFeatureModel>> {
        public AnalysisPeakPlotModel(
            ObservableCollection<ChromatogramPeakFeatureModel> spots,
            Func<ChromatogramPeakFeatureModel, double> horizontalSelector,
            Func<ChromatogramPeakFeatureModel, double> verticalSelector,
            IReactiveProperty<ChromatogramPeakFeatureModel?> targetSource,
            IObservable<string?> labelSource,
            BrushMapData<ChromatogramPeakFeatureModel> selectedBrush,
            IList<BrushMapData<ChromatogramPeakFeatureModel>> brushes,
            PeakLinkModel peakLinkModel,
            IAxisManager<double>? horizontalAxis = null,
            IAxisManager<double>? verticalAxis = null):
            base(spots, horizontalSelector, verticalSelector, targetSource, labelSource, selectedBrush, brushes, peakLinkModel, horizontalAxis, verticalAxis) {

        }
    }
    
    internal class AnalysisPeakPlotModel<T, U> : DisposableModelBase where U: IList, IEnumerable<T>, INotifyCollectionChanged
    {
        private readonly PeakLinkModel _peakLinkModel;

        public AnalysisPeakPlotModel(
            U spots,
            Func<T, double> horizontalSelector,
            Func<T, double> verticalSelector,
            IReactiveProperty<T?> targetSource,
            IObservable<string?> labelSource,
            BrushMapData<T> selectedBrush,
            IList<BrushMapData<T>> brushes,
            PeakLinkModel peakLinkModel,
            IAxisManager<double>? horizontalAxis = null,
            IAxisManager<double>? verticalAxis = null) {
            if (brushes is null) {
                throw new ArgumentNullException(nameof(brushes));
            }

            Spots = spots ?? throw new ArgumentNullException(nameof(spots));
            LabelSource = labelSource ?? throw new ArgumentNullException(nameof(labelSource));
            _selectedBrush = selectedBrush ?? throw new ArgumentNullException(nameof(selectedBrush));
            Brushes = new ReadOnlyCollection<BrushMapData<T>>(brushes);
            TargetSource = targetSource ?? throw new ArgumentNullException(nameof(targetSource));
            GraphTitle = string.Empty;
            HorizontalTitle = string.Empty;
            VerticalTitle = string.Empty;
            HorizontalProperty = string.Empty;
            VerticalProperty = string.Empty;

            HorizontalAxis = horizontalAxis ?? spots.CollectionChangedAsObservable().ToUnit().StartWith(Unit.Default).Throttle(TimeSpan.FromSeconds(.01d))
                .Select(_ => spots.Any() ? new AxisRange(spots.Min(horizontalSelector), spots.Max(horizontalSelector)) : new AxisRange(0, 1))
                .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05))
                .AddTo(Disposables);
            VerticalAxis = verticalAxis ?? spots.CollectionChangedAsObservable().ToUnit().StartWith(Unit.Default).Throttle(TimeSpan.FromSeconds(.01d))
                .Select(_ => spots.Any() ? new AxisRange(spots.Min(verticalSelector), spots.Max(verticalSelector)) : new AxisRange(0, 1))
                .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05))
                .AddTo(Disposables);
            _peakLinkModel = peakLinkModel;
        }

        public IList Spots { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public IReactiveProperty<T?> TargetSource { get; }

        public string GraphTitle {
            get => _graphTitle;
            set => SetProperty(ref _graphTitle, value);
        }
        private string _graphTitle = string.Empty;

        public string HorizontalTitle {
            get => _horizontalTitle;
            set => SetProperty(ref _horizontalTitle, value);
        }
        private string _horizontalTitle = string.Empty;

        public string VerticalTitle {
            get => _verticalTitle;
            set => SetProperty(ref _verticalTitle, value);
        }
        private string _verticalTitle = string.Empty;

        public string HorizontalProperty {
            get => _horizontalProperty;
            set => SetProperty(ref _horizontalProperty, value);
        }
        private string _horizontalProperty = string.Empty;

        public string VerticalProperty {
            get => _verticalProperty;
            set => SetProperty(ref _verticalProperty, value);
        }
        private string _verticalProperty = string.Empty;

        public IObservable<string?> LabelSource { get; }
        public BrushMapData<T> SelectedBrush {
            get => _selectedBrush;
            set => SetProperty(ref _selectedBrush, value);
        }
        private BrushMapData<T> _selectedBrush;

        public ReadOnlyCollection<BrushMapData<T>> Brushes { get; }

        public ObservableCollection<SpotLinker> Links => _peakLinkModel.Links;
        public ObservableCollection<SpotAnnotator> Annotations => _peakLinkModel.Annotations;
        public IBrushMapper<ISpotLinker> LinkerBrush => _peakLinkModel.LinkerBrush;
        public IBrushMapper<SpotAnnotator> SpotLabelBrush => _peakLinkModel.SpotLabelBrush;

        public IExportMrmprobsUsecase? ExportMrmprobs { get; set; }

        public ExportMrmprobsModel? ExportMrmprobsModel() {
            if (ExportMrmprobs is null) {
                return null;
            }
            return new ExportMrmprobsModel(ExportMrmprobs);
        }
    }
}
