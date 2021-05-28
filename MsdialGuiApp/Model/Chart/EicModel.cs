using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart
{
    class EicModel : ValidatableBase
    {
        public EicModel(EicLoader loader)
            : this(loader, string.Empty, string.Empty, string.Empty) {
            
        }

        public EicModel(EicLoader loader, string graphTitle)
            : this(loader, graphTitle, string.Empty, string.Empty) {
            
        }

        public EicModel(EicLoader loader, string graphTitle, string horizontalTitle, string verticalTitle) {

            Loader = loader;

            GraphTitle = graphTitle;
            HorizontalTitle = horizontalTitle;
            VerticalTitle = verticalTitle;

            Eic = new List<ChromatogramPeakWrapper>();
            EicPeak = new List<ChromatogramPeakWrapper>();
            EicFocused = new List<ChromatogramPeakWrapper>();

            HorizontalProperty = nameof(ChromatogramPeakWrapper.ChromXValue);
            VerticalProperty = nameof(ChromatogramPeakWrapper.Intensity);
        }

        public EicLoader Loader { get; }

        public List<ChromatogramPeakWrapper> Eic {
            get => eic;
            set => SetProperty(ref eic, value);
        }
        private List<ChromatogramPeakWrapper> eic;

        public List<ChromatogramPeakWrapper> EicPeak {
            get => eicPeak;
            set {
                if (SetProperty(ref eicPeak, value)) {
                    OnPropertyChanged(nameof(ChromRange));
                }
            }
        }
        private List<ChromatogramPeakWrapper> eicPeak;

        public List<ChromatogramPeakWrapper> EicFocused {
            get => eicFocused;
            set {
                if (SetProperty(ref eicFocused, value)) {
                    OnPropertyChanged(nameof(AbundanceRange));
                }
            }
        }
        private List<ChromatogramPeakWrapper> eicFocused;

        public double MaxIntensity {
            get {
                if (!Eic.Any()) {
                    return 0;
                }
                return Eic.Max(peak => peak.Intensity);
            }
        }

        public Range ChromRange {
            get {
                if (!Eic.Any()) {
                    return new Range(0, 1);
                }
                var minimum = Eic.Min(peak => peak.ChromXValue);
                var maximum = Eic.Max(peak => peak.ChromXValue);
                return new Range(minimum ?? 0, maximum ?? 1);
            }
        }

        public Range AbundanceRange {
            get {
                if (!Eic.Any()) {
                    return new Range(0, 1);
                }
                return new Range(0, EicFocused.Max(peak => peak.Intensity));
            }
        }

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

        public async Task LoadEicAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            (Eic, EicPeak, EicFocused) = await Loader.LoadEicAsync(target, token);
        }

        public void LoadEic(ChromatogramPeakFeatureModel target) {
            (Eic, EicPeak, EicFocused) = Loader.LoadEic(target);
        }
    }
}
