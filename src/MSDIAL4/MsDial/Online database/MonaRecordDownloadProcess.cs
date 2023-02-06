using edu.ucdavis.fiehnlab.mona;
using edu.ucdavis.fiehnlab.MonaRestApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class MonaRecordDownloadProcess
    {
        private ProgressBarWin prb;
        private BackgroundWorker bgWorker;

        private static string prolog = @"http://49.212.184.212/mnn/users/rfmejia";
        private static string posilog = @"/mona-ms2-positive-6-23/spectra";
        private static string negalog = @"/mona-ms2-negative-6-24/spectra";
        private string progressHeader;

        public void Process(MainWindow mainWindow, string outputFile, IonMode ionMode)
        {
            mainWindow.IsEnabled = false;
            initilization(mainWindow, ionMode);

            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow, outputFile, ionMode });
        }

        private void initilization(MainWindow mainWindow, IonMode ionMode)
        {
            this.progressHeader = "DL " + ionMode.ToString() + " MS/MS: ";

            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);

            this.prb = new ProgressBarWin();
            this.prb.Owner = mainWindow;
            this.prb.ProgressView.Maximum = 100;
            this.prb.ProgressView.Minimum = 0;
            this.prb.ProgressView.Value = 0;
            this.prb.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            this.prb.ProgressBar_Label.Content = this.progressHeader;
            this.prb.Show();
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var arg = (object[])e.Argument;
            var mainwindow = (MainWindow)arg[0];
            var outputFile = arg[1].ToString();
            var ionMode = (IonMode)arg[2];

            var url = prolog; if (ionMode == IonMode.Positive) url += posilog; else url += negalog;

            var monaResponse = MonaRestProtocol.GetMonaResponse(url);
            var totalNumber = getTotalRecordNumber(monaResponse);
            var counter = 1000;

            using (var sw = new StreamWriter(outputFile, false, Encoding.ASCII))
            {
                url += "?offset=0&limit=1000";

                while (totalNumber > 0 && monaResponse != null && monaResponse.Embedded != null && monaResponse.Embedded.Items != null && monaResponse.Embedded.Items.Length != 0)
                {
                    monaResponse = MonaRestProtocol.GetMonaResponse(url);
                    if (monaResponse.Links == null || monaResponse.Links.Next == null || monaResponse.Links.Next.Href == null || monaResponse.Links.Next.Href == string.Empty) break;

                    writeMsp(monaResponse, sw);
                    url = monaResponse.Links.Next.Href;

                    this.bgWorker.ReportProgress((int)((double)counter * 100.0 / (double)totalNumber));

                    counter += 1000;
                }
            }

            e.Result = new object[] { mainwindow };
        }

        private void writeMsp(MonaResponse monaResponse, StreamWriter sw)
        {
            foreach (var item in monaResponse.Embedded.Items)
            {
                sw.WriteLine("NAME: " + item.Metadata.Title);
                sw.WriteLine("PRECURSORMZ: " + item.Metadata.PrecursorMz);
                sw.WriteLine("PRECURSORTYPE: " + item.Metadata.PrecursorType);
                sw.WriteLine("INSTRUMENTTYPE: ");
                sw.WriteLine("INSTRUMENT: ");
                sw.WriteLine("License: ");
                sw.WriteLine("SMILES: ");
                sw.WriteLine("INCHIKEY: " + item.Metadata.InchiKey);
                sw.WriteLine("COLLISIONENERGY: ");
                sw.WriteLine("FORMULA: " + item.Metadata.Formula);
                sw.WriteLine("RETENTIONTIME: " + item.Metadata.RetentionTime);
                sw.WriteLine("IONMODE: " + item.Metadata.IonMode);
                sw.WriteLine("Comment: " + item.Metadata.OriginId);
                sw.WriteLine("Num Peaks: " + item.Metadata.Peaks.Count);

                var peaks = item.Metadata.Peaks.OrderBy(n => n[0]).ToList();

                foreach (var peak in peaks)
                {
                    sw.WriteLine(peak[0] + "\t" + peak[1]);
                }

                sw.WriteLine();
            }
        }

        private int getTotalRecordNumber(MonaResponse monaResponse)
        {
            if (monaResponse == null) return 0;
           
            int total;
            if (int.TryParse(monaResponse.Total, out total)) return total;
            else return 0;
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.prb.ProgressView.Value = e.ProgressPercentage;
            this.prb.ProgressBar_Label.Content = this.progressHeader + e.ProgressPercentage + "%"; 
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.prb.Close();

            var arg = (object[])e.Result;
            var mainWindow = (MainWindow)arg[0];

            mainWindow.IsEnabled = true;
            MessageBox.Show("Download finished!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
