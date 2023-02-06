using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Text;
using Msdial.Lcms.Dataprocess.Utility;

namespace Rfx.Riken.OsakaUniv
{

    
    /// <summary>
    ///  This class will be removed because TableViewerTaskHandler was created to be fast
    /// </summary>

    public sealed class TableViewerHandler
    {
        private ProgressBarWin pbw;
        private BackgroundWorker worker;

        public TableViewerHandler() { }

        public void StartUpPeakSpotTableViewer(MainWindow mainWindow) {
            if (mainWindow.ProjectProperty.Ionization == Ionization.EI) {
                initialize(mainWindow, mainWindow.Ms1DecResults.Count);
                foreach (var res in mainWindow.Ms1DecResults) {
                    res.MetaboliteName = mainWindow.MainWindowDisplayVM.GetCompoundName(res.MspDbID);
                }
            }
            else {
                initialize(mainWindow, mainWindow.AnalysisFiles[mainWindow.FocusedFileID].PeakAreaBeanCollection.Count);
            }
            worker.DoWork += workerForPeak_DoWork;
            worker.RunWorkerCompleted += workerForPeak_RunWorkerCompleted;
            pbw.Title = "Progress of preparing peak spot table viewer";
            pbw.ProgressBar_Label.Content = "Peak table viewer";
            pbw.Show();
            worker.RunWorkerAsync(new Object[] { mainWindow });
        }

        public void StartUpAlignmentSpotTableViewer(MainWindow mainWindow) {
            initialize(mainWindow, mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection.Count);
            worker.DoWork += workerForAlignment_DoWork;
            worker.RunWorkerCompleted += workerForAlignment_RunWorkerCompleted;
            pbw.Title = "Progress of preparing alignment spot table viewer";
            pbw.ProgressBar_Label.Content = "Alignment table viewer";
            pbw.Show();
            worker.RunWorkerAsync(new Object[] { mainWindow });
        }

        #region initializer, common
        private void initialize(MainWindow mainWindow, int num) {
            Mouse.OverrideCursor = Cursors.Wait;
            mainWindow.IsEnabled = false;

            pbw = new ProgressBarWin();
            pbw.Owner = mainWindow;
            pbw.ProgressView.Minimum = 0;
            pbw.ProgressView.Maximum = num;
            pbw.ProgressView.Value = 0;
            pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += worker_ProgressChanged;
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            this.pbw.ProgressView.Value = e.ProgressPercentage;
        }
        #endregion

