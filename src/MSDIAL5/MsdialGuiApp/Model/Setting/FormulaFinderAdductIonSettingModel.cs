using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting
{
    public class FormulaFinderAdductIonSettingModel : BindableBase
    {
        private readonly AnalysisParamOfMsfinder parameter;
        private readonly IonMode ionMode;
        private enum AdductListType { Ms1Pos, Ms1Neg, Ms2Pos, Ms2Neg };
        private List<AdductIon> adductIons;
        private ObservableCollection<AdductIon> ms1adductIons;
        private ObservableCollection<AdductIon> ms2adductIons;

        public FormulaFinderAdductIonSettingModel(AnalysisParamOfMsfinder parameter, IonMode ionMode)
        {
            this.parameter = parameter;
            this.ionMode = ionMode;
            switch (ionMode)
            {
                case IonMode.Positive:
                    ms1adductIons = new ObservableCollection<AdductIon>(parameter.MS1PositiveAdductIonList);
                    ms2adductIons = new ObservableCollection<AdductIon>(parameter.MS2PositiveAdductIonList);
                    break;
                case IonMode.Negative:
                    ms1adductIons = new ObservableCollection<AdductIon>(parameter.MS1NegativeAdductIonList);
                    ms2adductIons = new ObservableCollection<AdductIon>(parameter.MS2NegativeAdductIonList);
                    break;
            }
            //adductIons = AdductResourceParser.GetAdductIonInformationList(ionMode);
            //ms1adductIons = new ObservableCollection<AdductIon>(adductIons);
            //ms2adductIons = new ObservableCollection<AdductIon>(adductIons);

            Ms1AdductIonSetting = new MsfinderAdductIonSettingModel(ms1adductIons);
            Ms2AdductIonSetting = new MsfinderAdductIonSettingModel(ms2adductIons);
        }

        public MsfinderAdductIonSettingModel Ms1AdductIonSetting { get; }
        public MsfinderAdductIonSettingModel Ms2AdductIonSetting { get; }

        public void Commit() {
            switch (ionMode) {
                case IonMode.Positive:
                    parameter.MS1PositiveAdductIonList = ms1adductIons.ToList();
                    parameter.MS2PositiveAdductIonList = ms2adductIons.ToList();
                    break;
                case IonMode.Negative:
                    parameter.MS1NegativeAdductIonList = ms1adductIons.ToList();
                    parameter.MS2NegativeAdductIonList = ms2adductIons.ToList();
                    break;
            }
        }
    }

    public class MsfinderAdductIonSettingModel : BindableBase, IAdductIonSettingModel {
        private ObservableCollection<AdductIon> _adductIons;

        public MsfinderAdductIonSettingModel(ObservableCollection<AdductIon> adductIons)
        {
            _adductIons = adductIons; 
        }

        public bool IsReadOnly => false;

        public string UserDefinedAdductName {
            get => _userDefinedAdductName;
            set => SetProperty(ref _userDefinedAdductName, value);
        }
        private string _userDefinedAdductName;

        public AdductIon UserDefinedAdduct => AdductIon.GetAdductIon(_userDefinedAdductName);

        public ObservableCollection<AdductIon> AdductIons => _adductIons;

        public void AddAdductIon() {
            var adduct = UserDefinedAdduct;
            if (adduct?.FormatCheck ?? false) {
                adduct.IsIncluded = true;
                AdductIons.Add(adduct);
            }
        }

        public void RemoveAdductIon(AdductIon adduct) {
            if (AdductIons.Contains(adduct)) {
                AdductIons.Remove(adduct);
            }
        }
    }
}
