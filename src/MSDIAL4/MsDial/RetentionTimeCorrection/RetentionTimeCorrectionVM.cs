using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;
using Msdial.Lcms.Dataprocess.Utility;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Controls;

using CompMs.Graphics.Core.Base;

namespace Rfx.Riken.OsakaUniv.RetentionTimeCorrection
{
    public enum RtDiffLabel { id, rt, name };
    public class ViewModel : ViewModelBase {

        #region members and properties
        public MainWindow mainWindow;
        public RetentionTimeCorrectionWin RtWin;
        public RetentionTimeCorrectionCommon RtCorrectionCommon { get; set; }
        public RetentionTimeCorrectionParam RtCorrectionParam { get; set; }
        public List<CommonStdData> CommonStdList { get; set; }
        public bool Processed { get; set; } = false;
        public bool IsViewMode { get; set; }

        // parameters 
        public string[] Interpolation { get; set; } = new string[] { "Linear" };
        public string[] ExtrapolationMethod_Begin { get; set; } = new string[] { "Fixed value", "Use the RT difference of 1st STD", "Linear extrapolation" };
        public string[] ExtrapolationMethod_End { get; set; } = new string[] { "Use the RT difference of last STD", "Linear extrapolation" };
        public string[] RtDiffCalcMethod { get; set; } = new string[] { "Sample - Sample Average", "Sample - Reference" };
        public string[] Combobox_LabelArr { get; set; } = new string[] { "ID", "RT", "Name" };
        public RtDiffLabel RtDiffLabel { get; set; } = RtDiffLabel.id;
        public bool CheckBox_SkipCheck { get; set; } = false;
        public bool CheckBox_RunWithRtCorrection { get; set; } = true;
        public bool CheckBox_WithSmoothing { get { return RtCorrectionParam.doSmoothing; } set { RtCorrectionParam.doSmoothing = value; SettingChanged(); } }
        private System.Threading.Tasks.ParallelOptions parallelOptions = new System.Threading.Tasks.ParallelOptions();

        #region intercept
        private float _intercept = 0.0f;
        public float UserSettingIntercept {
            get { return _intercept; }
            set {
                if (_intercept == value) return;
                _intercept = value;
                RtCorrectionParam.UserSettingIntercept = value;
                SetDemoData();
            }
        }

        #endregion

        #region Standard data table
        private List<StandardCompoundVM> _standardData;
        public List<StandardCompoundVM> StandardData {
            get { return _standardData; }
            set {
                if (_standardData == value) return;
                _standardData = value; OnPropertyChanged("StandardData");
            }
        }
        #endregion

        #region UserControls for graph
        private DefaultUC allRtDiffUc;
        public DefaultUC AllRtDiffUC {
            get {
                return allRtDiffUc;
            }
            set {
                allRtDiffUc = value; OnPropertyChanged("AllRtDiffUC");
            }
        }

        private StackPanel _stackEachRtDiffUc;
        public StackPanel StackPanel_EachRtDiffUc {
            get {
                return _stackEachRtDiffUc;
            }
            set {
                _stackEachRtDiffUc = value; OnPropertyChanged("StackPanel_EachRtDiffUc");
            }
        }

        private StackPanel _stackEachPeakHeight;
        public StackPanel StackPanel_EachStdPeakHeightUc {
            get {
                return _stackEachPeakHeight;
            }
            set {
                _stackEachPeakHeight = value; OnPropertyChanged("StackPanel_EachStdPeakHeightUc");
            }
        }


        private Grid _grid_EachStdEICUc;
        public Grid Grid_EachStdEICUc {
            get {
                return _grid_EachStdEICUc;
            }
            set {
                _grid_EachStdEICUc = value; OnPropertyChanged("Grid_EachStdEICUc");
            }
        }

        #endregion


        #endregion

        #region constructor
        public ViewModel(){}

