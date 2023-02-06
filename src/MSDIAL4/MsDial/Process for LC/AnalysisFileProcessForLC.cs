using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using System.Linq;
using Msdial.Lcms.DataProcess;
using Msdial.Lcms.Dataprocess.Utility;
using CompMs.Common.MessagePack;

namespace Rfx.Riken.OsakaUniv {
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

	public class AnalysisFileProcessForLC
    {
		#region // members
		private BackgroundWorker bgWorker;
		private ProgressBarWin pbw;
		private string progressHeader = "File progress: ";
		private string progressFileMax;
		private int currentProgress;
		#endregion

		#region // data processing method summary
		public void Process(MainWindow mainWindow)
        {
			bgWorkerInitialize(mainWindow);

			this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_Process_DoWork);
            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow });
        }

        private void bgWorkerInitialize(MainWindow mainWindow)
        {
			mainWindow.IsEnabled = false;

			this.progressFileMax = mainWindow.AnalysisFiles.Count.ToString();
			this.currentProgress = 0;

			this.pbw = new ProgressBarWin();
			this.pbw.Owner = mainWindow;
			this.pbw.ProgressBar_Label.Content = this.progressHeader + "0/" + this.progressFileMax;
			this.pbw.ProgressView.Minimum = 0;
			this.pbw.ProgressView.Maximum = 100;
			this.pbw.ProgressView.Value = 0;
			this.pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			this.pbw.Show();

			this.bgWorker = new BackgroundWorker();
			this.bgWorker.WorkerReportsProgress = true;
			this.bgWorker.WorkerSupportsCancellation = true;
			this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
			this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
		}
		#endregion

		#region // background workers
		private void bgWorker_Process_DoWork(object sender, DoWorkEventArgs e)
		{
			object[] arg = (object[])e.Argument;

			MainWindow mainWindow = (MainWindow)arg[0];
            if (mainWindow.MspDB != null || mainWindow.MspDB.Count >= 0)
                mainWindow.MspDB = mainWindow.MspDB.OrderBy(n => n.PrecursorMz).ToList();

			var projectPropertyBean = mainWindow.ProjectProperty;
			var rdamPropertyBean = mainWindow.RdamProperty;
			var files = mainWindow.AnalysisFiles;
			var param = mainWindow.AnalysisParamForLC;
			var mspFormatCompoundInformationBeanList = mainWindow.MspDB;
            var iupac = mainWindow.IupacReference;
			var postIdentificationReferenceBeanList = mainWindow.PostIdentificationTxtDB;
			var alignmentFileBeanCollection = mainWindow.AlignmentFiles;

            for (int i = 0; i < files.Count; i++)
            {
                var error = string.Empty;
                ProcessFile.Execute(projectPropertyBean, rdamPropertyBean, files[i], param
                    , iupac , mspFormatCompoundInformationBeanList
                    , postIdentificationReferenceBeanList, out error, null);
                if (error != string.Empty) {
                    MessageBox.Show(error);
                }
                
                this.currentProgress++;
            }

			e.Result = new object[] { mainWindow };
        }

		private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
			this.pbw.ProgressView.Value = e.ProgressPercentage;
			this.pbw.ProgressBar_Label.Content = this.progressHeader + this.currentProgress + "/" + this.progressFileMax;
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
			this.pbw.Close();
            
            object[] arg = (object[])e.Result;

            MainWindow mainWindow = (MainWindow)arg[0];
            if (mainWindow.MspDB != null || mainWindow.MspDB.Count > 0)
                mainWindow.MspDB = mainWindow.MspDB.OrderBy(n => n.Id).ToList();

            if (mainWindow.AnalysisFiles.Count != 1 && mainWindow.AnalysisParamForLC.TogetherWithAlignment)
            {
                new JointAlignerProcessLC().JointAligner(mainWindow); return;
            }
            
            var projectPropertyBean = mainWindow.ProjectProperty;
            var analysisFileBeanCollection = mainWindow.AnalysisFiles;

            mainWindow.IsEnabled = true;

            Mouse.OverrideCursor = Cursors.Wait;

            mainWindow.SaveProperty = DataStorageLcUtility.GetSavePropertyBean(mainWindow.ProjectProperty, mainWindow.RdamProperty, mainWindow.MspDB
                    , mainWindow.IupacReference, mainWindow.AnalysisParamForLC, mainWindow.AnalysisFiles, mainWindow.AlignmentFiles
                    , mainWindow.PostIdentificationTxtDB, mainWindow.TargetFormulaLibrary);
            MessagePackHandler.SaveToFile<SavePropertyBean>(mainWindow.SaveProperty, projectPropertyBean.ProjectFilePath);

            // DataStorageLcUtility.SaveToXmlFile(mainWindow.SaveProperty, projectPropertyBean.ProjectFilePath, typeof(SavePropertyBean));

            mainWindow.FileNavigatorUserControlsRefresh(analysisFileBeanCollection);
            // mainWindow.PeakViewerForLcRefresh(0);
            
            Mouse.OverrideCursor = null;
        }

        #endregion
    }
}
