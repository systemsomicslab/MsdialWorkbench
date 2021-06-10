using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.ChemView;
using CompMs.MsdialCore.DataObj;
using System;
using System.Windows.Media;
using CompMs.Common.Interfaces;
using CompMs.Common.DataObj.Property;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class ChromatogramPeakFeatureModel : ViewModelBase, IAnnotatedObject
    {
        #region Property
        public int MasterPeakID => innerModel.MasterPeakID;
        public double? ChromXValue => innerModel.ChromXs.Value;
        public double? ChromXLeftValue => innerModel.ChromXsLeft.Value;
        public double? ChromXRightValue => innerModel.ChromXsRight.Value;
        public double CollisionCrosSection => innerModel.CollisionCrossSection;
        public double Mass => innerModel.Mass;
        public double Intensity => innerModel.PeakHeightTop;
        public double PeakArea => innerModel.PeakAreaAboveZero;
        public int MS1RawSpectrumIdTop => innerModel.MS1RawSpectrumIdTop;
        public int MS2RawSpectrumId => innerModel.MS2RawSpectrumID;
        public MsScanMatchResultContainer MatchResults {
            get => innerModel.MatchResults;
            set => innerModel.MatchResults = value;
        }
        public MsScanMatchResult MspBasedMatchResult => innerModel.MspBasedMatchResult;
        public MsScanMatchResult TextDbBasedMatchResult => innerModel.TextDbBasedMatchResult;
        public MsScanMatchResult ScanMatchResult => innerModel.MatchResults?.Representative ?? innerModel.TextDbBasedMatchResult ?? innerModel.MspBasedMatchResult;
        public string AdductIonName => innerModel.AdductType.AdductIonName;
        public string Name {
            get => ((IMoleculeProperty)innerModel).Name;
            set {
                if (innerModel.Name != value) {
                    ((IMoleculeProperty)innerModel).Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        public Formula Formula {
            get => ((IMoleculeProperty)innerModel).Formula;
            set {
                if (innerModel.Formula != value) {
                    ((IMoleculeProperty)innerModel).Formula = value;
                    OnPropertyChanged(nameof(Formula));
                }
            }
        }
        public string Ontology {
            get => ((IMoleculeProperty)innerModel).Ontology;
            set {
                if (innerModel.Ontology != value) {
                    ((IMoleculeProperty)innerModel).Ontology = value;
                    OnPropertyChanged(nameof(Ontology));
                }
            }
        }
        public string SMILES {
            get => ((IMoleculeProperty)innerModel).SMILES;
            set {
                if (innerModel.SMILES != value) {
                    ((IMoleculeProperty)innerModel).SMILES = value;
                    OnPropertyChanged(nameof(SMILES));
                }
            }
        }
        public string InChIKey {
            get => ((IMoleculeProperty)innerModel).InChIKey;
            set {
                if (innerModel.InChIKey != value) {
                    ((IMoleculeProperty)innerModel).InChIKey = value;
                    OnPropertyChanged(nameof(InChIKey));
                }
            }
        }
        public string Comment {
            get => innerModel.Comment;
            set {
                if (innerModel.Comment != value) {
                    innerModel.Comment = value;
                    OnPropertyChanged(nameof(Comment));
                }
            }
        }

        public string Isotope => $"M + {innerModel.PeakCharacter.IsotopeWeightNumber}";
        public int IsotopeWeightNumber => innerModel.PeakCharacter.IsotopeWeightNumber;
        public bool IsRefMatched => innerModel.IsReferenceMatched;
        public bool IsSuggested => innerModel.IsAnnotationSuggested;
        public bool IsUnknown => innerModel.IsUnknown;
        public bool IsCcsMatch => ScanMatchResult?.IsCcsMatch ?? false;
        public bool IsMsmsContained => innerModel.IsMsmsContained;
        public double AmplitudeScore => innerModel.PeakShape.AmplitudeScoreValue;
        public double AmplitudeOrderValue => innerModel.PeakShape.AmplitudeOrderValue;

        public static readonly double KMIupacUnit;
        public static readonly double KMNominalUnit;
        public double KM => Mass / KMIupacUnit * KMNominalUnit;
        public double NominalKM => Math.Round(KM);
        public double KMD => NominalKM - KM;
        public double KMR => NominalKM % KMNominalUnit;

        public Brush SpotColor { get; set; }
        //public Brush SpotColorByOntology { get; set; }

        public ChromatogramPeakFeature InnerModel => innerModel;
        #endregion

        #region Field

        private readonly ChromatogramPeakFeature innerModel;
        #endregion

        static ChromatogramPeakFeatureModel() {
            KMIupacUnit = AtomMass.hMass * 2 + AtomMass.cMass; // CH2
            KMNominalUnit = Math.Round(KMIupacUnit);
        }

        public ChromatogramPeakFeatureModel(ChromatogramPeakFeature feature, bool coloredByOntology = false) {
            innerModel = feature;
            if (coloredByOntology) {
                SpotColor = ChemOntologyColor.Ontology2RgbaBrush.ContainsKey(innerModel.Ontology) ?
                  new SolidColorBrush(ChemOntologyColor.Ontology2RgbaBrush[innerModel.Ontology]) :
                  new SolidColorBrush(Color.FromArgb(180, 181, 181, 181));
            }
            else {
                SpotColor = new SolidColorBrush(Color.FromArgb(180
                            , (byte)(255 * innerModel.PeakShape.AmplitudeScoreValue)
                            , (byte)(255 * (1 - Math.Abs(innerModel.PeakShape.AmplitudeScoreValue - 0.5)))
                            , (byte)(255 - 255 * innerModel.PeakShape.AmplitudeScoreValue)));
            }

            SpotColor.Freeze();
        }

        public void RaisePropertyChanged() {
            OnPropertyChanged(string.Empty);
        }
    }
}