        public ViewModel(MainWindow mainWindow, RetentionTimeCorrectionWin win, bool isViewMode) {
            this.mainWindow = mainWindow;
            this.IsViewMode = isViewMode;

            this.RtWin = win;
            this.RtCorrectionCommon = mainWindow.AnalysisParamForLC.RetentionTimeCorrectionCommon;
            this.RtCorrectionParam = this.RtCorrectionCommon.RetentionTimeCorrectionParam;
            parallelOptions.MaxDegreeOfParallelism = mainWindow.AnalysisParamForLC.NumThreads;


            if (this.RtCorrectionCommon.StandardLibrary == null || this.RtCorrectionCommon.StandardLibrary.Count == 0)
                 StandardData = Model.InitializeStandardDataTable();
            else {
                StandardData = Model.ConvertTextFormatToCompoundVM(this.RtCorrectionCommon.StandardLibrary);
                this.RtWin.ComboBox_Interpolation.SelectedIndex = (int)RtCorrectionParam.InterpolationMethod;
                this.RtWin.ComboBox_Extrapolation_Begin.SelectedIndex = (int)RtCorrectionParam.ExtrapolationMethodBegin;
                this.RtWin.ComboBox_Extrapolation_End.SelectedIndex = (int)RtCorrectionParam.ExtrapolationMethodEnd;
                this.RtWin.ComboBox_RtDiffCalcMethod.SelectedIndex = (int)RtCorrectionParam.RtDiffCalcMethod;
                RtCorrectionResUpdate(false);
            }
        }


        #endregion

        #region Delegate commands

        #region Load Library
        private DelegateCommand _loadLibrary;
        public DelegateCommand LoadLibrary {
            get {
                return _loadLibrary ?? (_loadLibrary = new DelegateCommand(excuteLoadLibrary, canLoadLibrary));
            }
        }

        private void excuteLoadLibrary(object obj) {
            StandardData = Model.LoadLibraryFile();
            Processed = false;
            OnPropertyChanged("Processed");
        }

        private bool canLoadLibrary(object obj) {
            return false;
        }
        #endregion

        #region Excute RtCorrection
        private DelegateCommand _rtCorrection;
        public DelegateCommand RtCorrection {
            get {
                return _rtCorrection ?? 
                    (_rtCorrection = new DelegateCommand(excuteRtCorrection, canRtCorrection));
            }
        }

        private void excuteRtCorrection(object obj) {
            foreach(var f in this.mainWindow.AnalysisFiles) {
                f.RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean();
            }
            RtCorrectionCommon.SampleCellInfoListList = new List<List<SampleListCellInfo>>();
            RtCorrectionCommon.StandardLibrary = Model.ConvertCompoundVMtoTextFormat(StandardData);
            new RtCorrectionProcessForLC().Process(this.mainWindow, this.RtWin);
        }

        private bool canRtCorrection(object obj) {
            if (IsViewMode) {
                ShowMessage_ViewMode();
                return false;
            }

            var targetNum = this.StandardData.Count(x => x.IsTarget == true);
            if (targetNum == 0) {
                ShowMessage_PleaseSelectStd();
                return false;
            }

            return true;
        }
        #endregion

        #region AutoFill
        private DelegateCommand _autoFill;
        public DelegateCommand AutoFill {
            get {
                return _autoFill ??
                    (_autoFill = new DelegateCommand(excuteAutoFill, canAutoFill));
            }
        }

        private void excuteAutoFill(object obj) {
            this.RtWin.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            updateRtTune();
            var numModified = 0;
            for(var i = 0; i < CommonStdList.Count; i++) {
                if (CommonStdList[i].NumHit > 0) {
                    var pre = CommonStdList[i].RetentionTimeList.FirstOrDefault(x => x > 0);
                    for (var j = 0; j < mainWindow.AnalysisFiles.Count; j++) {
                        if(SampleListVMs[j].Values[i].Rt  == 0) {
                            SampleListVMs[j].Values[i].Rt = pre;
                            SampleListVMs[j].Values[i].SampleListCellInfo = SampleListCellInfo.AutoModified;
                            numModified++;
                        }
                        else {
                            pre = CommonStdList[i].RetentionTimeList[j];
                        }
                    }
                }
            }
            updateRtTune();
            SettingChanged();
            ShowMessage_AutoFill(numModified);
            Mouse.OverrideCursor = null;
            this.RtWin.IsEnabled = true;
        }

