using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class AlignmentPeakPlotModel : DisposableModelBase
    {
        public AlignmentPeakPlotModel(
            ObservableCollection<AlignmentSpotPropertyModel> spots,
            Func<AlignmentSpotPropertyModel, double> horizontalSelector,
            Func<AlignmentSpotPropertyModel, double> verticalSelector,
            IReactiveProperty<AlignmentSpotPropertyModel> targetSource,
            IObservable<string> labelSource,
            BrushMapData<AlignmentSpotPropertyModel> selectedBrush,
            IList<BrushMapData<AlignmentSpotPropertyModel>> brushes) {
            if (brushes is null) {
                throw new ArgumentNullException(nameof(brushes));
            }

            Spots = spots ?? throw new ArgumentNullException(nameof(spots));
            TargetSource = targetSource ?? throw new ArgumentNullException(nameof(targetSource));
            HorizontalSelector = horizontalSelector ?? throw new ArgumentNullException(nameof(horizontalSelector));
            VerticalSelector = verticalSelector ?? throw new ArgumentNullException(nameof(verticalSelector));
            LabelSource = labelSource ?? throw new ArgumentNullException(nameof(labelSource));
            SelectedBrush = selectedBrush ?? throw new ArgumentNullException(nameof(selectedBrush));
            Brushes = new ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>>(brushes);

            GraphTitle = string.Empty;
            HorizontalTitle = string.Empty;
            VerticalTitle = string.Empty;
            HorizontalProperty = string.Empty;
            VerticalProperty = string.Empty;

            HorizontalAxis = this.ObserveProperty(m => m.HorizontalRange).ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05)).AddTo(Disposables);
            VerticalAxis = this.ObserveProperty(m => m.VerticalRange).ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05)).AddTo(Disposables);
        }

        public ObservableCollection<AlignmentSpotPropertyModel> Spots { get; }

        public IReactiveProperty<AlignmentSpotPropertyModel> TargetSource { get; }

        public Range HorizontalRange {
            get {
                if (!Spots.Any() || HorizontalSelector == null) {
                    return new Range(0, 1);
                }
                var minimum = Spots.Min(HorizontalSelector);
                var maximum = Spots.Max(HorizontalSelector);
                return new Range(minimum, maximum);
            }
        }

        public Range VerticalRange {
            get {
                if (!Spots.Any() || VerticalSelector == null) {
                    return new Range(0, 1);
                }
                var minimum = Spots.Min(VerticalSelector);
                var maximum = Spots.Max(VerticalSelector);
                return new Range(minimum, maximum);
            }
        }

        public IAxisManager<double> HorizontalAxis { get; }
        public IAxisManager<double> VerticalAxis { get; }

        public Func<AlignmentSpotPropertyModel, double> HorizontalSelector {
            get => horizontalSelector;
            set {
                if (SetProperty(ref horizontalSelector, value)) {
                    OnPropertyChanged(nameof(HorizontalRange));
                }
            }
        }
        private Func<AlignmentSpotPropertyModel, double> horizontalSelector;

        public Func<AlignmentSpotPropertyModel, double> VerticalSelector {
            get => verticalSelector;
            set {
                if (SetProperty(ref verticalSelector, value)) {
                    OnPropertyChanged(nameof(VerticalRange));
                }
            }
        }
        private Func<AlignmentSpotPropertyModel, double> verticalSelector;

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle;

        public string HorizontalTitle {
            get => horizontalTitle;
            set => SetProperty(ref horizontalTitle, value);
        }
        private string horizontalTitle;

        public string VerticalTitle {
            get => verticalTitle;
            set => SetProperty(ref verticalTitle, value);
        }
        private string verticalTitle;

        public string HorizontalProperty {
            get => horizontalProperty;
            set => SetProperty(ref horizontalProperty, value);
        }
        private string horizontalProperty;

        public string VerticalProperty {
            get => verticalProperty;
            set => SetProperty(ref verticalProperty, value);
        }
        private string verticalProperty;

        public IObservable<string> LabelSource { get; }

        public BrushMapData<AlignmentSpotPropertyModel> SelectedBrush {
            get => _selectedBrush;
            set => SetProperty(ref _selectedBrush, value);
        }
        private BrushMapData<AlignmentSpotPropertyModel> _selectedBrush;

        public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }
    }
}
