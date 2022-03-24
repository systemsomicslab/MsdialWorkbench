using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj {
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


    public class AlignmentSpotPropertyModel : BindableBase, IAnnotatedObject
    {
        public int AlignmentID => innerModel.AlignmentID;
        public int MasterAlignmentID => innerModel.MasterAlignmentID;

        public ChromXType ChromXType => innerModel.TimesCenter.MainType;
        public ChromXUnit ChromXUnit => innerModel.TimesCenter.Unit;
        public double MassCenter => innerModel.MassCenter;
        public double HeightAverage => innerModel.HeightAverage;
        public ReadOnlyCollection<AlignmentChromPeakFeature> AlignedPeakProperties => innerModel.AlignedPeakProperties.AsReadOnly();
        public ReadOnlyCollection<AlignmentChromPeakFeatureModel> AlignedPeakPropertiesModel { get; }

        public double TimesCenter {
            get => innerModel.TimesCenter.Value;
            set {
                if (innerModel.TimesCenter.Value != value) {
                    innerModel.TimesCenter = new ChromXs(value, ChromXType, ChromXUnit);
                    OnPropertyChanged(nameof(TimesCenter));
                }
            }
        }

        public string Name {
            get => ((IMoleculeProperty)innerModel).Name;
            set {
                if (innerModel.Name != value) {
                    ((IMoleculeProperty)innerModel).Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        public string Protein {
            get => innerModel.Protein;
        }
        public int ProteinGroupID {
            get => innerModel.ProteinGroupID;
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

        public string AdductIonName => innerModel.AdductType.AdductIonName;

        public string AnnotatorID => innerModel.MatchResults.Representative.AnnotatorID;

        public string Comment {
            get => innerModel.Comment;
            set
            {
                if (innerModel.Comment != value)
                {
                    innerModel.Comment = value;
                    OnPropertyChanged(nameof(Comment));
                }
            }
        }

        public double CollisionCrossSection => innerModel.CollisionCrossSection;
        public double SignalToNoiseAve => innerModel.SignalToNoiseAve;
        public double FillPercentage => innerModel.FillParcentage;
        public double AnovaPvalue => innerModel.AnovaPvalue;
        public double FoldChange => innerModel.FoldChange;
        public MsScanMatchResultContainer MatchResults {
            get => innerModel.MatchResults;
            set => innerModel.MatchResults = value;
        }
        public MsScanMatchResult MspBasedMatchResult => innerModel.MspBasedMatchResult;
        public MsScanMatchResult TextDbBasedMatchResult => innerModel.TextDbBasedMatchResult;
        public MsScanMatchResult ScanMatchResult => innerModel.MatchResults?.Representative ?? innerModel.TextDbBasedMatchResult ?? innerModel.MspBasedMatchResult;

        public bool IsRefMatched(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            return innerModel.IsReferenceMatched(evaluator);
        }

        public bool IsSuggested(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            return innerModel.IsAnnotationSuggested(evaluator);
        }

        public bool IsUnknown => innerModel.IsUnknown;
        public bool IsMsmsAssigned => innerModel.IsMsmsAssigned;
        public bool IsBaseIsotopeIon => innerModel.PeakCharacter.IsotopeWeightNumber == 0;
        public bool IsBlankFiltered => innerModel.FeatureFilterStatus.IsBlankFiltered;
        public bool IsFragmentQueryExisted => innerModel.FeatureFilterStatus.IsFragmentExistFiltered;

        public bool IsManuallyModifiedForQuant {
            get => innerModel.IsManuallyModifiedForQuant;
            set {
                if (innerModel.IsManuallyModifiedForQuant != value) {
                    innerModel.IsManuallyModifiedForQuant = value;
                    OnPropertyChanged(nameof(IsManuallyModifiedForQuant));
                }
            }
        }

        internal readonly AlignmentSpotProperty innerModel;

        public static readonly double KMIupacUnit;
        public static readonly double KMNominalUnit;
        public double KM => MassCenter / KMIupacUnit * KMNominalUnit;
        public double NominalKM => Math.Round(KM);
        public double KMD => NominalKM - KM;
        public double KMR => NominalKM % KMNominalUnit;

        static AlignmentSpotPropertyModel() {
            KMIupacUnit = AtomMass.hMass * 2 + AtomMass.cMass; // CH2
            KMNominalUnit = Math.Round(KMIupacUnit);
        }

        public AlignmentSpotPropertyModel(AlignmentSpotProperty innerModel) {
            this.innerModel = innerModel;
            this.AlignedPeakPropertiesModel = this.innerModel.AlignedPeakProperties.Select(n => new AlignmentChromPeakFeatureModel(n)).ToList().AsReadOnly();
        }

        public void RaisePropertyChanged() {
            OnPropertyChanged(string.Empty);
        }
    }
}
