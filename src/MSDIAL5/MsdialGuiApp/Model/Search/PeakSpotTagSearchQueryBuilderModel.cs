using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Search
{
    public sealed class PeakSpotTagSearchQueryBuilderModel : BindableBase
    {
        public bool Confirmed {
            get => _confirmed;
            set => SetProperty(ref _confirmed, value);
        }
        private bool _confirmed;

        public bool LowQualitySpectrum {
            get => _lowQualitySpectrum;
            set => SetProperty(ref _lowQualitySpectrum, value);
        }
        private bool _lowQualitySpectrum;

        public bool Misannotation {
            get => _misannotation;
            set => SetProperty(ref _misannotation, value);
        }
        private bool _misannotation;

        public bool Coelution {
            get => _coelution;
            set => SetProperty(ref _coelution, value);
        }
        private bool _coelution;

        public bool Overannotation {
            get => _overannotation;
            set => SetProperty(ref _overannotation, value);
        }
        private bool _overannotation;

        public bool Excludes {
            get => _excludes;
            set => SetProperty(ref _excludes, value);
        }
        private bool _excludes;

        public PeakSpotTagSearchQuery CreateQuery() {
            var tags = new List<PeakSpotTag>();
            if (Confirmed) {
                tags.Add(PeakSpotTag.CONFIRMED);
            }
            if (LowQualitySpectrum) {
                tags.Add(PeakSpotTag.LOW_QUALITY_SPECTRUM);
            }
            if (Misannotation) {
                tags.Add(PeakSpotTag.MISANNOTATION);
            }
            if (Coelution) {
                tags.Add(PeakSpotTag.COELUTION);
            }
            if (Overannotation) {
                tags.Add(PeakSpotTag.OVERANNOTATION);
            }

            if (Excludes) {
                return PeakSpotTagSearchQuery.None(tags.ToArray());
            }
            else {
                return PeakSpotTagSearchQuery.Any(tags.ToArray());
            }
        }

        public bool Filter(IFilterable peak) {
            return peak.TagCollection.IsSelected(CreateQuery());
        }
    }
}
