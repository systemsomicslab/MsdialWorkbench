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

namespace Rfx.Riken.OsakaUniv
{
    /*
     * Summary for MS-DIAL programing
     * 
     * List<double[]> peaklist: shows data point list of chromatogram; double[] { scan number, retention time [min], base peak m/z, base peak intensity }
     * List<double[]> masslist: shows data point list of mass spectrum; double[] { m/z, intensity }
     * 
     * .abf (binary) includes raw data. How to use => look at "private void bgWorker_AllProcess_DoWork(object sender, DoWorkEventArgs e)" of AnalysisFileProcess.cs
     * 
     * .mtb (serialized) stores SavePropertyBean. How to use => look at "private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)" of AnalysisFileProcess.cs
     * 
     * .med (serialized) stores data processing parameters. How to use => look at "private void saveParametersMenu_Click(object sender, RoutedEventArgs e)" of MainWindow.xaml.cs
     * 
     * .pai (serialized) stores the peak detection result, i.e. ObservableCollection<PeakAreaBean>. How to use => look at "private void bgWorker_AllProcess_DoWork(object sender, DoWorkEventArgs e)" of AnalysisFileProcess.cs
     * 
     * .arf (serialized) stores the alignment result, i.e. ObservableCollection<AlignmentPropertyBean>. How to use => look at "private void bgWorker_DoWork(object sender, DoWorkEventArgs e)" of JointAlignerProcess.cs
     * 
     * .lbm (ascii) stores the LipidBlast MS/MS spectrum as the MSP format. How to use => "public LipidDbSetVM(MainWindow mainWindow, Window window)" of LipidDbSetVM.cs
     * 
     * MS-DIAL utilizes some resources including .lbm and others prepared in 'Resources' folder. These resources will be retrieved when MS-DIAL is opened or when the project will be started.
     * How to use. => follow "private void newProjectMenu_Click(object sender, RoutedEventArgs e)" of MainWindow.xaml.cs
     * 
     * .dcl (binary) stores the deconvolution result as described below. How to use => look at "private static void writeMS2DecResult" of SpectralDeconvolution.cs
     *
     * The main algorithm is mainly written in "Dataprocessing" assembly and IdaModeDataProcessing.cs and SwathDataAnalyzerDataprocessing.
     * 
     * The data property is mainly stored in the C# class library of "Bean" folder of "MSDIAL" assembly and "Common" assembly.
     * 
     * The graphical user interface is mainly constructed by MVVM consisting of "Bean", "ViewModel", and "Window" folders.
     * 
     * <Deconvolution file (.dcl)>
     * [first header] Ver. Number
     * [second header] number of PeakAreaBean's collection
     * [third header] seekpointer list
     * (peak 1: meta data -> spectra -> datapoint detail)
     * (float)peak top retention time
     * (float)ms1 accurate mass
     * (float)unique mass
     * (int)ms1 peak height
     * (float)ms1 Isotopic ion [M+1] peak height
     * (float)ms1 Isotopic ion [M+2] peak height
     * (int)deconvoluted ions total area
     * [(int)spectra number:SN]
     * [(int)data point number:DN]
     * [(float)mz1], [(int)intensity1](spectrum1)
     * [(float)mz2], [(int)intensity2](spectrum2)
     * [(float)mz3], [(int)intensity3](spectrum3)
     * *********************************************
     * [(float)mzSN], [(int)intensitySN](spectrumSN)
     * [(int)scan num.], [(float)rt], [(float)BasePeakMz], [(int)BasePeakIntensity](datapoint1)
     * [(int)scan num.], [(float)rt], [(float)BasePeakMz], [(int)BasePeakIntensity](datapoint2)
     * [(int)scan num.], [(float)rt], [(float)BasePeakMz], [(int)BasePeakIntensity](datapoint3)
     * ********************************************************************************************
     * [(int)scan num.], [(float)rt], [(float)BasePeakMz], [(int)BasePeakIntensity](datapointDN)
     * 
     *  (peak 2: unique mass -> spectra -> datapoint detail)
     * </Deconvolution>
     * 
     */

    public class AnalysisFileProcessForLCMulti
    {
        #region // members
        private ProgressBarMultiContainerWin progressBarMultiContainer;
        private string progressHeader = "File progress: ";
        private int progressFileMax;
        private int currentProgress;
        private int remainProcess;
        private int currentId;
        private List<Task> tasks;
        private List<ProgressBarEach> pblist;
        private MainWindow mainWindow;
        private bool isAllThreadsRunning;

        public CancellationTokenSource CancellationTokenSource;
        public CancellationToken CancellationToken;
        #endregion

        public async Task<bool> Process(MainWindow mainWindow) {
            Initialize(mainWindow);
            if (this.progressBarMultiContainer == null) return false;

            tasks = new List<Task>();
            var numMax = Math.Min(GetNumThread(mainWindow.AnalysisParamForLC.NumThreads), progressFileMax);
            isAllThreadsRunning = false;
            for (var i = 0; i < numMax; i++) {
                tasks.Add(RunEachProcess(i, mainWindow));
                await Task.Delay(200); // not necessary; to be stable
            }
            isAllThreadsRunning = true;
            await Task.WhenAll(tasks);
            if (CancellationToken.IsCancellationRequested) return false;

            FinalizeAllProcess(this.mainWindow);
            return true;
        }

