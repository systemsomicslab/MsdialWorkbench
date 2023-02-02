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
    /// NormalizationPropertySettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LcmsNormalizationPropertySetWin : Window
    {
        private MainWindow mainWindow;

        public LcmsNormalizationPropertySetWin(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            if (this.mainWindow.AnalysisParamForLC.IsIonMobility) {
                changeColumnForIonMobility();
            }
        }

        private void changeColumnForIonMobility() {
            var dg = this.Datagrid_AlignmentProperty;
            // add mobility
            var factory = new FrameworkElementFactory(typeof(TextBox));
            factory.SetBinding(TextBox.TextProperty, new Binding("Mobility") { StringFormat = "0.000" });
            factory.SetValue(TextBox.IsReadOnlyProperty, true);
            var mobilityColumn = new DataGridTemplateColumn() {
                SortMemberPath = "Mobility", Width = new DataGridLength(1.0, DataGridLengthUnitType.Star),
                IsReadOnly = false, Header = "Mobility",
                CellTemplate = new DataTemplate() {
                    VisualTree = factory
                }
            };


            dg.Columns.Insert(3, mobilityColumn);
            dg.UpdateLayout();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new LcmsNormalizationPropertySetVM(this.mainWindow, this);
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MenuItem_AutoFill_Click(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_AlignmentProperty;
            var currentItem = (LcmsNormalizationProperty)grid.CurrentItem;
            var currentID = grid.Items.IndexOf(currentItem);

            for (int i = currentID; i < grid.Items.Count; i++) {
                var propVM = (LcmsNormalizationProperty)grid.Items[i];
                propVM.InternalStandardID = currentItem.InternalStandardID;
            }

            this.Datagrid_AlignmentProperty.CommitEdit();
            this.Datagrid_AlignmentProperty.CommitEdit();
            this.Datagrid_AlignmentProperty.Items.Refresh();
        }

        private void MenuItem_CopyToClipboard(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_AlignmentProperty;
            var currentItem = (LcmsNormalizationProperty)grid.CurrentItem;

            Clipboard.SetText(currentItem.InternalStandardID.ToString());
        }

        private void MenuItem_Paste(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_AlignmentProperty;
            var currentItem = (LcmsNormalizationProperty)grid.CurrentItem;

            e.Handled = true;
            var clipboardArrayList = getClipboardContentAsStringArrayList();
            if (clipboardArrayList == null || clipboardArrayList.Count == 0 || clipboardArrayList[0].Length == 0) return;

            int isid = 0;
            if(int.TryParse(clipboardArrayList[0][0], out isid)) {
                currentItem.InternalStandardID = isid;
            }
        }

        private List<string[]> getClipboardContentAsStringArrayList() {
            var clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
            var clipTextList = new List<string[]>();
            for (int i = 0; i < clipText.Length; i++) { clipTextList.Add(clipText[i].Split('\t')); }
            if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

            return clipTextList;
        }

        private void Datagrid_AlignmentProperty_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            var isMobility = this.mainWindow.AnalysisParamForLC.IsIonMobility;
            var columnIndex = isMobility ? 6 : 5;


            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                int id;
                string[] clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
                List<string[]> clipTextList = new List<string[]>();
                for (int i = 0; i < clipText.Length; i++) { clipTextList.Add(clipText[i].Split('\t')); }
                if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

                if (this.Datagrid_AlignmentProperty.SelectedCells.Count == 1 && this.Datagrid_AlignmentProperty.SelectedCells[0].Column.DisplayIndex == columnIndex)
                {
                    int startRow = this.Datagrid_AlignmentProperty.Items.IndexOf(this.Datagrid_AlignmentProperty.SelectedCells[0].Item);
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_AlignmentProperty.Items.Count - 1) break;
                        if (int.TryParse(clipTextList[i][0], out id))
                            ((LcmsNormalizationProperty)this.Datagrid_AlignmentProperty.Items[startRow + i]).InternalStandardID = id;
                    }
                    this.Datagrid_AlignmentProperty.UpdateLayout();
                }
                else if (this.Datagrid_AlignmentProperty.SelectedCells[0].Column.DisplayIndex == columnIndex)
                {
                    int startRow = this.Datagrid_AlignmentProperty.Items.IndexOf(this.Datagrid_AlignmentProperty.SelectedCells[0].Item);
                    for (int i = 0; i < this.Datagrid_AlignmentProperty.SelectedCells.Count; i++)
                    {
                        if (this.Datagrid_AlignmentProperty.SelectedCells[i].Column.DisplayIndex != columnIndex) continue;
                        if (startRow + i > this.Datagrid_AlignmentProperty.Items.Count - 1) break;
                        if (int.TryParse(clipTextList[0][0], out id))
                            ((LcmsNormalizationProperty)this.Datagrid_AlignmentProperty.Items[startRow + i]).InternalStandardID = id;
                    }
                }
            }
        }

        private void Datagrid_AlignmentProperty_CurrentCellChanged(object sender, EventArgs e)
        {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.Datagrid_AlignmentProperty.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.Datagrid_AlignmentProperty.BeginEdit();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.mainWindow.WindowOpened = false;
        }
    }
}
