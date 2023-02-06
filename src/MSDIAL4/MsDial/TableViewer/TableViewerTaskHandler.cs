using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Msdial.Lcms.Dataprocess.Utility;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class TableViewerTaskHandler
    {
        private ProgressBarWin pbw;
        private int progressValues = 0;
        private int stepSize = 0;
        private int maxProgressValue = 100;
        public TableViewerTaskHandler() { }

        public async Task StartUpPeakSpotTableViewer(MainWindow mainWindow) {
            if (mainWindow.ProjectProperty.Ionization == Ionization.EI) {
                initialize(mainWindow, mainWindow.Ms1DecResults.Count);
                foreach (var res in mainWindow.Ms1DecResults) {
                    res.MetaboliteName = mainWindow.MainWindowDisplayVM.GetCompoundName(res.MspDbID);
                }
            }
            else {
                initialize(mainWindow, mainWindow.AnalysisFiles[mainWindow.FocusedFileID].PeakAreaBeanCollection.Count);
            }
            pbw.Title = "Progress of preparing peak spot table viewer";
            pbw.ProgressBar_Label.Content = "Peak table viewer";
            pbw.Show();
            var source = await Task.Run(() => MainWorkerPeakView(mainWindow));
            CompleteProgress();
            FinalizeMethodPeakView(mainWindow, source);
        }

        public async Task StartUpAlignmentSpotTableViewer(MainWindow mainWindow) {
            initialize(mainWindow, mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection.Count);
            pbw.Title = "Progress of preparing alignment spot table viewer";
            pbw.ProgressBar_Label.Content = "Alignment table viewer";
            pbw.Show();
            var source = await Task.Run(() => MainWorkerAlignmentView(mainWindow));
            CompleteProgress();
            FinalizeMethodAlignmentView(mainWindow, source);
        }

        #region initializer, common
        private void initialize(MainWindow mainWindow, int num) {
            maxProgressValue = num < maxProgressValue ? num : maxProgressValue;
            pbw = new ProgressBarWin();
            pbw.Owner = mainWindow;
            pbw.ProgressView.Minimum = 0;
            pbw.ProgressView.Maximum = maxProgressValue;
            pbw.ProgressView.Value = 0;
            pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            stepSize = num / maxProgressValue;
        }
        private void AddProgress() {
            progressValues += 1;
            if (progressValues % stepSize == 0)
                this.pbw.AddProgressValue(1);
        }

        private void CompleteProgress()
        {
            this.pbw.SetProgressValue(maxProgressValue);
        }

        #endregion
        #region For peak spot table viewer
        private ObservableCollection<PeakSpotRow> MainWorkerPeakView(MainWindow mainWindow) {
            var source = new ObservableCollection<PeakSpotRow>();
            var projectProperty = mainWindow.ProjectProperty;
            var id = mainWindow.FocusedPeakID;

            if (mainWindow.ProjectProperty.Ionization == Ionization.EI) {
                var spectrumList = mainWindow.GcmsSpectrumList;
                var paramGC = mainWindow.AnalysisParamForGC;
                var ms1DecResults = mainWindow.Ms1DecResults;
                var rows = new PeakSpotRow[ms1DecResults.Count];
                Parallel.For(0, ms1DecResults.Count, (i) =>
                {
                    rows[i] = new PeakSpotRow(spectrumList, ms1DecResults[i], projectProperty, paramGC);
                    AddProgress();
                });
                source = new ObservableCollection<PeakSpotRow>(rows);
            }
            else {
                var param = mainWindow.AnalysisParamForLC;
                if (param.IsIonMobility) {
                    var peakAreaBean = mainWindow.AnalysisFiles[mainWindow.FocusedFileID].PeakAreaBeanCollection;
                    var accumulatedMs1Spectra = mainWindow.AccumulatedMs1Specra;
                    var allSpectra = mainWindow.LcmsSpectrumCollection;
                    var rows = new System.Collections.Concurrent.ConcurrentBag<PeakSpotRow>();
                    //for (var i = 0; i < peakAreaBean.Count; i++) {
                    Parallel.For(0, peakAreaBean.Count, (i) =>
                    {
                        var data = new PeakSpotRow(peakAreaBean[i].MasterPeakID, peakAreaBean[i], new DriftSpotBean(), accumulatedMs1Spectra, projectProperty, param);
                        rows.Add(data);

                        for (int j = 0; j < peakAreaBean[i].DriftSpots.Count; j++)
                        {
                            var driftSpot = peakAreaBean[i].DriftSpots[j];
                            data = new PeakSpotRow(driftSpot.MasterPeakID, peakAreaBean[i], driftSpot, allSpectra, projectProperty, param);
                            rows.Add(data);
                        }
                        AddProgress();
                        source = new ObservableCollection<PeakSpotRow>(rows.OrderBy(x => x.PeakID));
                    });
                }
                else {
                    var peakAreaBean = mainWindow.AnalysisFiles[mainWindow.FocusedFileID].PeakAreaBeanCollection;
                    var spectrumCollection = mainWindow.LcmsSpectrumCollection;
                    var rows = new PeakSpotRow[peakAreaBean.Count];
                    Parallel.For(0, peakAreaBean.Count, (i) =>
                    {
                        rows[i] = new PeakSpotRow(peakAreaBean[i], spectrumCollection, projectProperty, param);
                        AddProgress();
                    });
                    source = new ObservableCollection<PeakSpotRow>(rows);
                }
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return source;
        }

        private void FinalizeMethodPeakView(MainWindow mainWindow, ObservableCollection<PeakSpotRow> source) {
            var isIonMobility = mainWindow.ProjectProperty.Ionization == Ionization.ESI && mainWindow.AnalysisParamForLC.IsIonMobility;
            var focusedPeakID = isIonMobility ? mainWindow.FocusedMasterID : mainWindow.FocusedPeakID;
            if (mainWindow.ProjectProperty.Ionization == Ionization.EI) focusedPeakID = mainWindow.FocusedMS1DecID;
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
            //Mouse.OverrideCursor = null;
            //mainWindow.IsEnabled = true;
            this.pbw.Close();
        }
        #endregion

        #region For alignment spot table viewer
        private ObservableCollection<AlignmentSpotRow> MainWorkerAlignmentView(MainWindow mainWindow) {
            var alignmentProperty = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection;
            var analysisFiles = mainWindow.AnalysisFiles;
            var source = new ObservableCollection<AlignmentSpotRow>();
            var numClass = analysisFiles.Select(x => x.AnalysisFilePropertyBean.AnalysisFileClass).Distinct().Count();
            var project = mainWindow.ProjectProperty;
            var width = numClass > 10 ? 400 : 200;
            var barChartDisplayMode = mainWindow.BarChartDisplayMode;
            var isBoxPlot = mainWindow.ProjectProperty.IsBoxPlotForAlignmentResult;
            var isIonMobility = project.Ionization == Ionization.ESI && mainWindow.AnalysisParamForLC.IsIonMobility;

            if (isIonMobility)
            {
                //for (var i = 0; i < alignmentProperty.Count; i++)
                // Thread safe list
                var rows = new System.Collections.Concurrent.ConcurrentBag<AlignmentSpotRow>();
                Parallel.For(0, alignmentProperty.Count, (i) =>
                {
                    var spot = alignmentProperty[i];
                    var data = new AlignmentSpotRow(spot.MasterID, spot, new AlignedDriftSpotPropertyBean(),
                        analysisFiles, project, width, mainWindow.MspDB, barChartDisplayMode, isBoxPlot);
                    rows.Add(data);
                    foreach (var drift in spot.AlignedDriftSpots)
                    {
                        data = new AlignmentSpotRow(drift.MasterID, spot, drift, analysisFiles,
                            project, width, mainWindow.MspDB, barChartDisplayMode, isBoxPlot);
                        rows.Add(data);
                    }
                    AddProgress();
                //}
                });
                source = new ObservableCollection<AlignmentSpotRow>(rows.OrderBy(x => x.MasterID));
            }
            else
            {
                var rows = new AlignmentSpotRow[alignmentProperty.Count];
                //var option = new ParallelOptions { MaxDegreeOfParallelism = 5 };
                //Parallel.For(0, alignmentProperty.Count, option, (i) =>
                Parallel.For(0, alignmentProperty.Count, (i) => {
                    rows[i] = new AlignmentSpotRow(alignmentProperty[i], analysisFiles,
                       project, width, mainWindow.MspDB, barChartDisplayMode, isBoxPlot);
                    AddProgress();
                });
                source = new ObservableCollection<AlignmentSpotRow>(rows);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return source;
        }

        private void FinalizeMethodAlignmentView(MainWindow mainWindow, ObservableCollection<AlignmentSpotRow> source) {
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
            this.pbw.Close();
            //Mouse.OverrideCursor = null;
            //mainWindow.IsEnabled = true;

        }
        #endregion

        #region Selecting file changed method
        public async Task FileChangePeakSpotTableViewer(MainWindow mainWindow) {
            if (mainWindow.ProjectProperty.Ionization == Ionization.EI) {
                initialize(mainWindow, mainWindow.Ms1DecResults.Count);
                foreach (var res in mainWindow.Ms1DecResults) {
                    res.MetaboliteName = mainWindow.MainWindowDisplayVM.GetCompoundName(res.MspDbID);
                }
            }
            else
                initialize(mainWindow, mainWindow.AnalysisFiles[mainWindow.FocusedFileID].PeakAreaBeanCollection.Count);
            pbw.Title = "Progress of preparing peak spot table viewer";
            pbw.ProgressBar_Label.Content = "Peak table viewer";
            pbw.Show();
            var source = await Task.Run(() => MainWorkerPeakView(mainWindow));
            CompleteProgress();
            FinalizeMethodChangePeakView(mainWindow, source);
        }

        private void FinalizeMethodChangePeakView(MainWindow mainWindow, ObservableCollection<PeakSpotRow> source) {
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

        //   Mouse.OverrideCursor = null;
        //    mainWindow.IsEnabled = true;
            mainWindow.PeakSpotTableViewer.IsEnabled = true;
            this.pbw.Close();
        }
        #endregion
    }
}
