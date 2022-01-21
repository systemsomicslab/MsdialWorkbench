using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class AlignmentSpotPropertyModel : BindableBase, IAnnotatedObject
    {
        public int AlignmentID => innerModel.AlignmentID;
        public int MasterAlignmentID => innerModel.MasterAlignmentID;
        public double TimesCenter => innerModel.TimesCenter.Value;
        public double MassCenter => innerModel.MassCenter;
        public double HeightAverage => innerModel.HeightAverage;
        public ReadOnlyCollection<AlignmentChromPeakFeature> AlignedPeakProperties => innerModel.AlignedPeakProperties.AsReadOnly();

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

        public string AdductIonName => innerModel.AdductType.AdductIonName;
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
        }

        public void RaisePropertyChanged() {
            OnPropertyChanged(string.Empty);
        }
    }
}
