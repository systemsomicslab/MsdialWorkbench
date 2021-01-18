using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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
            get => innerModel.Name;
            set {
                if (innerModel.Name != value) {
                    innerModel.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string AdductIonName => innerModel.PeakCharacter.AdductType.AdductIonName;
        public string Formula => innerModel.Formula.FormulaString;
        public string Ontology => innerModel.Ontology;
        public string InChIKey => innerModel.InChIKey;
        public string SMILES => innerModel.SMILES;
        public string Comment {
            get => innerModel.Comment;
            set {
                if (innerModel.Comment != value) {
                    innerModel.Comment = value;
                    OnPropertyChanged(nameof(Comment));
                }
            }
        }
        public double SignalToNoiseAve => innerModel.SignalToNoiseAve;
        public double FillPercentage => innerModel.FillParcentage;
        public double AnovaPvalue => innerModel.AnovaPvalue;
        public double FoldChange => innerModel.FoldChange;
        public MsScanMatchResult MspBasedMatchResult => innerModel.MspBasedMatchResult;
        public MsScanMatchResult TextDbBasedMatchResult => innerModel.TextDbBasedMatchResult;
        public MsScanMatchResult ScanMatchResult => innerModel.TextDbBasedMatchResult ?? innerModel.MspBasedMatchResult;
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

        static AlignmentSpotPropertyVM() {
            KMIupacUnit = CompMs.Common.DataObj.Property.AtomMass.hMass * 2 + CompMs.Common.DataObj.Property.AtomMass.cMass; // CH2
            KMNominalUnit = Math.Round(KMIupacUnit);
        }

        public AlignmentSpotPropertyVM(AlignmentSpotProperty innerModel, Dictionary<int, string> id2class = null) {
            this.innerModel = innerModel;

            if (id2class != null) {
                 _ = SetBarItems(innerModel, id2class);  // TODO: error handling
            }
        }

        private async Task SetBarItems(AlignmentSpotProperty innerModel, Dictionary<int, string> id2class)  {
            BarItems = await Task.Run(() => innerModel.AlignedPeakProperties
            .GroupBy(peak => id2class[peak.FileID])
            .Select(pair => new BarItem { Class = pair.Key, Height = pair.Average(peak => peak.PeakHeightTop) })
            .ToList());
        }
    }
}
