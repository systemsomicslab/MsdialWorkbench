using System;
using System.Collections.Generic;
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
    /// FilePropertySettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class FilePropertySetWin : Window
    {
        private MainWindow mainWindow;

        public FilePropertySetWin(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new FilePropertySettingVM(this.mainWindow, this);
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void datagrid_FileProperty_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V & Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                
                var clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
                var clipTextList = new List<string[]>();

                foreach (var clip in clipText) { clipTextList.Add(clip.Split('\t')); }
                
                if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

                var startRow = this.Datagrid_FileProperty.Items.IndexOf(this.Datagrid_FileProperty.SelectedCells[0].Item);
                int intValue;
                double doubleValue;
                if (clipTextList.Count > 1 && this.Datagrid_FileProperty.SelectedCells[0].Column.DisplayIndex == 0)
                {
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_FileProperty.Items.Count - 1) break;

                        if (clipTextList[i].Length > 1)
                        {
                            var fileType = clipTextList[i][1];
                            switch (fileType)
                            {
                                case "Sample":
                                    ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Sample;
                                    break;
                                case "Standard":
                                    ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Standard;
                                    break;
                                case "QC":
                                    ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileType = AnalysisFileType.QC;
                                    break;
                                case "Blank":
                                    ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Blank;
                                    break;
                                default:
                                    ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Sample;
                                    break;
                            }
                        }

                        if (clipTextList[i].Length > 2)
                        {
                            ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileClass = clipTextList[i][2];
                        }

                        if (clipTextList[i].Length > 3)
                        {
                            if (int.TryParse(clipTextList[i][3], out intValue))
                            {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisBatch = intValue;
                            }
                        }

                        if (clipTextList[i].Length > 4) {
                            if (int.TryParse(clipTextList[i][4], out intValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileAnalyticalOrder = intValue;
                            }
                        }

                        if (clipTextList[i].Length > 5) {
                            if (double.TryParse(clipTextList[i][5], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).InjectionVolume = doubleValue;
                            }
                        }

                        if (clipTextList[i].Length > 6) {
                            if (double.TryParse(clipTextList[i][5], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).ResponseVariable = doubleValue;
                            }
                        }
                    }
                }
                else if (clipTextList.Count > 1 && this.Datagrid_FileProperty.SelectedCells[0].Column.DisplayIndex == 1)
                {
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_FileProperty.Items.Count - 1) break;

                        var fileType = clipTextList[i][0];
                        switch (fileType)
                        {
                            case "Sample":
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Sample;
                                break;
                            case "Standard":
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Standard;
                                break;
                            case "QC":
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileType = AnalysisFileType.QC;
                                break;
                            case "Blank":
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Blank;
                                break;
                            default:
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Sample;
                                break;
                        }

                        if (clipTextList[i].Length > 1)
                        {
                            ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileClass = clipTextList[i][1];
                        }

                        if (clipTextList[i].Length > 2) {
                            if (int.TryParse(clipTextList[i][2], out intValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisBatch = intValue;
                            }
                        }

                        if (clipTextList[i].Length > 3) {
                            if (int.TryParse(clipTextList[i][3], out intValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileAnalyticalOrder = intValue;
                            }
                        }

                        if (clipTextList[i].Length > 4) {
                            if (double.TryParse(clipTextList[i][4], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).InjectionVolume = doubleValue;
                            }
                        }

                        if (clipTextList[i].Length > 5) {
                            if (double.TryParse(clipTextList[i][4], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).ResponseVariable = doubleValue;
                            }
                        }
                    }
                }
                else if (clipTextList.Count > 1 && this.Datagrid_FileProperty.SelectedCells[0].Column.DisplayIndex == 2)
                {
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_FileProperty.Items.Count - 1) break;
                        
                        ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileClass = clipTextList[i][0];
                        
                        if (clipTextList[i].Length > 1) {
                            if (int.TryParse(clipTextList[i][1], out intValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisBatch = intValue;
                            }
                        }

                        if (clipTextList[i].Length > 2) {
                            if (int.TryParse(clipTextList[i][2], out intValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileAnalyticalOrder = intValue;
                            }
                        }

                        if (clipTextList[i].Length > 3) {
                            if (double.TryParse(clipTextList[i][3], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).InjectionVolume = doubleValue;
                            }
                        }

                        if (clipTextList[i].Length > 4) {
                            if (double.TryParse(clipTextList[i][3], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).ResponseVariable = doubleValue;
                            }
                        }
                    }
                }
                else if (clipTextList.Count > 1 && this.Datagrid_FileProperty.SelectedCells[0].Column.DisplayIndex == 3)
                {
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_FileProperty.Items.Count - 1) break;

                        if (int.TryParse(clipTextList[i][0], out intValue)) {
                            ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisBatch = intValue;
                        }

                        if (clipTextList[i].Length > 1) {
                            if (int.TryParse(clipTextList[i][1], out intValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileAnalyticalOrder = intValue;
                            }
                        }

                        if (clipTextList[i].Length > 2) {
                            if (double.TryParse(clipTextList[i][2], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).InjectionVolume = doubleValue;
                            }
                        }

                        if (clipTextList[i].Length > 3) {
                            if (double.TryParse(clipTextList[i][2], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).ResponseVariable = doubleValue;
                            }
                        }
                    }
                }
                else if (clipTextList.Count > 1 && this.Datagrid_FileProperty.SelectedCells[0].Column.DisplayIndex == 4) {
                    for (int i = 0; i < clipTextList.Count; i++) {
                        if (startRow + i > this.Datagrid_FileProperty.Items.Count - 1) break;

                        if (int.TryParse(clipTextList[i][0], out intValue)) {
                            ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileAnalyticalOrder = intValue;
                        }

                        if (clipTextList[i].Length > 1) {
                            if (double.TryParse(clipTextList[i][1], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).InjectionVolume = doubleValue;
                            }
                        }

                        if (clipTextList[i].Length > 2) {
                            if (double.TryParse(clipTextList[i][1], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).ResponseVariable = doubleValue;
                            }
                        }
                    }
                }
                else if (clipTextList.Count > 1 && this.Datagrid_FileProperty.SelectedCells[0].Column.DisplayIndex == 5) {
                    for (int i = 0; i < clipTextList.Count; i++) {
                        if (startRow + i > this.Datagrid_FileProperty.Items.Count - 1) break;

                        if (double.TryParse(clipTextList[i][0], out doubleValue)) {
                            ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).InjectionVolume = doubleValue;
                        }

                        if (clipTextList[i].Length > 1) {
                            if (double.TryParse(clipTextList[i][1], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).ResponseVariable = doubleValue;
                            }
                        }
                    }
                }

                else if (clipTextList.Count > 1 && this.Datagrid_FileProperty.SelectedCells[0].Column.DisplayIndex == 6) {
                    for (int i = 0; i < clipTextList.Count; i++) {
                        if (startRow + i > this.Datagrid_FileProperty.Items.Count - 1) break;

                        if (double.TryParse(clipTextList[i][0], out doubleValue)) {
                            ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).ResponseVariable = doubleValue;
                        }
                    }
                }

                else if (clipTextList.Count == 1 && this.Datagrid_FileProperty.SelectedCells[0].Column.DisplayIndex == 2)
                {
                    for (int i = 0; i < this.Datagrid_FileProperty.SelectedCells.Count; i++)
                    {
                        if (this.Datagrid_FileProperty.SelectedCells[i].Column.DisplayIndex != 2) continue;
                        
                        if (startRow + i > this.Datagrid_FileProperty.Items.Count - 1) break;
                        
                        ((AnalysisFilePropertyVM)this.Datagrid_FileProperty.Items[startRow + i]).AnalysisFileClass = clipTextList[0][0];
                    }
                }

                this.Datagrid_FileProperty.UpdateLayout();

            }
        }

        private void Datagrid_FileProperty_CurrentCellChanged(object sender, EventArgs e)
        {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.Datagrid_FileProperty.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.Datagrid_FileProperty.BeginEdit();
        }

        private void MenuItem_AutoFill_Click(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_FileProperty;
            var currentItem = (AnalysisFilePropertyVM)grid.CurrentItem;
            var currentID = grid.Items.IndexOf(currentItem);

            for (int i = currentID; i < grid.Items.Count; i++) {
                var propVM = (AnalysisFilePropertyVM)grid.Items[i];
                propVM.InjectionVolume = currentItem.InjectionVolume;
            }

            this.Datagrid_FileProperty.CommitEdit();
            this.Datagrid_FileProperty.CommitEdit();
            this.Datagrid_FileProperty.Items.Refresh();
        }

        private void MenuItem_CopyToClipboard(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_FileProperty;
            var currentItem = (AnalysisFilePropertyVM)grid.CurrentItem;

            Clipboard.SetText(currentItem.InjectionVolume.ToString());
        }

        private void MenuItem_Paste(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_FileProperty;
            var currentItem = (AnalysisFilePropertyVM)grid.CurrentItem;

            e.Handled = true;
            var clipboardArrayList = getClipboardContentAsStringArrayList();
            if (clipboardArrayList == null || clipboardArrayList.Count == 0 || clipboardArrayList[0].Length == 0) return;

            double volume = 0;
            if (double.TryParse(clipboardArrayList[0][0], out volume)) {
                currentItem.InjectionVolume = volume;
            }
        }

        private void MenuItem_Responses_AutoFill_Click(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_FileProperty;
            var currentItem = (AnalysisFilePropertyVM)grid.CurrentItem;
            var currentID = grid.Items.IndexOf(currentItem);

            for (int i = currentID; i < grid.Items.Count; i++) {
                var propVM = (AnalysisFilePropertyVM)grid.Items[i];
                propVM.ResponseVariable = currentItem.ResponseVariable;
            }

            this.Datagrid_FileProperty.CommitEdit();
            this.Datagrid_FileProperty.CommitEdit();
            this.Datagrid_FileProperty.Items.Refresh();
        }

        private void MenuItem_Responses_CopyToClipboard(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_FileProperty;
            var currentItem = (AnalysisFilePropertyVM)grid.CurrentItem;

            Clipboard.SetText(currentItem.ResponseVariable.ToString());
        }

        private void MenuItem_Responses_Paste(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_FileProperty;
            var currentItem = (AnalysisFilePropertyVM)grid.CurrentItem;

            e.Handled = true;
            var clipboardArrayList = getClipboardContentAsStringArrayList();
            if (clipboardArrayList == null || clipboardArrayList.Count == 0 || clipboardArrayList[0].Length == 0) return;

            double volume = 0;
            if (double.TryParse(clipboardArrayList[0][0], out volume)) {
                currentItem.ResponseVariable = volume;
            }
        }

        private List<string[]> getClipboardContentAsStringArrayList() {
            var clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
            var clipTextList = new List<string[]>();
            for (int i = 0; i < clipText.Length; i++) { clipTextList.Add(clipText[i].Split('\t')); }
            if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

            return clipTextList;
        }

    }
}