        private bool canAutoFill(object obj) {
            if (IsViewMode) {
                ShowMessage_ViewMode();
                return false;
            }
            if (!(bool)obj) {
                ShowMessage_CannotRun2();
                return false;
            }
            if (isInternalStdChanged()) {
                ShowMessage_CannotRun3();
                return false;
            }

            return true;
        }
        #endregion

        #region UpdateRtTune
        private DelegateCommand _updateRtTune;
        public DelegateCommand UpdateRtTune {
            get {
                return _updateRtTune ??
                    (_updateRtTune = new DelegateCommand(excuteUpdateRtTune, canUpdateRtTune));
            }
        }

        private void excuteUpdateRtTune(object obj) {
            this.RtWin.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            updateRtTune();
            SettingChanged();
            Mouse.OverrideCursor = null;
            this.RtWin.IsEnabled = true;
        }

        private bool canUpdateRtTune(object obj) {
            if (IsViewMode) {
                ShowMessage_ViewMode();
                return false;
            }
            if (!(bool)obj) {
                ShowMessage_CannotRun2();
                return false;
            }
            
            if (isInternalStdChanged()) {
                ShowMessage_CannotRun3();
                return false;
            }
            return true;
        }

        private bool isInternalStdChanged() {
            var refIdList1 = StandardData.Where(x => x.IsTarget).Select(y => y.ReferenceId).OrderBy(z => z);
            var refIdList2 = CommonStdList.Select(y => y.Reference.ReferenceId).OrderBy(z => z);
            foreach(var x1 in refIdList1) { Console.WriteLine(x1); }
            Console.WriteLine("2nd list");
            foreach (var x1 in refIdList2) { Console.WriteLine(x1); }
            return !refIdList1.SequenceEqual(refIdList2);
        }

        private void updateRtTune() {
            RtCorrectionCommon.SampleCellInfoListList = new List<List<SampleListCellInfo>>();
            for (var i = 0; i < SampleListVMs.Count; i++) {
                var l = new List<SampleListCellInfo>();
                for (var j = 0; j < SampleListVMs[i].Values.Count; j++) {
                    l.Add(SampleListVMs[i].Values[j].SampleListCellInfo);
                    mainWindow.AnalysisFiles[i].RetentionTimeCorrectionBean.StandardList[j].SamplePeakAreaBean.RtAtPeakTop = SampleListVMs[i].Values[j].Rt;
                }
                RtCorrectionCommon.SampleCellInfoListList.Add(l);
            }
            CommonStdList = Model.MakeCommonStdList(mainWindow.AnalysisFiles, this.RtCorrectionCommon.StandardLibrary);
        }

        #endregion

        #region Run 
        private DelegateCommand _next;
        public DelegateCommand Next {
            get {
                return _next ??
                    (_next = new DelegateCommand(excuteNext, canNext));
            }
        }

        private void excuteNext(object obj) {
            if (!CheckBox_RunWithRtCorrection) {
                foreach (var f in this.mainWindow.AnalysisFiles) {
                    f.RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean();
                }
            }
            this.RtWin.DialogResult = true;
            this.RtWin.Close();
        }

        private bool canNext(object obj) {
            if (IsViewMode) {
                ShowMessage_ViewMode();
                return false;
            }
            if (!(bool)obj && CheckBox_RunWithRtCorrection) {
                ShowMessage_CannotRun();
                return false;
            }
            if (!CheckBox_RunWithRtCorrection) {
                return true;
            }
            return (bool)obj;
         }
        #endregion

        #endregion

        #region SampleList DataGrid
        public List<SampleListVM> SampleListVMs { get; set; }