        private int GetNumThread(int numThreadParam)
        {
            var numThreads = 1;
            if (numThreadParam > 1)
            {
                // for advanced settings; ignore max ProcessorCount
                var lp = Environment.ProcessorCount;
                if (numThreadParam > lp + 1)
                {
                    numThreadParam = lp;
                }
                numThreads = numThreadParam;
            }
            return numThreads;
        }

        #region Initialize
        private void Initialize(MainWindow mainWindow) {
            mainWindow.IsEnabled = false;
            this.mainWindow = mainWindow;

            // Init CancellationToken
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            // to run old project
            if (mainWindow.AnalysisParamForLC.RetentionTimeCorrectionCommon == null) {
                mainWindow.AnalysisParamForLC.RetentionTimeCorrectionCommon = new RetentionTimeCorrection.RetentionTimeCorrectionCommon();
            }

            if (mainWindow.AnalysisParamForLC.ProcessOption == ProcessOption.All && mainWindow.AnalysisParamForLC.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection) {
                
                mainWindow.AnalysisFiles = 
                    new ObservableCollection<AnalysisFileBean>(mainWindow.AnalysisFiles.OrderBy(x => x.AnalysisFilePropertyBean.AnalysisBatch).
                    ThenBy(x => x.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder));
                var rtCorrectionWin = new RetentionTimeCorrection.RetentionTimeCorrectionWin(mainWindow);
                rtCorrectionWin.Owner = mainWindow;
                rtCorrectionWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (rtCorrectionWin.ShowDialog() == false) {
                    mainWindow.IsEnabled = true;
                    return;
                }
                mainWindow.AnalysisFiles = new ObservableCollection<AnalysisFileBean>(mainWindow.AnalysisFiles.OrderBy(x => x.AnalysisFilePropertyBean.AnalysisFileId));
            }

            if (mainWindow.MspDB != null && mainWindow.MspDB.Count >= 0)
                mainWindow.MspDB = mainWindow.MspDB.OrderBy(n => n.PrecursorMz).ToList();

            this.progressFileMax = mainWindow.AnalysisFiles.Count;
            this.currentProgress = 0;
            this.remainProcess = mainWindow.AnalysisFiles.Count;
            this.currentId = 0;

            this.progressBarMultiContainer = new ProgressBarMultiContainerWin();
            this.progressBarMultiContainer.Owner = mainWindow;
            this.progressBarMultiContainer.ProgressBar_Label.Content = this.progressHeader + "0/" + this.progressFileMax;
            this.progressBarMultiContainer.ProgressView.Minimum = 0;
            this.progressBarMultiContainer.ProgressView.Maximum = mainWindow.AnalysisFiles.Count;
            this.progressBarMultiContainer.ProgressView.Value = 0;
            this.progressBarMultiContainer.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            this.pblist = new List<ProgressBarEach>();
            for (var j = 0; j < mainWindow.AnalysisFiles.Count; j++) {
                var text = mainWindow.AnalysisFiles[j].AnalysisFilePropertyBean.AnalysisFileName;
                this.pblist.Add(new ProgressBarEach(text, mainWindow.ProjectProperty.Ionization, CancellationToken));
            }
            this.progressBarMultiContainer.SetProgressBar(pblist);
            this.progressBarMultiContainer.Closed += ClosedEvent;
            this.progressBarMultiContainer.Show();
        }
        #endregion

        public void ClosedEvent(object sender, EventArgs e)
        {
            CancellationTokenSource.Cancel();            
        }

        private async Task RunEachProcess(int i, MainWindow mainWindow) {
            currentId++;
            remainProcess--;
            await this.pblist[i].StartLcProcess(mainWindow.ProjectProperty, mainWindow.RdamProperty, mainWindow.AnalysisFiles[i], mainWindow.AnalysisParamForLC, mainWindow.IupacReference, mainWindow.MspDB, mainWindow.PostIdentificationTxtDB);
            await FinalizeOneProcess();
        }

        private async Task FinalizeOneProcess() {
            //Increment the overall progress bar
            this.progressBarMultiContainer.ProgressView.Value++;
            this.currentProgress++;
            this.progressBarMultiContainer.ProgressBar_Label.Content = this.progressHeader + this.currentProgress + "/" + this.progressFileMax;

            if (CancellationToken.IsCancellationRequested) return;
            while (!isAllThreadsRunning)
                await Task.Delay(1000);
            if (this.remainProcess > 0)
            {
                await RunEachProcess(this.currentId, this.mainWindow);                
            }
            if (this.currentProgress == this.progressFileMax)
                return;
        }

        private void FinalizeAllProcess(MainWindow mainWindow) {
            if (CancellationToken.IsCancellationRequested) return;
            this.progressBarMultiContainer.Close();

            if (mainWindow.MspDB != null && mainWindow.MspDB.Count > 0)
                mainWindow.MspDB = mainWindow.MspDB.OrderBy(n => n.Id).ToList();
        }
    }
}
