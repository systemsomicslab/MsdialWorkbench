using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Helper;
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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CompMs.App.Msdial.View.Setting {
    /// <summary>
    /// Interaction logic for RetentionTimeCorrectionWinLegacy.xaml
    /// </summary>
    public partial class RetentionTimeCorrectionWinLegacy : Window {
        public RetentionTimeCorrectionViewModelLegacy VM { get; set; }
        public RetentionTimeCorrectionWinLegacy(
            IReadOnlyList<AnalysisFileBean> files, ParameterBase param,
            bool isViewMode = false) {
            InitializeComponent();

            this.VM = new RetentionTimeCorrectionViewModelLegacy(files, param, this, isViewMode);
            this.DataContext = this.VM;
        }



        #region ComboBox PropertyChanged
        private void ComboBox_Interpolation_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.VM.RtCorrectionParam.InterpolationMethod = (InterpolationMethod)((ComboBox)sender).SelectedIndex;
            this.VM.SettingChanged();
        }

        private void ComboBox_ExtrapolationBegin_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.VM.RtCorrectionParam.ExtrapolationMethodBegin = (ExtrapolationMethodBegin)((ComboBox)sender).SelectedIndex;

            if (this.VM.RtCorrectionParam.ExtrapolationMethodBegin == 0) {
                this.UserSettingValue.IsEnabled = true;
            }
            else {
                this.UserSettingValue.IsEnabled = false;
            }

            this.VM.SettingChanged();
        }

        private void ComboBox_ExtrapolationEnd_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.VM.RtCorrectionParam.ExtrapolationMethodEnd = (ExtrapolationMethodEnd)((ComboBox)sender).SelectedIndex;
            this.VM.SettingChanged();
        }

        private void ComboBox_RtDiffCalcMethod_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            this.VM.RtCorrectionParam.RtDiffCalcMethod = (RtDiffCalcMethod)((ComboBox)sender).SelectedIndex;
            this.VM.SettingChanged();
        }

        private void ComboBox_RtDiff_Label_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var index = ((ComboBox)sender).SelectedIndex;
            this.VM.RtDiffLabel = (RtDiffLabel)index;
            this.VM.SettingChanged();
        }

        #endregion

        #region Paste to Standard table DataGrid_PreviewKeyDown

        private void pasteText(List<string[]> clipTextList, int displayIndex, int startRow, int startColumn = 1) {
            float floatVal;
            for (int i = 0; i < clipTextList.Count; i++) {
                if (startRow + i > this.DataGrid_StdData.Items.Count - 1) break;
                int counter = startColumn - displayIndex;

                if (counter >= 0) {
                    if (clipTextList[i].Length > counter) {
                        ((StandardCompoundVM)this.DataGrid_StdData.Items[startRow + i]).MetaboliteName = clipTextList[i][counter];
                    }
                }
                counter++;

                if (counter >= 0) {
                    if (clipTextList[i].Length > counter) {
                        if (float.TryParse(clipTextList[i][counter], out floatVal))
                            ((StandardCompoundVM)this.DataGrid_StdData.Items[startRow + i]).RetentionTime = floatVal;
                    }
                }
                counter++;

                if (counter >= 0) {
                    if (clipTextList[i].Length > counter) {
                        if (float.TryParse(clipTextList[i][counter], out floatVal)) {
                            ((StandardCompoundVM)this.DataGrid_StdData.Items[startRow + i]).RetentionTimeTolerance = floatVal;
                        }
                    }
                }
                counter++;

                if (counter >= 0) {
                    if (clipTextList[i].Length > counter) {
                        if (float.TryParse(clipTextList[i][counter], out floatVal)) {
                            ((StandardCompoundVM)this.DataGrid_StdData.Items[startRow + i]).AccurateMass = floatVal;
                        }
                    }
                }
                counter++;

                if (counter >= 0) {
                    if (clipTextList[i].Length > counter) {
                        if (float.TryParse(clipTextList[i][counter], out floatVal)) {
                            ((StandardCompoundVM)this.DataGrid_StdData.Items[startRow + i]).AccurateMassTolerance = floatVal;
                        }
                    }
                }
                counter++;

                if (counter >= 0) {
                    if (clipTextList[i].Length > counter) {
                        if (float.TryParse(clipTextList[i][counter], out floatVal)) {
                            ((StandardCompoundVM)this.DataGrid_StdData.Items[startRow + i]).MinimumPeakHeight = floatVal;
                        }
                    }
                }
            }
        }

        private void Datagrid_StandardTable_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.V & Keyboard.Modifiers == ModifierKeys.Control) {
                e.Handled = true;

                string[] clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
                List<string[]> clipTextList = new List<string[]>();
                for (int i = 0; i < clipText.Length; i++) { clipTextList.Add(clipText[i].Split('\t')); }
                if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

                int startRow = this.DataGrid_StdData.Items.IndexOf(this.DataGrid_StdData.SelectedCells[0].Item);
                pasteText(clipTextList, this.DataGrid_StdData.SelectedCells[0].Column.DisplayIndex, startRow, 1);
            }
        }
        #endregion

        private void Datagrid_StdData_CurrentCellChanged(object sender, EventArgs e) {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.DataGrid_StdData.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.DataGrid_StdData.BeginEdit();
        }

        private void Datagrid_SampleData_CurrentCellChanged(object sender, EventArgs e) {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.DataGrid_SampleTable.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.DataGrid_SampleTable.BeginEdit();
        }

        private void DataGrid_GotFocus(object sender, RoutedEventArgs e) {
            // Lookup for the source to be DataGridCell
            if (e.OriginalSource.GetType() == typeof(DataGridCell)) {
                // Starts the Edit on the row;
                DataGrid grd = (DataGrid)sender;
                grd.BeginEdit(e);
            }
        }

        #region ContextMenu
        private void contextMenu_SaveImageAs_Click(object sender, RoutedEventArgs e) {
            var target = ((ContextMenu)((MenuItem)e.Source).Parent).PlacementTarget;

            SaveImageAsWinLegacy window = new SaveImageAsWinLegacy(target);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_CopyImageAs_Click(object sender, RoutedEventArgs e) {
            var target = ((ContextMenu)((MenuItem)e.Source).Parent).PlacementTarget;

            CopyImageAsWinLegacy window = new CopyImageAsWinLegacy(target);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }



        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (this.DialogResult == true) return;

            var result = MessageBox.Show("Continue to process your project after closed?", "Question", MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Yes) {
                this.DialogResult = true;
            }
            else if (result == MessageBoxResult.No) {
                this.DialogResult = false;
            }
            else if (result == MessageBoxResult.Cancel) {
                e.Cancel = true;
                return;
            }
        }

        private void TabControl_CorrectedRes_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sender == null || (TabControl)sender == null) return;
            var index = ((TabControl)sender).SelectedIndex;

            switch (index) {
                case 0:
                    this.ComboBox_RtDiff_Label.IsEnabled = true;
                    break;
                case 1:
                    this.ComboBox_RtDiff_Label.IsEnabled = true;
                    break;
                case 2:
                    this.ComboBox_RtDiff_Label.IsEnabled = false;
                    break;
                case 3:
                    this.ComboBox_RtDiff_Label.IsEnabled = false;
                    break;
                default:
                    this.ComboBox_RtDiff_Label.IsEnabled = true;
                    break;
            }
        }
    }
}
