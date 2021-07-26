using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    class AnalysisPeakPlotModel : BindableBase
    {
        public AnalysisPeakPlotModel(
            ObservableCollection<ChromatogramPeakFeatureModel> spots,
            Func<ChromatogramPeakFeatureModel, double> horizontalSelector,
            Func<ChromatogramPeakFeatureModel, double> verticalSelector,
            IReactiveProperty<ChromatogramPeakFeatureModel> targetSource,
            IObservable<string> labelSource) {
            if (spots is null) {
                throw new ArgumentNullException(nameof(spots));
            }

            if (targetSource is null) {
                throw new ArgumentNullException(nameof(targetSource));
            }

            if (labelSource is null) {
                throw new ArgumentNullException(nameof(labelSource));
            }

            Spots = spots;
            HorizontalSelector = horizontalSelector ?? throw new ArgumentNullException(nameof(horizontalSelector));
            VerticalSelector = verticalSelector ?? throw new ArgumentNullException(nameof(verticalSelector));
            LabelSource = labelSource;
            TargetSource = targetSource;
            GraphTitle = string.Empty;
            HorizontalTitle = string.Empty;
            VerticalTitle = string.Empty;
            HorizontalProperty = string.Empty;
            VerticalProperty = string.Empty;
        }

        public ObservableCollection<ChromatogramPeakFeatureModel> Spots { get; }

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

        public IReactiveProperty<ChromatogramPeakFeatureModel> TargetSource { get; }

        public Func<ChromatogramPeakFeatureModel, double> HorizontalSelector {
            get => horizontalSelector;
            set {
                if (SetProperty(ref horizontalSelector, value)) {
                    OnPropertyChanged(nameof(HorizontalRange));
                }
            }
        }
        private Func<ChromatogramPeakFeatureModel, double> horizontalSelector;

        public Func<ChromatogramPeakFeatureModel, double> VerticalSelector {
            get => verticalSelector;
            set {
                if (SetProperty(ref verticalSelector, value)) {
                    OnPropertyChanged(nameof(VerticalRange));
                }
            }
        }
        private Func<ChromatogramPeakFeatureModel, double> verticalSelector;

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