        public void CreateSampleList() {
            SampleListVMs = new List<SampleListVM>();
            if (RtCorrectionCommon.SampleCellInfoListList == null || RtCorrectionCommon.SampleCellInfoListList.Count == 0) {
                for (var i = 0; i < mainWindow.AnalysisFiles.Count; i++) {
                    var vm = new SampleListVM(mainWindow.AnalysisFiles[i]) { SampleID = i };
                    foreach (var x in CommonStdList) {
                        var rt = x.RetentionTimeList[i];
                        var cell = new SampleListCell() { Rt = rt };
                        if (rt == 0) cell.SampleListCellInfo = SampleListCellInfo.Zero;
                        vm.Values.Add(cell);
                        vm.StartBgChange();
                    }
                    SampleListVMs.Add(vm);
                }
            }
            else {
                for (var i = 0; i < mainWindow.AnalysisFiles.Count; i++) {
                    var vm = new SampleListVM(mainWindow.AnalysisFiles[i]) { SampleID = i };
                    var counter = 0;
                    foreach (var x in CommonStdList) {
                        var rt = x.RetentionTimeList[i];
                        var cell = new SampleListCell() { Rt = rt };
                        cell.SampleListCellInfo = RtCorrectionCommon.SampleCellInfoListList[i][counter];
                        vm.Values.Add(cell);
                        vm.StartBgChange();
                        counter++;
                    }
                    SampleListVMs.Add(vm);
                }
            }
            var header = CommonStdList.Select(x => x.Reference.MetaboliteName).ToList();
            AddColumns(this.RtWin.DataGrid_SampleTable, header);
        }


        private void AddColumns(DataGrid dg, List<string> header) {
            RemoveColumn(dg);

            var headerStyle = (Style)RtWin.Grid_SampleData.FindResource("HeaderStyle");
            var textStyle = (Style)RtWin.Grid_SampleData.FindResource("TextStyle");

            for (var i = 0; i < header.Count; i++) {
                var hed = header[i];
                var path = string.Format("Values[{0}].Rt", i);

                var binding = new Binding(path);
                binding.StringFormat = "0.####";
                binding.Mode = BindingMode.TwoWay;
                binding.ValidatesOnDataErrors = true;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
                binding.NotifyOnValidationError = true;
                var bgBinding = new Binding(string.Format("Values[{0}].BgColor", i));
                bgBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

                var style = new Style(typeof(DataGridCell));
                var setter = new Setter();
                setter.Property = DataGridCell.BackgroundProperty;
                setter.Value = bgBinding;
                style.Setters.Add(setter);

                var column = new DataGridTextColumn() {
                    Header = hed,
                    HeaderStyle = headerStyle,
                    ElementStyle = textStyle,
                    Width = 50,
                    ClipboardContentBinding = binding,
                    IsReadOnly = false,
                    CanUserSort = true,
                    CellStyle = style,
                    SortMemberPath = path,
                    Binding = binding
                };
/*                var elementName = "TextBox_Sample_" + i;
                FrameworkElementFactory factory1 = new FrameworkElementFactory(typeof(TextBox));
                factory1.SetValue(TextBox.TextProperty, binding);
                factory1.SetValue(TextBox.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                factory1.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Stretch);
                factory1.SetValue(TextBox.TextAlignmentProperty, TextAlignment.Center);
                factory1.SetValue(TextBox.BorderThicknessProperty, new Thickness(0, 0, 0, 0));
                factory1.SetValue(TextBox.NameProperty, elementName);
                factory1.SetValue(TextBox.BackgroundProperty, bgBinding);
                DataTemplate cellTemplate1 = new DataTemplate();
                cellTemplate1.VisualTree = factory1;
                column.CellTemplate = cellTemplate1;
  */              /*
                var binding3 = new Binding() { ElementName = elementName };
                FrameworkElementFactory factory3 = new FrameworkElementFactory(typeof(Grid));
                factory3.SetBinding(FocusManager.FocusedElementProperty, binding3);
  
                var binding2 = new Binding(path);
                binding2.Mode = BindingMode.TwoWay;
                binding2.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
                binding2.StringFormat = "0.0";

                FrameworkElementFactory factory2 = new FrameworkElementFactory(typeof(TextBox));
                factory2.SetValue(TextBox.TextProperty, binding2);
                factory2.SetValue(TextBox.NameProperty, elementName);
                factory3.AppendChild(factory2);
                Console.WriteLine(factory3.FirstChild.Name);

                DataTemplate cellTemplate2 = new DataTemplate();
                cellTemplate2.VisualTree = factory3;
                column.CellEditingTemplate = cellTemplate2;
                */
                dg.Columns.Add(column);
            }
            dg.UpdateLayout();
        }

