using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class AlignmentChromPeakFeatureModel : BindableBase{

        public AlignmentChromPeakFeature innerModel { get; }
        public AlignmentChromPeakFeatureModel(AlignmentChromPeakFeature innerModel) {
            this.innerModel = innerModel;
        }

        public int FileID => innerModel.FileID;

        public ChromXs ChromXsLeft { 
            get => this.innerModel.ChromXsLeft; 
            set {
                if (innerModel.ChromXsLeft != value) {
                    innerModel.ChromXsLeft = value;
                    OnPropertyChanged(nameof(ChromXsLeft));
                }
            } 
        }

        public ChromXs ChromXsTop {
            get => this.innerModel.ChromXsTop;
            set {
                if (innerModel.ChromXsTop != value) {
                    innerModel.ChromXsTop = value;
                    OnPropertyChanged(nameof(ChromXsTop));
                }
            }
        }

        public ChromXs ChromXsRight {
            get => this.innerModel.ChromXsRight;
            set {
                if (innerModel.ChromXsRight != value) {
                    innerModel.ChromXsRight = value;
                    OnPropertyChanged(nameof(ChromXsRight));
                }
            }
        }
        public double PeakHeightLeft {
            get => this.innerModel.PeakHeightLeft;
            set {
                if (innerModel.PeakHeightLeft != value) {
                    innerModel.PeakHeightLeft = value;
                    OnPropertyChanged(nameof(PeakHeightLeft));
                }
            }
        }

        public double PeakHeightTop {
            get => this.innerModel.PeakHeightTop;
            set {
                if (innerModel.PeakHeightTop != value) {
                    innerModel.PeakHeightTop = value;
                    OnPropertyChanged(nameof(PeakHeightTop));
                }
            }
        }

        public double PeakHeightRight {
            get => this.innerModel.PeakHeightRight;
            set {
                if (innerModel.PeakHeightRight != value) {
                    innerModel.PeakHeightRight = value;
                    OnPropertyChanged(nameof(PeakHeightRight));
                }
            }
        }

        public double PeakAreaAboveZero {
            get => this.innerModel.PeakAreaAboveZero;
            set {
                if (innerModel.PeakAreaAboveZero != value) {
                    innerModel.PeakAreaAboveZero = value;
                    OnPropertyChanged(nameof(PeakAreaAboveZero));
                }
            }
        }

        public double PeakAreaAboveBaseline {
            get => this.innerModel.PeakAreaAboveBaseline;
            set {
                if (innerModel.PeakAreaAboveBaseline != value) {
                    innerModel.PeakAreaAboveBaseline = value;
                    OnPropertyChanged(nameof(PeakAreaAboveBaseline));
                }
            }
        }

        public double NormalizedPeakHeight {
            get => this.innerModel.NormalizedPeakHeight;
            set {
                if (innerModel.NormalizedPeakHeight != value) {
                    innerModel.NormalizedPeakHeight = value;
                    OnPropertyChanged(nameof(NormalizedPeakHeight));
                }
            }
        }

        public double NormalizedPeakAreaAboveZero {
            get => this.innerModel.PeakAreaAboveZero;
            set {
                if (innerModel.NormalizedPeakAreaAboveZero != value) {
                    innerModel.NormalizedPeakAreaAboveZero = value;
                    OnPropertyChanged(nameof(NormalizedPeakAreaAboveZero));
                }
            }
        }

        public double NormalizedPeakAreaAboveBaseline {
            get => this.innerModel.NormalizedPeakAreaAboveBaseline;
            set {
                if (innerModel.NormalizedPeakAreaAboveBaseline != value) {
                    innerModel.NormalizedPeakAreaAboveBaseline = value;
                    OnPropertyChanged(nameof(NormalizedPeakAreaAboveBaseline));
                }
            }
        }

        public int MS1AccumulatedMs1RawSpectrumIdTop {
            get => this.innerModel.MS1AccumulatedMs1RawSpectrumIdTop;
            set {
                if (innerModel.MS1AccumulatedMs1RawSpectrumIdTop != value) {
                    innerModel.MS1AccumulatedMs1RawSpectrumIdTop = value;
                    OnPropertyChanged(nameof(MS1AccumulatedMs1RawSpectrumIdTop));
                }
            }
        }

        public int MS1AccumulatedMs1RawSpectrumIdLeft {
            get => this.innerModel.MS1AccumulatedMs1RawSpectrumIdLeft;
            set {
                if (innerModel.MS1AccumulatedMs1RawSpectrumIdLeft != value) {
                    innerModel.MS1AccumulatedMs1RawSpectrumIdLeft = value;
                    OnPropertyChanged(nameof(MS1AccumulatedMs1RawSpectrumIdLeft));
                }
            }
        }

        public int MS1AccumulatedMs1RawSpectrumIdRight {
            get => this.innerModel.MS1AccumulatedMs1RawSpectrumIdRight;
            set {
                if (innerModel.MS1AccumulatedMs1RawSpectrumIdRight != value) {
                    innerModel.MS1AccumulatedMs1RawSpectrumIdRight = value;
                    OnPropertyChanged(nameof(MS1AccumulatedMs1RawSpectrumIdRight));
                }
            }
        }

        public int MS1RawSpectrumIdTop {
            get => this.innerModel.MS1RawSpectrumIdTop;
            set {
                if (innerModel.MS1RawSpectrumIdTop != value) {
                    innerModel.MS1RawSpectrumIdTop = value;
                    OnPropertyChanged(nameof(MS1RawSpectrumIdTop));
                }
            }
        }

        public int MS1RawSpectrumIdLeft {
            get => this.innerModel.MS1RawSpectrumIdLeft;
            set {
                if (innerModel.MS1RawSpectrumIdLeft != value) {
                    innerModel.MS1RawSpectrumIdLeft = value;
                    OnPropertyChanged(nameof(MS1RawSpectrumIdLeft));
                }
            }
        }

        public int MS1RawSpectrumIdRight {
            get => this.innerModel.MS1RawSpectrumIdRight;
            set {
                if (innerModel.MS1RawSpectrumIdRight != value) {
                    innerModel.MS1RawSpectrumIdRight = value;
                    OnPropertyChanged(nameof(MS1RawSpectrumIdRight));
                }
            }
        }

        public float EstimatedNoise {
            get => this.innerModel.PeakShape.EstimatedNoise;
            set {
                if (innerModel.PeakShape.EstimatedNoise != value) {
                    innerModel.PeakShape.EstimatedNoise = value;
                    OnPropertyChanged(nameof(EstimatedNoise));
                }
            }
        }

        public float SignalToNoise {
            get => this.innerModel.PeakShape.SignalToNoise;
            set {
                if (innerModel.PeakShape.SignalToNoise != value) {
                    innerModel.PeakShape.SignalToNoise = value;
                    OnPropertyChanged(nameof(SignalToNoise));
                }
            }
        }

        public bool IsManuallyModifiedForQuant {
            get => innerModel.IsManuallyModifiedForQuant;
            set {
                if (innerModel.IsManuallyModifiedForQuant != value) {
                    innerModel.IsManuallyModifiedForQuant = value;
                    OnPropertyChanged(nameof(IsManuallyModifiedForQuant));
                }
            }
        }
    }
}
