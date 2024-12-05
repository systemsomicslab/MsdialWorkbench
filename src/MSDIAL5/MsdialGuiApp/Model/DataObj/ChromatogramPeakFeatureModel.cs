using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class ChromatogramPeakFeatureModel : DisposableModelBase, IPeakSpotModel, IFilterable, IMoleculeProperty, IIonProperty, IChromatogramPeak, IAnnotatedObject {
        #region Property
        public int MasterPeakID => innerModel.MasterPeakID;
        public double? ChromXValue => innerModel.ChromXs.Value;
        public double ChromXLeftValue => innerModel.PeakFeature.ChromXsLeft.Value;
        public double ChromXRightValue => innerModel.PeakFeature.ChromXsRight.Value;
        public double CollisionCrossSection => innerModel.CollisionCrossSection;
        public MzValue Mz => innerModel.ChromXs.Mz;
        public DriftTime Drift => innerModel.ChromXs.Drift;
        public RetentionTime RT => innerModel.ChromXs.RT;
        public ChromXs ChromXs => innerModel.ChromXs;

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

        public double PeakArea => innerModel.PeakFeature.PeakAreaAboveZero;
        public int MS1RawSpectrumIdTop => innerModel.MS1RawSpectrumIdTop;
        public int MS1RawSpectrumIdLeft => innerModel.MS1RawSpectrumIdLeft;
        public int MS1RawSpectrumIdRight => innerModel.MS1RawSpectrumIdRight;
        public int MS2RawSpectrumId => innerModel.MS2RawSpectrumID;
        MsScanMatchResultContainer IAnnotatedObject.MatchResults => innerModel.MatchResults;
        public MsScanMatchResultContainerModel MatchResultsModel { get; }
        public MsScanMatchResult MspBasedMatchResult => innerModel.MspBasedMatchResult;
        public MsScanMatchResult TextDbBasedMatchResult => innerModel.TextDbBasedMatchResult;
        public MsScanMatchResult ScanMatchResult => MatchResultsModel.Representative ?? innerModel.TextDbBasedMatchResult ?? innerModel.MspBasedMatchResult;
        public string? AdductIonName => innerModel.AdductType?.AdductIonName;
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

        public AdductIon AdductType {
            get => innerModel.AdductType;
            set {
                if (innerModel.AdductType != value) {
                    innerModel.AdductType = value;
                    OnPropertyChanged(nameof(AdductType));
                    OnPropertyChanged(nameof(AdductIonName));
                }
            }
        }

        public string AnnotatorID => MatchResultsModel.Representative.AnnotatorID;

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

        public bool Confirmed {
            get => innerModel.TagCollection.IsSelected(PeakSpotTag.CONFIRMED);
            set => SetPeakSpotTag(PeakSpotTag.CONFIRMED, value, nameof(Confirmed));
        }
        public bool LowQualitySpectrum {
            get => innerModel.TagCollection.IsSelected(PeakSpotTag.LOW_QUALITY_SPECTRUM);
            set => SetPeakSpotTag(PeakSpotTag.LOW_QUALITY_SPECTRUM, value, nameof(LowQualitySpectrum));
        }
        public bool Misannotation {
            get => innerModel.TagCollection.IsSelected(PeakSpotTag.MISANNOTATION);
            set => SetPeakSpotTag(PeakSpotTag.MISANNOTATION, value, nameof(Misannotation));
        }
        public bool Coelution {
            get => innerModel.TagCollection.IsSelected(PeakSpotTag.COELUTION);
            set => SetPeakSpotTag(PeakSpotTag.COELUTION, value, nameof(Coelution));
        }
        public bool Overannotation {
            get => innerModel.TagCollection.IsSelected(PeakSpotTag.OVERANNOTATION);
            set => SetPeakSpotTag(PeakSpotTag.OVERANNOTATION, value, nameof(Overannotation));
        }

        private bool SetPeakSpotTag(PeakSpotTag tag, bool value, string propertyname) {
            if (value == innerModel.TagCollection.IsSelected(tag)) {
                return false;
            }
            if (value) {
                innerModel.TagCollection.Select(tag);
            }
            else {
                innerModel.TagCollection.Deselect(tag);
            }
            OnPropertyChanged(propertyname);
            return true;
        }

        public void SwitchPeakSpotTag(PeakSpotTag tag) {
            if (tag == PeakSpotTag.CONFIRMED) {
                Confirmed = !Confirmed;
            }
            if (tag == PeakSpotTag.LOW_QUALITY_SPECTRUM) {
                LowQualitySpectrum = !LowQualitySpectrum;
            }
            if (tag == PeakSpotTag.MISANNOTATION) {
                Misannotation = !Misannotation;
            }
            if (tag == PeakSpotTag.COELUTION) {
                Coelution = !Coelution;
            }
            if (tag == PeakSpotTag.OVERANNOTATION) {
                Overannotation = !Overannotation;
            }
        }

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
        bool IFilterable.IsMonoIsotopicIon => innerModel.PeakCharacter.IsMonoIsotopicIon;
        bool IFilterable.IsBlankFiltered => innerModel.FeatureFilterStatus.IsBlankFiltered;
        bool IFilterable.IsManuallyModifiedForAnnotation => innerModel.IsManuallyModifiedForAnnotation;
        bool IFilterable.IsBlankFilteredByPostCurator => true;
        bool IFilterable.IsBlankGhostFilteredByPostCurator => true;
        bool IFilterable.IsMzFilteredByPostCurator => true;
        bool IFilterable.IsRmdFilteredByPostCurator => true;
        bool IFilterable.IsRsdFilteredByPostCurator => true;

        double IFilterable.RelativeAmplitudeValue => innerModel.PeakShape.AmplitudeScoreValue;
        PeakSpotTagCollection IFilterable.TagCollection => innerModel.TagCollection;

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
            MatchResultsModel = new MsScanMatchResultContainerModel(feature.MatchResults).AddTo(Disposables);
        }


        // IPeakSpotModel
        IMSIonProperty IPeakSpotModel.MSIon => innerModel;
        IMoleculeProperty IPeakSpotModel.Molecule => innerModel;

        public void SetUnknown(UndoManager undoManager) {
            IDoCommand command = new SetUnknownDoCommand(this, MatchResultsModel);
            command.Do();
            undoManager.Add(command);
        }

        // IIonProperty
        void IIonProperty.SetAdductType(AdductIon adduct) {
            innerModel.SetAdductType(adduct);
            OnPropertyChanged(nameof(AdductType));
            OnPropertyChanged(nameof(AdductIonName));
        }
    }
}
