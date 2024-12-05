using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class GcmsDataCollectionSettingModel : BindableBase, IDataCollectionSettingModel
    {
        private readonly ParameterBase _parameter;

        public GcmsDataCollectionSettingModel(ParameterBase parameter, PeakPickBaseParameterModel peakPickBaseParameterModel, ProcessOption process) {
            _parameter = parameter;
            IsReadOnly = (process & ProcessOption.PeakSpotting) == 0;

            MassRange = new Ms1CollectionRangeSetting(peakPickBaseParameterModel, needAccmulation: false);
            RtRange = new RetentionTimeCollectionRangeSetting(peakPickBaseParameterModel, needAccmulation: false);
            NumberOfThreads = parameter.ProcessBaseParam.NumThreads;
        }

        public bool IsReadOnly { get; }

        public Ms1CollectionRangeSetting MassRange { get; }

        public RetentionTimeCollectionRangeSetting RtRange { get; }

        public int NumberOfThreads {
            get => _numberOfThreads;
            set => SetProperty(ref _numberOfThreads, value);
        }
        private int _numberOfThreads;

        public bool TryCommit() {
            if (IsReadOnly) {
                return false;
            }
            _parameter.ProcessBaseParam.NumThreads = NumberOfThreads;
            MassRange.Commit();
            RtRange.Commit();
            return true;
        }

        public void LoadParameter(ParameterBase parameter) {
            if (IsReadOnly) {
                return;
            }

            MassRange.Update(parameter);
            RtRange.Update(parameter);
            NumberOfThreads = parameter.ProcessBaseParam.NumThreads;
        }
    }
}
