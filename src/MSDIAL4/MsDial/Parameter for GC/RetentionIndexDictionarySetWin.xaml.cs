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
    /// Interaction logic for RetentionIndexDictionarySetWin.xaml
    /// </summary>
    public partial class RetentionIndexDictionarySetWin : Window
    {
        public RetentionIndexDictionarySetWin()
        {
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        }

        private void MenuItem_AutoFill_Click(object sender, RoutedEventArgs e)
        {
            var grid = this.Datagrid_AnalysisFiles;
            var currentItem = (AnalysisFilePropertyVM)grid.CurrentItem;
            var currentID = grid.Items.IndexOf(currentItem);

            for (int i = currentID; i < grid.Items.Count; i++) {
                var propVM = (AnalysisFilePropertyVM)grid.Items[i];
                propVM.RiDictionaryFilePath = currentItem.RiDictionaryFilePath;
            }
        }

        private void MenuItem_CopyToClipboard(object sender, RoutedEventArgs e)
        {
            var grid = this.Datagrid_AnalysisFiles;
            var currentItem = (AnalysisFilePropertyVM)grid.CurrentItem;

            Clipboard.SetText(currentItem.RiDictionaryFilePath);
        }

        private void MenuItem_Paste(object sender, RoutedEventArgs e)
        {
            var grid = this.Datagrid_AnalysisFiles;
            var currentItem = (AnalysisFilePropertyVM)grid.CurrentItem;

            e.Handled = true;
            var clipboardArrayList = getClipboardContentAsStringArrayList();
            if (clipboardArrayList == null || clipboardArrayList.Count == 0 || clipboardArrayList[0].Length == 0) return;

            currentItem.RiDictionaryFilePath = clipboardArrayList[0][0];
        }

        private void datagrid_AnalysisFiles_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control) {
                e.Handled = true;

                var clipboardArrayList = getClipboardContentAsStringArrayList();
                var grid = this.Datagrid_AnalysisFiles;

                if (grid.SelectedCells.Count == 1 && grid.SelectedCells[0].Column.DisplayIndex == 2) {
                    
                    var currentItem = grid.SelectedCells[0].Item;
                    var startRow = grid.Items.IndexOf(currentItem);

                    for (int i = 0; i < clipboardArrayList.Count; i++) {
                        if (startRow + i > grid.Items.Count - 1) break;
                        ((AnalysisFilePropertyVM)grid.Items[startRow + i]).RiDictionaryFilePath = clipboardArrayList[i][0];
                    }
                    
                }
                else if (grid.SelectedCells.Count > 1 && grid.SelectedCells[0].Column.DisplayIndex == 2) {

                    var currentItem = grid.SelectedCells[0].Item;
                    var startRow = grid.Items.IndexOf(currentItem);
                    
                    for (int i = 0; i < grid.SelectedCells.Count; i++) {
                        if (grid.SelectedCells[i].Column.DisplayIndex != 2) continue;
                        if (startRow + i > grid.Items.Count - 1) break;

                        ((AnalysisFilePropertyVM)grid.Items[startRow + i]).RiDictionaryFilePath = clipboardArrayList[0][0];
                    }
                }
            }
        }

        private List<string[]> getClipboardContentAsStringArrayList()
        {
            var clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
            var clipTextList = new List<string[]>();
            for (int i = 0; i < clipText.Length; i++) { clipTextList.Add(clipText[i].Split('\t')); }
            if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

            return clipTextList;
        }
    }
}
