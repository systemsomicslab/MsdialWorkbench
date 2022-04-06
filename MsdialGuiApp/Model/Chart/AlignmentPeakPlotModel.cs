using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    class AlignmentPeakPlotModel : BindableBase
    {
        public AlignmentPeakPlotModel(
            ObservableCollection<AlignmentSpotPropertyModel> spots,
            Func<AlignmentSpotPropertyModel, double> horizontalSelector,
            Func<AlignmentSpotPropertyModel, double> verticalSelector,
            IReactiveProperty<AlignmentSpotPropertyModel> targetSource,
            IObservable<string> labelSource) {
            if (spots is null) {
                throw new ArgumentNullException(nameof(spots));
            }

            if (horizontalSelector is null) {
                throw new ArgumentNullException(nameof(horizontalSelector));
            }

            if (verticalSelector is null) {
                throw new ArgumentNullException(nameof(verticalSelector));
            }

            if (targetSource is null) {
                throw new ArgumentNullException(nameof(targetSource));
            }

            if (labelSource is null) {
                throw new ArgumentNullException(nameof(labelSource));
            }

            Spots = spots;
            TargetSource = targetSource;

            HorizontalSelector = horizontalSelector;
            VerticalSelector = verticalSelector;

            LabelSource = labelSource;

            GraphTitle = string.Empty;
            HorizontalTitle = string.Empty;
            VerticalTitle = string.Empty;
            HorizontalProperty = string.Empty;
            VerticalProperty = string.Empty;

            HorizontalAxis = this.ObserveProperty(m => m.HorizontalRange).ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05));
            VerticalAxis = this.ObserveProperty(m => m.VerticalRange).ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05));
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
    }
}
