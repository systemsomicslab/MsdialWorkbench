using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    class MsSpectrumModel<T> : BindableBase
    {
        public MsSpectrumModel(
            Func<SpectrumPeak, double> horizontalSelector,
            Func<SpectrumPeak, double> verticalSelector) {

            UpperSpectrum = new List<SpectrumPeak>(0);
            LowerSpectrum = new List<SpectrumPeak>(0);

            HorizontalSelector = horizontalSelector ?? throw new ArgumentNullException(nameof(horizontalSelector));
            VerticalSelector = verticalSelector ?? throw new ArgumentNullException(nameof(verticalSelector));
        }

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
