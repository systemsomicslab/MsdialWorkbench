using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class FormulaFinderCurrentSearch
    {
        private FormulaFinderCurrentSearch() { }

        public static void Process(MainWindowVM mainWindowVM)
        {
            var rawDataFilePath = mainWindowVM.AnalysisFiles[mainWindowVM.SelectedRawFileId].RawDataFilePath;
            var formulaFilePath = mainWindowVM.AnalysisFiles[mainWindowVM.SelectedRawFileId].FormulaFilePath;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            var rawDataVM = mainWindowVM.RawDataVM;
            var rawData = mainWindowVM.RawDataVM.RawData;

            RefreshUtility.UpdateRawDataFile(rawDataFilePath, rawDataVM, rawData);

            //var formulaDB = mainWindowVM.QuickFormulaDB;
            var productIonDB = mainWindowVM.ProductIonDB;
            var neutralLossDB = mainWindowVM.NeutralLossDB;
            var existFormulaDB = mainWindowVM.ExistFormulaDB;
            var chemicalOntDB = mainWindowVM.ChemicalOntologies;

            mainWindowVM.DataStorageBean.FormualResults = new List<FormulaResult>();
            if (param.IsRunInSilicoFragmenterSearch) {
                mainWindowVM.DataStorageBean.FormualResults = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, 
                    neutralLossDB, existFormulaDB, rawData, param);
                //mainWindowVM.DataStorageBean.FormualResults = MolecularFormulaFinder.GetMolecularFormulaList(formulaDB, productIonDB,
                //   neutralLossDB, existFormulaDB, rawData, param);
                ChemicalOntologyAnnotation.ProcessByOverRepresentationAnalysis(mainWindowVM.DataStorageBean.FormualResults, chemicalOntDB, 
                    rawData.IonMode, param, AdductIonParcer.GetAdductIonBean(rawData.PrecursorType), productIonDB, neutralLossDB);
            }
            if (param.IsRunSpectralDbSearch)
                mainWindowVM.DataStorageBean.FormualResults.Add(FormulaResultParcer.GetFormulaResultTemplateForSpectralDbSearch());

            FormulaResultParcer.FormulaResultsWriter(formulaFilePath, mainWindowVM.DataStorageBean.FormualResults);
            mainWindowVM.FormulaResultVMs = DataAccessUtility.GetFormulaVmList(mainWindowVM.DataStorageBean.FormualResults, rawData);
        }
    }
}
