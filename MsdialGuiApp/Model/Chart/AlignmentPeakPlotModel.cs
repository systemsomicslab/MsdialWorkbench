using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    class AlignmentPeakPlotModel : BindableBase
    {
        public AlignmentPeakPlotModel(
            ObservableCollection<AlignmentSpotPropertyModel> spots,
            Func<AlignmentSpotPropertyModel, double> horizontalSelector,
            Func<AlignmentSpotPropertyModel, double> verticalSelector) {
            if (spots is null) {
                throw new ArgumentNullException(nameof(spots));
            }

            if (horizontalSelector is null) {
                throw new ArgumentNullException(nameof(horizontalSelector));
            }

            if (verticalSelector is null) {
                throw new ArgumentNullException(nameof(verticalSelector));
            }

            Spots = spots;
            Target = null;

            HorizontalSelector = horizontalSelector;
            VerticalSelector = verticalSelector;

            GraphTitle = string.Empty;
            HorizontalTitle = string.Empty;
            VerticalTitle = string.Empty;
            HorizontalProperty = string.Empty;
            VerticalProperty = string.Empty;
        }

        public AlignmentPeakPlotModel(
            IEnumerable<AlignmentSpotPropertyModel> spots,
            Func<AlignmentSpotPropertyModel, double> horizontalSelector,
            Func<AlignmentSpotPropertyModel, double> verticalSelector)
            : this(new ObservableCollection<AlignmentSpotPropertyModel>(spots), horizontalSelector, verticalSelector) {
        }

        public ObservableCollection<AlignmentSpotPropertyModel> Spots { get; }

        // nullable value
        public AlignmentSpotPropertyModel Target {
            get => target;
            set => SetProperty(ref target, value);
        }
        private AlignmentSpotPropertyModel target;

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
    }
}
