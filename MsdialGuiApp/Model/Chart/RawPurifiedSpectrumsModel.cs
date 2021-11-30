using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart {
    class RawPurifiedSpectrumsModel : BindableBase {
        
        public RawPurifiedSpectrumsModel(
            IObservable<ChromatogramPeakFeatureModel> targetSource,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> rawLoader,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> decLoader,
            Func<SpectrumPeak, double> horizontalSelector,
            Func<SpectrumPeak, double> verticalSelector) {

            RawLoader = rawLoader;
            DecLoader = decLoader;
            var upperSpectrum = targetSource.Select(target => RawLoader.LoadSpectrum(target));
            var lowerSpectrum = targetSource.Select(target => DecLoader.LoadSpectrum(target));

            if (upperSpectrum is null) {
                throw new ArgumentNullException(nameof(upperSpectrum));
            }

            if (lowerSpectrum is null) {
                throw new ArgumentNullException(nameof(lowerSpectrum));
            }

            UpperSpectrumSource = upperSpectrum;
            LowerSpectrumSource = lowerSpectrum;
            HorizontalSelector = horizontalSelector ?? throw new ArgumentNullException(nameof(horizontalSelector));
            VerticalSelector = verticalSelector ?? throw new ArgumentNullException(nameof(verticalSelector));

            var upperEmpty = upperSpectrum.Where(spec => spec is null || !spec.Any());
            var upperAny = upperSpectrum.Where(spec => spec?.Any() ?? false);
            UpperVerticalRangeSource = upperAny
                .Select(spec => new Range(spec.Min(VerticalSelector), spec.Max(VerticalSelector)))
                .Merge(upperEmpty.Select(_ => new Range(0, 1)));
            var lowerEmpty = lowerSpectrum.Where(spec => spec is null || !spec.Any());
            var lowerAny = lowerSpectrum.Where(spec => spec?.Any() ?? false);
            LowerVerticalRangeSource = lowerAny
                .Select(spec => new Range(spec.Min(VerticalSelector), spec.Max(VerticalSelector)))
                .Merge(lowerEmpty.Select(_ => new Range(0, 1)));

            HorizontalRangeSource = new[]
            {
                upperSpectrum.Where(spec => !(spec is null)).StartWith(new List<SpectrumPeak>(0)),
                lowerSpectrum.Where(spec => !(spec is null)).StartWith(new List<SpectrumPeak>(0)),
            }.CombineLatest(specs => specs.SelectMany(spec => spec))
            .Where(spec => spec.Any())
            .Select(spec => new Range(spec.Min(HorizontalSelector), spec.Max(HorizontalSelector)));

            UpperSpectrumSource.Subscribe(spectrum => UpperSpectrum = spectrum);
            LowerSpectrumSource.Subscribe(spectrum => LowerSpectrum = spectrum);
        }

        public IMsSpectrumLoader<ChromatogramPeakFeatureModel> RawLoader { get; }

        public IMsSpectrumLoader<ChromatogramPeakFeatureModel> DecLoader { get; }

        public IObservable<List<SpectrumPeak>> UpperSpectrumSource { get; }

        public IObservable<List<SpectrumPeak>> LowerSpectrumSource { get; }

        public List<SpectrumPeak> UpperSpectrum {
            get => upperSpectrum;
            set {
                if (SetProperty(ref upperSpectrum, value)) {
                    OnPropertyChanged(nameof(HorizontalRange));
                    OnPropertyChanged(nameof(UpperVerticalRange));
                }
            }
        }
        private List<SpectrumPeak> upperSpectrum;

        public List<SpectrumPeak> LowerSpectrum {
            get => lowerSpectrum;
            set {
                if (SetProperty(ref lowerSpectrum, value)) {
                    OnPropertyChanged(nameof(HorizontalRange));
                    OnPropertyChanged(nameof(LowerVerticalRange));
                }
            }
        }
        private List<SpectrumPeak> lowerSpectrum;

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

        public string LabelProperty {
            get => labelProperty;
            set => SetProperty(ref labelProperty, value);
        }
        private string labelProperty;

        public string OrderingProperty {
            get => orderingProperty;
            set => SetProperty(ref orderingProperty, value);
        }
        private string orderingProperty;

        public Func<SpectrumPeak, double> HorizontalSelector { get; }
        public Func<SpectrumPeak, double> VerticalSelector { get; }

        public IObservable<Range> HorizontalRangeSource { get; }

        public IObservable<Range> UpperVerticalRangeSource { get; }

        public IObservable<Range> LowerVerticalRangeSource { get; }

        public Range HorizontalRange {
            get {
                if ((UpperSpectrum.Any() || LowerSpectrum.Any()) && HorizontalSelector != null) {
                    var minimum = UpperSpectrum.Concat(LowerSpectrum).Min(HorizontalSelector);
                    var maximum = UpperSpectrum.Concat(LowerSpectrum).Max(HorizontalSelector);
                    return new Range(minimum, maximum);
                }
                return new Range(0, 1);
            }
        }

        public Range UpperVerticalRange {
            get {
                if (UpperSpectrum.Any() && VerticalSelector != null) {
                    var minimum = UpperSpectrum.Min(VerticalSelector);
                    var maximum = UpperSpectrum.Max(VerticalSelector);
                    return new Range(minimum, maximum);
                }
                return new Range(0, 1);
            }
        }

        public Range LowerVerticalRange {
            get {
                if (LowerSpectrum.Any() && VerticalSelector != null) {
                    var minimum = LowerSpectrum.Min(VerticalSelector);
                    var maximum = LowerSpectrum.Max(VerticalSelector);
                    return new Range(minimum, maximum);
                }
                return new Range(0, 1);
            }
        }

    }
}
