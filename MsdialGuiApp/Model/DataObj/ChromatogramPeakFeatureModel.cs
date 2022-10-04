using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class ChromatogramPeakFeatureModel : BindableBase, IFilterable, IChromatogramPeak
    {
        #region Property
        public int MasterPeakID => innerModel.MasterPeakID;
        public double? ChromXValue => innerModel.ChromXs.Value;
        public double? ChromXLeftValue => innerModel.ChromXsLeft.Value;
        public double? ChromXRightValue => innerModel.ChromXsRight.Value;
        public double CollisionCrossSection => innerModel.CollisionCrossSection;
        public double Mass {
            get => innerModel.PeakFeature.Mass;
            set {
                if (innerModel.PeakFeature.Mass != value) {
                    innerModel.PeakFeature.Mass = value;
                    OnPropertyChanged(nameof(Mass));
                }
            }
        }

        public double Intensity {
            get => innerModel.PeakFeature.PeakHeightTop;
            set {
                if (innerModel.PeakFeature.PeakHeightTop != value) {
                    innerModel.PeakFeature.PeakHeightTop = value;
                    OnPropertyChanged(nameof(Intensity));
                }
            }
        }

        public double PeakArea => innerModel.PeakAreaAboveZero;
        public int MS1RawSpectrumIdTop => innerModel.MS1RawSpectrumIdTop;
        public int MS1RawSpectrumIdLeft => innerModel.MS1RawSpectrumIdLeft;
        public int MS1RawSpectrumIdRight => innerModel.MS1RawSpectrumIdRight;
        public int MS2RawSpectrumId => innerModel.MS2RawSpectrumID;
        public MsScanMatchResultContainer MatchResults {
            get => innerModel.MatchResults;
            set => innerModel.MatchResults = value;
        }
        public MsScanMatchResult MspBasedMatchResult => innerModel.MspBasedMatchResult;
        public MsScanMatchResult TextDbBasedMatchResult => innerModel.TextDbBasedMatchResult;
        public MsScanMatchResult ScanMatchResult => innerModel.MatchResults?.Representative ?? innerModel.TextDbBasedMatchResult ?? innerModel.MspBasedMatchResult;
        public string AdductIonName => innerModel.AdductType?.AdductIonName;
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

        public string AnnotatorID => innerModel.MatchResults.Representative.AnnotatorID;

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

        public bool IsRefMatched(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            return innerModel.IsReferenceMatched(evaluator);
        }

        public bool IsSuggested(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            return innerModel.IsAnnotationSuggested(evaluator);
        }

        public bool IsUnknown => innerModel.IsUnknown;
        public bool IsCcsMatch => ScanMatchResult?.IsCcsMatch ?? false;
        public bool IsMsmsContained => innerModel.IsMsmsContained || (innerModel.DriftChromFeatures?.Any(peak => peak.IsMsmsContained) ?? false);
        public bool IsFragmentQueryExisted => innerModel.FeatureFilterStatus.IsFragmentExistFiltered;

        public double AmplitudeScore => innerModel.PeakShape.AmplitudeScoreValue;
        public double AmplitudeOrderValue => innerModel.PeakShape.AmplitudeOrderValue;

        public static readonly double KMIupacUnit;
        public static readonly double KMNominalUnit;
        public double KM => Mass / KMIupacUnit * KMNominalUnit;
        public double NominalKM => Math.Round(KM);
        public double KMD => NominalKM - KM;
        public double KMR => NominalKM % KMNominalUnit;

        public ChromatogramPeakFeature InnerModel => innerModel;
        public int MSDecResultIDUsedForAnnotation => innerModel.GetMSDecResultID();
        #endregion


        private readonly ChromatogramPeakFeature innerModel;

        // IFilterable
        bool IFilterable.IsMsmsAssigned => IsMsmsContained;
        bool IFilterable.IsBaseIsotopeIon => innerModel.PeakCharacter.IsotopeWeightNumber == 0;
        bool IFilterable.IsBlankFiltered => innerModel.FeatureFilterStatus.IsBlankFiltered;
        bool IFilterable.IsManuallyModifiedForAnnotation => innerModel.IsManuallyModifiedForAnnotation;
        bool IFilterable.IsBlankFilteredByPostCurator => true;
        bool IFilterable.IsBlankGhostFilteredByPostCurator => true;
        bool IFilterable.IsMzFilteredByPostCurator => true;
        bool IFilterable.IsRmdFilteredByPostCurator => true;
        bool IFilterable.IsRsdFilteredByPostCurator => true;

        double IFilterable.RelativeAmplitudeValue => innerModel.PeakShape.AmplitudeScoreValue;

        // IChromatogramPeak
        int IChromatogramPeak.ID => ((IChromatogramPeak)innerModel).ID;
        ChromXs IChromatogramPeak.ChromXs {
            get => ((IChromatogramPeak)innerModel).ChromXs;
            set {
                ((IChromatogramPeak)innerModel).ChromXs = value;
                OnPropertyChanged(nameof(ChromXValue));
            }
        }
        double ISpectrumPeak.Mass {
            get => ((ISpectrumPeak)innerModel).Mass;
            set {
                ((ISpectrumPeak)innerModel).Mass = value;
                OnPropertyChanged(nameof(Mass));
            }
        }
        double ISpectrumPeak.Intensity {
            get => ((ISpectrumPeak)innerModel).Intensity;
            set {
                ((ISpectrumPeak)innerModel).Intensity = value;
                OnPropertyChanged(nameof(Intensity));
            }
        }

        static ChromatogramPeakFeatureModel() {
            KMIupacUnit = AtomMass.hMass * 2 + AtomMass.cMass; // CH2
            KMNominalUnit = Math.Round(KMIupacUnit);
        }

        public ChromatogramPeakFeatureModel(ChromatogramPeakFeature feature) {
            innerModel = feature;
        }

        public void RaisePropertyChanged() {
            OnPropertyChanged(string.Empty);
        }

        public void SetUnknown() {
            DataAccess.ClearMoleculePropertyInfomation(this);
            MatchResults.RemoveManuallyResults();
            MatchResults.AddResult(new MsScanMatchResult { Source = SourceType.Manual | SourceType.Unknown });
        }
    }
}
