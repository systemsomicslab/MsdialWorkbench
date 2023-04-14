using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class PeakPickSettingModel : BindableBase {
        private readonly PeakPickBaseParameter _parameter;

        public PeakPickSettingModel(PeakPickBaseParameter parameter) {
            _parameter = parameter;
            MinimumAmplitude = parameter.MinimumAmplitude;
            MassSliceWidth = parameter.MassSliceWidth;
            SmoothingMethod = parameter.SmoothingMethod;
            SmoothingLevel = parameter.SmoothingLevel;
            MinimumDatapoints = parameter.MinimumDatapoints;
            CentroidMs1Tolerance = parameter.CentroidMs1Tolerance;
            ExcludedMassList = new ObservableCollection<MzSearchQuery>(
                parameter.ExcludedMassList.Select(
                    query => new MzSearchQuery {
                        Mass = query.Mass,
                        RelativeIntensity = query.RelativeIntensity,
                        SearchType = query.SearchType,
                        MassTolerance = query.MassTolerance,
                    }));
        }

        public double MinimumAmplitude {
            get => _minimumAmplitude;
            set => SetProperty(ref _minimumAmplitude, value);
        }
        private double _minimumAmplitude;

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

        public float CentroidMs1Tolerance {
            get => _centroidMs1Tolerance;
            set => SetProperty(ref _centroidMs1Tolerance, value);
        }
        private float _centroidMs1Tolerance;

        public ObservableCollection<MzSearchQuery> ExcludedMassList { get; }

        public void AddQuery(double mass, double tolerance) {
            ExcludedMassList.Add(new MzSearchQuery { Mass = mass, MassTolerance = tolerance });
        }

        public void RemoveQuery(MzSearchQuery query) {
            ExcludedMassList.Remove(query);
        }

        public void Commit() {
            _parameter.MinimumAmplitude = MinimumAmplitude;
            _parameter.MassSliceWidth = MassSliceWidth;
            _parameter.SmoothingMethod = SmoothingMethod;
            _parameter.SmoothingLevel = SmoothingLevel;
            _parameter.MinimumDatapoints = MinimumDatapoints;
            _parameter.CentroidMs1Tolerance = CentroidMs1Tolerance;
            _parameter.ExcludedMassList = ExcludedMassList.ToList();
        }

        public void LoadParameter(PeakPickBaseParameter parameter) {
            MinimumAmplitude = parameter.MinimumAmplitude;
            MassSliceWidth = parameter.MassSliceWidth;
            SmoothingMethod = parameter.SmoothingMethod;
            SmoothingLevel = parameter.SmoothingLevel;
            MinimumDatapoints = parameter.MinimumDatapoints;
            CentroidMs1Tolerance = parameter.CentroidMs1Tolerance;
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
