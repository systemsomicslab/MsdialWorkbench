using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    class EicModel : ValidatableBase
    {
        public EicModel(IObservable<ChromatogramPeakFeatureModel> targetSource, EicLoader loader, string graphTitle, string horizontalTitle, string verticalTitle) {
            Loader = loader;

            GraphTitle = graphTitle;
            HorizontalTitle = horizontalTitle;
            VerticalTitle = verticalTitle;

            HorizontalProperty = nameof(ChromatogramPeakWrapper.ChromXValue);
            VerticalProperty = nameof(ChromatogramPeakWrapper.Intensity);

            var sources = targetSource.Select(t => loader.LoadEicAsync(t, default)).Switch();

            EicSource = sources.Select(src => src.Item1);
            EicPeakSource = sources.Select(src => src.Item2);
            EicFocusedSource = sources.Select(src => src.Item3);

            MaxIntensitySource = EicPeakSource.Select(src => src.Any() ? src.Max(peak => peak.Intensity) : 0);
            ChromRangeSource = EicSource.Select(src => src.Any() ? new Range(src.Min(peak => peak.ChromXValue) ?? 0, src.Max(peak => peak.ChromXValue) ?? 1) : new Range(0, 1));
            AbundanceRangeSource = EicFocusedSource.Select(src => src.Any() ? new Range(0, src.Max(peak => peak.Intensity)) : new Range(0, 1));
        }

        public EicModel(IObservable<ChromatogramPeakFeatureModel> targetSource, EicLoader loader)
            : this(targetSource, loader, string.Empty, string.Empty, string.Empty) {

        }

        public EicLoader Loader { get; }

        public IObservable<List<ChromatogramPeakWrapper>> EicSource { get; }

        public IObservable<List<ChromatogramPeakWrapper>> EicPeakSource { get; }

        public IObservable<List<ChromatogramPeakWrapper>> EicFocusedSource { get; }

        public IObservable<double> MaxIntensitySource { get; }
        public IObservable<Range> ChromRangeSource { get; }
        public IObservable<Range> AbundanceRangeSource { get; }

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

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle;

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
