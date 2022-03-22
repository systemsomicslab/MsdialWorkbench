using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    public class PeakDetectionSettingModel : BindableBase
    {
        private readonly PeakPickBaseParameter parameter;

        public PeakDetectionSettingModel(PeakPickBaseParameter parameter, ProcessOption process) {
            this.parameter = parameter;
            IsReadOnly = (process & ProcessOption.PeakSpotting) == 0;
            MinimumAmplitude = parameter.MinimumAmplitude;
            MassSliceWidth = parameter.MassSliceWidth;
            SmoothingMethod = parameter.SmoothingMethod;
            SmoothingLevel = parameter.SmoothingLevel;
            MinimumDatapoints = parameter.MinimumDatapoints;
            ExcludedMassList = new ObservableCollection<MzSearchQuery>(
                parameter.ExcludedMassList.Select(
                    query => new MzSearchQuery {
                        Mass = query.Mass,
                        RelativeIntensity = query.RelativeIntensity,
                        SearchType = query.SearchType,
                        MassTolerance = query.MassTolerance,
                    }));
        }

        public bool IsReadOnly { get; }

        public double MinimumAmplitude {
            get => minimumAmplitude;
            set => SetProperty(ref minimumAmplitude, value);
        }
        private double minimumAmplitude;

        public float MassSliceWidth {
            get => massSliceWidth;
            set => SetProperty(ref massSliceWidth, value);
        }
        private float massSliceWidth;

        public SmoothingMethod SmoothingMethod {
            get => smoothingMethod;
            set => SetProperty(ref smoothingMethod, value);
        }
        private SmoothingMethod smoothingMethod;

        public int SmoothingLevel {
            get => smoothingLevel; 
            set => SetProperty(ref smoothingLevel, value); 
        }
        private int smoothingLevel;

        public double MinimumDatapoints {
            get => minimumDataPoints;
            set => SetProperty(ref minimumDataPoints, value);
        }
        private double minimumDataPoints;

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
            parameter.MinimumAmplitude = MinimumAmplitude;
            parameter.MassSliceWidth = MassSliceWidth;
            parameter.SmoothingMethod = SmoothingMethod;
            parameter.SmoothingLevel = SmoothingLevel;
            parameter.MinimumDatapoints = MinimumDatapoints;
            parameter.ExcludedMassList = ExcludedMassList.ToList();
        }
    }
}
