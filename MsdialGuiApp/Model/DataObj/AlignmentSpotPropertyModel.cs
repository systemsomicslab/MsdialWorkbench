using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class AlignmentSpotPropertyModel : BindableBase, IFilterable
    {
        public int AlignmentID => innerModel.AlignmentID;
        public int MasterAlignmentID => innerModel.MasterAlignmentID;
        public int RepresentativeFileID => innerModel.RepresentativeFileID;
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

        public BarItemCollection BarItemCollection { get; }

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

        public AlignmentSpotPropertyModel(AlignmentSpotProperty innerModel) : this(innerModel, Observable.Return((IBarItemsLoader)null)) {

        }

        public AlignmentSpotPropertyModel(AlignmentSpotProperty innerModel, IObservable<IBarItemsLoader> barItemsLoader) {
            this.innerModel = innerModel;
            this.AlignedPeakPropertiesModel = this.innerModel.AlignedPeakProperties.Select(n => new AlignmentChromPeakFeatureModel(n)).ToList().AsReadOnly();

            BarItemCollection = BarItemCollection.Create(this, barItemsLoader);
        }

        public void RaisePropertyChanged() {
            OnPropertyChanged(string.Empty);
        }

        // IChromatogramPeak
        int IChromatogramPeak.ID => ((IChromatogramPeak)innerModel).ID;
        ChromXs IChromatogramPeak.ChromXs { get => ((IChromatogramPeak)innerModel).ChromXs; set => ((IChromatogramPeak)innerModel).ChromXs = value; }
        double ISpectrumPeak.Mass { get => ((ISpectrumPeak)innerModel).Mass; set => ((ISpectrumPeak)innerModel).Mass = value; }
        double ISpectrumPeak.Intensity { get => ((ISpectrumPeak)innerModel).Intensity; set => ((ISpectrumPeak)innerModel).Intensity = value; }

        double IFilterable.RelativeAmplitudeValue => innerModel.RelativeAmplitudeValue;
    }
}
