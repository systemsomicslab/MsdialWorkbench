using CompMs.CommonMVVM;
using System.Collections;

namespace CompMs.App.Msdial.Model.Search
{
    public class PeakSpotNavigatorModel : BindableBase
    {
        public PeakSpotNavigatorModel(IList peakSpots, PeakFilterModel peakFilterModel) {
            PeakSpots = peakSpots ?? throw new System.ArgumentNullException(nameof(peakSpots));
            PeakFilterModel = peakFilterModel ?? throw new System.ArgumentNullException(nameof(peakFilterModel));
            AmplitudeLowerValue = 0d;
            AmplitudeUpperValue = 1d;
        }

        public string SelectedAnnotationLabel {
            get => selectedAnnotationLabel;
            set => SetProperty(ref selectedAnnotationLabel, value);
        }
        private string selectedAnnotationLabel;

        public IList PeakSpots { get; }

        public double AmplitudeLowerValue {
            get => amplitudeLowerValue;
            set => SetProperty(ref amplitudeLowerValue, value);
        }
        private double amplitudeLowerValue;
        public double AmplitudeUpperValue { 
            get => amplitudeUpperValue;
            set => SetProperty(ref amplitudeUpperValue, value);
        }
        private double amplitudeUpperValue;

        public PeakFilterModel PeakFilterModel { get; }
    }
}
