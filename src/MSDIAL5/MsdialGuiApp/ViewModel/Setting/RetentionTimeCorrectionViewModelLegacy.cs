using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.View.Setting;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Setting {
    public enum RtDiffLabel { id, rt, name };
    public class RetentionTimeCorrectionViewModelLegacy : ViewModelBase {

        #region members and properties
        public RetentionTimeCorrectionWinLegacy RtWin;
        public RetentionTimeCorrectionCommon RtCorrectionCommon { get; set; }
        public RetentionTimeCorrectionParam RtCorrectionParam { get; set; }
        public List<CommonStdData> CommonStdList { get; set; } = new List<CommonStdData>(0);
        public List<AnalysisFileBean> AnalysisFiles { get; set; }
        public ParameterBase Parameter { get; set; }
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
        private DefaultUC? allRtDiffUc;
        public DefaultUC? AllRtDiffUC {
            get {
                return allRtDiffUc;
            }
            set {
                allRtDiffUc = value; OnPropertyChanged("AllRtDiffUC");
            }
        }

        private StackPanel? _stackEachRtDiffUc;
        public StackPanel? StackPanel_EachRtDiffUc {
            get {
                return _stackEachRtDiffUc;
            }
            set {
                _stackEachRtDiffUc = value; OnPropertyChanged("StackPanel_EachRtDiffUc");
            }
        }

        private StackPanel? _stackEachPeakHeight;
        public StackPanel? StackPanel_EachStdPeakHeightUc {
            get {
                return _stackEachPeakHeight;
            }
            set {
                _stackEachPeakHeight = value; OnPropertyChanged("StackPanel_EachStdPeakHeightUc");
            }
        }


        private Grid? _grid_EachStdEICUc;
        public Grid? Grid_EachStdEICUc {
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
        public RetentionTimeCorrectionViewModelLegacy(
            IReadOnlyList<AnalysisFileBean> files, ParameterBase param, 
            RetentionTimeCorrectionWinLegacy win, bool isViewMode) {
            this.AnalysisFiles = files.ToList();
            this.Parameter = param;
            this.IsViewMode = isViewMode;

            this.RtWin = win;
            this.RtCorrectionCommon = param.RetentionTimeCorrectionCommon;
            //this.RtCorrectionCommon.AnalysisFileNames = files.Select(n => n.AnalysisFileName).ToList();
            this.RtCorrectionParam = this.RtCorrectionCommon.RetentionTimeCorrectionParam;
            parallelOptions.MaxDegreeOfParallelism = param.NumThreads;


            if (this.RtCorrectionCommon.StandardLibrary == null || this.RtCorrectionCommon.StandardLibrary.Count == 0)
                _standardData = RetentionTimeCorrectionModelLegacy.InitializeStandardDataTable();
            else {
                _standardData = RetentionTimeCorrectionModelLegacy.ConvertTextFormatToCompoundVM(this.RtCorrectionCommon.StandardLibrary) ?? new List<StandardCompoundVM>(0);
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
        private DelegateCommand? _loadLibrary;
        public DelegateCommand LoadLibrary {
            get {
                return _loadLibrary ??= new DelegateCommand(excuteLoadLibrary, canLoadLibrary);
            }
        }

        private void excuteLoadLibrary() {
            StandardData = RetentionTimeCorrectionModelLegacy.LoadLibraryFile();
            Processed = false;
            OnPropertyChanged("Processed");
            RtCorrection.RaiseCanExecuteChanged();
        }

        private bool canLoadLibrary() {
            return true;
        }
        #endregion

        #region Excute RtCorrection
        private DelegateCommand? _rtCorrection;
        public DelegateCommand RtCorrection {
            get {
                return _rtCorrection ??= new DelegateCommand(excuteRtCorrection, canRtCorrection);
            }
        }

        private void excuteRtCorrection() {
            //foreach (var f in this.AnalysisFiles) {
            //    f.RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean();
            //}
            RtCorrectionCommon.SampleCellInfoListList = new List<List<SampleListCellInfo>>();
            RtCorrectionCommon.StandardLibrary = RetentionTimeCorrectionModelLegacy.ConvertCompoundVMtoTextFormat(StandardData);
            new RtCorrectionProcessModelLegacy().Process(this.AnalysisFiles, this.Parameter, this.RtWin);
        }

        private bool canRtCorrection() {
            if (IsViewMode) {
                ShowMessage_ViewMode();
                return false;
            }

            var targetNum = this.StandardData.Count(x => x.IsTarget == true);
            if (targetNum == 0) {
                //ShowMessage_PleaseSelectStd();
                return false;
            }

            return true;
        }
        #endregion

        #region AutoFill
        private DelegateCommand<bool>? _autoFill;
        public DelegateCommand<bool> AutoFill {
            get {
                return _autoFill ??= new DelegateCommand<bool>(excuteAutoFill, canAutoFill);
            }
        }

        private void excuteAutoFill(bool obj) {
            this.RtWin.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            updateRtTune();
            var numModified = 0;
            for (var i = 0; i < CommonStdList.Count; i++) {
                if (CommonStdList[i].NumHit > 0) {
                    var pre = CommonStdList[i].RetentionTimeList.FirstOrDefault(x => x > 0);
                    for (var j = 0; j < this.AnalysisFiles.Count; j++) {
                        if (SampleListVMs[j].Values[i].Rt == 0) {
                            SampleListVMs[j].Values[i].Rt = (float)pre;
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

        private bool canAutoFill(bool obj) {
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
        private DelegateCommand<bool>? _updateRtTune;
        public DelegateCommand<bool> UpdateRtTune {
            get {
                return _updateRtTune ??= new DelegateCommand<bool>(excuteUpdateRtTune, canUpdateRtTune);
            }
        }

        private void excuteUpdateRtTune(bool obj) {
            this.RtWin.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            updateRtTune();
            SettingChanged();
            Mouse.OverrideCursor = null;
            this.RtWin.IsEnabled = true;
        }

        private bool canUpdateRtTune(bool obj) {
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
            var refIdList2 = CommonStdList.Select(y => y.Reference.ScanID).OrderBy(z => z);
            foreach (var x1 in refIdList1) { Console.WriteLine(x1); }
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
                    this.AnalysisFiles[i].RetentionTimeCorrectionBean.StandardList[j].SamplePeakAreaBean.ChromXs.RT = new RetentionTime(SampleListVMs[i].Values[j].Rt, AnalysisFiles[i].RetentionTimeCorrectionBean.StandardList[j].SamplePeakAreaBean.ChromXs.RT.Unit);
                }
                RtCorrectionCommon.SampleCellInfoListList.Add(l);
            }
            CommonStdList = RetentionTimeCorrectionMethod.MakeCommonStdList(this.AnalysisFiles, this.RtCorrectionCommon.StandardLibrary);
        }

        #endregion

        #region Run 
        private DelegateCommand<bool>? _next;
        public DelegateCommand<bool> Next {
            get {
                return _next ??= new DelegateCommand<bool>(excuteNext, canNext);
            }
        }

        private void excuteNext(bool obj) {
            //if (!CheckBox_RunWithRtCorrection) {
            //    foreach (var f in this.AnalysisFiles) {
            //        f.RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean();
            //    }
            //}
            Parameter.AdvancedProcessOptionBaseParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection = CheckBox_RunWithRtCorrection; 
            this.RtWin.DialogResult = true;
            this.RtWin.Close();
        }

        private bool canNext(bool obj) {
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
        public List<SampleListVM> SampleListVMs { get; set; } = new List<SampleListVM>(0);

        public void CreateSampleList() {
            SampleListVMs = new List<SampleListVM>();
            if (RtCorrectionCommon.SampleCellInfoListList == null || RtCorrectionCommon.SampleCellInfoListList.Count == 0) {
                for (var i = 0; i < this.AnalysisFiles.Count; i++) {
                    var vm = new SampleListVM(this.AnalysisFiles[i]) { SampleID = i };
                    foreach (var x in CommonStdList) {
                        var rt = x.RetentionTimeList[i];
                        var cell = new SampleListCell() { Rt = (float)rt };
                        if (rt == 0) cell.SampleListCellInfo = SampleListCellInfo.Zero;
                        vm.Values.Add(cell);
                        vm.StartBgChange();
                    }
                    SampleListVMs.Add(vm);
                }
            }
            else {
                for (var i = 0; i < this.AnalysisFiles.Count; i++) {
                    var vm = new SampleListVM(this.AnalysisFiles[i]) { SampleID = i };
                    var counter = 0;
                    foreach (var x in CommonStdList) {
                        var rt = x.RetentionTimeList[i];
                        var cell = new SampleListCell() { Rt = (float)rt };
                        cell.SampleListCellInfo = RtCorrectionCommon.SampleCellInfoListList[i][counter];
                        vm.Values.Add(cell);
                        vm.StartBgChange();
                        counter++;
                    }
                    SampleListVMs.Add(vm);
                }
            }
            var header = CommonStdList.Select(x => x.Reference.Name).ToList();
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

        public DefaultUC Set_OverviewRtDiff(List<AnalysisFileBean> analysisFiles, ParameterBase param, 
            RetentionTimeCorrectionParam rtParam, List<CommonStdData> detectedStdList, RtDiffLabel label, List<SolidColorBrush> solidColorBrushList) {
            var drawing = RetentionTimeCorrectionChart.GetDrawing_RtDiff_OverView(analysisFiles, param, rtParam, detectedStdList, label, solidColorBrushList);
            drawing.Area.GraphBorder = new Pen(Brushes.Transparent, 0);
            drawing.Area.BackGroundColor = Brushes.White;
            drawing.Area.AxisX.MinorScaleEnabled = false;
            drawing.Area.AxisY.MinorScaleEnabled = false;

            return new DefaultUC(drawing);
        }

        public StackPanel Set_EachRtDiffUC(List<AnalysisFileBean> files, ParameterBase param, RetentionTimeCorrectionParam rtParam,
            List<CommonStdData> detectedStdList, RtDiffLabel label) {
            var chartStackPanel = new StackPanel() { Orientation = Orientation.Vertical };
            var num = files.Count;
            var yMax = float.MinValue; var yMin = float.MaxValue;
            foreach (var f in files) {
                var rtDiff = f.RetentionTimeCorrectionBean.RtDiff;
                if (rtDiff != null) {
                    var tmp_max = rtDiff.Max();
                    var tmp_min = rtDiff.Min();
                    if (yMax < tmp_max) yMax = (float)tmp_max;
                    if (yMin > tmp_min) yMin = (float)tmp_min;
                }
            }
            //var dict = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            var classnameToBytes = param.ClassnameToColorBytes;
            var classnameToBrushes = ChartBrushes.ConvertToSolidBrushDictionary(classnameToBytes);
            for (var i = 0; i < num; i++) {
                var uc = new DefaultUC(RetentionTimeCorrectionChart.GetDrawing_RtDiff_Each(files[i], param, rtParam, detectedStdList, label,
                    classnameToBrushes[files[i].AnalysisFileClass], (float)yMin, (float)yMax), new MouseActionSetting() { CanMouseAction = false });
                var grid = new Grid() { Height = 240, HorizontalAlignment = HorizontalAlignment.Stretch };
                uc.ContextMenu = this.RtWin.Resources["menuDefaultUC"] as ContextMenu;

                grid.Children.Add(uc);
                chartStackPanel.Children.Add(grid);
            }
            return chartStackPanel;
        }

        public StackPanel Set_EachStdPeakHeightUC(List<AnalysisFileBean> files, List<CommonStdData> detectedStdDict, ParameterBase param) {
            var chartStackPanel = new StackPanel() { Orientation = Orientation.Vertical };
            var classnameToBytes = param.ClassnameToColorBytes;
            var classnameToBrushes = ChartBrushes.ConvertToSolidBrushDictionary(classnameToBytes);
            //var brushDict = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            foreach (var std in detectedStdDict) {
                if (std.NumHit == 0) continue;
                var uc = new DefaultUC(RetentionTimeCorrectionChart.GetDrawVisual_IntensityPlot_EachCompound(files, std, classnameToBrushes), new MouseActionSetting() { FixMaxX = true, FixMinX = true });
                var grid = new Grid() { Height = 200, HorizontalAlignment = HorizontalAlignment.Stretch };
                uc.ContextMenu = this.RtWin.Resources["menuDefaultUC"] as ContextMenu;

                grid.Children.Add(uc);
                chartStackPanel.Children.Add(grid);
            }
            return chartStackPanel;
        }

        public Grid Set_EachStdEICUc(List<AnalysisFileBean> files, List<CommonStdData> commonStdList, ParameterBase param) {

            var maingrid = new Grid() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            maingrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 150 });
            maingrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 150 });

            var chartStackPanelLeft = new StackPanel() { Orientation = Orientation.Vertical };
            var chartStackPanelRight = new StackPanel() { Orientation = Orientation.Vertical };
            //var brushDict = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            var classnameToBytes = param.ClassnameToColorBytes;
            var classnameToBrushes = ChartBrushes.ConvertToSolidBrushDictionary(classnameToBytes);
            foreach (var std in commonStdList) {
                var spaceGrid = new Grid() { Height = 15 };
                var spaceGrid2 = new Grid() { Height = 15 };

                var label1 = new TextBox() {
                    Text = std.Reference.Name + ", mz: " + std.Reference.PrecursorMz,
                    Height = 20, FontFamily = new FontFamily("Helvetica"), FontSize = 15,
                    HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(7, 0, 0, -5), BorderThickness = new Thickness(0, 0, 0, 0), IsReadOnly = true
                };

                var uc = new DefaultUC(RetentionTimeCorrectionChart.GetDrawVisual_EIC_Overview(files, std, param, classnameToBrushes), new MouseActionSetting() { FixMaxX = true, FixMinX = true });
                var grid = new Grid() { MinHeight = 200, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 0) };
                grid.Children.Add(uc);
                uc.ContextMenu = this.RtWin.Resources["menuDefaultUC"] as ContextMenu;

                var label3 = new TextBox() {
                    Text = "", Height = 20, BorderThickness = new Thickness(0, 0, 0, 0), Margin = new Thickness(0, 0, 0, -5), IsReadOnly = true
                };

                var uc2 = new DefaultUC(RetentionTimeCorrectionChart.GetDrawVisual_correctedEIC_Overview(files, std, param, classnameToBrushes), new MouseActionSetting() { FixMaxX = true, FixMinX = true });
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
                excuteNext(false);

            CommonStdList = RetentionTimeCorrectionMethod.MakeCommonStdList(this.AnalysisFiles, this.RtCorrectionCommon.StandardLibrary);
            if (isAfterRtTune) {
                var NoHitCompList = CommonStdList.Where(x => x.NumHit == 0).Select(x => x.Reference).ToList();
                ShowMessage_NoHitCompounds(NoHitCompList);
            }

            //this.RtCorrectionCommon.CommonStdList = CommonStdList;

            RetentionTimeCorrectionMethod.UpdateRtCorrectionBean(this.AnalysisFiles, this.parallelOptions, this.RtCorrectionParam, CommonStdList);
            CreateSampleList();
            OnPropertyChanged("SampleListVMs");
            Update_AllViewer();

        }

        public void SettingChanged() {
            if (!Processed) { SetDemoData(); return; }
            this.RtWin.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            RetentionTimeCorrectionMethod.UpdateRtCorrectionBean(this.AnalysisFiles, this.parallelOptions, this.RtCorrectionParam, CommonStdList);
            Update_AllViewer();
            Mouse.OverrideCursor = null;
            this.RtWin.IsEnabled = true;
        }

        public void Update_AllViewer() {
            AllRtDiffUC = Set_OverviewRtDiff(this.AnalysisFiles, this.Parameter, RtCorrectionParam, CommonStdList, RtDiffLabel, ChartBrushes.SolidColorBrushList.ToList());
            StackPanel_EachRtDiffUc = Set_EachRtDiffUC(this.AnalysisFiles, this.Parameter, RtCorrectionParam, CommonStdList, RtDiffLabel);

            StackPanel_EachStdPeakHeightUc = Set_EachStdPeakHeightUC(this.AnalysisFiles, CommonStdList, this.Parameter);
            Grid_EachStdEICUc = Set_EachStdEICUc(this.AnalysisFiles, CommonStdList, this.Parameter);
        }

        public void SetDemoData() {
            if (!Processed)
                AllRtDiffUC = new DefaultUC(RetentionTimeCorrectionChart.GetDemoData(this.RtCorrectionParam, UserSettingIntercept));
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
            var w = new ShortMessageWindow() {
                Owner = this.RtWin,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Text = text
            };
            w.Width = width;
            w.Show();
        }

        private void ShowMessage_NoHitCompounds(List<MoleculeMsReference> lib) {
            if (lib.Count == 0) return;
            var text = new StringBuilder();
            text.AppendLine("No hits: " + lib.Count + " compounds");
            foreach (var l in lib) {
                text.AppendLine("id: " + l.ScanID + ", " + l.Name);
            }
            var w = new ShortMessageWindow() {
                Owner = this.RtWin,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Text = text.ToString()
            };
            w.Width = 700;
            w.Height += 15 * lib.Count;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.Show();
        }


        #endregion


    }

    public class SampleListVM {
        
        private AnalysisFileBean file;
        public SampleListVM(AnalysisFileBean f) { file = f; }
        public int SampleID { get; set; }
        public string FileName { get { return file.AnalysisFileName; } }
        public string SampleClass { get { return file.AnalysisFileClass; } }
        public int NumHit { get { return Values.Count(x => x.Rt > 0); } }
        public List<SampleListCell> Values { get; set; } = new List<SampleListCell>();

        public void StartBgChange() {
            foreach (var v in Values) v.CanBgChange = true;
        }
    }

    public class SampleListCell : ViewModelBase {
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

    public class StandardCompoundVM : ViewModelBase {
        public MoleculeMsReference TextFormatCompoundInformationBean { get; set; }
        public StandardCompoundVM() {
            TextFormatCompoundInformationBean = new MoleculeMsReference();
        }

        public StandardCompoundVM(MoleculeMsReference lib) {
            TextFormatCompoundInformationBean = lib;
        }

        public int ReferenceId {
            get { return TextFormatCompoundInformationBean.ScanID; }
            set { TextFormatCompoundInformationBean.ScanID = value; }
        }

        public string MetaboliteName {
            get { return TextFormatCompoundInformationBean.Name; }
            set {
                if (TextFormatCompoundInformationBean.Name == value) return; TextFormatCompoundInformationBean.Name = value;
                OnPropertyChanged("MetaboliteName");
            }
        }

        public double RetentionTime {
            get { return TextFormatCompoundInformationBean.ChromXs.RT.Value; }
            set {
                if (TextFormatCompoundInformationBean.ChromXs.RT.Value == value) return;
                TextFormatCompoundInformationBean.ChromXs.RT = new RetentionTime(value, TextFormatCompoundInformationBean.ChromXs.RT.Unit);
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

        public double AccurateMass {
            get { return TextFormatCompoundInformationBean.PrecursorMz; }
            set {
                if (TextFormatCompoundInformationBean.PrecursorMz == value) return; TextFormatCompoundInformationBean.PrecursorMz = value;
                OnPropertyChanged("AccurateMass");
            }
        }

        public float AccurateMassTolerance {
            get { return TextFormatCompoundInformationBean.MassTolerance; }
            set {
                if (TextFormatCompoundInformationBean.MassTolerance == value) return; TextFormatCompoundInformationBean.MassTolerance = value;
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
            get { return TextFormatCompoundInformationBean.IsTargetMolecule; }
            set {
                if (TextFormatCompoundInformationBean.IsTargetMolecule == value) return; TextFormatCompoundInformationBean.IsTargetMolecule = value;
                OnPropertyChanged("IsTarget");
            }
        }
    }
}
