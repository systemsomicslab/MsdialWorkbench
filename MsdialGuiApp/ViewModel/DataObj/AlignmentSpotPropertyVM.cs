using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.ChemView;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public class AlignmentSpotPropertyVM : ViewModelBase
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
        public string Comment => innerModel.Comment;

        public double SignalToNoiseAve => innerModel.SignalToNoiseAve;
        public double FillPercentage => innerModel.FillParcentage;
        public double AnovaPvalue => innerModel.AnovaPvalue;
        public double FoldChange => innerModel.FoldChange;
        public MsScanMatchResult MspBasedMatchResult => innerModel.MspBasedMatchResult;
        public MsScanMatchResult TextDbBasedMatchResult => innerModel.TextDbBasedMatchResult;
        public MsScanMatchResult ScanMatchResult => innerModel.MatchResults?.Representative ?? innerModel.TextDbBasedMatchResult ?? innerModel.MspBasedMatchResult;
        public bool IsRefMatched => innerModel.IsReferenceMatched;
        public bool IsSuggested => innerModel.IsAnnotationSuggested;
        public bool IsUnknown => innerModel.IsUnknown;
        public bool IsMsmsAssigned => innerModel.IsMsmsAssigned;
        public bool IsBaseIsotopeIon => innerModel.PeakCharacter.IsotopeWeightNumber == 0;
        public bool IsBlankFiltered => innerModel.FeatureFilterStatus.IsBlankFiltered;

        public List<BarItem> BarItems { get; private set; }
        internal readonly AlignmentSpotProperty innerModel;

        public static readonly double KMIupacUnit;
        public static readonly double KMNominalUnit;
        public double KM => MassCenter / KMIupacUnit * KMNominalUnit;
        public double NominalKM => Math.Round(KM);
        public double KMD => NominalKM - KM;
        public double KMR => NominalKM % KMNominalUnit;
        public Brush SpotColor { get; set; }

        static AlignmentSpotPropertyVM() {
            KMIupacUnit = CompMs.Common.DataObj.Property.AtomMass.hMass * 2 + CompMs.Common.DataObj.Property.AtomMass.cMass; // CH2
            KMNominalUnit = Math.Round(KMIupacUnit);
        }

        public AlignmentSpotPropertyVM(AlignmentSpotProperty innerModel, Dictionary<int, string> id2class = null, bool coloredByOntology = false) {
            this.innerModel = innerModel;

            if (id2class != null) {
                 _ = SetBarItems(innerModel, id2class);  // TODO: error handling
            }
            if (coloredByOntology) {
                SpotColor = ChemOntologyColor.Ontology2RgbaBrush.ContainsKey(innerModel.Ontology) ?
                  new SolidColorBrush(ChemOntologyColor.Ontology2RgbaBrush[innerModel.Ontology]) :
                  new SolidColorBrush(Color.FromArgb(180, 181, 181, 181));
            }
            else {
                SpotColor = new SolidColorBrush(Color.FromArgb(180
                            , (byte)(255 * innerModel.RelativeAmplitudeValue)
                            , (byte)(255 * (1 - Math.Abs(innerModel.RelativeAmplitudeValue - 0.5)))
                            , (byte)(255 - 255 * innerModel.RelativeAmplitudeValue)));
            }

            SpotColor.Freeze();
        }

        private async Task SetBarItems(AlignmentSpotProperty innerModel, Dictionary<int, string> id2class)  {
            BarItems = await Task.Run(() => innerModel.AlignedPeakProperties
            .GroupBy(peak => id2class[peak.FileID])
            .Select(pair => new BarItem { Class = pair.Key, Height = pair.Average(peak => peak.PeakHeightTop) })
            .ToList());
        }

        public void RaisePropertyChanged() {
            OnPropertyChanged(string.Empty);
        }
    }
}
