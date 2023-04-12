using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class GcmsPeakDetectionSettingModel : BindableBase
    {
        private readonly PeakPickBaseParameter _peakPickBaseParameter;
        private readonly ChromDecBaseParameter _chromDecBaseParameter;

        public GcmsPeakDetectionSettingModel(PeakPickBaseParameter peackPickBaseParameter, ChromDecBaseParameter chromDecBaseParameter, ProcessOption process) {
            _peakPickBaseParameter = peackPickBaseParameter;
            _chromDecBaseParameter = chromDecBaseParameter;
            IsReadOnly = (process & ProcessOption.PeakSpotting) == 0;
            
            AccuracyType = chromDecBaseParameter.AccuracyType;
            MinimumAmplitude = peackPickBaseParameter.MinimumAmplitude;
            MassSliceWidth = peackPickBaseParameter.MassSliceWidth;
            SmoothingMethod = peackPickBaseParameter.SmoothingMethod;
            SmoothingLevel = peackPickBaseParameter.SmoothingLevel;
            MinimumDatapoints = peackPickBaseParameter.MinimumDatapoints;
            ExcludedMassList = new ObservableCollection<MzSearchQuery>(
                peackPickBaseParameter.ExcludedMassList.Select(
                    query => new MzSearchQuery {
                        Mass = query.Mass,
                        RelativeIntensity = query.RelativeIntensity,
                        SearchType = query.SearchType,
                        MassTolerance = query.MassTolerance,
                    }));
        }

        public bool IsReadOnly { get; }

        public double MinimumAmplitude {
            get => _minimumAmplitude;
            set => SetProperty(ref _minimumAmplitude, value);
        }
        private double _minimumAmplitude;

        public AccuracyType AccuracyType {
            get => _accuracyType;
            set {
                if (SetProperty(ref _accuracyType, value)) {
                    OnPropertyChanged(nameof(IsAccurateMS));
                }
            }
        }
        private AccuracyType _accuracyType;

        public bool IsAccurateMS {
            get {
                return AccuracyType == AccuracyType.IsAccurate;
            }
            set {
                if (value) {
                    AccuracyType = AccuracyType.IsAccurate;
                }
                else {
                    AccuracyType = AccuracyType.IsNominal;
                }
            }
        }

        public float MassSliceWidth {
            get => _massSliceWidth;
            set => SetProperty(ref _massSliceWidth, value);
        }
        private float _massSliceWidth;

        public SmoothingMethod SmoothingMethod {
            get => _smoothingMethod;
            set => SetProperty(ref _smoothingMethod, value);
        }
        private SmoothingMethod _smoothingMethod;

        public int SmoothingLevel {
            get => _smoothingLevel; 
            set => SetProperty(ref _smoothingLevel, value); 
        }
        private int _smoothingLevel;

        public double MinimumDatapoints {
            get => _minimumDataPoints;
            set => SetProperty(ref _minimumDataPoints, value);
        }
        private double _minimumDataPoints;

        public ObservableCollection<MzSearchQuery> ExcludedMassList { get; }

        public void AddQuery(double mass, double tolerance) {
            ExcludedMassList.Add(new MzSearchQuery { Mass = mass, MassTolerance = tolerance });
        }

        public void RemoveQuery(MzSearchQuery query) {
            ExcludedMassList.Remove(query);
        }

        public void Commit() {
            if (IsReadOnly) {
                return;
            }
            _peakPickBaseParameter.MinimumAmplitude = MinimumAmplitude;
            _chromDecBaseParameter.AccuracyType = AccuracyType;
            _peakPickBaseParameter.MassSliceWidth = MassSliceWidth;
            _peakPickBaseParameter.SmoothingMethod = SmoothingMethod;
            _peakPickBaseParameter.SmoothingLevel = SmoothingLevel;
            _peakPickBaseParameter.MinimumDatapoints = MinimumDatapoints;
            _peakPickBaseParameter.ExcludedMassList = ExcludedMassList.ToList();
        }

        public void LoadParameter(PeakPickBaseParameter parameter) {
            if (IsReadOnly) {
                return;
            }
            MinimumAmplitude = parameter.MinimumAmplitude;
            AccuracyType = _chromDecBaseParameter.AccuracyType;
            MassSliceWidth = parameter.MassSliceWidth;
            SmoothingMethod = parameter.SmoothingMethod;
            SmoothingLevel = parameter.SmoothingLevel;
            MinimumDatapoints = parameter.MinimumDatapoints;
            ExcludedMassList.Clear();
            foreach (var query in parameter.ExcludedMassList) {
                ExcludedMassList.Add(new MzSearchQuery
                {
                    Mass = query.Mass,
                    RelativeIntensity = query.RelativeIntensity,
                    SearchType = query.SearchType,
                    MassTolerance = query.MassTolerance,
                });
            }
        }
    }
}
