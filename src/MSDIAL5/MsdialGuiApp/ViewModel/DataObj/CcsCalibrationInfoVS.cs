using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public sealed class CcsCalibrationInfoVS : BindableBase
    {
        public CcsCalibrationInfoVS(AnalysisFileBean file, CoefficientsForCcsCalculation ccsCoef) {
            filePath = file.AnalysisFilePath;
            FileId = file.AnalysisFileId;
            filename = file.AnalysisFileName;
            this.ccsCoef = ccsCoef;
        }

        private readonly CoefficientsForCcsCalculation ccsCoef;

        public string FilePath {
            get => filePath;
            set => SetProperty(ref filePath, value);
        }
        private string filePath;

        public int FileId {
            get => fileId;
            set => SetProperty(ref fileId, value);
        }
        private int fileId;

        public string Filename {
            get => filename;
            set => SetProperty(ref filename, value);
        }
        private string filename;

        public double AgilentBeta {
            get => ccsCoef.AgilentBeta;
            set {
                if (ccsCoef.AgilentBeta != value) {
                    ccsCoef.AgilentBeta = value;
                    OnPropertyChanged(nameof(AgilentBeta));
                }
            }
        }

        public double AgilentTFix {
            get => ccsCoef.AgilentTFix;
            set {
                if (ccsCoef.AgilentTFix != value) {
                    ccsCoef.AgilentTFix = value;
                    OnPropertyChanged(nameof(AgilentTFix));
                }
            }
        }

        public double WatersCoefficient {
            get => ccsCoef.WatersCoefficient;
            set {
                if (ccsCoef.WatersCoefficient != value) {
                    ccsCoef.WatersCoefficient = value;
                    OnPropertyChanged(nameof(WatersCoefficient));
                }
            }
        }

        public double WatersT0 {
            get => ccsCoef.WatersT0;
            set {
                if (ccsCoef.WatersT0 != value) {
                    ccsCoef.WatersT0 = value;
                    OnPropertyChanged(nameof(WatersT0));
                }
            }
        }

        public double WatersExponent {
            get => ccsCoef.WatersExponent;
            set {
                if (ccsCoef.WatersExponent != value) {
                    ccsCoef.WatersExponent = value;
                    OnPropertyChanged(nameof(WatersExponent));
                }
            }
        }
    }
}