        private void RemoveColumn(DataGrid dg) {
            while (dg.Columns.Count > 4) {
                System.Diagnostics.Debug.WriteLine("num column: " + dg.Columns.Count);
                dg.Columns.RemoveAt(dg.Columns.Count - 1);
            }
        }

        #endregion


        #region set UIs

        public DefaultUC Set_OverviewRtDiff(ObservableCollection<AnalysisFileBean> analysisFiles, AnalysisParametersBean param, RetentionTimeCorrectionParam rtParam, List<CommonStdData> detectedStdList, RtDiffLabel label, List<SolidColorBrush> solidColorBrushList) {
            var drawing = Utility.GetDrawing_RtDiff_OverView(analysisFiles, param, rtParam, detectedStdList, label, solidColorBrushList);
            drawing.Area.GraphBorder = new Pen(Brushes.Transparent, 0);
            drawing.Area.BackGroundColor = Brushes.White;
            drawing.Area.AxisX.MinorScaleEnabled = false;
            drawing.Area.AxisY.MinorScaleEnabled = false;

            return new DefaultUC(drawing);
         }

        public StackPanel Set_EachRtDiffUC(ObservableCollection<AnalysisFileBean> files, AnalysisParametersBean param, RetentionTimeCorrectionParam rtParam,
            List<CommonStdData> detectedStdList, RtDiffLabel label, ProjectPropertyBean project) {
            var chartStackPanel = new StackPanel() { Orientation = Orientation.Vertical };
            var num = files.Count;
            var yMax = float.MinValue; var yMin = float.MaxValue;
            foreach (var f in files) {
                if (f.RetentionTimeCorrectionBean.RtDiff != null) {
                    var tmp_max = f.RetentionTimeCorrectionBean.RtDiff.Max();
                    var tmp_min = f.RetentionTimeCorrectionBean.RtDiff.Min();
                    if (yMax < tmp_max) yMax = (float)tmp_max;
                    if (yMin > tmp_min) yMin = (float)tmp_min;
                }
            }
            //var dict = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            var classnameToBytes = project.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);
            for (var i = 0; i < num; i++) {
                var uc = new DefaultUC(Utility.GetDrawing_RtDiff_Each(files[i], param, rtParam, detectedStdList, label, 
                    classnameToBrushes[files[i].AnalysisFilePropertyBean.AnalysisFileClass], (float)yMin, (float)yMax), new MouseActionSetting() { CanMouseAction = false });
                var grid = new Grid() { Height = 240, HorizontalAlignment = HorizontalAlignment.Stretch };
                uc.ContextMenu = this.RtWin.Resources["menuDefaultUC"] as ContextMenu;

                grid.Children.Add(uc);
                chartStackPanel.Children.Add(grid);
            }
            return chartStackPanel;
        }

        public StackPanel Set_EachStdPeakHeightUC(ObservableCollection<AnalysisFileBean> files, List<CommonStdData> detectedStdDict, ProjectPropertyBean project) {
            var chartStackPanel = new StackPanel() { Orientation = Orientation.Vertical };
            var classnameToBytes = project.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);
            //var brushDict = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            foreach (var std in detectedStdDict) {
                if (std.NumHit == 0) continue;
                var uc = new DefaultUC(Utility.GetDrawVisual_IntensityPlot_EachCompound(files, std, classnameToBrushes), new MouseActionSetting() { FixMaxX = true, FixMinX = true });
                var grid = new Grid() { Height = 200, HorizontalAlignment = HorizontalAlignment.Stretch };
                uc.ContextMenu = this.RtWin.Resources["menuDefaultUC"] as ContextMenu;

