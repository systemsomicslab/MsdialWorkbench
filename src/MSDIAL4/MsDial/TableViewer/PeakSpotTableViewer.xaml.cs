using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Forms.DataVisualization.Charting;
using Msdial.Lcms.Dataprocess.Utility;
using Msdial.Gcms.Dataprocess.Utility;
using Msdial.Gcms.Dataprocess.Algorithm;
using CompMs.Common.DataObj;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// PeakSpotTableViewer.xaml の相互作用ロジック
    /// </summary>
    public partial class PeakSpotTableViewer : Window
    {
        #region member variables and properties
        public PeakSpotTableViewerVM PeakSpotTableViewerVM { set; get; }
        //   List<Color> ColorList = new List<Color>() { Color.Black, Color.Red, Color.Green, Color.YellowGreen, Color.Blue, Color.SteelBlue, Color.DeepPink, Color.OrangeRed };
        #endregion

        public PeakSpotTableViewer() { }

        public PeakSpotTableViewer(ObservableCollection<PeakSpotRow> source, int id) {
            InitializeComponent();
            if (source[0].Ms1DecRes != null) {
                changeColumnForGC();
            }
            else if (source[0].DriftSpotBean != null) {
                changeColumnForIonMobility();
            }
            this.PeakSpotTableViewerVM = new PeakSpotTableViewerVM(this, source);
            this.DataContext = this.PeakSpotTableViewerVM;
            this.PeakSpotTableViewerVM.SelectedData = this.PeakSpotTableViewerVM.Source[id];
        }

        public void ChangeSource(ObservableCollection<PeakSpotRow> source, int id) {
            this.PeakSpotTableViewerVM = new PeakSpotTableViewerVM(this, source);
            this.DataContext = this.PeakSpotTableViewerVM;
            this.PeakSpotTableViewerVM.SelectedData = this.PeakSpotTableViewerVM.Source[id];
        }

        private void Closing_Mehotd(object sender, CancelEventArgs e) {

            this.DataGrid_RawData.CommitEdit();
            this.DataGrid_RawData.CancelEdit();
            foreach (var s in this.PeakSpotTableViewerVM.Source) {
                s.Image = null;
            }
            this.DataContext = null;
        }

        #region changeColumnForGC
        private void changeColumnForGC() {
            this.Width = 1000;
            var dg = DataGrid_RawData;

            // change column Binding & Header
            ((DataGridTextColumn)dg.Columns[0]).Binding = new Binding("Ms1DecRes.Ms1DecID");
            ((DataGridTextColumn)dg.Columns[1]).Binding = new Binding("Ms1DecRes.RetentionTime") { StringFormat = "0.00" };
            ((DataGridTextColumn)dg.Columns[2]).Binding = new Binding("Ms1DecRes.BasepeakMz");
            ((DataGridTextColumn)dg.Columns[2]).Header = "Quant mass";
            ((DataGridTextColumn)dg.Columns[4]).Binding = new Binding("Ms1DecRes.MetaboliteName");
            ((DataGridTextColumn)dg.Columns[4]).IsReadOnly = true;
            ((DataGridTextColumn)dg.Columns[6]).Binding = new Binding("Ms1DecRes.BasepeakHeight");
            ((DataGridTextColumn)dg.Columns[7]).Binding = new Binding("Ms1DecRes.BasepeakArea");
            ((DataGridTextColumn)dg.Columns[9]).Binding = new Binding("Ms1DecRes.SignalNoiseRatio") { StringFormat = "0.0" };

            // remove
            dg.Columns.RemoveAt(8); // remove Gaussian Sim
            dg.Columns.RemoveAt(5); // remove Comment
            dg.Columns.RemoveAt(3); // remove Type

            // add RI
            var RIcolumn = new DataGridTextColumn() {
                Header = "RI",
                IsReadOnly = true,
                CanUserSort = true,
                Binding = new Binding("Ms1DecRes.RetentionIndex") { StringFormat = "0.0" },
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
            this.Width = 1380;
            var dg = DataGrid_RawData;

            ((DataGridTextColumn)dg.Columns[0]).Binding = new Binding("PeakID");
            ((DataGridTextColumn)dg.Columns[1]).Binding = new Binding("RT") { StringFormat = "0.00" };
            ((DataGridTextColumn)dg.Columns[2]).Binding = new Binding("Mz");
            ((DataGridTextColumn)dg.Columns[3]).Binding = new Binding("Type");
            ((DataGridTextColumn)dg.Columns[4]).Binding = new Binding("MetaboliteName");
            ((DataGridTextColumn)dg.Columns[5]).Binding = new Binding("Comment");
            ((DataGridTextColumn)dg.Columns[6]).Binding = new Binding("Height") { StringFormat = "0" };
            ((DataGridTextColumn)dg.Columns[7]).Binding = new Binding("Area") { StringFormat = "0" };
            ((DataGridTextColumn)dg.Columns[8]).Binding = new Binding("Gaussian") { StringFormat = "0.00" };
            ((DataGridTextColumn)dg.Columns[9]).Binding = new Binding("SN") { StringFormat = "0.00" };

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

            // add mobility
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


        private void Button_ResetFilter_Click(object sender, RoutedEventArgs e) {
            this.PeakSpotTableViewerVM.initializeFilter();
        }
    }


    public class PeakSpotTableViewerVM : ViewModelBase
    {
        #region member variables and properties
        private PeakSpotTableViewer peakSpotTableViewer;
        private PeakSpotTableViewerVM peakSpotTableViewerVM;
        private PeakSpotRow selectedData;
        private ObservableCollection<PeakSpotRow> source;
        private string _metaboliteNameFilter;
        private string _commentFilter;
        private bool isMsFinderExporter;
        public DelegateCommand ChangetToMsFinderExportWindow { get; set; }
        public TableViewer.FilteredTable FilteredTable { get; set; }
        public TableViewer.FilterSettings Settings { get; set; }

        public PeakSpotRow SelectedData {
            get { return selectedData; }
            set {
                if (value != null) {
                    selectedData = value; OnPropertyChanged("SelectedData");
                }
            }
        }
        public ObservableCollection<PeakSpotRow> Source {
            get { return source; }
            set { source = value; OnPropertyChanged("Source"); }
        }
        public ICollectionView SourceView {
            get { return FilteredTable.View; }
        }

        // filter
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

        public PeakSpotTableViewerVM(PeakSpotTableViewer peakSpotTableViewer, ObservableCollection<PeakSpotRow> source) {
            this.peakSpotTableViewer = peakSpotTableViewer;
            this.peakSpotTableViewerVM = this;
            this.Source = source;
            this.FilteredTable = new TableViewer.FilteredTable(source);
            this.Settings = new TableViewer.FilterSettings(this.FilteredTable.View, TableViewer.FilterSettings.ViewerType.Peak);
            this.FilteredTable.View.Filter = Settings.SpotFilter;
            initializeFilter();

            this.ChangetToMsFinderExportWindow = new DelegateCommand(x => ChangeToMsFinderExporterView(), x => true);
        }

        public void initializeFilter() {
            if (Source[0].Ms1DecRes != null) {
                setSlidersForGC();
            }
            else {
                if (Source[0].DriftSpotBean != null) {
                    setSlidersForIonMobility();
                }
                else {
                    setSlidersForLC();
                }
            }
            MetaboliteNameFilter = "";
            CommentFilter = "";
        }

        public void ChangeSelectedData(int id) {
            SelectedData = this.Source[id];
            if(this.peakSpotTableViewer.DataGrid_RawData.SelectedItem != null)
                this.peakSpotTableViewer.DataGrid_RawData.ScrollIntoView(this.peakSpotTableViewer.DataGrid_RawData.SelectedItem);
        }

        #region slider settings
        private void setCommonSlider() {
            //mz
            this.peakSpotTableViewer.doubleSlider_MzFilter.LowerSlider.ValueChanged -= mzLowerSlider_ValueChanged;
            this.peakSpotTableViewer.doubleSlider_MzFilter.LowerSlider.ValueChanged += mzLowerSlider_ValueChanged;
            this.peakSpotTableViewer.doubleSlider_MzFilter.UpperSlider.ValueChanged -= mzUpperSlider_ValueChanged;
            this.peakSpotTableViewer.doubleSlider_MzFilter.UpperSlider.ValueChanged += mzUpperSlider_ValueChanged;
            this.Settings.MzSliderLowerValue = (float)this.peakSpotTableViewer.doubleSlider_MzFilter.LowerSlider.Value;
            this.Settings.MzSliderUpperValue = (float)this.peakSpotTableViewer.doubleSlider_MzFilter.UpperSlider.Value;

            //rt
            this.peakSpotTableViewer.doubleSlider_RtFilter.LowerSlider.ValueChanged -= rtLowerSlider_ValueChanged;
            this.peakSpotTableViewer.doubleSlider_RtFilter.LowerSlider.ValueChanged += rtLowerSlider_ValueChanged;
            this.peakSpotTableViewer.doubleSlider_RtFilter.UpperSlider.ValueChanged -= rtUpperSlider_ValueChanged;
            this.peakSpotTableViewer.doubleSlider_RtFilter.UpperSlider.ValueChanged += rtUpperSlider_ValueChanged;
            this.Settings.RtSliderLowerValue = (float)this.peakSpotTableViewer.doubleSlider_RtFilter.LowerSlider.Value;
            this.Settings.RtSliderUpperValue = (float)this.peakSpotTableViewer.doubleSlider_RtFilter.UpperSlider.Value;

            // textBox -> slider
            this.Settings.PropertyChanged -= settings_propertyChanged;
            this.Settings.PropertyChanged += settings_propertyChanged;

        }
        private void setSlidersForLC() {
            setCommonSlider();
            // mz
            this.peakSpotTableViewer.doubleSlider_MzFilter.Maximum = this.source.Max(x => x.PeakAreaBean.AccurateMass);
            this.peakSpotTableViewer.doubleSlider_MzFilter.UpperValue = this.peakSpotTableViewer.doubleSlider_MzFilter.Maximum;
            this.peakSpotTableViewer.doubleSlider_MzFilter.Minimum = this.source.Min(x => x.PeakAreaBean.AccurateMass);
            this.peakSpotTableViewer.doubleSlider_MzFilter.LowerValue = this.peakSpotTableViewer.doubleSlider_MzFilter.Minimum;


            // rt
            this.peakSpotTableViewer.doubleSlider_RtFilter.Maximum = this.source.Max(x => x.PeakAreaBean.RtAtPeakTop);
            this.peakSpotTableViewer.doubleSlider_RtFilter.UpperValue = this.peakSpotTableViewer.doubleSlider_RtFilter.Maximum;
            this.peakSpotTableViewer.doubleSlider_RtFilter.Minimum = this.source.Min(x => x.PeakAreaBean.RtAtPeakTop);
            this.peakSpotTableViewer.doubleSlider_RtFilter.LowerValue = this.peakSpotTableViewer.doubleSlider_RtFilter.Minimum;

        }

        private void setSlidersForIonMobility() {
            setCommonSlider();
            // mz
            this.peakSpotTableViewer.doubleSlider_MzFilter.Maximum = this.source.Max(x => x.Mz);
            this.peakSpotTableViewer.doubleSlider_MzFilter.UpperValue = this.peakSpotTableViewer.doubleSlider_MzFilter.Maximum;
            this.peakSpotTableViewer.doubleSlider_MzFilter.Minimum = this.source.Min(x => x.Mz);
            this.peakSpotTableViewer.doubleSlider_MzFilter.LowerValue = this.peakSpotTableViewer.doubleSlider_MzFilter.Minimum;

            // rt
            this.peakSpotTableViewer.doubleSlider_RtFilter.Maximum = this.source.Max(x => x.RT);
            this.peakSpotTableViewer.doubleSlider_RtFilter.UpperValue = this.peakSpotTableViewer.doubleSlider_RtFilter.Maximum;
            this.peakSpotTableViewer.doubleSlider_RtFilter.Minimum = this.source.Min(x => x.RT);
            this.peakSpotTableViewer.doubleSlider_RtFilter.LowerValue = this.peakSpotTableViewer.doubleSlider_RtFilter.Minimum;

        }

        private void setSlidersForGC() {
            setCommonSlider();
            // mz
            this.peakSpotTableViewer.doubleSlider_MzFilter.Maximum = this.source.Max(x => x.Ms1DecRes.BasepeakMz);
            this.peakSpotTableViewer.doubleSlider_MzFilter.UpperValue = this.peakSpotTableViewer.doubleSlider_MzFilter.Maximum;
            this.peakSpotTableViewer.doubleSlider_MzFilter.Minimum = this.source.Min(x => x.Ms1DecRes.BasepeakMz);
            this.peakSpotTableViewer.doubleSlider_MzFilter.LowerValue = this.peakSpotTableViewer.doubleSlider_MzFilter.Minimum;


            // rt
            this.peakSpotTableViewer.doubleSlider_RtFilter.Maximum = this.source.Max(x => x.Ms1DecRes.RetentionTime);
            this.peakSpotTableViewer.doubleSlider_RtFilter.UpperValue = this.peakSpotTableViewer.doubleSlider_RtFilter.Maximum;
            this.peakSpotTableViewer.doubleSlider_RtFilter.Minimum = this.source.Min(x => x.Ms1DecRes.RetentionTime);
            this.peakSpotTableViewer.doubleSlider_RtFilter.LowerValue = this.peakSpotTableViewer.doubleSlider_RtFilter.Minimum;

            this.peakSpotTableViewer.TextBox_CommentFilter.IsEnabled = false;
        }


        private void settings_propertyChanged(object sender, PropertyChangedEventArgs e) {
            if(e.PropertyName=="MzSliderLowerValue" && this.Settings.MzSliderLowerValue != this.peakSpotTableViewer.doubleSlider_MzFilter.LowerSlider.Value) {
                this.peakSpotTableViewer.doubleSlider_MzFilter.LowerSlider.Value = this.Settings.MzSliderLowerValue;
            }
            if (e.PropertyName == "MzSliderUpperValue" && this.Settings.MzSliderUpperValue != this.peakSpotTableViewer.doubleSlider_MzFilter.UpperSlider.Value) {
                this.peakSpotTableViewer.doubleSlider_MzFilter.UpperSlider.Value = this.Settings.MzSliderUpperValue;
            }
            if (e.PropertyName == "RtSliderLowerValue" && this.Settings.RtSliderLowerValue != this.peakSpotTableViewer.doubleSlider_RtFilter.LowerSlider.Value) {
                this.peakSpotTableViewer.doubleSlider_RtFilter.LowerSlider.Value = this.Settings.RtSliderLowerValue;
            }
            if (e.PropertyName == "RtSliderUpperValue" && this.Settings.RtSliderUpperValue != this.peakSpotTableViewer.doubleSlider_RtFilter.UpperSlider.Value) {
                this.peakSpotTableViewer.doubleSlider_RtFilter.UpperSlider.Value = this.Settings.RtSliderUpperValue;
            }
        }

        private void mzLowerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            this.Settings.MzSliderLowerValue = (float)this.peakSpotTableViewer.doubleSlider_MzFilter.LowerSlider.Value;
        }

        private void mzUpperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            this.Settings.MzSliderUpperValue = (float)this.peakSpotTableViewer.doubleSlider_MzFilter.UpperSlider.Value;
        }

        private void rtLowerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            this.Settings.RtSliderLowerValue = (float)this.peakSpotTableViewer.doubleSlider_RtFilter.LowerSlider.Value;
        }

        private void rtUpperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            this.Settings.RtSliderUpperValue = (float)this.peakSpotTableViewer.doubleSlider_RtFilter.UpperSlider.Value;
        }
        #endregion

        #region MsFinder

        public void ChangeToMsFinderExporterView()
        {
            if (isMsFinderExporter) return;
            if (Source[0].Ms1DecRes != null || Source[0].DriftSpotBean != null)
            {
                MessageBox.Show("Sorry, this function supports LC-MS only.", "Error", MessageBoxButton.OK);
                return;
            }

            isMsFinderExporter = true;
            var viewer = this.peakSpotTableViewer;

            viewer.Title = "MS-FINDER exporter: Peak Spot Table";

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
            var dg2 = new System.Windows.Controls.Grid() { Name = "footer", VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };

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
            System.Windows.Controls.Grid.SetRow(dg, 0);
            System.Windows.Controls.Grid.SetRow(dg2, 1);

            viewer.Height = 400;
            viewer.UpdateLayout();
        }

        public void ChangeToNormalView()
        {
            if (!isMsFinderExporter) return;
            isMsFinderExporter = false;
            var viewer = this.peakSpotTableViewer;
            viewer.Title = "Peak Spot Table";
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
            w.Owner = this.peakSpotTableViewer;
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
            OnPropertyChanged("ExportPeakSpotsAsMsp");
        }

        private void button_export_click(object sender, RoutedEventArgs e)
        {
            OnPropertyChanged("ExportPeakSpotsToMsFinder");
        }

        private void button_cancel_click(object sender, RoutedEventArgs e)
        {
            ChangeToNormalView();
        }

        #endregion

    }
    #region PeakSpotRow
    public class PeakSpotRow : ViewModelBase
    {
        #region member variables and properties
        private int height = 40;
        private int width = 200;
        private bool _checked = true;

        public bool Checked { get { return _checked; } set { if (_checked == value) return; _checked = value; OnPropertyChanged("Checked"); } }

        public int PeakID { get; set; }
        public MS1DecResult Ms1DecRes { get; set; }
        public PeakAreaBean PeakAreaBean { set; get; }
        public DriftSpotBean DriftSpotBean { set; get; }
        public DrawingImage Image { set; get; }

        // for ion mobility
        public float RT { get; set; }
        public float Mobility { get; set; }
        public float Ccs { get; set; }
        public float Mz { get; set; }
        public string Type { get; set; }
        public float Height { get; set; }
        public float Area { get; set; }
        public float Gaussian { get; set; }
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
                    DriftSpotBean.MetaboliteName = value;
                else
                    PeakAreaBean.MetaboliteName = value;
                OnPropertyChanged("MetaboliteName");
            }
        }

        public string Comment {
            get {
                return comment;
            }

            set {
                comment = value;
                if (Mobility > 0)
                    DriftSpotBean.Comment = value;
                else
                    PeakAreaBean.Comment = value;

                OnPropertyChanged("Comment");
            }
        }



        #endregion

        #region constructor
        // Ion mobility
        public PeakSpotRow(int id, PeakAreaBean pab, ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectPropertyBean, AnalysisParametersBean analysisParametersBean) {
            PeakAreaBean = pab;
            PeakID = id;
            Image = GetDrawingImageForLC(pab, spectrumCollection, projectPropertyBean, analysisParametersBean);
        }

        public PeakSpotRow(int id, PeakAreaBean pab, DriftSpotBean dsb, ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectPropertyBean, AnalysisParametersBean analysisParametersBean) {
            PeakAreaBean = pab;
            DriftSpotBean = dsb;
            PeakID = id;

            if (dsb.DriftTimeAtPeakTop > 0) {
                RT = pab.RtAtPeakTop;
                Mobility = dsb.DriftTimeAtPeakTop;
                Ccs = dsb.Ccs;
                Mz = dsb.AccurateMass;
                MetaboliteName = dsb.MetaboliteName;
                Comment = dsb.Comment;
                Type = dsb.AdductIonName;
                Height = dsb.IntensityAtPeakTop;
                Area = dsb.AreaAboveZero;
                Gaussian = dsb.GaussianSimilarityValue;
                SN = dsb.SignalToNoise;
                Image = GetDrawingImageForLC(pab, dsb, spectrumCollection, projectPropertyBean, analysisParametersBean);
            }
            else {
                RT = pab.RtAtPeakTop;
                Mobility = -1;
                Ccs = -1;
                Mz = pab.AccurateMass;
                MetaboliteName = pab.MetaboliteName;
                Comment = pab.Comment;
                Type = pab.AdductIonName;
                Height = pab.IntensityAtPeakTop;
                Area = pab.AreaAboveZero;
                Gaussian = pab.GaussianSimilarityValue;
                SN = pab.SignalToNoise;
                Image = GetDrawingImageForLC(pab, spectrumCollection, projectPropertyBean, analysisParametersBean);
            }

        }

        // LC
        public PeakSpotRow(PeakAreaBean pab, ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectPropertyBean, AnalysisParametersBean analysisParametersBean) {
            PeakAreaBean = pab;
            PeakID = pab.PeakID;
            Image = GetDrawingImageForLC(pab, spectrumCollection, projectPropertyBean, analysisParametersBean);
        }

        //GC
        public PeakSpotRow(List<RawSpectrum> spectrumCollectionGC, MS1DecResult ms1DecResult,
            ProjectPropertyBean projectPropertyBean, AnalysisParamOfMsdialGcms analysisParamGC) {
            PeakID = ms1DecResult.Ms1DecID;
            Ms1DecRes = ms1DecResult;
            Image = GetDrawingImageForGC(ms1DecResult, spectrumCollectionGC, analysisParamGC);
        }
        #endregion

        // Ion mobility
        private DrawingImage GetDrawingImageForLC(PeakAreaBean peakSpot, DriftSpotBean driftSpot, ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean project, AnalysisParametersBean param) {
            var chromatogramXicVM = getChromatogramXicViewModelForLC(spectrumCollection, peakSpot, driftSpot, param, project);
            var img = new PlainChromatogramXicForTableViewer(height, width, 100, 100).GetChromatogramDrawingImage(chromatogramXicVM);
            return img;
        }
        private BitmapSource getBitmapImageForLC(PeakAreaBean peakSpot, DriftSpotBean driftSpot, ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean project, AnalysisParametersBean param) {
            var chromatogramXicVM = getChromatogramXicViewModelForLC(spectrumCollection, peakSpot, driftSpot, param, project);
            return new PlainChromatogramXicForTableViewer(height, width, 100, 100).DrawChromatogramXic2BitmapSource(chromatogramXicVM);
        }

        private static ChromatogramXicViewModel getChromatogramXicViewModelForLC(ObservableCollection<RawSpectrum> spectrumCollection,
            PeakAreaBean peakSpot, DriftSpotBean driftSpot, AnalysisParametersBean param, ProjectPropertyBean projectPropertyBean) {
            float targetMz = peakSpot.AccurateMass;
            float targetRt = peakSpot.RtAtPeakTop;
            float targetLeftRt = peakSpot.RtAtLeftPeakEdge;
            float targetRightRt = peakSpot.RtAtRightPeakEdge;
            var rtWidth = targetRightRt - targetLeftRt;
            if (rtWidth > 0.6) rtWidth = 0.6F;
            if (rtWidth < 0.2) rtWidth = 0.2F;

            float dtWidth = driftSpot.DriftTimeAtRightPeakEdge - driftSpot.DriftTimeAtLeftPeakEdge;
            float minDt = driftSpot.DriftTimeAtPeakTop - 1.5F * dtWidth;
            float maxDt = driftSpot.DriftTimeAtPeakTop + 1.5F * dtWidth;

            //var smoothedPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(
            //    DataAccessLcUtility.GetDriftChromatogramByRtMz(
            //        spectrumCollection, targetRt, param.AccumulatedRtRagne,
            //        targetMz, param.CentroidMs1Tolerance, minDt, maxDt),
            //    param.SmoothingMethod, param.SmoothingLevel);
            var smoothedPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(
                DataAccessLcUtility.GetDriftChromatogramByRtMz(
                    spectrumCollection, targetRt, rtWidth,
                    targetMz, param.CentroidMs1Tolerance, minDt, maxDt),
                param.SmoothingMethod, param.SmoothingLevel);
            var chromatogramBean = new ChromatogramBean(true, System.Windows.Media.Brushes.Blue, 1, "", targetMz,
                new ObservableCollection<double[]>(smoothedPeaklist), new ObservableCollection<DriftSpotBean>() { driftSpot });
            return new ChromatogramXicViewModel(chromatogramBean, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None,
                ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Relative, 0, "", targetMz,
                param.CentroidMs1Tolerance,
                driftSpot.DriftTimeAtPeakTop,
                driftSpot.DriftTimeAtLeftPeakEdge,
                driftSpot.DriftTimeAtRightPeakEdge,
                true);
        }

        // LC
        private DrawingImage GetDrawingImageForLC(PeakAreaBean peakAreaBean, ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectPropertyBean, AnalysisParametersBean analysisParametersBean) {
            var chromatogramXicVM = getChromatogramXicViewModelForLC(spectrumCollection, peakAreaBean, analysisParametersBean, projectPropertyBean);
            var img = new PlainChromatogramXicForTableViewer(height, width, 100, 100).GetChromatogramDrawingImage(chromatogramXicVM);
            return img;
        }
        private BitmapSource getBitmapImageForLC(PeakAreaBean peakAreaBean, ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectPropertyBean, AnalysisParametersBean analysisParametersBean) {
            var chromatogramXicVM = getChromatogramXicViewModelForLC(spectrumCollection, peakAreaBean, analysisParametersBean, projectPropertyBean);
            return new PlainChromatogramXicForTableViewer(height, width, 100, 100).DrawChromatogramXic2BitmapSource(chromatogramXicVM);
        }

        private static ChromatogramXicViewModel getChromatogramXicViewModelForLC(ObservableCollection<RawSpectrum> spectrumCollection, PeakAreaBean peakAreaBean, AnalysisParametersBean analysisParametersBean, ProjectPropertyBean projectPropertyBean) {
            float targetMz = peakAreaBean.AccurateMass;
            float targetRt = peakAreaBean.RtAtPeakTop;
            float targetLeftRt = peakAreaBean.RtAtLeftPeakEdge;
            float targetRightRt = peakAreaBean.RtAtRightPeakEdge;
            float Begin = targetLeftRt - (targetRightRt - targetLeftRt) * 1.5f;
            float End = targetRightRt + (targetRightRt - targetLeftRt) * 1.5f;
            List<double[]> smoothedPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectPropertyBean, targetMz, analysisParametersBean.CentroidMs1Tolerance, Begin, End), analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel);
            ChromatogramBean chromatogramBean = new ChromatogramBean(true, System.Windows.Media.Brushes.Blue, 1, "", targetMz,
                new ObservableCollection<double[]>(smoothedPeaklist), new ObservableCollection<PeakAreaBean>() { peakAreaBean });
            return new ChromatogramXicViewModel(chromatogramBean, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None,
                ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Relative, 0, "", targetMz,
                analysisParametersBean.CentroidMs1Tolerance,
                targetRt, targetLeftRt, targetRightRt, true);
        }

        // GC
        private DrawingImage GetDrawingImageForGC(MS1DecResult ms1dec, List<RawSpectrum> spectrumCollectionGC, AnalysisParamOfMsdialGcms analysisParamGC) {
            var chromatogramXicVM = getChromatogramXicVM(spectrumCollectionGC, ms1dec, analysisParamGC);
            var image = new PlainChromatogramXicForTableViewer(height, width, 100, 100).GetChromatogramDrawingImage(chromatogramXicVM);
            return image;
        }

        private BitmapSource getBitmapImageForGC(MS1DecResult ms1dec, List<RawSpectrum> spectrumCollectionGC, AnalysisParamOfMsdialGcms analysisParamGC) {
            var chromatogramXicVM = getChromatogramXicVM(spectrumCollectionGC, ms1dec, analysisParamGC);
            return new PlainChromatogramXicForTableViewer(height, width, 100, 100).DrawChromatogramXic2BitmapSource(chromatogramXicVM);
        }

        private static ChromatogramXicViewModel getChromatogramXicVM(List<RawSpectrum> spectrumList, MS1DecResult ms1dec, AnalysisParamOfMsdialGcms param) {
            float targetMz = ms1dec.BasepeakMz;
            float targetRt = ms1dec.RetentionTime;
            float targetLeftRt = (float)ms1dec.BasepeakChromatogram[0].RetentionTime;
            float targetRightRt = (float)ms1dec.BasepeakChromatogram[ms1dec.BasepeakChromatogram.Count - 1].RetentionTime;
            float Begin = targetLeftRt - (targetRightRt - targetLeftRt) * 1.5f;
            float End = targetRightRt + (targetRightRt - targetLeftRt) * 1.5f;

            //var peaklist = new List<double[]>();
            var peaklist = DataAccessGcUtility.GetMs1SlicePeaklist(spectrumList, targetMz, param.MassAccuracy, Begin, End, param.IonMode);
            //foreach (var peak in ms1dec.BasepeakChromatogram) { peaklist.Add(new double[] { peak.ScanNumber, peak.RetentionTime, peak.Mz, peak.Intensity }); }
            var smoothedPeaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            var chromatogramBean = new ChromatogramBean(true, System.Windows.Media.Brushes.Blue, 1, "", targetMz, param.MassSliceWidth, new ObservableCollection<double[]>(smoothedPeaklist));
            return new ChromatogramXicViewModel(chromatogramBean, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None,
                ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Relative, 0, "", targetMz, param.MassAccuracy,
                targetRt, targetLeftRt, targetRightRt, true);

        }

        #region old programs
        /*
         *

       public PeakSpotRow(PeakAreaBean pab, List<RAW_Spectrum> spectrumCollectionGC, MS1DecResult ms1DecResult,
            ProjectPropertyBean projectPropertyBean, AnalysisParamOfMsdialGcms analysisParamGC) {
            PeakAreaBean = pab;
            Image = getBitmapImageForGC(pab, spectrumCollectionGC, analysisParamGC);
        }

         private BitmapSource getBitmapImageForGC(PeakAreaBean peakAreaBean, List<RAW_Spectrum> spectrumCollectionGC, AnalysisParamOfMsdialGcms analysisParamGC) {
            var chromatogramXicVM = getChromatogramXicViewModelForGC(peakAreaBean, spectrumCollectionGC, analysisParamGC);
            return new PlainChromatogramXicForTableViewer(40, 200, 100, 100).DrawChromatogramXic2BitmapSource(chromatogramXicVM);
        }

        private static ChromatogramXicViewModel getChromatogramXicViewModelForGC(PeakAreaBean peakAreaBean, List<RAW_Spectrum> spectrumCollectionGC, AnalysisParamOfMsdialGcms param) {
            float targetMz = peakAreaBean.AccurateMass;
            float targetRt = peakAreaBean.RtAtPeakTop; float targetLeftRt = peakAreaBean.RtAtLeftPeakEdge; float targetRightRt = peakAreaBean.RtAtRightPeakEdge;
            float Begin = targetLeftRt - (targetRightRt - targetLeftRt) * 1.5f;
            float End = targetRightRt + (targetRightRt - targetLeftRt) * 1.5f;
            var smoothedPeaklist = DataAccessGcUtility.GetSmoothedPeaklist(DataAccessGcUtility.GetMs1SlicePeaklist(spectrumCollectionGC, targetMz, param.MassAccuracy, Begin, End, param.IonMode), param.SmoothingMethod, param.SmoothingLevel);
            var chromatogramBean = new ChromatogramBean(true, System.Windows.Media.Brushes.Blue, 1, "", targetMz, param.MassSliceWidth, new ObservableCollection<double[]>(smoothedPeaklist));

            return new ChromatogramXicViewModel(chromatogramBean, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Relative, 0, "", targetMz, param.MassAccuracy, targetRt, peakAreaBean.RtAtLeftPeakEdge, peakAreaBean.RtAtRightPeakEdge, true);
        }


        private BitmapImage GetBitmapImageForGC(PeakAreaBean peakAreaBean, List<RAW_Spectrum> spectrumCollectionGC, ProjectPropertyBean projectPropertyBean, AnalysisParamOfMsdialGcms analysisParamGC) {
            BitmapImage bitmap = new BitmapImage();
            float targetMz = peakAreaBean.AccurateMass;
            float targetRt = peakAreaBean.RtAtPeakTop; float targetLeftRt = peakAreaBean.RtAtLeftPeakEdge; float targetRightRt = peakAreaBean.RtAtRightPeakEdge;
            float Begin = targetLeftRt - (targetRightRt - targetLeftRt) * 1.5f;
            float End = targetRightRt + (targetRightRt - targetLeftRt) * 1.5f;
            List<double[]> smoothedPeaklist = new List<double[]>();
            smoothedPeaklist = DataAccessGcUtility.GetSmoothedPeaklist(DataAccessGcUtility.GetMs1SlicePeaklist(spectrumCollectionGC, targetMz, analysisParamGC.MassAccuracy, Begin, End, analysisParamGC.IonMode), analysisParamGC.SmoothingMethod, analysisParamGC.SmoothingLevel);
            var chart = GetChart(smoothedPeaklist, 1, "Test", "Test", "mz", "Freq.");
            using (var memory = new System.IO.MemoryStream()) {
                chart.SaveImage(memory, ChartImageFormat.Bmp);
                bitmap.BeginInit();
                bitmap.StreamSource = memory;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            return bitmap;

        }
        private BitmapImage GetBitmapImageForLC(PeakAreaBean peakAreaBean, ObservableCollection<RAW_Spectrum> spectrumCollection, ProjectPropertyBean projectPropertyBean, AnalysisParametersBean analysisParametersBean) {
            BitmapImage bitmap = new BitmapImage();
            float targetMz = peakAreaBean.AccurateMass;
            float targetRt = peakAreaBean.RtAtPeakTop; float targetLeftRt = peakAreaBean.RtAtLeftPeakEdge; float targetRightRt = peakAreaBean.RtAtRightPeakEdge;
            float Begin = targetLeftRt - (targetRightRt - targetLeftRt) * 1.5f;
            float End = targetRightRt + (targetRightRt - targetLeftRt) * 1.5f;
            List<double[]> smoothedPeaklist = new List<double[]>();
            smoothedPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectPropertyBean, targetMz, analysisParametersBean.CentroidMs1Tolerance, Begin, End), analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel);
            var chart = GetChart(smoothedPeaklist, 1, "Test", "Test", "mz", "Freq.");

            using (var memory = new System.IO.MemoryStream()) {
                chart.SaveImage(memory, ChartImageFormat.Bmp);
                bitmap.BeginInit();
                bitmap.StreamSource = memory;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            return bitmap;
        }

        private static List<double[]> GetMs1Peaklist(ObservableCollection<RAW_Spectrum> spectrumCollection, float focusedMass, float ms1Tolerance, float retentionTimeBegin, float retentionTimeEnd, Dictionary<int, AnalystExperimentInformationBean> experimentID_AnalystExperimentInformationBean) {
            var peaklist = new List<double[]>();
            RAW_Spectrum spectrum;
            RAW_PeakElement[] massSpectra;
            int startIndex = 0;
            double sum = 0, maxIntensityMz, maxMass;

            int ms1LevelId = 0, experimentNumber = experimentID_AnalystExperimentInformationBean.Count, counter;
            foreach (var value in experimentID_AnalystExperimentInformationBean) { if (value.Value.MsType == MsType.SCAN) { ms1LevelId = value.Key; break; } }
            counter = ms1LevelId;

            while (counter < spectrumCollection.Count) {
                spectrum = spectrumCollection[counter];

                if (spectrum.ScanStartTime < retentionTimeBegin) { counter += experimentNumber; continue; }
                if (spectrum.ScanStartTime > retentionTimeEnd) break;

                sum = 0;
                massSpectra = spectrum.Spectrum;
                maxIntensityMz = double.MinValue;
                maxMass = focusedMass;
                startIndex = GetMs1StartIndex(focusedMass, ms1Tolerance, massSpectra);

                for (int k = startIndex; k < massSpectra.Length; k++) {
                    if (massSpectra[k].Mz < focusedMass - ms1Tolerance) continue;
                    else if (focusedMass - ms1Tolerance <= massSpectra[k].Mz && massSpectra[k].Mz <= focusedMass + ms1Tolerance) {
                        sum += massSpectra[k].Intensity;
                        if (maxIntensityMz < massSpectra[k].Intensity) {
                            maxIntensityMz = massSpectra[k].Intensity;
                            maxMass = massSpectra[k].Mz;
                        }
                    }
                    else if (massSpectra[k].Mz > focusedMass + ms1Tolerance) break;
                }

                peaklist.Add(new double[] { spectrum.ScanStartTime, sum });

                counter += experimentNumber;
            }
            return peaklist;
        }

        public static int GetMs1StartIndex(float focusedMass, float ms1Tolerance, RAW_PeakElement[] massSpectra) {
            if (massSpectra == null || massSpectra.Length == 0) return 0;
            float targetMass = focusedMass - ms1Tolerance;
            int startIndex = 0, endIndex = massSpectra.Length - 1;
            int counter = 0;
            if (targetMass > massSpectra[endIndex].Mz) return endIndex;

            while (counter < 10) {
                if (massSpectra[startIndex].Mz <= targetMass && targetMass < massSpectra[(startIndex + endIndex) / 2].Mz) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (massSpectra[(startIndex + endIndex) / 2].Mz <= targetMass && targetMass < massSpectra[endIndex].Mz) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }


        public static List<double[]> LinearWeightedMovingAverage(List<double[]> peaklist, int smoothingLevel) {
            List<double[]> chromatogramDataPointCollection = new List<double[]>();
            double[] smoothedPeakInformation;
            double smoothedPeakIntensity;
            double sum;
            int lwmaNormalizationValue = smoothingLevel + 1;

            for (int i = 1; i <= smoothingLevel; i++)
                lwmaNormalizationValue += i * 2;

            for (int i = 0; i < peaklist.Count; i++) {
                smoothedPeakIntensity = 0;
                sum = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++) {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += peaklist[i][1] * (smoothingLevel - Math.Abs(j) + 1);
                    else sum += peaklist[i + j][1] * (smoothingLevel - Math.Abs(j) + 1);
                }
                smoothedPeakIntensity = (double)(sum / lwmaNormalizationValue);
                smoothedPeakInformation = new double[] { peaklist[i][0], smoothedPeakIntensity };
                chromatogramDataPointCollection.Add(smoothedPeakInformation);
            }
            return chromatogramDataPointCollection;
        }



        public Chart GetChart(List<double[]> num, int thickness, string titleIn, string legend, string xlab, string ylab) {
            var chart1 = new Chart();
            chart1.Series.Clear();
            chart1.ChartAreas.Clear();
            chart1.Titles.Clear();
            chart1.Width = 150;
            chart1.Height = 40;

            ChartArea area1 = new ChartArea("Area1");
            area1.AxisX.Enabled = AxisEnabled.False;
            area1.AxisY.Enabled = AxisEnabled.False;
            area1.AxisY.Minimum = num.Min(x => x[3]) - 0.0002;
            area1.AxisY.Maximum = num.Max(x => x[3]) + 0.0002;

            Series seriesLine = new Series();
            seriesLine.Legend = null;
            seriesLine.ChartType = SeriesChartType.Line;
            seriesLine.BorderWidth = thickness;
            seriesLine.MarkerStyle = MarkerStyle.None;
            //   seriesLine.MarkerSize = 1;
            foreach (var n in num) {
                seriesLine.Points.AddXY(n[0], n[3]);
            }
            // chartarea
            seriesLine.ChartArea = "Area1";

            chart1.ChartAreas.Add(area1);
            chart1.Series.Add(seriesLine);
            return chart1;
        }
        */
        #endregion
    }
    #endregion
}
