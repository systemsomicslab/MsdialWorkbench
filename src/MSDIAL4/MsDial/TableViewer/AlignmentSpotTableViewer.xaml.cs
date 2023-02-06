using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Drawing;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Accord.Statistics.Testing;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// AlignmentSpotTableViewer.xaml の相互作用ロジック
    /// </summary>
    public partial class AlignmentSpotTableViewer : Window
    {
        public AlignmentSpotTableViewerVM AlignmentSpotTableViewerVM { set; get; }
        public AlignmentSpotTableViewer() { }
        public List<MspFormatCompoundInformationBean> MspDB { get; set; }
        private TableViewer.IdentifiedCompoundGroup compoundGroupWindow { get; set; }
        public TableViewer.SpotRelationTable SpotRelationsWindow { get; set; }

        public AlignmentSpotTableViewer(ObservableCollection<AlignmentSpotRow> source, int id, List<MspFormatCompoundInformationBean> msps) {
            InitializeComponent();
            if (source[0].AlignmentPropertyBean.QuantMass > 0) {
                changeColumnForGC();
            }
            else if (source[0].AlignedDriftSpotPropertyBean != null) {
                changeColumnForIonMobility();
            }
            else
            {
                AddCorrelationColumn();
            }
            this.MspDB = msps;
            this.AlignmentSpotTableViewerVM = new AlignmentSpotTableViewerVM(this, source);
            this.DataContext = this.AlignmentSpotTableViewerVM;
            this.AlignmentSpotTableViewerVM.SelectedData = this.AlignmentSpotTableViewerVM.Source[id];
        }


        public void ResizeBarChartColumn(int numClass) {
            if (numClass > 10) {
                this.Width = 1240;
                var dg = this.DataGrid_RawData;
                ((DataGridTemplateColumn)dg.Columns[dg.Columns.Count - 1]).Width = new DataGridLength(450);
            }
        }


        private void Button_ShowCompTable_Click(object sender, RoutedEventArgs e) {
            compoundGroupWindow = new TableViewer.IdentifiedCompoundGroup(this, MspDB);
            compoundGroupWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            compoundGroupWindow.Owner = this;
            compoundGroupWindow.Show();
        }

        private void Button_ShowSpotRelation_Click(object sender, RoutedEventArgs e) {
            SpotRelationsWindow = new TableViewer.SpotRelationTable(this);
            SpotRelationsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SpotRelationsWindow.Owner = this;
            SpotRelationsWindow.Show();
        }

        private void Button_ResetFilter_Click(object sender, RoutedEventArgs e) {
            this.AlignmentSpotTableViewerVM.InitializeFilter();
        }

        private void Button_AdditionalFilter_Click(object sender, RoutedEventArgs e)
        {
            this.AlignmentSpotTableViewerVM.SetAdditionalFilterSettings();
        }

        private void Closing_Method(object sender, EventArgs e) {
            this.DataGrid_RawData.CommitEdit();
            this.DataGrid_RawData.CancelEdit();
            foreach (var s in this.AlignmentSpotTableViewerVM.Source) {
                s.Image = null;
            }
            this.DataContext = null;
            if (SpotRelationsWindow != null) SpotRelationsWindow.Close();
            if (compoundGroupWindow != null) compoundGroupWindow.Close();
        }

        #region add correlation
        public void AddCorrelationColumn()
        {
            var dg = DataGrid_RawData;
            var correlationColumn = new DataGridTextColumn()
            {
                Header = "Correlation",
                IsReadOnly = true,
                CanUserSort = true,
                Binding = new Binding("Correlation") { StringFormat = "0.00" },
                HeaderStyle = dg.ColumnHeaderStyle,
                CellStyle = dg.CellStyle,
                Width = new DataGridLength(80)
            };
            dg.Columns.Insert(7, correlationColumn);

        }
        #endregion

        #region changeColumnForGC
        private void changeColumnForGC() {

            var dg = DataGrid_RawData;

            // Quant mass
            ((DataGridTextColumn)dg.Columns[2]).Binding = new Binding("AlignmentPropertyBean.QuantMass") { StringFormat = "0.0" };
            ((DataGridTextColumn)dg.Columns[2]).Header = "Quant mass";

            // Remove Type Column
            dg.Columns.RemoveAt(3);

            // add RI
            var RIcolumn = new DataGridTextColumn() {
                Header = "RI",
                IsReadOnly = true,
                CanUserSort = true,
                Binding = new Binding("AlignmentPropertyBean.CentralRetentionIndex") { StringFormat = "0.0" },
                HeaderStyle = dg.ColumnHeaderStyle,
                CellStyle = dg.CellStyle,
                Width = new DataGridLength(0.8, DataGridLengthUnitType.Star)
            };
            dg.Columns.Insert(2, RIcolumn);

            dg.UpdateLayout();
        }
        #endregion

        #region changeColumnForIonMobility
        private void changeColumnForIonMobility() {

            var dg = DataGrid_RawData;

            ((DataGridTextColumn)dg.Columns[0]).Binding = new Binding("MasterID");
            ((DataGridTextColumn)dg.Columns[1]).Binding = new Binding("RT") { StringFormat = "0.00" };
            ((DataGridTextColumn)dg.Columns[2]).Binding = new Binding("Mz");
            ((DataGridTextColumn)dg.Columns[3]).Binding = new Binding("Type");
            ((DataGridTextColumn)dg.Columns[4]).Binding = new Binding("Fill");
            ((DataGridTextColumn)dg.Columns[5]).Binding = new Binding("MetaboliteName");
            ((DataGridTextColumn)dg.Columns[6]).Binding = new Binding("Comment");
            ((DataGridTextColumn)dg.Columns[7]).Binding = new Binding("SN") { StringFormat = "0.00" };
            ((DataGridTextColumn)dg.Columns[8]).Binding = new Binding("Anova") { StringFormat = "0.00E00" };
            ((DataGridTextColumn)dg.Columns[9]).Binding = new Binding("FC") { StringFormat = "0.00" };

            // add mobility
            var mobilityColumn = new DataGridTextColumn() {
                Header = "Mobility",
                IsReadOnly = true,
                CanUserSort = true,
                Binding = new Binding("Mobility") { StringFormat = "0.000" },
                HeaderStyle = dg.ColumnHeaderStyle,
                CellStyle = dg.CellStyle,
                Width = new DataGridLength(0.8, DataGridLengthUnitType.Star)
            };
            dg.Columns.Insert(2, mobilityColumn);

            // add ccs
            var ccsColumn = new DataGridTextColumn() {
                Header = "CCS",
                IsReadOnly = true,
                CanUserSort = true,
                Binding = new Binding("Ccs") { StringFormat = "0.00" },
                HeaderStyle = dg.ColumnHeaderStyle,
                CellStyle = dg.CellStyle,
                Width = new DataGridLength(0.8, DataGridLengthUnitType.Star)
            };
            dg.Columns.Insert(3, ccsColumn);

            dg.UpdateLayout();
        }
        #endregion
    }

    public class AlignmentSpotTableViewerVM : ViewModelBase
    {
        #region member variables and properties
        private AlignmentSpotTableViewer alignmentTableViewer;
        private AlignmentSpotTableViewerVM alignmentTableViewerVM;
        private AlignmentSpotRow selectedData;
        private ObservableCollection<AlignmentSpotRow> source;
        private string _metaboliteNameFilter;
        private string _commentFilter;
        private bool isLc;
        private bool isIonMobility;
        private bool isMsFinderExporter;

        // for correlations
        public bool IsCalcCorrelation { get; set; } = false;
        public float RetentionTimeTolForCorrelation { get; set; } = 100;

        // filter
        public TableViewer.FilteredTable FilteredTable { get; set; }
        public TableViewer.FilterSettings Settings { get; set; }

        // MS-FINDER
        public DelegateCommand ChangetToMsFinderExportWindow { get; set; }

        // Ctrl + D
        public DelegateCommand CtrlDCommand { get; set; }


        public AlignmentSpotRow SelectedData {
            get { return selectedData; }
            set {
                if (value != null && selectedData != value) {
                    selectedData = value;
                    UpdateCorrelationData();
                    if (alignmentTableViewer.SpotRelationsWindow != null) alignmentTableViewer.SpotRelationsWindow.ChangeCharacter(alignmentTableViewer);
                    OnPropertyChanged("SelectedData");
                }
            }
        }
        public ObservableCollection<AlignmentSpotRow> Source {
            get { return source; }
            set { source = value; }
        }
        public ICollectionView SourceView {
            get { return FilteredTable.View; }
        }

        public int NumRows {
            get { return Settings.NumRows; }
        }

        public string MetaboliteNameFilter {
            get { return _metaboliteNameFilter; }
            set { if (_metaboliteNameFilter == value) return; _metaboliteNameFilter = value; this.Settings.MetaboliteNameFilter = value; OnPropertyChanged("MetaboliteNameFilter"); }
        }
        public string CommentFilter {
            get { return _commentFilter; }
            set { if (_commentFilter == value) return; _commentFilter = value; this.Settings.CommentFilter = value; OnPropertyChanged("CommentFilter"); }
        }
        #endregion

        public AlignmentSpotTableViewerVM(AlignmentSpotTableViewer alignmentTableViewer, ObservableCollection<AlignmentSpotRow> source) {
            this.alignmentTableViewer = alignmentTableViewer;
            this.alignmentTableViewerVM = this;
            this.Source = source;
            this.FilteredTable = new TableViewer.FilteredTable(source);
            this.Settings = new TableViewer.FilterSettings(this.FilteredTable.View, TableViewer.FilterSettings.ViewerType.Alignment);
            this.FilteredTable.View.Filter = this.Settings.SpotFilter;
            if (source[0].AlignmentPropertyBean.QuantMass > 0) isLc = false; else isLc = true;
            if (source[0].AlignmentPropertyBean.AlignedDriftSpots != null && source[0].AlignmentPropertyBean.AlignedDriftSpots.Count > 0) isIonMobility = true; else isIonMobility = false;

            InitializeFilter();
            SetCommands();
            if (isLc && !isIonMobility) IsCalcCorrelation = true;
        }

        private void SetCommands()
        {
            //Console.WriteLine("working set commands");
            ChangetToMsFinderExportWindow = new DelegateCommand(
                x => { ChangeToMsFinderExporterView(); }, null);
            CtrlDCommand = new DelegateCommand(x => { OnPropertyChanged("CtrlD"); }, null);
        }

        public void ChangeSelectedData(int id) {
            SelectedData = this.Source[id];
            if (this.alignmentTableViewer.DataGrid_RawData.SelectedItem != null) {
                var row = (AlignmentSpotRow)this.alignmentTableViewer.DataGrid_RawData.SelectedItem;
                if (isIonMobility && row.MasterID != SelectedData.MasterID) {
                    this.alignmentTableViewer.DataGrid_RawData.SelectedItem = SelectedData;
                }
                //Console.WriteLine(row.MetaboliteName);
                this.alignmentTableViewer.DataGrid_RawData.ScrollIntoView(this.alignmentTableViewer.DataGrid_RawData.SelectedItem);
            }
            this.alignmentTableViewer.UpdateLayout();
        }

        public void UpdateSelectedData(ObservableCollection<AnalysisFileBean> analysisFiles,
            ProjectPropertyBean projectProp, BarChartDisplayMode mode, bool isBoxPlot) {
            SelectedData.Update_Image(analysisFiles, projectProp, mode, isBoxPlot);
            SelectedData.Update_StatisticalValues(analysisFiles);
        }

        public void UpdateAllImages(ObservableCollection<AnalysisFileBean> analysisFiles,
            ProjectPropertyBean projectProp, BarChartDisplayMode mode, bool isBoxPlot) {
            foreach (var spot in source) {
                spot.Update_Image(analysisFiles, projectProp, mode, isBoxPlot);
                spot.Update_StatisticalValues(analysisFiles);
            }
        }

        public void UpdateCorrelationData()
        {
            var targetRow = SelectedData;
            if (targetRow == null || !this.IsCalcCorrelation) return;
            foreach (var r in this.Source)
            {
                if(Math.Abs(r.AlignmentPropertyBean.CentralRetentionTime - targetRow.AlignmentPropertyBean.CentralRetentionTime) > RetentionTimeTolForCorrelation)
                {
                    r.Correlation = 0;
                }
                else
                {
                    r.Correlation = IonAbundanceCorrelation(r.AlignmentPropertyBean, targetRow.AlignmentPropertyBean);
                }
            }
        }

        public static float IonAbundanceCorrelation(AlignmentPropertyBean spot1, AlignmentPropertyBean spot2)
        {
            // use all samples including Blank and QC
            var v1 = spot1.AlignedPeakPropertyBeanCollection.Select(n => n.Variable).ToArray();
            var v2 = spot2.AlignedPeakPropertyBeanCollection.Select(n => n.Variable).ToArray();
            return BasicMathematics.Coefficient(v1, v2);
        }

        public void SetAdditionalFilterSettings()
        {
            if (!isLc || isIonMobility)
            {
                MessageBox.Show("Sorry, this function supports LC-MS only.", "Error", MessageBoxButton.OK);
                return;
            }

            var win = new TableViewer.AdditionalFilteringSetting(this);
            win.Owner = alignmentTableViewer;
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();
        }

        public void InitializeFilter() {
            if (isLc) {
                setSlidersForLC();
                this.MetaboliteNameFilter = "";
                this.CommentFilter = "";
                this.Settings.ShortInchiKeyFilter = "";
            }
            else {
                setSlidersForGC();
                this.MetaboliteNameFilter = "";
                this.CommentFilter = "";
                this.Settings.ShortInchiKeyFilter = "";
            }
        }

        #region slider settings
        private void setCommonSlider() {
            //mz
            this.alignmentTableViewer.doubleSlider_MzFilter.LowerSlider.ValueChanged -= mzLowerSlider_ValueChanged;
            this.alignmentTableViewer.doubleSlider_MzFilter.LowerSlider.ValueChanged += mzLowerSlider_ValueChanged;
            this.alignmentTableViewer.doubleSlider_MzFilter.UpperSlider.ValueChanged -= mzUpperSlider_ValueChanged;
            this.alignmentTableViewer.doubleSlider_MzFilter.UpperSlider.ValueChanged += mzUpperSlider_ValueChanged;
            this.Settings.MzSliderLowerValue = (float)this.alignmentTableViewer.doubleSlider_MzFilter.LowerSlider.Value;
            this.Settings.MzSliderUpperValue = (float)this.alignmentTableViewer.doubleSlider_MzFilter.UpperSlider.Value;

            //rt
            this.alignmentTableViewer.doubleSlider_RtFilter.LowerSlider.ValueChanged -= rtLowerSlider_ValueChanged;
            this.alignmentTableViewer.doubleSlider_RtFilter.LowerSlider.ValueChanged += rtLowerSlider_ValueChanged;
            this.alignmentTableViewer.doubleSlider_RtFilter.UpperSlider.ValueChanged -= rtUpperSlider_ValueChanged;
            this.alignmentTableViewer.doubleSlider_RtFilter.UpperSlider.ValueChanged += rtUpperSlider_ValueChanged;
            this.Settings.RtSliderLowerValue = (float)this.alignmentTableViewer.doubleSlider_RtFilter.LowerSlider.Value;
            this.Settings.RtSliderUpperValue = (float)this.alignmentTableViewer.doubleSlider_RtFilter.UpperSlider.Value;
            this.alignmentTableViewer.doubleSlider_RtFilter.Maximum = this.source.Max(x => x.AlignmentPropertyBean.CentralRetentionTime);
            this.alignmentTableViewer.doubleSlider_RtFilter.UpperValue = this.alignmentTableViewer.doubleSlider_RtFilter.Maximum;
            this.alignmentTableViewer.doubleSlider_RtFilter.Minimum = this.source.Min(x => x.AlignmentPropertyBean.CentralRetentionTime);
            this.alignmentTableViewer.doubleSlider_RtFilter.LowerValue = this.alignmentTableViewer.doubleSlider_RtFilter.Minimum;

            // textBox -> slider
            this.Settings.PropertyChanged -= settings_propertyChanged;
            this.Settings.PropertyChanged += settings_propertyChanged;

        }
        private void setSlidersForLC() {
            setCommonSlider();
            // mz
            this.alignmentTableViewer.doubleSlider_MzFilter.Maximum = this.source.Max(x => x.AlignmentPropertyBean.CentralAccurateMass);
            this.alignmentTableViewer.doubleSlider_MzFilter.UpperValue = this.alignmentTableViewer.doubleSlider_MzFilter.Maximum;
            this.alignmentTableViewer.doubleSlider_MzFilter.Minimum = this.source.Min(x => x.AlignmentPropertyBean.CentralAccurateMass);
            this.alignmentTableViewer.doubleSlider_MzFilter.LowerValue = this.alignmentTableViewer.doubleSlider_MzFilter.Minimum;
        }

        private void setSlidersForGC() {
            setCommonSlider();
            // mz
            this.alignmentTableViewer.doubleSlider_MzFilter.Maximum = this.source.Max(x => x.AlignmentPropertyBean.QuantMass);
            this.alignmentTableViewer.doubleSlider_MzFilter.UpperValue = this.alignmentTableViewer.doubleSlider_MzFilter.Maximum;
            this.alignmentTableViewer.doubleSlider_MzFilter.Minimum = this.source.Min(x => x.AlignmentPropertyBean.QuantMass);
            this.alignmentTableViewer.doubleSlider_MzFilter.LowerValue = this.alignmentTableViewer.doubleSlider_MzFilter.Minimum;
        }


        private void settings_propertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "MzSliderLowerValue" && this.Settings.MzSliderLowerValue != this.alignmentTableViewer.doubleSlider_MzFilter.LowerSlider.Value) {
                this.alignmentTableViewer.doubleSlider_MzFilter.LowerSlider.Value = this.Settings.MzSliderLowerValue;
            }
            if (e.PropertyName == "MzSliderUpperValue" && this.Settings.MzSliderUpperValue != this.alignmentTableViewer.doubleSlider_MzFilter.UpperSlider.Value) {
                this.alignmentTableViewer.doubleSlider_MzFilter.UpperSlider.Value = this.Settings.MzSliderUpperValue;
            }
            if (e.PropertyName == "RtSliderLowerValue" && this.Settings.RtSliderLowerValue != this.alignmentTableViewer.doubleSlider_RtFilter.LowerSlider.Value) {
                this.alignmentTableViewer.doubleSlider_RtFilter.LowerSlider.Value = this.Settings.RtSliderLowerValue;
            }
            if (e.PropertyName == "RtSliderUpperValue" && this.Settings.RtSliderUpperValue != this.alignmentTableViewer.doubleSlider_RtFilter.UpperSlider.Value) {
                this.alignmentTableViewer.doubleSlider_RtFilter.UpperSlider.Value = this.Settings.RtSliderUpperValue;
            }
        }

        private void mzLowerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            this.Settings.MzSliderLowerValue = (float)this.alignmentTableViewer.doubleSlider_MzFilter.LowerSlider.Value;
        }

        private void mzUpperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            this.Settings.MzSliderUpperValue = (float)this.alignmentTableViewer.doubleSlider_MzFilter.UpperSlider.Value;
        }

        private void rtLowerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            this.Settings.RtSliderLowerValue = (float)this.alignmentTableViewer.doubleSlider_RtFilter.LowerSlider.Value;
        }

        private void rtUpperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            this.Settings.RtSliderUpperValue = (float)this.alignmentTableViewer.doubleSlider_RtFilter.UpperSlider.Value;
        }
        #endregion

        #region MS-FINDER export

        public void ChangeToMsFinderExporterView()
        {
            if (isMsFinderExporter) return;
            if(!isLc || isIonMobility)
            {
                MessageBox.Show("Sorry, this function supports LC-MS only.", "Error", MessageBoxButton.OK);
                return;
            }

            isMsFinderExporter = true;
            var viewer = this.alignmentTableViewer;

            viewer.Title = "MS-FINDER exporter: Alignment Spot Table";

            var dg = viewer.DataGrid_RawData;
            var b1 = new Binding("Checked") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            FrameworkElementFactory fact = new FrameworkElementFactory(typeof(CheckBox));

            fact.SetValue(CheckBox.IsCheckedProperty, b1);
            fact.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            fact.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            var cellTemp = new DataTemplate();
            cellTemp.VisualTree = fact;
            var TargetColumn = new DataGridTemplateColumn()
            {
                CellTemplate = cellTemp,
                Header = "Target",
                CanUserSort = true,
                HeaderStyle = dg.ColumnHeaderStyle,
                Width = new DataGridLength(0.5, DataGridLengthUnitType.Star)
            };
            //dg.Columns.Insert(0, TargetColumn);
            dg.Columns.Add(TargetColumn);

            // added fotter
            var dg2 = new Grid() { Name = "footer", VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };

            // Export
            var butExport = new System.Windows.Controls.Button()
            {
                Name = "Button_Export",
                Content = "Export",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 25,
                Width = 70,
                Margin = new Thickness(5, 5, 80, 5)
            };
            butExport.Click += button_export_click;

            // close
            var butClose = new System.Windows.Controls.Button()
            {
                Name = "Button_Close",
                Content = "Cancel",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 25,
                Width = 70,
                Margin = new Thickness(5, 5, 5, 5)
            };
            butClose.Click += button_cancel_click;

            // settings
            var butSetting = new System.Windows.Controls.Button()
            {
                Name = "Button_Setting",
                Content = "Setting",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 25,
                Width = 70,
                Margin = new Thickness(5, 5, 230, 5)
            };
            butSetting.Click += button_setting_click;

            var butSaveAsMSP = new System.Windows.Controls.Button()
            {
                Name = "Button_SaveAsMsp",
                Content = "Save as MSP",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 25,
                Width = 70,
                Margin = new Thickness(5, 5, 155, 5)
            };
            butSaveAsMSP.Click += button_saveAsMsp_click;

            var butCheckAll = new System.Windows.Controls.Button()
            {
                Name = "Button_CheckAll",
                Content = "Uncheck all",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 25,
                Width = 70,
                Margin = new Thickness(5, 5, 5, 5)
            };
            butCheckAll.Click += button_checkAll_click;

            dg2.Children.Add(butExport);
            dg2.Children.Add(butClose);
            dg2.Children.Add(butSetting);
            dg2.Children.Add(butSaveAsMSP);
            dg2.Children.Add(butCheckAll);

            var dg3 = viewer.Data;
            dg3.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star), MinHeight = 250 });
            dg3.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40, GridUnitType.Pixel), MinHeight = 40 });

            dg3.Children.Add(dg2);
            Grid.SetRow(dg, 0);
            Grid.SetRow(dg2, 1);

            viewer.Height = 400;
            viewer.UpdateLayout();
        }

        public void ChangeToNormalView()
        {
            if (!isMsFinderExporter) return;
            isMsFinderExporter = false;
            var viewer = this.alignmentTableViewer;
            viewer.Title = "Alignment Spot Table";
            var dg = viewer.DataGrid_RawData;
            dg.Columns.RemoveAt(dg.Columns.Count - 1);
            var dg3 = viewer.Data;
            dg3.Children.RemoveAt(dg3.Children.Count - 1);
            dg3.RowDefinitions.RemoveAt(dg3.RowDefinitions.Count - 1);
            dg3.RowDefinitions.RemoveAt(dg3.RowDefinitions.Count - 1);
            viewer.UpdateLayout();
        }

        private void button_setting_click(object sender, RoutedEventArgs e)
        {
            var source = this.Source;
            var w = new Rfx.Riken.OsakaUniv.ForAIF.MsFinderExporterSettingWin(source);
            w.Owner = this.alignmentTableViewer;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.Show();
        }

        private void button_checkAll_click(object sender, RoutedEventArgs e)
        {
            var isAllChecked = true;
            if (((Button)sender).Content.ToString() == "Check all")
            {
                isAllChecked = false;
            }
            var source = this.Source;
            if (isAllChecked)
            {
                foreach (var spot in source)
                {
                    spot.Checked = false;
                }
            }
            else
            {
                foreach (var spot in source)
                {
                    spot.Checked = true;
                }
            }

            var content = isAllChecked ? "Check all" : "Uncheck all";
            ((Button)sender).Content = content;
        }


        private void button_saveAsMsp_click(object sender, RoutedEventArgs e)
        {
            OnPropertyChanged("ExportAlignmentSpotsAsMspFormat");
        }

        private void button_export_click(object sender, RoutedEventArgs e)
        {
            OnPropertyChanged("ExportAlignmentSpotsToMsFinder");
        }

        private void button_cancel_click(object sender, RoutedEventArgs e)
        {
            ChangeToNormalView();
        }

        #endregion
    }

    public class AlignmentSpotRow : ViewModelBase
    {
        #region member variables and properties
        private bool _checked = true;
        public bool Checked { get { return _checked; } set { if (_checked == value) return; _checked = value; OnPropertyChanged("Checked"); } }

        public AlignmentPropertyBean AlignmentPropertyBean { get; set; }
        public AlignedDriftSpotPropertyBean AlignedDriftSpotPropertyBean { get; set; }
        public string ShortInchiKey { get; set; } = "";
        public DrawingImage Image { get; set; }
        private int height = 40;
        private int width = 200;

        // correlations
        private float correlation = 0;
        public float Correlation { get { return correlation; } set { correlation = value; OnPropertyChanged("Correlation"); } }


        // for ion mobility
        public int MasterID { get; set; }
        public float RT { get; set; }
        public float Mobility { get; set; }
        public float Ccs { get; set; }
        public float Mz { get; set; }
        public string Type { get; set; }
        public float Fill { get; set; }
        public float Anova { get; set; }
        public float FC { get; set; }
        public float SN { get; set; }

        private string metaboliteName;
        private string comment;
        public string MetaboliteName {
            get {
                return metaboliteName;
            }

            set {
                metaboliteName = value;
                if (Mobility > 0)
                    AlignedDriftSpotPropertyBean.MetaboliteName = value;
                else
                    AlignmentPropertyBean.MetaboliteName = value;
                OnPropertyChanged("MetaboliteName");
            }
        }

        public void UpdateMetaboliteNameOnTable(bool isIonMobility) {
            if (isIonMobility)
                MetaboliteName = this.AlignedDriftSpotPropertyBean.MetaboliteName;
            else
                MetaboliteName = this.AlignmentPropertyBean.MetaboliteName;
            OnPropertyChanged("MetaboliteName");
        }

        public string Comment {
            get {
                return comment;
            }

            set {
                comment = value;
                if (Mobility > 0)
                    AlignedDriftSpotPropertyBean.Comment = value;
                else
                    AlignmentPropertyBean.Comment = value;

                OnPropertyChanged("Comment");
            }
        }

        #endregion

        public AlignmentSpotRow(AlignmentPropertyBean alignmentPropertyBean,
            ObservableCollection<AnalysisFileBean> analysisFiles,
            ProjectPropertyBean projectProp, int width,
            List<MspFormatCompoundInformationBean> msp, BarChartDisplayMode mode, bool isBoxPlot) {
            AlignmentPropertyBean = alignmentPropertyBean;
            this.width = width;
            if (alignmentPropertyBean.LibraryID >= 0) {
                var inchiKey = msp[alignmentPropertyBean.LibraryID].InchiKey;
                ShortInchiKey = inchiKey.Split('-')[0];
            }

            Image = GetDrawingImageFromAlignment(alignmentPropertyBean, analysisFiles, projectProp, mode, isBoxPlot);
        }

        public AlignmentSpotRow(AlignmentPropertyBean alignmentPropertyBean,
            List<MspFormatCompoundInformationBean> msp)
        {
            AlignmentPropertyBean = alignmentPropertyBean;
            if (alignmentPropertyBean.LibraryID >= 0 && msp != null && msp.Count > 0) {
                var inchiKey = msp[alignmentPropertyBean.LibraryID].InchiKey;
                ShortInchiKey = inchiKey.Split('-')[0];
            }
        }

        public void Update_Image(ObservableCollection<AnalysisFileBean> analysisFiles, ProjectPropertyBean projectProp,
            BarChartDisplayMode mode, bool isBoxPlot) {

            if (this.Mobility > 0)
                Image = GetDrawingImageFromAlignment(AlignedDriftSpotPropertyBean, analysisFiles, projectProp, mode, isBoxPlot);
            else
                Image = GetDrawingImageFromAlignment(AlignmentPropertyBean, analysisFiles, projectProp, mode, isBoxPlot);

            OnPropertyChanged("Image");
        }

        public void Update_StatisticalValues(ObservableCollection<AnalysisFileBean> analysisFiles) {
            if (this.Mobility > 0)
                TableViewerUtility.CalcStatisticsForParticularAlignmentSpot(AlignedDriftSpotPropertyBean, analysisFiles);
            else
                TableViewerUtility.CalcStatisticsForParticularAlignmentSpot(AlignmentPropertyBean, analysisFiles);

            AlignmentPropertyBean.UpdateStatisticalValues();
        }

        private BitmapSource getBitmapImageFromAlignment(AlignmentPropertyBean alignmentPropertyBean,
            ObservableCollection<AnalysisFileBean> analysisFiles, ProjectPropertyBean projectProp,
            BarChartDisplayMode mode, bool isBoxPlot) {
            var barChartBean = MsDialStatistics.GetBarChartBean(alignmentPropertyBean, analysisFiles, projectProp, mode, isBoxPlot);
            return new Common.BarChart.PlainBarChartForTable(height, width, 150, 150).DrawBarChart2BitmapSource(barChartBean);
        }

        private DrawingImage GetDrawingImageFromAlignment(AlignmentPropertyBean alignmentPropertyBean,
            ObservableCollection<AnalysisFileBean> analysisFiles, ProjectPropertyBean projectProp,
            BarChartDisplayMode mode, bool isBoxPlot) {
            DrawingImage image = null;
            var barChartBean = MsDialStatistics.GetBarChartBean(alignmentPropertyBean, analysisFiles, projectProp, mode, isBoxPlot);
            image = new Common.BarChart.PlainBarChartForTable(height, width, 150, 150).GetDrawingImage(barChartBean);
            return image;
        }

        #region ion mobility
        public AlignmentSpotRow(
            int peakID, AlignmentPropertyBean alignedSpot,
            AlignedDriftSpotPropertyBean alignedDriftSpotPropertyBean,
            ObservableCollection<AnalysisFileBean> analysisFiles,
            ProjectPropertyBean projectProp, int width,
            List<MspFormatCompoundInformationBean> msp,
            BarChartDisplayMode mode, bool isBoxPlot) {
            AlignmentPropertyBean = alignedSpot;
            AlignedDriftSpotPropertyBean = alignedDriftSpotPropertyBean;

            this.MasterID = peakID;
            this.RT = alignedSpot.CentralRetentionTime;
            this.Mz = alignedSpot.CentralAccurateMass;
            this.Mobility = alignedDriftSpotPropertyBean.CentralDriftTime;
            this.Ccs = alignedDriftSpotPropertyBean.CentralCcs;

            if (this.Mobility > 0) {
                this.Type = alignedDriftSpotPropertyBean.AdductIonName;
                this.Fill = alignedDriftSpotPropertyBean.FillParcentage;
                this.Anova = alignedDriftSpotPropertyBean.AnovaPval;
                this.FC = alignedDriftSpotPropertyBean.FoldChange;
                this.SN = alignedDriftSpotPropertyBean.SignalToNoiseAve;
                this.MetaboliteName = alignedDriftSpotPropertyBean.MetaboliteName;
                this.Comment = alignedDriftSpotPropertyBean.Comment;
                if (alignedDriftSpotPropertyBean.LibraryID >= 0) {
                    var inchiKey = msp[alignedDriftSpotPropertyBean.LibraryID].InchiKey;
                    ShortInchiKey = inchiKey.Split('-')[0];
                }
                Image = GetDrawingImageFromAlignment(alignedDriftSpotPropertyBean, analysisFiles, projectProp, mode, isBoxPlot);
            }
            else {
                this.Type = alignedSpot.AdductIonName;
                this.Fill = alignedSpot.FillParcentage;
                this.Anova = alignedSpot.AnovaPval;
                this.FC = alignedSpot.FoldChange;
                this.SN = alignedSpot.SignalToNoiseAve;
                this.MetaboliteName = alignedSpot.MetaboliteName;
                this.Comment = alignedSpot.Comment;
                if (alignedSpot.LibraryID >= 0) {
                    var inchiKey = msp[alignedSpot.LibraryID].InchiKey;
                    ShortInchiKey = inchiKey.Split('-')[0];
                }
                Image = GetDrawingImageFromAlignment(alignedSpot, analysisFiles, projectProp, mode, isBoxPlot);
            }
            this.width = width;

        }

        public AlignmentSpotRow(AlignedDriftSpotPropertyBean alignedDriftSpotPropertyBean,
            List<MspFormatCompoundInformationBean> msp) {
            AlignedDriftSpotPropertyBean = alignedDriftSpotPropertyBean;
            if (alignedDriftSpotPropertyBean.LibraryID >= 0 && msp != null && msp.Count > 0) {
                var inchiKey = msp[alignedDriftSpotPropertyBean.LibraryID].InchiKey;
                ShortInchiKey = inchiKey.Split('-')[0];
            }
        }

        public void Update_ImageOnDrift(ObservableCollection<AnalysisFileBean> analysisFiles, ProjectPropertyBean projectProp,
            BarChartDisplayMode mode, bool isBoxPlot) {
            Image = GetDrawingImageFromAlignment(AlignedDriftSpotPropertyBean, analysisFiles, projectProp, mode, isBoxPlot);
            OnPropertyChanged("Image");
        }

        public void Update_StatisticalValuesOnDrift(ObservableCollection<AnalysisFileBean> analysisFiles) {
            TableViewerUtility.CalcStatisticsForParticularAlignmentSpot(AlignedDriftSpotPropertyBean, analysisFiles);
            AlignedDriftSpotPropertyBean.UpdateStatisticalValues();
        }

        private BitmapSource getBitmapImageFromAlignment(AlignedDriftSpotPropertyBean alignedDriftSpotPropertyBean,
            ObservableCollection<AnalysisFileBean> analysisFiles, ProjectPropertyBean projectProp,
            BarChartDisplayMode mode, bool isBoxPlot) {
            var barChartBean = MsDialStatistics.GetBarChartBean(alignedDriftSpotPropertyBean, analysisFiles, projectProp, mode, isBoxPlot);
            return new Common.BarChart.PlainBarChartForTable(height, width, 150, 150).DrawBarChart2BitmapSource(barChartBean);
        }

        private DrawingImage GetDrawingImageFromAlignment(AlignedDriftSpotPropertyBean alignedDriftSpotPropertyBean,
            ObservableCollection<AnalysisFileBean> analysisFiles, ProjectPropertyBean projectProp,
            BarChartDisplayMode mode, bool isBoxPlot) {
            var barChartBean = MsDialStatistics.GetBarChartBean(alignedDriftSpotPropertyBean, analysisFiles, projectProp, mode, isBoxPlot);
            DrawingImage image = new Common.BarChart.PlainBarChartForTable(height, width, 150, 150).GetDrawingImage(barChartBean);
            return image;
        }
        #endregion
    }
}
