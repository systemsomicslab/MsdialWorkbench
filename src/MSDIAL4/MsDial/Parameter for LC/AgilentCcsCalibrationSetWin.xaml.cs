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

namespace Rfx.Riken.OsakaUniv {
    /// <summary>
    /// Interaction logic for AgilentCcsCalibrationSetWin.xaml
    /// </summary>
    public partial class AgilentCcsCalibrationSetWin : Window {
        public AgilentCcsCalibrationSetWin() {
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        }


        private void MenuItem_AutoFill_Click(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_AnalysisFiles;
            var currentItem = (CcsCalibrationInfoVS)grid.CurrentItem;
            var currentID = grid.Items.IndexOf(currentItem);
            var selectedCol = grid.SelectedCells[0].Column.DisplayIndex;

            for (int i = currentID; i < grid.Items.Count; i++) {
                var propVM = (CcsCalibrationInfoVS)grid.Items[i];
                if (selectedCol == 1) {
                    propVM.AgilentBeta = currentItem.AgilentBeta;
                } else if (selectedCol == 2) {
                    propVM.AgilentTFix = currentItem.AgilentTFix;
                }
            }
        }

        private void MenuItem_CopyToClipboard(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_AnalysisFiles;
            var currentItem = (CcsCalibrationInfoVS)grid.CurrentItem;
            var selectedCol = grid.SelectedCells[0].Column.DisplayIndex;
            if (selectedCol == 1) {
                Clipboard.SetText(currentItem.AgilentBeta.ToString());
            }
            else if (selectedCol == 2) {
                Clipboard.SetText(currentItem.AgilentTFix.ToString());
            }
        }

        private void MenuItem_Paste(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_AnalysisFiles;
            var currentItem = (CcsCalibrationInfoVS)grid.CurrentItem;
            var selectedCol = grid.SelectedCells[0].Column.DisplayIndex;
            e.Handled = true;
            var clipboardArrayList = getClipboardContentAsStringArrayList();
            if (clipboardArrayList == null || clipboardArrayList.Count == 0 || clipboardArrayList[0].Length == 0) return;

            double doubleValue;
            if (double.TryParse(clipboardArrayList[0][0], out doubleValue)) {
                if (selectedCol == 1) {
                    currentItem.AgilentBeta = doubleValue;
                }
                else if (selectedCol == 2) {
                    currentItem.AgilentTFix = doubleValue;
                }
            }
        }

        private void datagrid_AnalysisFiles_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control) {
                e.Handled = true;

                var clipboardArrayList = getClipboardContentAsStringArrayList();
                if (clipboardArrayList.Count == 0) return;
                var grid = this.Datagrid_AnalysisFiles;
                var currentItem = grid.SelectedCells[0].Item;
                var startRow = grid.Items.IndexOf(currentItem);

                if (grid.SelectedCells[0].Column.DisplayIndex == 1) {
                    for (int i = 0; i < clipboardArrayList.Count; i++) {
                        if (startRow + i > grid.Items.Count - 1) break;
                        double doubleValue;
                        if (double.TryParse(clipboardArrayList[i][0], out doubleValue))
                            ((CcsCalibrationInfoVS)grid.Items[startRow + i]).AgilentBeta = doubleValue;

                        if (clipboardArrayList[i].Length > 1) {
                            if (double.TryParse(clipboardArrayList[i][1], out doubleValue))
                                ((CcsCalibrationInfoVS)grid.Items[startRow + i]).AgilentTFix = doubleValue;
                        }
                    }
                } else if (grid.SelectedCells[0].Column.DisplayIndex == 2) {
                    for (int i = 0; i < clipboardArrayList.Count; i++) {
                        if (startRow + i > grid.Items.Count - 1) break;
                        double doubleValue;
                        if (double.TryParse(clipboardArrayList[i][0], out doubleValue))
                            ((CcsCalibrationInfoVS)grid.Items[startRow + i]).AgilentTFix = doubleValue;
                    }
                }
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
