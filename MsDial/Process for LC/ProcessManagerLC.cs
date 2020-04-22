using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using Msdial.Lcms.DataProcess;
using Msdial.Lcms.Dataprocess.Utility;
using System.Linq;
using CompMs.Common.MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    public class ProcessManagerLC
    {
        private bool isRerun;
        public ProcessManagerLC(bool isRerun  = false)
        {
            this.isRerun = isRerun;
        }

        public async Task<bool> RunAllProcess(MainWindow mainWindow)
        {
            // from peak picking to identification process
            var isValid = await new AnalysisFileProcessForLCMulti().Process(mainWindow);
            if (!isValid) { BasicErrorOccured(mainWindow); return isValid; }

            // alignment process
            if (mainWindow.AnalysisFiles.Count != 1 && mainWindow.AnalysisParamForLC.TogetherWithAlignment)
            {
                return await RunAlignmentProcess(mainWindow);
            }
            else
            {
                BasicSuccessProcess(mainWindow, null);
            }

            return isValid;
        }

        public async Task<bool> RunAlignmentProcess(MainWindow mainWindow)
        {
            var alignmentResult = await new JointAlignerProcessLC().JointAligner(mainWindow);
            if (alignmentResult.AlignmentPropertyBeanCollection == null || alignmentResult.AlignmentPropertyBeanCollection.Count == 0)
            {
                MessageBox.Show("There is no peak information for peak alignment. Please check your ion mode setting.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                BasicSuccessProcess(mainWindow, null);
                return true; // no alignment result but it is not error
            }
            alignmentResult = await new GapFillingProcessLC().GapFilling(mainWindow, alignmentResult);
            alignmentResult = await new AlignmentFinalizeProcessLC().Finalize(mainWindow, alignmentResult);
            BasicSuccessProcess(mainWindow, alignmentResult);
            return true;
        }


        public void BasicSuccessProcess(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            var projectPropertyBean = mainWindow.ProjectProperty;
            var analysisFileBeanCollection = mainWindow.AnalysisFiles;
            Mouse.OverrideCursor = Cursors.Wait;
            mainWindow.SaveProperty = DataStorageLcUtility.GetSavePropertyBean(mainWindow.ProjectProperty, mainWindow.RdamProperty, mainWindow.MspDB,
                mainWindow.IupacReference, mainWindow.AnalysisParamForLC, mainWindow.AnalysisFiles, mainWindow.AlignmentFiles,
                mainWindow.PostIdentificationTxtDB, mainWindow.TargetFormulaLibrary);
            MessagePackHandler.SaveToFile<SavePropertyBean>(mainWindow.SaveProperty, projectPropertyBean.ProjectFilePath);

            if (alignmentResult != null) 
            { 
                var alignmentFileBean = mainWindow.AlignmentFiles[mainWindow.AlignmentFiles.Count - 1];
                alignmentResult.IonizationType = Ionization.ESI;
                alignmentResult.AnalysisParamForLC = mainWindow.AnalysisParamForLC;
                mainWindow.BarChartDisplayMode = BarChartDisplayMode.OriginalHeight;
                MessagePackHandler.SaveToFile<AlignmentResultBean>(alignmentResult, alignmentFileBean.FilePath);
            }

            mainWindow.FileNavigatorUserControlsRefresh(analysisFileBeanCollection);
            mainWindow.PeakViewerForLcRefresh(0);

            mainWindow.IsEnabled = true;
            Mouse.OverrideCursor = null;
        }

        public void BasicErrorOccured(MainWindow mainWindow)
        {
            if (isRerun)
            {
                // remove alignment file
                if (mainWindow.AnalysisParamForLC.TogetherWithAlignment)
                {
                    if (mainWindow.AlignmentFiles != null && mainWindow.AlignmentFiles.Count > 0)
                    {
                        mainWindow.AlignmentFiles.RemoveAt(mainWindow.AlignmentFiles.Count - 1);
                        Mouse.OverrideCursor = Cursors.Wait;

                        mainWindow.FileNavigatorUserControlsRefresh(mainWindow.AnalysisFiles);
                        mainWindow.PeakViewerForLcRefresh(0);
                        Mouse.OverrideCursor = null;
                    }
                }
            }
            if (!mainWindow.IsEnabled) mainWindow.IsEnabled = true;
        }
    }
}
