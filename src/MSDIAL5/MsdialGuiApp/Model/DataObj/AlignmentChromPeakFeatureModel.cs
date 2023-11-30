using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class AlignmentChromPeakFeatureModel : BindableBase, IAnnotatedObject, IChromatogramPeak {

        private readonly AlignmentChromPeakFeature _innerModel;
        public AlignmentChromPeakFeatureModel(AlignmentChromPeakFeature innerModel) {
            _innerModel = innerModel;
        }

        public string Name => _innerModel.Name;
        public int FileID => _innerModel.FileID;
        public string FileName => _innerModel.FileName;
        public int MasterPeakID => _innerModel.MasterPeakID;
        public int PeakID => _innerModel.PeakID;
        public int MSDecResultID => _innerModel.GetMSDecResultID();
        public double TotalScore => _innerModel.MatchResults.Representative.TotalScore;
        public AdductIon Adduct => _innerModel.PeakCharacter.AdductType;
        public double Mass => _innerModel.Mass;
        public double Time => ChromXsTop.Value;

        public ChromXs ChromXsLeft { 
            get => _innerModel.ChromXsLeft; 
            set {
                if (_innerModel.ChromXsLeft != value) {
                    _innerModel.ChromXsLeft = value;
                    OnPropertyChanged(nameof(ChromXsLeft));
                }
            } 
        }

        public ChromXs ChromXsTop {
            get => _innerModel.ChromXsTop;
            set {
                if (_innerModel.ChromXsTop != value) {
                    _innerModel.ChromXsTop = value;
                    OnPropertyChanged(nameof(ChromXsTop));
                }
            }
        }

        public ChromXs ChromXsRight {
            get => _innerModel.ChromXsRight;
            set {
                if (_innerModel.ChromXsRight != value) {
                    _innerModel.ChromXsRight = value;
                    OnPropertyChanged(nameof(ChromXsRight));
                }
            }
        }
        public double PeakHeightLeft {
            get => _innerModel.PeakHeightLeft;
            set {
                if (_innerModel.PeakHeightLeft != value) {
                    _innerModel.PeakHeightLeft = value;
                    OnPropertyChanged(nameof(PeakHeightLeft));
                }
            }
        }

        public double PeakHeightTop {
            get => _innerModel.PeakHeightTop;
            set {
                if (_innerModel.PeakHeightTop != value) {
                    _innerModel.PeakHeightTop = value;
                    OnPropertyChanged(nameof(PeakHeightTop));
                }
            }
        }

        public double PeakHeightRight {
            get => _innerModel.PeakHeightRight;
            set {
                if (_innerModel.PeakHeightRight != value) {
                    _innerModel.PeakHeightRight = value;
                    OnPropertyChanged(nameof(PeakHeightRight));
                }
            }
        }

        public double PeakAreaAboveZero {
            get => _innerModel.PeakAreaAboveZero;
            set {
                if (_innerModel.PeakAreaAboveZero != value) {
                    _innerModel.PeakAreaAboveZero = value;
                    OnPropertyChanged(nameof(PeakAreaAboveZero));
                }
            }
        }

        public double PeakAreaAboveBaseline {
            get => _innerModel.PeakAreaAboveBaseline;
            set {
                if (_innerModel.PeakAreaAboveBaseline != value) {
                    _innerModel.PeakAreaAboveBaseline = value;
                    OnPropertyChanged(nameof(PeakAreaAboveBaseline));
                }
            }
        }

        public double NormalizedPeakHeight {
            get => _innerModel.NormalizedPeakHeight;
            set {
                if (_innerModel.NormalizedPeakHeight != value) {
                    _innerModel.NormalizedPeakHeight = value;
                    OnPropertyChanged(nameof(NormalizedPeakHeight));
                }
            }
        }

        public double NormalizedPeakAreaAboveZero {
            get => _innerModel.PeakAreaAboveZero;
            set {
                if (_innerModel.NormalizedPeakAreaAboveZero != value) {
                    _innerModel.NormalizedPeakAreaAboveZero = value;
                    OnPropertyChanged(nameof(NormalizedPeakAreaAboveZero));
                }
            }
        }

        public double NormalizedPeakAreaAboveBaseline {
            get => _innerModel.NormalizedPeakAreaAboveBaseline;
            set {
                if (_innerModel.NormalizedPeakAreaAboveBaseline != value) {
                    _innerModel.NormalizedPeakAreaAboveBaseline = value;
                    OnPropertyChanged(nameof(NormalizedPeakAreaAboveBaseline));
                }
            }
        }

        public int MS1AccumulatedMs1RawSpectrumIdTop {
            get => _innerModel.MS1AccumulatedMs1RawSpectrumIdTop;
            set {
                if (_innerModel.MS1AccumulatedMs1RawSpectrumIdTop != value) {
                    _innerModel.MS1AccumulatedMs1RawSpectrumIdTop = value;
                    OnPropertyChanged(nameof(MS1AccumulatedMs1RawSpectrumIdTop));
                }
            }
        }

        public int MS1AccumulatedMs1RawSpectrumIdLeft {
            get => _innerModel.MS1AccumulatedMs1RawSpectrumIdLeft;
            set {
                if (_innerModel.MS1AccumulatedMs1RawSpectrumIdLeft != value) {
                    _innerModel.MS1AccumulatedMs1RawSpectrumIdLeft = value;
                    OnPropertyChanged(nameof(MS1AccumulatedMs1RawSpectrumIdLeft));
                }
            }
        }

        public int MS1AccumulatedMs1RawSpectrumIdRight {
            get => _innerModel.MS1AccumulatedMs1RawSpectrumIdRight;
            set {
                if (_innerModel.MS1AccumulatedMs1RawSpectrumIdRight != value) {
                    _innerModel.MS1AccumulatedMs1RawSpectrumIdRight = value;
                    OnPropertyChanged(nameof(MS1AccumulatedMs1RawSpectrumIdRight));
                }
            }
        }

        public int MS1RawSpectrumIdTop {
            get => _innerModel.MS1RawSpectrumIdTop;
            set {
                if (_innerModel.MS1RawSpectrumIdTop != value) {
                    _innerModel.MS1RawSpectrumIdTop = value;
                    OnPropertyChanged(nameof(MS1RawSpectrumIdTop));
                }
            }
        }

        public int MS1RawSpectrumIdLeft {
            get => _innerModel.MS1RawSpectrumIdLeft;
            set {
                if (_innerModel.MS1RawSpectrumIdLeft != value) {
                    _innerModel.MS1RawSpectrumIdLeft = value;
                    OnPropertyChanged(nameof(MS1RawSpectrumIdLeft));
                }
            }
        }

        public int MS1RawSpectrumIdRight {
            get => _innerModel.MS1RawSpectrumIdRight;
            set {
                if (_innerModel.MS1RawSpectrumIdRight != value) {
                    _innerModel.MS1RawSpectrumIdRight = value;
                    OnPropertyChanged(nameof(MS1RawSpectrumIdRight));
                }
            }
        }

        public float EstimatedNoise {
            get => _innerModel.PeakShape.EstimatedNoise;
            set {
                if (_innerModel.PeakShape.EstimatedNoise != value) {
                    _innerModel.PeakShape.EstimatedNoise = value;
                    OnPropertyChanged(nameof(EstimatedNoise));
                }
            }
        }

        public float SignalToNoise {
            get => _innerModel.PeakShape.SignalToNoise;
            set {
                if (_innerModel.PeakShape.SignalToNoise != value) {
                    _innerModel.PeakShape.SignalToNoise = value;
                    OnPropertyChanged(nameof(SignalToNoise));
                }
            }
        }

        public bool IsManuallyModifiedForQuant {
            get => _innerModel.IsManuallyModifiedForQuant;
            set {
                if (_innerModel.IsManuallyModifiedForQuant != value) {
                    _innerModel.IsManuallyModifiedForQuant = value;
                    OnPropertyChanged(nameof(IsManuallyModifiedForQuant));
                }
            }
        }

        public bool IsMsmsAssigned => _innerModel.IsMsmsAssigned;

        // IAnnotatedObject interface
        MsScanMatchResultContainer IAnnotatedObject.MatchResults => _innerModel.MatchResults;

        // IChromatogramPeak interface
        int IChromatogramPeak.ID => _innerModel.MasterPeakID;

        ChromXs IChromatogramPeak.ChromXs {
            get => ChromXsTop;
            set => ChromXsTop = value;
        }
        double ISpectrumPeak.Mass {
            get => Mass;
            set => _innerModel.Mass = value;
        }
        double ISpectrumPeak.Intensity {
            get => PeakHeightTop;
            set => PeakHeightTop = value;
        }
    }
}