        #region For peak spot table viewer
        private void workerForPeak_DoWork(object sender, DoWorkEventArgs e) {
            object[] arg = (object[])e.Argument;

            var mainWindow = (MainWindow)arg[0];
            var Source = new ObservableCollection<PeakSpotRow>();
            var projectProperty = mainWindow.ProjectProperty;
            var id = mainWindow.FocusedPeakID;
            if (mainWindow.ProjectProperty.Ionization == Ionization.EI) {
                var spectrumList = mainWindow.GcmsSpectrumList;
                var paramGC = mainWindow.AnalysisParamForGC;
                var ms1DecResults = mainWindow.Ms1DecResults;
                for (var i = 0; i < ms1DecResults.Count; i++) {
                    var data = new PeakSpotRow(spectrumList, ms1DecResults[i], projectProperty, paramGC);
                    Source.Add(data);
                    worker.ReportProgress(i);
                }
            }
            else {
                var param = mainWindow.AnalysisParamForLC;
                if (param.IsIonMobility) {
                    var peakAreaBean = mainWindow.AnalysisFiles[mainWindow.FocusedFileID].PeakAreaBeanCollection;
                    var accumulatedMs1Spectra = mainWindow.AccumulatedMs1Specra;
                    var allSpectra = mainWindow.LcmsSpectrumCollection;
                    for (var i = 0; i < peakAreaBean.Count; i++) {
                        var data = new PeakSpotRow(peakAreaBean[i].MasterPeakID, peakAreaBean[i], new DriftSpotBean(), accumulatedMs1Spectra, projectProperty, param);
                        Source.Add(data);

                        for (int j = 0; j < peakAreaBean[i].DriftSpots.Count; j++) {
                            var driftSpot = peakAreaBean[i].DriftSpots[j];
                            data = new PeakSpotRow(driftSpot.MasterPeakID, peakAreaBean[i], driftSpot, allSpectra, projectProperty, param);
                            Source.Add(data);
                        }

                        worker.ReportProgress(i);
                    }
                }
                else {
                    var peakAreaBean = mainWindow.AnalysisFiles[mainWindow.FocusedFileID].PeakAreaBeanCollection;
                    var spectrumCollection = mainWindow.LcmsSpectrumCollection;

                    for (var i = 0; i < peakAreaBean.Count; i++) {
                        var data = new PeakSpotRow(peakAreaBean[i], spectrumCollection, projectProperty, param);
                        Source.Add(data);
                        worker.ReportProgress(i);
                    }
                }
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            e.Result = new object[] { mainWindow, Source };
        }

        private void workerForPeak_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            this.pbw.Close();
            object[] arg = (object[])e.Result;
            var mainWindow = (MainWindow)arg[0];
            var source = (ObservableCollection<PeakSpotRow>)arg[1];
            worker.Dispose();

            var isIonMobility = mainWindow.ProjectProperty.Ionization == Ionization.ESI && mainWindow.AnalysisParamForLC.IsIonMobility;
            var focusedPeakID = isIonMobility ? mainWindow.FocusedMasterID : mainWindow.FocusedPeakID;
            mainWindow.PeakSpotTableViewer = new PeakSpotTableViewer(source, focusedPeakID);
            mainWindow.PeakSpotTableViewer.PeakSpotTableViewerVM.PropertyChanged -= mainWindow.PeakTableViewer_propertyChanged;
            mainWindow.PeakSpotTableViewer.PeakSpotTableViewerVM.PropertyChanged += mainWindow.PeakTableViewer_propertyChanged;
            mainWindow.PeakSpotTableViewer.Closed += mainWindow.PeakSpotTableViewerClose;
            mainWindow.MainWindowDisplayVM.Refresh();

            mainWindow.PeakSpotTableViewer.Owner = mainWindow;
            mainWindow.PeakSpotTableViewer.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            mainWindow.PeakSpotTableViewer.Show();
            if(mainWindow.PeakSpotTableViewer.DataGrid_RawData.SelectedItem != null)
                mainWindow.PeakSpotTableViewer.DataGrid_RawData.ScrollIntoView(mainWindow.PeakSpotTableViewer.DataGrid_RawData.SelectedItem);
            mainWindow.PeakSpotTableViewer.UpdateLayout();
            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;

        }
        #endregion

