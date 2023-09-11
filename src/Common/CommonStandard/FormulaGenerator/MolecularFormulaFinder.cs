using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Parameter;
using System.Collections.Generic;

namespace CompMs.Common.FormulaGenerator {
    public sealed class MolecularFormulaFinder
    {
        //public static List<FormulaResult> GetMolecularFormulaList(List<Formula> formulaDB, List<ProductIon> productIonDB,
        public static List<FormulaResult> GetMolecularFormulaList(List<ProductIon> productIonDB,
            List<NeutralLoss> neutralLossDB, List<ExistFormulaQuery> existFormulaDB, 
            RawData rawData, AnalysisParamOfMsfinder param)
        {
            var adductIon = AdductIon.GetAdductIon(rawData.PrecursorType);

            double monoIsotopicMass = adductIon.ConvertToExactMass(rawData.PrecursorMz);

            double subtractedM1Intensity = rawData.NominalIsotopicPeakList[1].RelativeAbundance - adductIon.M1Intensity;
            double subtractedM2Intensity = rawData.NominalIsotopicPeakList[2].RelativeAbundance - adductIon.M2Intensity;

            if (subtractedM1Intensity < 0) subtractedM1Intensity = 0;
            if (subtractedM2Intensity < 0) subtractedM2Intensity = 0;

            var formulaGenerator = new FormulaGenerator(param);
            var isotopeCheck = true; if (rawData.Ms1PeakNumber < 2) isotopeCheck = false;

            return formulaGenerator.GetFormulaCandidateList(productIonDB, neutralLossDB, existFormulaDB, 
                param, monoIsotopicMass, subtractedM1Intensity, subtractedM2Intensity, rawData, adductIon, isotopeCheck);
        }

        //public static List<FormulaResult> GetMolecularFormulaList(RawData rawData, List<Formula> formulaDB, List<ExistFormulaQuery> existFormulaDB,
        public static List<FormulaResult> GetMolecularFormulaList(RawData rawData, List<ExistFormulaQuery> existFormulaDB,
            AdductIon adductIon, double precursorMz, double m1Intensity, double m2Intensity, AnalysisParamOfMsfinder analysisParam)
        {
            double monoIsotopicMass = adductIon.ConvertToExactMass(precursorMz);

            var formulaGenerator = new FormulaGenerator(analysisParam);
            var isotopeCheck = true; if (m1Intensity < 0 || m2Intensity < 0) isotopeCheck = false;
            return formulaGenerator.GetFormulaCandidateList(rawData, existFormulaDB, analysisParam, monoIsotopicMass, m1Intensity, m2Intensity, adductIon, isotopeCheck);
            //return formulaGenerator.GetFormulaCandidateList(rawData, formulaDB, existFormulaDB, analysisParam, monoIsotopicMass, m1Intensity, m2Intensity, adductIon, isotopeCheck);
        }

        public static FormulaResult GetMolecularFormulaScore(List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB, 
            List<ExistFormulaQuery> existFormulaDB, RawData rawData, AnalysisParamOfMsfinder analysisParam)
        {
            var adductIon = AdductIon.GetAdductIon(rawData.PrecursorType);

            double monoIsotopicMass = adductIon.ConvertToExactMass(rawData.PrecursorMz);

            double subtractedM1Intensity = rawData.NominalIsotopicPeakList[1].RelativeAbundance - adductIon.M1Intensity;
            double subtractedM2Intensity = rawData.NominalIsotopicPeakList[2].RelativeAbundance - adductIon.M2Intensity;

            if (subtractedM1Intensity < 0) subtractedM1Intensity = 0;
            if (subtractedM2Intensity < 0) subtractedM2Intensity = 0;

            var formulaGenerator = new FormulaGenerator(analysisParam);
            var formulaString = rawData.Formula;
            var isotopeCheck = true; if (rawData.Ms1PeakNumber < 2) isotopeCheck = false;

            return formulaGenerator.GetFormulaScore(formulaString, productIonDB, neutralLossDB, existFormulaDB, analysisParam, monoIsotopicMass, subtractedM1Intensity, subtractedM2Intensity, rawData, adductIon, isotopeCheck);
        }


    }
}