                grid.Children.Add(uc);
                chartStackPanel.Children.Add(grid);
            }
            return chartStackPanel;
        }

        public Grid Set_EachStdEICUc(ObservableCollection<AnalysisFileBean> files, List<CommonStdData> commonStdList, 
            AnalysisParametersBean param, ProjectPropertyBean projectProperty) {

            var maingrid = new Grid() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            maingrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 150 });
            maingrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 150 });

            var chartStackPanelLeft = new StackPanel() { Orientation = Orientation.Vertical };
            var chartStackPanelRight = new StackPanel() { Orientation = Orientation.Vertical };
            //var brushDict = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            var classnameToBytes = projectProperty.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);
            foreach (var std in commonStdList) {
                var spaceGrid = new Grid() { Height = 15 };
                var spaceGrid2 = new Grid() { Height = 15 };

                var label1 = new TextBox() {
                    Text = std.Reference.MetaboliteName + ", mz: " + std.Reference.AccurateMass,
                    Height = 20, FontFamily = new FontFamily("Helvetica"), FontSize = 15,
                    HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(7, 0, 0, -5), BorderThickness = new Thickness(0, 0, 0, 0), IsReadOnly = true
                };

                var uc = new DefaultUC(Utility.GetDrawVisual_EIC_Overview(files, std, param, classnameToBrushes), new MouseActionSetting() { FixMaxX = true, FixMinX = true });
                var grid = new Grid() { MinHeight = 200, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 0) };
                grid.Children.Add(uc);
                uc.ContextMenu = this.RtWin.Resources["menuDefaultUC"] as ContextMenu;

                var label3 = new TextBox() {
                    Text = "", Height = 20, BorderThickness = new Thickness(0, 0, 0, 0), Margin = new Thickness(0, 0, 0, -5), IsReadOnly = true
                };

                var uc2 = new DefaultUC(Utility.GetDrawVisual_correctedEIC_Overview(files, std, param, projectProperty, classnameToBrushes), new MouseActionSetting() { FixMaxX = true, FixMinX = true });
                var grid2 = new Grid() { MinHeight = 200, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 0) };
                grid2.Children.Add(uc2);
                uc2.ContextMenu = this.RtWin.Resources["menuDefaultUC"] as ContextMenu;

                chartStackPanelLeft.Children.Add(label1);
                chartStackPanelRight.Children.Add(label3);
                chartStackPanelLeft.Children.Add(grid);
                chartStackPanelRight.Children.Add(grid2);
                chartStackPanelLeft.Children.Add(spaceGrid);
                chartStackPanelRight.Children.Add(spaceGrid2);

            }
            Grid.SetColumn(chartStackPanelLeft, 0);
            Grid.SetColumn(chartStackPanelRight, 1);
            maingrid.Children.Add(chartStackPanelLeft);
            maingrid.Children.Add(chartStackPanelRight);
            return maingrid;
        }
        #endregion

        #region UI update, setting changed
        public void RtCorrectionResUpdate(bool isAfterRtTune = true) {
            if (!Processed) {
                Processed = true; OnPropertyChanged("Processed");
            }
            if (this.CheckBox_SkipCheck)
                excuteNext(null);

            CommonStdList = Model.MakeCommonStdList(mainWindow.AnalysisFiles, this.RtCorrectionCommon.StandardLibrary);
            if (isAfterRtTune) {
                var NoHitCompList = CommonStdList.Where(x => x.NumHit == 0).Select(x => x.Reference).ToList();
                ShowMessage_NoHitCompounds(NoHitCompList);
            }

            Model.UpdateRtCorrectionBean(mainWindow.AnalysisFiles, this.parallelOptions, this.RtCorrectionParam, CommonStdList);
            CreateSampleList();
            OnPropertyChanged("SampleListVMs");
            Update_AllViewer();

        }

        public void SettingChanged() {
            if (!Processed) { SetDemoData(); return; }
            this.RtWin.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            Model.UpdateRtCorrectionBean(mainWindow.AnalysisFiles, this.parallelOptions, this.RtCorrectionParam, CommonStdList);
            Update_AllViewer();
            Mouse.OverrideCursor = null;
            this.RtWin.IsEnabled = true;
        }

        public void Update_AllViewer() {
            AllRtDiffUC = Set_OverviewRtDiff(mainWindow.AnalysisFiles, mainWindow.AnalysisParamForLC, RtCorrectionParam, CommonStdList, RtDiffLabel, mainWindow.SolidColorBrushList);
            StackPanel_EachRtDiffUc = Set_EachRtDiffUC(mainWindow.AnalysisFiles, mainWindow.AnalysisParamForLC, RtCorrectionParam, CommonStdList, RtDiffLabel, mainWindow.ProjectProperty);

            StackPanel_EachStdPeakHeightUc = Set_EachStdPeakHeightUC(mainWindow.AnalysisFiles, CommonStdList, mainWindow.ProjectProperty);
            Grid_EachStdEICUc = Set_EachStdEICUc(mainWindow.AnalysisFiles, CommonStdList, mainWindow.AnalysisParamForLC, mainWindow.ProjectProperty);
        }

        public void SetDemoData() {
            if (!Processed)
                AllRtDiffUC = new DefaultUC(Utility.GetDemoData(this.RtCorrectionParam, UserSettingIntercept));
        }

        #endregion

        #region show messages

        private void ShowMessage_ViewMode() {
            ShowMessage("RT correction cannot run because you opend as view mode. " +
                          "\nIf you want to rerun all process, please click \"Menubar > Data processing > All processing", 550);
        }

        private void ShowMessage_PleaseSelectStd() {
            ShowMessage("Please select at least one standard");
        }

        private void ShowMessage_CannotRun() {
            ShowMessage("Please click RT correction before Running.\r\nOr please uncheck \"" + this.RtWin.Label_checkbox1.Content + "\"", 400);
        }

        private void ShowMessage_CannotRun2() {
            ShowMessage("Please click RT correction before Running");
        }

        private void ShowMessage_CannotRun3() {
            ShowMessage("Please click RT correction if you changed internal standard list", 400);
        }
        private void ShowMessage_AutoFill(int num) {
            ShowMessage("Auto filled " + num + " cels.\r\n");
        }

        private void ShowMessage(string text, int width = 250) {
            var w = new ShortMessageWindow(text);
            w.Width = width;
            w.Owner = this.RtWin;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.Show();
        }

        private void ShowMessage_NoHitCompounds(List<TextFormatCompoundInformationBean> lib) {
            if (lib.Count == 0) return;
            var text = new StringBuilder();
            text.AppendLine("No hits: " + lib.Count + " compounds"); 
            foreach(var l in lib) {
                text.AppendLine("id: " + l.ReferenceId + ", " + l.MetaboliteName);
            }
            var w = new ShortMessageWindow(text.ToString());
            w.Width = 700;
            w.Height += 15 * lib.Count;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.Show();
        }


        #endregion
    }

    public class SampleListVM
    {
        private AnalysisFileBean file;
        public SampleListVM(AnalysisFileBean f) { file = f; }
        public int SampleID { get; set; }
        public string FileName { get { return file.AnalysisFilePropertyBean.AnalysisFileName; } }
        public string SampleClass { get { return file.AnalysisFilePropertyBean.AnalysisFileClass; } }
        public int NumHit { get { return Values.Count(x => x.Rt > 0); } }
        public List<SampleListCell> Values { get; set; } = new List<SampleListCell>();
        
        public void StartBgChange() {
            foreach (var v in Values) v.CanBgChange = true; 
        }        
    }

    public class SampleListCell: ViewModelBase
    {
        private float _rt;
        private SampleListCellInfo _sampleListCellInfo = SampleListCellInfo.Normal;
        public SampleListCellInfo SampleListCellInfo { get { return _sampleListCellInfo; } set { if (_sampleListCellInfo == value) return; _sampleListCellInfo = value; OnPropertyChanged("BgColor"); } }
        public bool CanBgChange { get; set; } = false;
        public SolidColorBrush BgColor { get { return cellInfoToBgColor(); } }
        public float Rt { get { return _rt; } set { if (_rt == value) return; _rt = value; OnPropertyChanged("Rt"); if (CanBgChange) { if (value == 0) SampleListCellInfo = SampleListCellInfo.Zero; else SampleListCellInfo = SampleListCellInfo.ManualModified; } } }

        private SolidColorBrush cellInfoToBgColor() {
            if (SampleListCellInfo == SampleListCellInfo.Normal) return Brushes.White;
            else if (SampleListCellInfo == SampleListCellInfo.Zero) return Brushes.Orange;
            else if (SampleListCellInfo == SampleListCellInfo.ManualModified) return Brushes.LightBlue;
            else if (SampleListCellInfo == SampleListCellInfo.AutoModified) return Brushes.LightGreen;
            else return Brushes.Black;
        }
    }

    public class StandardCompoundVM: ViewModelBase
    {
        public TextFormatCompoundInformationBean TextFormatCompoundInformationBean { get; set; }
        public StandardCompoundVM() {
            TextFormatCompoundInformationBean = new TextFormatCompoundInformationBean();
        }

        public StandardCompoundVM(TextFormatCompoundInformationBean lib) {
            TextFormatCompoundInformationBean = lib;
        }

        public int ReferenceId {
            get { return TextFormatCompoundInformationBean.ReferenceId; }
            set { TextFormatCompoundInformationBean.ReferenceId = value; }
        }

        public string MetaboliteName {
            get { return TextFormatCompoundInformationBean.MetaboliteName; }
            set { if(TextFormatCompoundInformationBean.MetaboliteName == value) return; TextFormatCompoundInformationBean.MetaboliteName = value;
                    OnPropertyChanged("MetaboliteName");
            }
        }

        public float RetentionTime {
            get { return TextFormatCompoundInformationBean.RetentionTime; }
            set {
                if (TextFormatCompoundInformationBean.RetentionTime == value) return; TextFormatCompoundInformationBean.RetentionTime = value;
                OnPropertyChanged("RetentionTime");
            }
        }

        public float RetentionTimeTolerance {
            get { return TextFormatCompoundInformationBean.RetentionTimeTolerance; }
            set {
                if (TextFormatCompoundInformationBean.RetentionTimeTolerance == value) return; TextFormatCompoundInformationBean.RetentionTimeTolerance = value;
                OnPropertyChanged("RetentionTimeTolerance");
            }
        }

        public float AccurateMass {
            get { return TextFormatCompoundInformationBean.AccurateMass; }
            set {
                if (TextFormatCompoundInformationBean.AccurateMass == value) return; TextFormatCompoundInformationBean.AccurateMass = value;
                OnPropertyChanged("AccurateMass");
            }
        }

        public float AccurateMassTolerance {
            get { return TextFormatCompoundInformationBean.AccurateMassTolerance; }
            set {
                if (TextFormatCompoundInformationBean.AccurateMassTolerance == value) return; TextFormatCompoundInformationBean.AccurateMassTolerance = value;
                OnPropertyChanged("AccurateMassTolerance");
            }
        }

        public float MinimumPeakHeight {
            get { return TextFormatCompoundInformationBean.MinimumPeakHeight; }
            set {
                if (TextFormatCompoundInformationBean.MinimumPeakHeight == value) return; TextFormatCompoundInformationBean.MinimumPeakHeight = value;
                OnPropertyChanged("MinimumPeakHeight");
            }
        }

        public bool IsTarget {
            get { return TextFormatCompoundInformationBean.IsTarget; }
            set {
                if (TextFormatCompoundInformationBean.IsTarget == value) return; TextFormatCompoundInformationBean.IsTarget = value;
                OnPropertyChanged("IsTarget");
            }
        }
    }

}
