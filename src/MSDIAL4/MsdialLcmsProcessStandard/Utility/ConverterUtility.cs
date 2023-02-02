using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rfx.Riken.OsakaUniv;


namespace Msdial.Lcms.Dataprocess.Utility
{
    public static class ConverterUtility
    {
        public static List<float> ConvertAccurateMass2OtherAducts(List<AdductIon> searchedAdducts, float exactMass) {
            var adductMzList = new List<float>();
            foreach (var searchedAdduct in searchedAdducts) {
                adductMzList.Add((float)MolecularFormulaUtility.ConvertExactMassToPrecursorMz(searchedAdduct, exactMass));
            }
            return adductMzList;
        }

        public static List<AdductIon> GetAddutIonsFromName(AnalysisParametersBean param, ProjectPropertyBean projectProp) {
            var searchedAdducts = new List<AdductIon>();
            for (int i = 0; i < param.AdductIonInformationBeanList.Count; i++) {
                if (param.AdductIonInformationBeanList[i].Included)
                    searchedAdducts.Add(AdductIonParcer.GetAdductIonBean(param.AdductIonInformationBeanList[i].AdductName));
            }
            if (searchedAdducts.Count == 0) {
                if (projectProp.IonMode == IonMode.Positive) searchedAdducts.Add(AdductIonParcer.GetAdductIonBean("[M+H]+"));
                else searchedAdducts.Add(AdductIonParcer.GetAdductIonBean("[M-H]-"));
            }
            return searchedAdducts;
        }

        public static float GetExactMass(ProjectPropertyBean projectProp, float accurateMass) {
            var adductName = "";
            if (projectProp.IonMode == IonMode.Positive) adductName = "[M+H]+";
            else adductName = "[M-H]-";            
            var centralAdduct = AdductIonParcer.GetAdductIonBean(adductName);
            var exactMass = (float)MolecularFormulaUtility.ConvertPrecursorMzToExactMass(accurateMass,
                centralAdduct.AdductIonAccurateMass,
                centralAdduct.ChargeNumber, centralAdduct.AdductIonXmer, projectProp.IonMode);
            return exactMass;
        }
    }
}
