using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class AlignmentSpotPropertyModel : DisposableModelBase, IPeakSpotModel, IFilterable, IAnnotatedObject, IChromatogramPeak, IMoleculeProperty, IIonProperty
    {
        public int AlignmentID => innerModel.AlignmentID;
        public int MasterAlignmentID => innerModel.MasterAlignmentID;
        public int RepresentativeFileID => innerModel.RepresentativeFileID;
        public ChromXType ChromXType => innerModel.TimesCenter.MainType;
        public ChromXUnit ChromXUnit => innerModel.TimesCenter.Unit;
        public double MassCenter => innerModel.MassCenter;
        public double Mass => MassCenter; // Alias of MassCenter used in PeakSpotNavigatorView
        public double HeightAverage => innerModel.HeightAverage;
        [Obsolete("Use AlignedPeakPropertiesModelAsObservable property.")]
        public ReadOnlyCollection<AlignmentChromPeakFeature> AlignedPeakProperties => innerModel.AlignedPeakProperties.AsReadOnly();
        [Obsolete("Use AlignedPeakPropertiesModelAsObservable property.")]
        public ReadOnlyCollection<AlignmentChromPeakFeatureModel>? AlignedPeakPropertiesModel => _alignedPeakPropertiesModelProperty.Value;
        public IReadOnlyReactiveProperty<ReadOnlyCollection<AlignmentChromPeakFeatureModel>?> AlignedPeakPropertiesModelProperty => _alignedPeakPropertiesModelProperty;
        private readonly ReactiveProperty<ReadOnlyCollection<AlignmentChromPeakFeatureModel>?> _alignedPeakPropertiesModelProperty;

        public double RT => innerModel.TimesCenter.RT.Value;
        public double RI => innerModel.TimesCenter.RI.Value;
        public double Drift => innerModel.TimesCenter.Drift.Value;

        public double TimesCenter {
            get => innerModel.TimesCenter.Value;
            set {
                if (innerModel.TimesCenter.Value != value) {
                    innerModel.TimesCenter.SetChromX(ChromX.Convert(value, ChromXType, ChromXUnit));
                    OnPropertyChanged(nameof(TimesCenter));
                }
            }
        }
        public double ChromXValue => TimesCenter; // Alias of TimesCenter used in PeakSpotNavigatorView

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
                    innerModel.SetAdductType(value);
                    OnPropertyChanged(nameof(AdductType));
                    OnPropertyChanged(nameof(AdductIonName));
                }
            }
        }

        public string AdductIonName => innerModel.AdductType.AdductIonName;

        public string AnnotatorID => MatchResultsModel.Representative.AnnotatorID;

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

        public IonAbundanceUnit IonAbundanceUnit {
            get => innerModel.IonAbundanceUnit;
            set {
                if (innerModel.IonAbundanceUnit != value) {
                    innerModel.IonAbundanceUnit = value;
                    OnPropertyChanged(nameof(IonAbundanceUnit));
                }
            }
        }

        public double CollisionCrossSection => innerModel.CollisionCrossSection;
        public double SignalToNoiseAve => innerModel.SignalToNoiseAve;
        public double FillPercentage => innerModel.FillParcentage;
        public double AnovaPvalue => innerModel.AnovaPvalue;
        public double FoldChange => innerModel.FoldChange;
        MsScanMatchResultContainer IAnnotatedObject.MatchResults => innerModel.MatchResults;
        public MsScanMatchResultContainerModel MatchResultsModel { get; }

        public string Isotope => $"M + {innerModel.PeakCharacter.IsotopeWeightNumber}";

        public MsScanMatchResult MspBasedMatchResult => innerModel.MspBasedMatchResult;
        public MsScanMatchResult TextDbBasedMatchResult => innerModel.TextDbBasedMatchResult;
        public MsScanMatchResult ScanMatchResult => MatchResultsModel.Representative ?? innerModel.TextDbBasedMatchResult ?? innerModel.MspBasedMatchResult;

        public bool IsRefMatched(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            return innerModel.IsReferenceMatched(evaluator);
        }

        public bool IsSuggested(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            return innerModel.IsAnnotationSuggested(evaluator);
        }

        public bool IsUnknown => innerModel.IsUnknown;
        public bool IsMsmsAssigned => innerModel.IsMsmsAssigned || (innerModel.AlignmentDriftSpotFeatures?.Any(spot => spot.IsMsmsAssigned) ?? false);
        public bool IsMonoIsotopicIon => innerModel.PeakCharacter.IsMonoIsotopicIon;
        public bool IsBlankFiltered => innerModel.FeatureFilterStatus.IsBlankFiltered;
        public bool IsFragmentQueryExisted => innerModel.FeatureFilterStatus.IsFragmentExistFiltered;
        public bool IsManuallyModifiedForAnnotation => innerModel.IsManuallyModifiedForAnnotation;

        public bool IsManuallyModifiedForQuant {
            get => innerModel.IsManuallyModifiedForQuant;
            set {
                if (innerModel.IsManuallyModifiedForQuant != value) {
                    innerModel.IsManuallyModifiedForQuant = value;
                    OnPropertyChanged(nameof(IsManuallyModifiedForQuant));
                }
            }
        }

        public bool IsBlankFilteredByPostCurator {
            get => innerModel.IsBlankFilteredByPostCurator;
            set
            {
                if (innerModel.IsBlankFilteredByPostCurator != value) {
                    innerModel.IsBlankFilteredByPostCurator = value;
                    OnPropertyChanged(nameof(IsBlankFilteredByPostCurator));
                }
            }
        }
        public bool IsBlankGhostFilteredByPostCurator {
            get => innerModel.IsBlankGhostFilteredByPostCurator;
            set
            {
                if (innerModel.IsBlankGhostFilteredByPostCurator != value) {
                    innerModel.IsBlankGhostFilteredByPostCurator = value;
                    OnPropertyChanged(nameof(IsBlankGhostFilteredByPostCurator));
                }
            }
        }

        public bool IsMzFilteredByPostCurator {
            get => innerModel.IsMzFilteredByPostCurator;
            set
            {
                if (innerModel.IsMzFilteredByPostCurator != value) {
                    innerModel.IsMzFilteredByPostCurator = value;
                    OnPropertyChanged(nameof(IsMzFilteredByPostCurator));
                }
            }
        }

        public bool IsRsdFilteredByPostCurator {
            get => innerModel.IsRsdFilteredByPostCurator;
            set
            {
                if (innerModel.IsRsdFilteredByPostCurator != value) {
                    innerModel.IsRsdFilteredByPostCurator = value;
                    OnPropertyChanged(nameof(IsRsdFilteredByPostCurator));
                }
            }
        }

        public bool IsRmdFilteredByPostCurator {
            get => innerModel.IsRmdFilteredByPostCurator;
            set
            {
                if (innerModel.IsRmdFilteredByPostCurator != value) {
                    innerModel.IsRmdFilteredByPostCurator = value;
                    OnPropertyChanged(nameof(IsRmdFilteredByPostCurator));
                }
            }
        }

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

        internal readonly AlignmentSpotProperty innerModel;

        public static readonly double KMIupacUnit;
        public static readonly double KMNominalUnit;
        public double KM => MassCenter / KMIupacUnit * KMNominalUnit;
        public double NominalKM => Math.Round(KM);
        public double KMD => NominalKM - KM;
        public double KMR => NominalKM % KMNominalUnit;

        public bool IsMultiLayeredData => innerModel.IsMultiLayeredData();
        static AlignmentSpotPropertyModel() {
            KMIupacUnit = AtomMass.hMass * 2 + AtomMass.cMass; // CH2
            KMNominalUnit = Math.Round(KMIupacUnit);
        }

        public AlignmentSpotPropertyModel(AlignmentSpotProperty innerModel) {
            this.innerModel = innerModel;
            MatchResultsModel = new MsScanMatchResultContainerModel(innerModel.MatchResults).AddTo(Disposables);
            _alignedPeakPropertiesModelProperty = Observable.FromAsync(() => innerModel.AlignedPeakPropertiesTask)
                .Select(peaks => peaks?.Select(peak => new AlignmentChromPeakFeatureModel(peak)).ToList().AsReadOnly())
                .ToReactiveProperty()
                .AddTo(Disposables);
        }

        public void RaisePropertyChanged() {
            OnPropertyChanged(string.Empty);
        }

        public AlignmentSpotPropertyModel Clone(int newMasterId, int newLocalId) {
            var id = newMasterId;
            var model = innerModel.Clone(ref id, newLocalId);
            return new AlignmentSpotPropertyModel(model);
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

        // IChromatogramPeak
        int IChromatogramPeak.ID => ((IChromatogramPeak)innerModel).ID;
        ChromXs IChromatogramPeak.ChromXs { get => ((IChromatogramPeak)innerModel).ChromXs; set => ((IChromatogramPeak)innerModel).ChromXs = value; }
        double ISpectrumPeak.Mass { get => ((ISpectrumPeak)innerModel).Mass; set => ((ISpectrumPeak)innerModel).Mass = value; }
        double ISpectrumPeak.Intensity { get => ((ISpectrumPeak)innerModel).Intensity; set => ((ISpectrumPeak)innerModel).Intensity = value; }

        double IFilterable.RelativeAmplitudeValue => innerModel.RelativeAmplitudeValue;
        PeakSpotTagCollection IFilterable.TagCollection => innerModel.TagCollection;
    }
}
