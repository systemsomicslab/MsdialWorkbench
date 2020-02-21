using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Interaction logic for PcaResultWin.xaml
    /// </summary>
    public partial class PcaResultWin : Window, INotifyPropertyChanged
    {
        private MainWindow mainWindow;
        private PrincipalComponentAnalysisResult pcaResult;

        public PcaResultWin(MainWindow mainWindow, PrincipalComponentAnalysisResult pcaResult)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.pcaResult = pcaResult;

            principalComponentAnalysisUserControlSetting();
        }

        #region context menu items
        private void contextMenu_SaveImageAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            SaveImageAsWin window = new SaveImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_CopyImageAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            CopyImageAsWin window = new CopyImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_SavePcaTableAs_Click(object sender, RoutedEventArgs e)
        {
            if (this.pcaResult == null || this.ComboBox_PcaScoreplotXaxis.SelectedIndex < 0 || this.ComboBox_PcaScoreplotYaxis.SelectedIndex < 0) return;

            SaveDataTableAsTextWin window = new SaveDataTableAsTextWin(this.pcaResult);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_CopyPcaTableAs_Click(object sender, RoutedEventArgs e)
        {
            if (this.pcaResult == null || this.ComboBox_PcaScoreplotXaxis.SelectedIndex < 0 || this.ComboBox_PcaScoreplotYaxis.SelectedIndex < 0) return;

            TableExportUtility.CopyToClipboardPcaTableAsTextFormat(this.pcaResult);
        }
        #endregion

        #region method for score and loading plot viewer
        private void principalComponentAnalysisUserControlSetting()
        {
            int pcNumber = this.pcaResult.ContributionCollection.Count;
            string[] pcArray = new string[pcNumber];
            for (int i = 0; i < pcNumber; i++) { pcArray[i] = (i + 1).ToString(); }
            this.ComboBox_PcaScoreplotXaxis.ItemsSource = pcArray;
            this.ComboBox_PcaScoreplotYaxis.ItemsSource = pcArray;
            this.ComboBox_PcaLoadingplotXaxis.ItemsSource = pcArray;
            this.ComboBox_PcaLoadingplotYaxis.ItemsSource = pcArray;
            this.ComboBox_ScorePlotSize.ItemsSource = new string[] { "1", "2", "3", "4", "5", "6", "8", "10", "12", "14", "16", "20", "24" };
            this.ComboBox_LoadingPlotSize.ItemsSource = new string[] { "1", "2", "3", "4", "5", "6", "8", "10", "12", "14", "16", "20", "24" };
            this.ComboBox_ScoreplotLabelType.ItemsSource = new string[] { "None", "X value", "Y value", "Label" };
            this.ComboBox_LoadingplotLabelType.ItemsSource = new string[] { "None", "X value", "Y value", "Label" };
            this.ComboBox_PcaScoreplotXaxis.SelectedIndex = 0;
            this.ComboBox_PcaScoreplotYaxis.SelectedIndex = 1;
            this.ComboBox_PcaLoadingplotXaxis.SelectedIndex = 0;
            this.ComboBox_PcaLoadingplotYaxis.SelectedIndex = 1;
            this.ComboBox_ScorePlotSize.SelectedIndex = 0;
            this.ComboBox_LoadingPlotSize.SelectedIndex = 0;
            this.ComboBox_ScoreplotLabelType.SelectedIndex = 0;
            this.ComboBox_LoadingplotLabelType.SelectedIndex = 0;

            string xAxisTitle = "PC " + pcArray[0] + " : " + Math.Round(this.pcaResult.ContributionCollection[0], 2).ToString() + " %";
            string yAxisTitle = "PC " + pcArray[1] + " : " + Math.Round(this.pcaResult.ContributionCollection[1], 2).ToString() + " %";

            ObservableCollection<double> xAxisScoreCollection = new ObservableCollection<double>(this.pcaResult.ScorePointsCollection[0]);
            ObservableCollection<double> yAxisScoreCollection = new ObservableCollection<double>(this.pcaResult.ScorePointsCollection[1]);
            ObservableCollection<double> xAxisLoadingCollection = new ObservableCollection<double>(this.pcaResult.LoadingPointsCollection[0]);
            ObservableCollection<double> yAxisLoadingCollection = new ObservableCollection<double>(this.pcaResult.LoadingPointsCollection[1]);

            PairwisePlotBean scorePlotBean = new PairwisePlotBean("Score plot", xAxisTitle, yAxisTitle, xAxisScoreCollection, yAxisScoreCollection, this.pcaResult.ScoreLabelCollection, this.pcaResult.ScoreBrushCollection, PairwisePlotDisplayLabel.None);
            PairwisePlotBean loadingPlotBean = new PairwisePlotBean("Loading plot", xAxisTitle, yAxisTitle, xAxisLoadingCollection, yAxisLoadingCollection, this.pcaResult.LoadingLabelCollecction, this.pcaResult.LoadingBrushCollection, PairwisePlotDisplayLabel.None);

            this.PcaScorePlotViewUI.Content = new PairwisePlotUI(scorePlotBean);
            this.PcaLoadingPlotViewUI.Content = new PairwisePlotUI(loadingPlotBean);
        }

        private void pcaScorePlot_ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.pcaResult == null) return;
            ((PairwisePlotUI)this.PcaScorePlotViewUI.Content).PairwisePlotFE.ResetGraphDisplayRange();
        }

        private void pcaLoadingPlot_ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.pcaResult == null) return;
            ((PairwisePlotUI)this.PcaLoadingPlotViewUI.Content).PairwisePlotFE.ResetGraphDisplayRange();
        }

        private void pcaScorePlotXaxisComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.pcaResult == null || this.ComboBox_PcaScoreplotXaxis.SelectedIndex < 0 || this.ComboBox_PcaLoadingplotYaxis.SelectedIndex < 0) return;
            int xAxisID = this.ComboBox_PcaScoreplotXaxis.SelectedIndex;
            int yAxisID = this.ComboBox_PcaScoreplotYaxis.SelectedIndex;
            this.ComboBox_PcaLoadingplotXaxis.SelectedIndex = xAxisID;
            this.ComboBox_PcaLoadingplotYaxis.SelectedIndex = yAxisID;

            string xAxisTitle = "PC " + (xAxisID + 1).ToString() + " : " + Math.Round(this.pcaResult.ContributionCollection[xAxisID], 2).ToString() + " %";
            string yAxisTitle = "PC " + (yAxisID + 1).ToString() + " : " + Math.Round(this.pcaResult.ContributionCollection[yAxisID], 2).ToString() + " %";

            ObservableCollection<double> xAxisScoreCollection = new ObservableCollection<double>(this.pcaResult.ScorePointsCollection[xAxisID]);
            ObservableCollection<double> yAxisScoreCollection = new ObservableCollection<double>(this.pcaResult.ScorePointsCollection[yAxisID]);
            ObservableCollection<double> xAxisLoadingCollection = new ObservableCollection<double>(this.pcaResult.LoadingPointsCollection[xAxisID]);
            ObservableCollection<double> yAxisLoadingCollection = new ObservableCollection<double>(this.pcaResult.LoadingPointsCollection[yAxisID]);

            PairwisePlotBean scorePlotBean = new PairwisePlotBean("Score plot", xAxisTitle, yAxisTitle, xAxisScoreCollection, yAxisScoreCollection, this.pcaResult.ScoreLabelCollection, this.pcaResult.ScoreBrushCollection, PairwisePlotDisplayLabel.None);
            PairwisePlotBean loadingPlotBean = new PairwisePlotBean("Loading plot", xAxisTitle, yAxisTitle, xAxisLoadingCollection, yAxisLoadingCollection, this.pcaResult.LoadingLabelCollecction, this.pcaResult.LoadingBrushCollection, PairwisePlotDisplayLabel.None);

            this.PcaScorePlotViewUI.Content = new PairwisePlotUI(scorePlotBean);
            this.PcaLoadingPlotViewUI.Content = new PairwisePlotUI(loadingPlotBean);
            this.ComboBox_ScoreplotLabelType.SelectedIndex = 0;
            this.ComboBox_LoadingplotLabelType.SelectedIndex = 0;
        }

        private void pcaLoadingPlotXaxisComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.pcaResult == null || this.ComboBox_PcaScoreplotXaxis.SelectedIndex < 0 || this.ComboBox_PcaLoadingplotYaxis.SelectedIndex < 0) return;
            int xAxisID = this.ComboBox_PcaLoadingplotXaxis.SelectedIndex;
            int yAxisID = this.ComboBox_PcaLoadingplotYaxis.SelectedIndex;
            this.ComboBox_PcaScoreplotXaxis.SelectedIndex = xAxisID;
            this.ComboBox_PcaScoreplotYaxis.SelectedIndex = yAxisID;

            string xAxisTitle = "PC " + (xAxisID + 1).ToString() + " : " + Math.Round(this.pcaResult.ContributionCollection[xAxisID], 2).ToString() + " %";
            string yAxisTitle = "PC " + (yAxisID + 1).ToString() + " : " + Math.Round(this.pcaResult.ContributionCollection[yAxisID], 2).ToString() + " %";

            ObservableCollection<double> xAxisScoreCollection = new ObservableCollection<double>(this.pcaResult.ScorePointsCollection[xAxisID]);
            ObservableCollection<double> yAxisScoreCollection = new ObservableCollection<double>(this.pcaResult.ScorePointsCollection[yAxisID]);
            ObservableCollection<double> xAxisLoadingCollection = new ObservableCollection<double>(this.pcaResult.LoadingPointsCollection[xAxisID]);
            ObservableCollection<double> yAxisLoadingCollection = new ObservableCollection<double>(this.pcaResult.LoadingPointsCollection[yAxisID]);

            PairwisePlotBean scorePlotBean = new PairwisePlotBean("Score plot", xAxisTitle, yAxisTitle, xAxisScoreCollection, yAxisScoreCollection, this.pcaResult.ScoreLabelCollection, this.pcaResult.ScoreBrushCollection, PairwisePlotDisplayLabel.None);
            PairwisePlotBean loadingPlotBean = new PairwisePlotBean("Loading plot", xAxisTitle, yAxisTitle, xAxisLoadingCollection, yAxisLoadingCollection, this.pcaResult.LoadingLabelCollecction, this.pcaResult.LoadingBrushCollection, PairwisePlotDisplayLabel.None);

            this.PcaScorePlotViewUI.Content = new PairwisePlotUI(scorePlotBean);
            this.PcaLoadingPlotViewUI.Content = new PairwisePlotUI(loadingPlotBean);
            this.ComboBox_ScoreplotLabelType.SelectedIndex = 0;
            this.ComboBox_LoadingplotLabelType.SelectedIndex = 0;
        }

        private void pcaScorePlotYaxisComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.pcaResult == null || this.ComboBox_PcaScoreplotXaxis.SelectedIndex < 0 || this.ComboBox_PcaLoadingplotYaxis.SelectedIndex < 0) return;
            int xAxisID = this.ComboBox_PcaScoreplotXaxis.SelectedIndex;
            int yAxisID = this.ComboBox_PcaScoreplotYaxis.SelectedIndex;
            this.ComboBox_PcaLoadingplotXaxis.SelectedIndex = xAxisID;
            this.ComboBox_PcaLoadingplotYaxis.SelectedIndex = yAxisID;

            string xAxisTitle = "PC " + (xAxisID + 1).ToString() + " : " + Math.Round(this.pcaResult.ContributionCollection[xAxisID], 2).ToString() + " %";
            string yAxisTitle = "PC " + (yAxisID + 1).ToString() + " : " + Math.Round(this.pcaResult.ContributionCollection[yAxisID], 2).ToString() + " %";

            ObservableCollection<double> xAxisScoreCollection = new ObservableCollection<double>(this.pcaResult.ScorePointsCollection[xAxisID]);
            ObservableCollection<double> yAxisScoreCollection = new ObservableCollection<double>(this.pcaResult.ScorePointsCollection[yAxisID]);
            ObservableCollection<double> xAxisLoadingCollection = new ObservableCollection<double>(this.pcaResult.LoadingPointsCollection[xAxisID]);
            ObservableCollection<double> yAxisLoadingCollection = new ObservableCollection<double>(this.pcaResult.LoadingPointsCollection[yAxisID]);

            PairwisePlotBean scorePlotBean = new PairwisePlotBean("Score plot", xAxisTitle, yAxisTitle, xAxisScoreCollection, yAxisScoreCollection, this.pcaResult.ScoreLabelCollection, this.pcaResult.ScoreBrushCollection, PairwisePlotDisplayLabel.None);
            PairwisePlotBean loadingPlotBean = new PairwisePlotBean("Loading plot", xAxisTitle, yAxisTitle, xAxisLoadingCollection, yAxisLoadingCollection, this.pcaResult.LoadingLabelCollecction, this.pcaResult.LoadingBrushCollection, PairwisePlotDisplayLabel.None);

            this.PcaScorePlotViewUI.Content = new PairwisePlotUI(scorePlotBean);
            this.PcaLoadingPlotViewUI.Content = new PairwisePlotUI(loadingPlotBean);
            this.ComboBox_ScoreplotLabelType.SelectedIndex = 0;
            this.ComboBox_LoadingplotLabelType.SelectedIndex = 0;
        }

        private void pcaLoadingPlotYaxisComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.pcaResult == null || this.ComboBox_PcaScoreplotXaxis.SelectedIndex < 0 || this.ComboBox_PcaLoadingplotYaxis.SelectedIndex < 0) return;
            int xAxisID = this.ComboBox_PcaLoadingplotXaxis.SelectedIndex;
            int yAxisID = this.ComboBox_PcaLoadingplotYaxis.SelectedIndex;
            this.ComboBox_PcaScoreplotXaxis.SelectedIndex = xAxisID;
            this.ComboBox_PcaScoreplotYaxis.SelectedIndex = yAxisID;

            string xAxisTitle = "PC " + (xAxisID + 1).ToString() + " : " + Math.Round(this.pcaResult.ContributionCollection[xAxisID], 2).ToString() + " %";
            string yAxisTitle = "PC " + (yAxisID + 1).ToString() + " : " + Math.Round(this.pcaResult.ContributionCollection[yAxisID], 2).ToString() + " %";

            ObservableCollection<double> xAxisScoreCollection = new ObservableCollection<double>(this.pcaResult.ScorePointsCollection[xAxisID]);
            ObservableCollection<double> yAxisScoreCollection = new ObservableCollection<double>(this.pcaResult.ScorePointsCollection[yAxisID]);
            ObservableCollection<double> xAxisLoadingCollection = new ObservableCollection<double>(this.pcaResult.LoadingPointsCollection[xAxisID]);
            ObservableCollection<double> yAxisLoadingCollection = new ObservableCollection<double>(this.pcaResult.LoadingPointsCollection[yAxisID]);

            PairwisePlotBean scorePlotBean = new PairwisePlotBean("Score plot", xAxisTitle, yAxisTitle, xAxisScoreCollection, yAxisScoreCollection, this.pcaResult.ScoreLabelCollection, this.pcaResult.ScoreBrushCollection, PairwisePlotDisplayLabel.None);
            PairwisePlotBean loadingPlotBean = new PairwisePlotBean("Loading plot", xAxisTitle, yAxisTitle, xAxisLoadingCollection, yAxisLoadingCollection, this.pcaResult.LoadingLabelCollecction, this.pcaResult.LoadingBrushCollection, PairwisePlotDisplayLabel.None);

            this.PcaScorePlotViewUI.Content = new PairwisePlotUI(scorePlotBean);
            this.PcaLoadingPlotViewUI.Content = new PairwisePlotUI(loadingPlotBean);
            this.ComboBox_ScoreplotLabelType.SelectedIndex = 0;
            this.ComboBox_LoadingplotLabelType.SelectedIndex = 0;
        }

        private void scoreplotLabeltypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.pcaResult == null || this.ComboBox_PcaScoreplotXaxis.SelectedIndex < 0 || this.ComboBox_PcaScoreplotYaxis.SelectedIndex < 0) return;

            if (((ComboBox)sender).SelectedIndex == 0) { ((PairwisePlotUI)this.PcaScorePlotViewUI.Content).PairwisePlotBean.DisplayLabel = PairwisePlotDisplayLabel.None; }
            else if (((ComboBox)sender).SelectedIndex == 1) { ((PairwisePlotUI)this.PcaScorePlotViewUI.Content).PairwisePlotBean.DisplayLabel = PairwisePlotDisplayLabel.X; }
            else if (((ComboBox)sender).SelectedIndex == 2) { ((PairwisePlotUI)this.PcaScorePlotViewUI.Content).PairwisePlotBean.DisplayLabel = PairwisePlotDisplayLabel.Y; }
            else if (((ComboBox)sender).SelectedIndex == 3) { ((PairwisePlotUI)this.PcaScorePlotViewUI.Content).PairwisePlotBean.DisplayLabel = PairwisePlotDisplayLabel.Label; }

            ((PairwisePlotUI)this.PcaScorePlotViewUI.Content).RefreshUI();
        }

        private void loadingplotLabeltypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.pcaResult == null || this.ComboBox_PcaLoadingplotXaxis.SelectedIndex < 0 || this.ComboBox_PcaLoadingplotYaxis.SelectedIndex < 0) return;

            if (((ComboBox)sender).SelectedIndex == 0) { ((PairwisePlotUI)this.PcaLoadingPlotViewUI.Content).PairwisePlotBean.DisplayLabel = PairwisePlotDisplayLabel.None; }
            else if (((ComboBox)sender).SelectedIndex == 1) { ((PairwisePlotUI)this.PcaLoadingPlotViewUI.Content).PairwisePlotBean.DisplayLabel = PairwisePlotDisplayLabel.X; }
            else if (((ComboBox)sender).SelectedIndex == 2) { ((PairwisePlotUI)this.PcaLoadingPlotViewUI.Content).PairwisePlotBean.DisplayLabel = PairwisePlotDisplayLabel.Y; }
            else if (((ComboBox)sender).SelectedIndex == 3) { ((PairwisePlotUI)this.PcaLoadingPlotViewUI.Content).PairwisePlotBean.DisplayLabel = PairwisePlotDisplayLabel.Label; }

            ((PairwisePlotUI)this.PcaLoadingPlotViewUI.Content).RefreshUI();
        }

        private void scoreplotSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.pcaResult == null) return;
            ((PairwisePlotUI)this.PcaScorePlotViewUI.Content).PlotSize = double.Parse(((ComboBox)sender).SelectedValue.ToString());
            ((PairwisePlotUI)this.PcaScorePlotViewUI.Content).RefreshUI();
        }

        private void loadingplotSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.pcaResult == null) return;
            ((PairwisePlotUI)this.PcaLoadingPlotViewUI.Content).PlotSize = double.Parse(((ComboBox)sender).SelectedValue.ToString());
            ((PairwisePlotUI)this.PcaLoadingPlotViewUI.Content).RefreshUI();
        }
        #endregion

        #region required methods for INotifyPropertyChanged
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler eventHandlers = this.PropertyChanged;
            if (null != eventHandlers)
                eventHandlers(this, e);
        }
        #endregion // Required Methods for INotifyPropertyChanged
    }
}
