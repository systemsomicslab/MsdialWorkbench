using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.Utility;

namespace Rfx.Riken.OsakaUniv
{
    public class SelectAdductTypeVM : ViewModelBase {
        private enum AdductListType { Ms1Pos, Ms1Neg, Ms2Pos, Ms2Neg };
        private List<AdductIon> positiveList;
        private List<AdductIon> negativeList;

        public List<DataGridWithCheckBox<AdductIon>> Ms1Positive { get; set; }
        public List<DataGridWithCheckBox<AdductIon>> Ms1Negative { get; set; }
        public List<DataGridWithCheckBox<AdductIon>> Ms2Positive { get; set; }
        public List<DataGridWithCheckBox<AdductIon>> Ms2Negative { get; set; }

        public AnalysisParamOfMsfinder Parameter { get; set; }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);
            SaveAdducts();
            CloseView();
        }

        public SelectAdductTypeVM(AnalysisParamOfMsfinder param) {
            this.Parameter = param;

            SetDefaultAdducts();
            SetAdducts(param.MS1PositiveAdductIonList, AdductListType.Ms1Pos);
            SetAdducts(param.MS2PositiveAdductIonList, AdductListType.Ms2Pos);
            SetAdducts(param.MS1NegativeAdductIonList, AdductListType.Ms1Neg);
            SetAdducts(param.MS2NegativeAdductIonList, AdductListType.Ms2Neg);
        }

        private void SetDefaultAdducts() {
            this.positiveList = AdductListParcer.GetAdductPositiveResources();
            this.negativeList = AdductListParcer.GetAdductNegativeResources();
        }

        private void SetAdducts(List<AdductIon> adducts, AdductListType type)
        {
            if (type == AdductListType.Ms1Pos) {
                if (adducts == null) Ms1Positive = ConvertList2Property(this.positiveList);
                Ms1Positive = ConvertList2Property(this.positiveList, adducts);
            }
            else if (type == AdductListType.Ms2Pos) {
                if (adducts == null) Ms2Positive = ConvertList2Property(this.positiveList);
                Ms2Positive = ConvertList2Property(this.positiveList, adducts);
            }
            else if (type == AdductListType.Ms1Neg) {
                if (adducts == null) Ms1Negative = ConvertList2Property(this.negativeList);
                Ms1Negative = ConvertList2Property(this.negativeList, adducts);
            }
            else if (type == AdductListType.Ms2Neg) {
                if (adducts == null) Ms2Negative = ConvertList2Property(this.negativeList);
                Ms2Negative = ConvertList2Property(this.negativeList, adducts);
            }
        }

        private List<DataGridWithCheckBox<AdductIon>> ConvertList2Property(List<AdductIon> adducts) {
            var results = new List<DataGridWithCheckBox<AdductIon>>();
            foreach (var adduct in adducts) {
                results.Add(new DataGridWithCheckBox<AdductIon>(adduct));
            }
            return results;
        }

        private List<DataGridWithCheckBox<AdductIon>> ConvertList2Property(List<AdductIon> defaultAdducts, List<AdductIon> adducts) {
            var results = new List<DataGridWithCheckBox<AdductIon>>();
            foreach (var adduct in defaultAdducts) {
                var checker = false;
                foreach (var a in adducts) {
                    if(adduct.AdductIonName == a.AdductIonName) {
                        checker = true; break;
                    }
                }
                results.Add(new DataGridWithCheckBox<AdductIon>(adduct, checker));
            }
            return results;
        }

        private void SaveAdducts()
        {
            if (this.Parameter == null) return;
            Parameter.MS1PositiveAdductIonList = GetAdductIonList(Ms1Positive);
            Parameter.MS2PositiveAdductIonList = GetAdductIonList(Ms2Positive);
            Parameter.MS1NegativeAdductIonList = GetAdductIonList(Ms1Negative);
            Parameter.MS2NegativeAdductIonList = GetAdductIonList(Ms2Negative);
        }

        private List<AdductIon> GetAdductIonList(List<DataGridWithCheckBox<AdductIon>> adductIons)
        {
            var adducts = new List<AdductIon>();
            foreach(var a in adductIons)
            {
                if (a.IsChecked)
                {
                    adducts.Add(a.Value);
                }
            }

            return adducts;
        }
    }

}
