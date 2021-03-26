using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel
{
    class MsdialProjectParameterFactory
    {
        public static ParameterBaseVM Create<T>(T innerModel) where T: ParameterBase {
            switch (innerModel) {
                case MsdialImmsParameter imms:
                    return new MsdialImmsParameterVM(imms);
                default:
                    return new ParameterBaseVM(innerModel);
            }
        }
    }

    class MsdialImmsParameterVM : ParameterBaseVM
    {
        protected new readonly MsdialImmsParameter innerModel;
        public MsdialImmsParameterVM(MsdialImmsParameter innerModel) : base(innerModel) {
            this.innerModel = innerModel;
        }

        public float DriftTimeBegin {
            get => innerModel.DriftTimeBegin;
            set {
                if (innerModel.DriftTimeBegin == value) return;
                innerModel.DriftTimeBegin = value;
                OnPropertyChanged(nameof(DriftTimeBegin));

            }
        }

        public float DriftTimeEnd {
            get => innerModel.DriftTimeEnd;
            set {
                if (innerModel.DriftTimeEnd == value) return;
                innerModel.DriftTimeEnd = value;
                OnPropertyChanged(nameof(DriftTimeEnd));
            }
        }

        public IonMobilityType IonMobilityType {
            get => innerModel.IonMobilityType;
            set {
                if (innerModel.IonMobilityType == value) return;
                innerModel.IonMobilityType = value;
                OnPropertyChanged(nameof(IonMobilityType));
                OnPropertyChanged(nameof(IsTIMS));
                OnPropertyChanged(nameof(IsDTIMS));
                OnPropertyChanged(nameof(IsTWIMS));
            }
        }

        public bool IsTIMS {
            get => IonMobilityType == IonMobilityType.Tims;
            set {
                if (value) {
                    IonMobilityType = IonMobilityType.Tims;
                }
            }
        }

        public bool IsDTIMS {
            get => IonMobilityType == IonMobilityType.Dtims;
            set {
                if (value) {
                    IonMobilityType = IonMobilityType.Dtims;
                }
            }
        }

        public bool IsTWIMS {
            get => IonMobilityType == IonMobilityType.Twims;
            set {
                if (value) {
                    IonMobilityType = IonMobilityType.Twims;
                }
            }
        }

        public float DriftTimeAlignmentTolerance {
            get => innerModel.DriftTimeAlignmentTolerance;
            set {
                if (innerModel.DriftTimeAlignmentTolerance == value) return;
                innerModel.DriftTimeAlignmentTolerance = value;
                OnPropertyChanged(nameof(DriftTimeAlignmentTolerance));
            }
        }

        public float DriftTimeAlignmentFactor {
            get => innerModel.DriftTimeAlignmentFactor;
            set {
                if (innerModel.DriftTimeAlignmentFactor == value) return;
                innerModel.DriftTimeAlignmentFactor = value;
                OnPropertyChanged(nameof(DriftTimeAlignmentFactor));
            }
        }

        public bool IsAllCalibrantDataImported {
            get => innerModel.IsAllCalibrantDataImported;
            set {
                if (innerModel.IsAllCalibrantDataImported == value) return;
                innerModel.IsAllCalibrantDataImported = value;
                OnPropertyChanged(nameof(IsAllCalibrantDataImported));
            }
        }

        public Dictionary<int, CoefficientsForCcsCalculation> FileID2CcsCoefficients {
            get => innerModel.FileID2CcsCoefficients;
            set {
                if (innerModel.FileID2CcsCoefficients == value) return;
                innerModel.FileID2CcsCoefficients = value;
                OnPropertyChanged(nameof(FileID2CcsCoefficients));
            }
        }
    }
}
