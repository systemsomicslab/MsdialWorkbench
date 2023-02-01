using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// ProgressBarEach.xaml の相互作用ロジック
    /// </summary>
    public partial class ProgressBarEach : UserControl
    {
        private int currentProgress = 0;
        private CancellationToken cancellationToken;
        public ProgressBarEach()
        {
            InitializeComponent();
        }

        public ProgressBarEach(string txt, Ionization ionization, CancellationToken token)
        {
            InitializeComponent();
            this.title.Text = txt;
            this.cancellationToken = token;
        }

        public async Task StartLcProcess(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, AnalysisFileBean analysisFile, AnalysisParametersBean param, IupacReferenceBean iupac, List<MspFormatCompoundInformationBean> msp, List<PostIdentificatioinReferenceBean> post)
        {
            try
            {
                await Task.Run(() =>
                {
                    Console.WriteLine("Run on this thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId);
                    var error = string.Empty;
                    Msdial.Lcms.DataProcess.ProcessFile.Execute(projectProperty, rdamProperty, analysisFile, param, iupac, msp, post, out error, i => ProgressChanged(i), this.cancellationToken);
                    if (error != string.Empty) {
                        MessageBox.Show(error);
                    }
                }, this.cancellationToken);
            }
            catch
            {
                Console.WriteLine("This process was cancelled");
            }
        }

        public async Task StartGcProcess(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, AnalysisFileBean file, AnalysisParamOfMsdialGcms param, List<MspFormatCompoundInformationBean> msp)
        {
            try
            {
                await Task.Run(() =>
                {
                    Console.WriteLine("Run on this thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId);
                    var error = string.Empty;
                    Msdial.Gcms.Dataprocess.ProcessFile.Execute(projectProperty, rdamProperty, file, param, msp, progress => ProgressChanged(progress), out error);
                    if (error != string.Empty) {
                        MessageBox.Show(error);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("error processing file: " + file.AnalysisFilePropertyBean.AnalysisFileName +
                    "\nMessage: " + ex.Message + "\n" + ex.Source +
                    "\n" + ex.TargetSite +
                    "\n" + ex.StackTrace);
            }
        }


        #region initializer and bgworker
        private void ProgressChanged(int value)
        {
            if (currentProgress == value) return;
            Dispatcher.BeginInvoke((Action)(() => { this.progressBar.Value = value; }));
            currentProgress = value;
        }
        #endregion

        // old codes whould be removed
        /*
        public BackgroundWorker bgWorker;

        public ProgressBarEach() {
            InitializeComponent();
        }

        public ProgressBarEach(string txt, Ionization ionization) {
            InitializeComponent();
            Initialize();
            if (ionization == Ionization.ESI)
                this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_ProcessForLC_DoWork);
            else
                this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_ProcessForGC_DoWork);
            this.title.Text = txt;
        }

        public void StartForLC(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, AnalysisFileBean analysisFile, AnalysisParametersBean param, IupacReferenceBean iupac, List<MspFormatCompoundInformationBean> msp, List<PostIdentificatioinReferenceBean> post) {
            this.bgWorker.RunWorkerAsync(new Object[] { projectProperty, rdamProperty, analysisFile, param, iupac, msp, post });
        }

        public void StartForGC(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, AnalysisFileBean analysisFile, AnalysisParamOfMsdialGcms param, List<MspFormatCompoundInformationBean> msp) {
            this.bgWorker.RunWorkerAsync(new Object[] { projectProperty, rdamProperty, analysisFile, param, msp});
        }


        #region initializer and bgworker
        private void Initialize() {
            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
        }

        private void bgWorker_ProcessForLC_DoWork(object sender, DoWorkEventArgs e) {
            object[] arg = (object[])e.Argument;
            var projectPropertyBean = (ProjectPropertyBean)arg[0];
            var rdamPropertyBean = (RdamPropertyBean)arg[1];
            var analysisFile = (AnalysisFileBean)arg[2];
            var param = (AnalysisParametersBean)arg[3];
            var iupac = (IupacReferenceBean)arg[4];
            var mspFormatCompoundInformationBeanList = (List<MspFormatCompoundInformationBean>)arg[5];
            var postIdentificationReferenceBeanList = (List<PostIdentificatioinReferenceBean>)arg[6];

            Msdial.Lcms.DataProcess.ProcessFile.Execute(projectPropertyBean, rdamPropertyBean, analysisFile, param, iupac, mspFormatCompoundInformationBeanList, postIdentificationReferenceBeanList, this.bgWorker);
        }
        
        private void bgWorker_ProcessForGC_DoWork(object sender, DoWorkEventArgs e) {
            object[] arg = (object[])e.Argument;

            var projectProperty = (ProjectPropertyBean)arg[0];
            var rdamPropertyBean = (RdamPropertyBean)arg[1];
            var file = (AnalysisFileBean)arg[2];
            var param = (AnalysisParamOfMsdialGcms)arg[3];
            var mspDB = (List<MspFormatCompoundInformationBean>)arg[4];

            try {
                Msdial.Gcms.Dataprocess.ProcessFile.Execute(projectProperty, rdamPropertyBean, file, param, mspDB, bgWorker);
            }
            catch (Exception ex) {
                System.Windows.MessageBox.Show("error processing file: " + file.AnalysisFilePropertyBean.AnalysisFileName +
                    "\nMessage: " + ex.Message + "\n" + ex.Source +
                    "\n" + ex.TargetSite +
                    "\n" + ex.StackTrace);
            }
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            this.progressBar.Value = e.ProgressPercentage;
        }
        #endregion
    */
    }
}
