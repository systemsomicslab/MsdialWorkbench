using CompMs.App.Msdial.ViewModel;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Setting
{
    public class MobilitySettingModel : BindableBase
    {
        private readonly MsdialImmsParameter immsParameter;
        private readonly MsdialLcImMsParameter lcimmsParameter;

        public MobilitySettingModel(MsdialImmsParameter parameter, List<AnalysisFileBean> files, ProcessOption process) {
            immsParameter = parameter;
            IsReadOnly = (process & ProcessOption.Alignment) == 0;
            IonMobilityType = parameter.IonMobilityType;

            if (parameter.FileID2CcsCoefficients is null) {
                parameter.FileID2CcsCoefficients = new Dictionary<int, CoefficientsForCcsCalculation>();
            }
            CalibrationInfoCollection = InitializeCalibrationInfoCollection(files, parameter.FileID2CcsCoefficients);
        }

        public MobilitySettingModel(MsdialLcImMsParameter parameter, List<AnalysisFileBean> files, ProcessOption process) {
            lcimmsParameter = parameter;
            IsReadOnly = (process & ProcessOption.Alignment) != 0;
            IonMobilityType = parameter.IonMobilityType;

            if (parameter.FileID2CcsCoefficients is null) {
                parameter.FileID2CcsCoefficients = new Dictionary<int, CoefficientsForCcsCalculation>();
            }
            CalibrationInfoCollection = InitializeCalibrationInfoCollection(files, parameter.FileID2CcsCoefficients);
        }

        public bool IsReadOnly { get; }

        public IonMobilityType IonMobilityType {
            get => ionMobilityType;
            set => SetProperty(ref ionMobilityType, value);
        }
        private IonMobilityType ionMobilityType;

        public ReadOnlyCollection<CcsCalibrationInfoVS> CalibrationInfoCollection { get; }

        public void Commit() {
            if (IsReadOnly) {
                return;
            }
            if (immsParameter != null) {
                immsParameter.IonMobilityType = IonMobilityType;
            }
            else if (lcimmsParameter != null) {
                lcimmsParameter.IonMobilityType = IonMobilityType;
            }
        }

        private static ReadOnlyCollection<CcsCalibrationInfoVS> InitializeCalibrationInfoCollection(List<AnalysisFileBean> files, Dictionary<int, CoefficientsForCcsCalculation> fileID2CcsCoefficients) {
            var calibrationInfoCollection = new List<CcsCalibrationInfoVS>();
            foreach (var file in files) {
                if (!fileID2CcsCoefficients.TryGetValue(file.AnalysisFileId, out var coef)) {
                    var calinfo = DataAccess.ReadIonMobilityCalibrationInfo(file.AnalysisFilePath) ?? new RawCalibrationInfo();
                    coef = fileID2CcsCoefficients[file.AnalysisFileId] = new CoefficientsForCcsCalculation(calinfo);
                }
                calibrationInfoCollection.Add(new CcsCalibrationInfoVS(file, coef));
            }
            return calibrationInfoCollection.AsReadOnly();
        }
    }
}
