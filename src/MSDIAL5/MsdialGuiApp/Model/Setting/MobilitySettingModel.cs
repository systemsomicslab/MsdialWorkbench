using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using Reactive.Bindings.Notifiers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace CompMs.App.Msdial.Model.Setting
{
    public class MobilitySettingModel : BindableBase
    {
        private readonly MsdialImmsParameter? immsParameter;
        private readonly MsdialLcImMsParameter? lcimmsParameter;

        public MobilitySettingModel(MsdialImmsParameter parameter, List<AnalysisFileBean> files, ProcessOption process) {
            immsParameter = parameter;
            IsReadOnly = false;
            IonMobilityType = parameter.IonMobilityType;

            if (parameter.FileID2CcsCoefficients is null) {
                parameter.FileID2CcsCoefficients = new Dictionary<int, CoefficientsForCcsCalculation>();
            }
            CalibrationInfoCollection = InitializeCalibrationInfoCollection(files, parameter.FileID2CcsCoefficients);
        }

        public MobilitySettingModel(MsdialLcImMsParameter parameter, List<AnalysisFileBean> files, ProcessOption process) {
            lcimmsParameter = parameter;
            IsReadOnly = false;
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

        public bool IsAllCalibrantDataImported {
            get => isAllCalibrantDataImported;
            set => SetProperty(ref isAllCalibrantDataImported, value);
        }
        private bool isAllCalibrantDataImported;

        public ReadOnlyCollection<CcsCalibrationInfoVS> CalibrationInfoCollection { get; }

        public bool TryCommit() {
            if (IsReadOnly) {
                return true;
            }
            if (!ShowMessageIfHasError()) {
                return false;
            }
            if (immsParameter != null) {
                immsParameter.IonMobilityType = IonMobilityType;
            }
            else if (lcimmsParameter != null) {
                lcimmsParameter.IonMobilityType = IonMobilityType;
            }
            return true;
        }

        private bool ShowMessageIfHasError() {
            if (!IsAllCalibrantDataImported) {
                var errorMessages = new List<string>();
                errorMessages.Add("You have to set the coefficients for all files.");

                switch (IonMobilityType) {
                    case IonMobilityType.Dtims:
                        errorMessages.Add("For Agilent single fieled-based CCS calculation, you have to set the coefficients for all files.");
                        break;
                    case IonMobilityType.Twims:
                        errorMessages.Add("For Waters CCS calculation, you have to set the coefficients for all files.");
                        break;
                }

                errorMessages.Add("Otherwise, the Mason–Schamp equation using gasweight=28.0134 and temperature=305.0 is used for CCS calculation for all data.");
                errorMessages.Add("Do you continue the CCS parameter setting?");
                var request = new ErrorMessageBoxRequest()
                {
                    Caption = "Error",
                    Content = string.Join(" ", errorMessages),
                    ButtonType = MessageBoxButton.YesNo,
                };
                MessageBroker.Default.Publish(request);
                if (request.Result != MessageBoxResult.No) {
                    return false;
                }
            }
            return true;
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
