using Riken.Metabolomics.MsfinderCommon.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class BatchJobProcess
    {
        private BatchJobProcess() { }

        public static void Process(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            var selectedRawFile = mainWindowVM.SelectedRawFileId;
            mainWindowVM.BatchJobStartTimeStamp = DateTime.Now;

            RefreshUtility.RawDataFileRefresh(mainWindowVM, selectedRawFile);
            RefreshUtility.FormulaDataFileRefresh(mainWindow, mainWindowVM, selectedRawFile);
            RefreshUtility.StructureDataFileRefresh(mainWindow, mainWindowVM, selectedRawFile);

            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            var time = mainWindowVM.BatchJobStartTimeStamp;
            var timeString = time.Year + "_" + time.Month + "_" + time.Day + "_" + time.Hour + "_" + time.Minute + "_" + time.Second;
            var paramfile = Path.Combine(mainWindowVM.DataStorageBean.ImportFolderPath, "batchparam-" + timeString + ".txt");
            MsFinderIniParcer.Write(param, paramfile);

            if (param.IsAllProcess == true || param.IsFormulaFinder)
            {
                var formulaFinderBatchJob = new FormulaFinderBatchProcess();
                formulaFinderBatchJob.Process(mainWindow, mainWindowVM);
            }
            else if (param.IsStructureFinder)
            {
                var structureFinderBatchJob = new StructureFinderBatchProcess();
                structureFinderBatchJob.Process(mainWindow, mainWindowVM);
            }
        }
    }
}
