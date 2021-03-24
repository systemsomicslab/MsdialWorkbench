using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Parameter;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class ImmsAnalysisParamSetVM : AnalysisParamSetVM<MsdialImmsParameter>
    {
        public ImmsAnalysisParamSetVM(MsdialImmsParameter parameter, IEnumerable<AnalysisFileBean> files) : base(parameter, files) {
            CcsCalibrationInfoVSs = new ObservableCollection<CcsCalibrationInfoVS>();
            if (parameter.FileID2CcsCoefficients == null) {
                parameter.FileID2CcsCoefficients = new Dictionary<int, CoefficientsForCcsCalculation>();
            }
            foreach (var file in files) {
                if (parameter.FileID2CcsCoefficients.ContainsKey(file.AnalysisFileId)) {
                    CcsCalibrationInfoVSs.Add(new CcsCalibrationInfoVS(file, parameter.FileID2CcsCoefficients[file.AnalysisFileId]));
                }
                else {
                    var calinfo = DataAccess.ReadIonMobilityCalibrationInfo(file.AnalysisFilePath);
                    var coef = parameter.FileID2CcsCoefficients[file.AnalysisFileId] = new CoefficientsForCcsCalculation(calinfo);
                    CcsCalibrationInfoVSs.Add(new CcsCalibrationInfoVS(file, coef));
                }
            }
            if (Param is MsdialImmsParameterVM immsparameter) {
                var rep = parameter.FileID2CcsCoefficients.Values.FirstOrDefault();
                if (rep != null) {
                    if (rep.IsAgilentIM) {
                        immsparameter.IsDTIMS = true;
                    }
                    else if (rep.IsWatersIM) {
                        immsparameter.IsTWIMS = true;
                    }
                    else if (rep.IsBrukerIM) {
                        immsparameter.IsTIMS = true;
                    }
                }
                immsparameter.PropertyChanged += CcsCalibrationInfoVSs_ItemChanged;
            }
        }

        public ObservableCollection<CcsCalibrationInfoVS> CcsCalibrationInfoVSs {
            get => ccsCalibrationInfoVSs;
            set {
                var oldValue = ccsCalibrationInfoVSs;
                if (SetProperty(ref ccsCalibrationInfoVSs, value)) {
                    if (oldValue != null) {
                        oldValue.CollectionChanged -= CcsCalibrationInfoVSs_CollectionChanged;
                    }
                    if (value != null) {
                        value.CollectionChanged += CcsCalibrationInfoVSs_CollectionChanged;
                    }
                    UpdateIsAllCalibrantDataImported(value);
                    OnPropertyChanged(nameof(IsAllCalibrantDataImported));
                }
            }
        }

        private ObservableCollection<CcsCalibrationInfoVS> ccsCalibrationInfoVSs;

        private void CcsCalibrationInfoVSs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (var calinfo in e.NewItems.OfType<INotifyPropertyChanged>()) {
                    calinfo.PropertyChanged += CcsCalibrationInfoVSs_ItemChanged;
                }
            }
            if (e.OldItems != null) {
                foreach (var calinfo in e.OldItems.OfType<INotifyPropertyChanged>()) {
                    calinfo.PropertyChanged -= CcsCalibrationInfoVSs_ItemChanged;
                }
            }
        }

        private void UpdateIsAllCalibrantDataImported(IReadOnlyList<CcsCalibrationInfoVS> vss) {
            if (vss != null) {
                switch (param.IonMobilityType) {
                    case IonMobilityType.Tims:
                        param.IsAllCalibrantDataImported = true;
                        return;
                    case IonMobilityType.Dtims:
                        param.IsAllCalibrantDataImported = vss.All(vs => vs.AgilentBeta != -1 && vs.AgilentTFix != -1);
                        return;
                    case IonMobilityType.Twims:
                        param.IsAllCalibrantDataImported = vss.All(vs => vs.WatersCoefficient != -1 && vs.WatersT0 != -1 && vs.WatersExponent != -1);
                        return;
                }
            }
            param.IsAllCalibrantDataImported = false;
        }

        private void CcsCalibrationInfoVSs_ItemChanged(object sender, PropertyChangedEventArgs e) {
            UpdateIsAllCalibrantDataImported(ccsCalibrationInfoVSs);
            OnPropertyChanged(nameof(IsAllCalibrantDataImported));
        }

        public bool IsAllCalibrantDataImported => param.IsAllCalibrantDataImported;

        protected override async Task<bool> ClosingMethod() {
            var result = await base.ClosingMethod();
            if (!result) {
                return false;
            }

            if (!param.IsAllCalibrantDataImported) {
                var errorMessage = "You have to set the coefficients for all files. ";

                switch (param.IonMobilityType) {
                    case IonMobilityType.Dtims:
                        errorMessage = "For Agilent single fieled-based CCS calculation, you have to set the coefficients for all files. ";
                        break;
                    case IonMobilityType.Twims:
                        errorMessage = "For Waters CCS calculation, you have to set the coefficients for all files. ";
                        break;
                }

                errorMessage += "Otherwise, the Mason–Schamp equation using gasweight=28.0134 and temperature=305.0 is used for CCS calculation for all data. ";
                errorMessage += "Do you continue the CCS parameter setting?";
                if (MessageBox.Show(errorMessage, "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.No) {
                    return false;
                }
            }

            return true;
        }
    }
}