        #region For alignment spot table viewer
        private void workerForAlignment_DoWork(object sender, DoWorkEventArgs e) {
            object[] arg = (object[])e.Argument;
            var mainWindow = (MainWindow)arg[0];
            var alignmentProperty = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection;
            var analysisFiles = mainWindow.AnalysisFiles;
            var Source = new ObservableCollection<AlignmentSpotRow>();
            var numClass = analysisFiles.Select(x => x.AnalysisFilePropertyBean.AnalysisFileClass).Distinct().Count();
            var project = mainWindow.ProjectProperty;
            var width = numClass > 10 ? 400 : 200;
            var barChartDisplayMode = mainWindow.BarChartDisplayMode;
            var isBoxPlot = mainWindow.ProjectProperty.IsBoxPlotForAlignmentResult;
            var isIonMobility = project.Ionization == Ionization.ESI && mainWindow.AnalysisParamForLC.IsIonMobility;

            if (isIonMobility) {
                for (var i = 0; i < alignmentProperty.Count; i++) {
                    var spot = alignmentProperty[i];
                    var data = new AlignmentSpotRow(spot.MasterID, spot, new AlignedDriftSpotPropertyBean(), 
                        analysisFiles, project, width, mainWindow.MspDB, barChartDisplayMode, isBoxPlot);
                    Source.Add(data);
                    foreach (var drift in spot.AlignedDriftSpots) {
                        data = new AlignmentSpotRow(drift.MasterID, spot, drift, analysisFiles,
                            project, width, mainWindow.MspDB, barChartDisplayMode, isBoxPlot);
                        Source.Add(data);
                    }
                    worker.ReportProgress(i);
                }
            }
            else {
                for (var i = 0; i < alignmentProperty.Count; i++) {
                    var data = new AlignmentSpotRow(alignmentProperty[i], analysisFiles,
                        project, width, mainWindow.MspDB, barChartDisplayMode, isBoxPlot);
                    Source.Add(data);
                    worker.ReportProgress(i);
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            e.Result = new object[] { mainWindow, Source };
        }

        private void workerForAlignment_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            this.pbw.Close();
            object[] arg = (object[])e.Result;
            var mainWindow = (MainWindow)arg[0];
            var source = (ObservableCollection<AlignmentSpotRow>)arg[1];
            worker.Dispose();

            mainWindow.AlignmentSpotTableViewer = new AlignmentSpotTableViewer(source, mainWindow.FocusedAlignmentPeakID, mainWindow.MspDB);
            mainWindow.AlignmentSpotTableViewer.AlignmentSpotTableViewerVM.PropertyChanged -= mainWindow.AlignmentSpotTableView_propertyChanged;
            mainWindow.AlignmentSpotTableViewer.AlignmentSpotTableViewerVM.PropertyChanged += mainWindow.AlignmentSpotTableView_propertyChanged;
            mainWindow.AlignmentSpotTableViewer.Closed += mainWindow.AlignmentSpotTableViewerClose;
            mainWindow.MainWindowDisplayVM.Refresh();

            var numClass = mainWindow.AnalysisFiles.Select(x => x.AnalysisFilePropertyBean.AnalysisFileClass).Distinct().Count();
            mainWindow.AlignmentSpotTableViewer.ResizeBarChartColumn(numClass);
            mainWindow.AlignmentSpotTableViewer.Owner = mainWindow;
            mainWindow.AlignmentSpotTableViewer.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            mainWindow.AlignmentSpotTableViewer.Show();
            if(mainWindow.AlignmentSpotTableViewer.DataGrid_RawData.SelectedItem != null)
                mainWindow.AlignmentSpotTableViewer.DataGrid_RawData.ScrollIntoView(mainWindow.AlignmentSpotTableViewer.DataGrid_RawData.SelectedItem);
            mainWindow.AlignmentSpotTableViewer.UpdateLayout();
            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;

        }
        #endregion

        #region Selecting file changed method
        public void FileChangePeakSpotTableViewer(MainWindow mainWindow) {
            if (mainWindow.ProjectProperty.Ionization == Ionization.EI) {
                initialize(mainWindow, mainWindow.Ms1DecResults.Count);
                foreach (var res in mainWindow.Ms1DecResults) {
                    res.MetaboliteName = mainWindow.MainWindowDisplayVM.GetCompoundName(res.MspDbID);
                }
            }
            else
                initialize(mainWindow, mainWindow.AnalysisFiles[mainWindow.FocusedFileID].PeakAreaBeanCollection.Count);
            worker.DoWork += workerForPeak_DoWork;
            worker.RunWorkerCompleted += workerForPeak_FileChange_RunWorkerCompleted;
            pbw.Title = "Progress of preparing peak spot table viewer";
            pbw.ProgressBar_Label.Content = "Peak table viewer";
            pbw.Show();
            worker.RunWorkerAsync(new Object[] { mainWindow });
        }

        private void workerForPeak_FileChange_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            this.pbw.Close();
            object[] arg = (object[])e.Result;
            var mainWindow = (MainWindow)arg[0];
            var source = (ObservableCollection<PeakSpotRow>)arg[1];
            worker.Dispose();


            var id = source[0].Ms1DecRes != null ? mainWindow.FocusedMS1DecID : mainWindow.FocusedPeakID;
            mainWindow.PeakSpotTableViewer.ChangeSource(source, id);
            mainWindow.MainWindowDisplayVM.Refresh();

            mainWindow.PeakSpotTableViewer.PeakSpotTableViewerVM.PropertyChanged -= mainWindow.PeakTableViewer_propertyChanged;
            mainWindow.PeakSpotTableViewer.PeakSpotTableViewerVM.PropertyChanged += mainWindow.PeakTableViewer_propertyChanged;
            mainWindow.PeakSpotTableViewer.Closed -= mainWindow.PeakSpotTableViewerClose;
            mainWindow.PeakSpotTableViewer.Closed += mainWindow.PeakSpotTableViewerClose;
            if (mainWindow.PeakSpotTableViewer.DataGrid_RawData.SelectedItem != null)
                mainWindow.PeakSpotTableViewer.DataGrid_RawData.ScrollIntoView(mainWindow.PeakSpotTableViewer.DataGrid_RawData.SelectedItem);
            mainWindow.PeakSpotTableViewer.UpdateLayout();

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
            mainWindow.PeakSpotTableViewer.IsEnabled = true;
        }
        #endregion

    }
}
