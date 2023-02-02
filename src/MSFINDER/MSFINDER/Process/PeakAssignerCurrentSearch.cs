using Riken.Metabolomics.MsfinderCommon.Process;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class PeakAssignerCurrentSearch
    {
        private PeakAssignerCurrentSearch() { }

        public static void Process(MainWindowVM mainWindowVM)
        {
            var file = mainWindowVM.AnalysisFiles[mainWindowVM.SelectedRawFileId];
            var rawDataFilePath = file.RawDataFilePath;
            var formulaFilePath = file.FormulaFilePath;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            var rawDataVM = mainWindowVM.RawDataVM;
            var rawData = mainWindowVM.RawDataVM.RawData;

            RefreshUtility.UpdateRawDataFile(rawDataFilePath, rawDataVM, rawData);

            var productIonDB = mainWindowVM.ProductIonDB;
            var neutralLossDB = mainWindowVM.NeutralLossDB;
            var existFormulaDB = mainWindowVM.ExistFormulaDB;
            var fragmentDB = mainWindowVM.EiFragmentDB;
            var fragmentOntologies = mainWindowVM.FragmentOntologyDB;

            if (param.IsUseEiFragmentDB)
                PeakAssigner.Process(file, rawData, param, productIonDB, neutralLossDB, existFormulaDB, fragmentDB, fragmentOntologies);
            else
                PeakAssigner.Process(file, rawData, param, productIonDB, neutralLossDB, existFormulaDB, null, fragmentOntologies);
        }
    }
}
