using Riken.Metabolomics.Common.Query;
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

namespace Rfx.Riken.OsakaUniv {
    /// <summary>
    /// Interaction logic for FragmentQuerySetWin.xaml
    /// </summary>
    public partial class FragmentQuerySetWin : Window {
        public FragmentQuerySetWin() {
            InitializeComponent();
        }

        private void Datagrid_FragmentQuery_CurrentCellChanged(object sender, EventArgs e) {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.Datagrid_FragmentQueries.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.Datagrid_FragmentQueries.BeginEdit();
        }

        private void Datagrid_FragmentQuery_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.V & Keyboard.Modifiers == ModifierKeys.Control) {
                e.Handled = true;
                string[] clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
                List<string[]> clipTextList = new List<string[]>();
                for (int i = 0; i < clipText.Length; i++) { clipTextList.Add(clipText[i].Split('\t')); }
                if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

                if (clipTextList.Count > 1 && this.Datagrid_FragmentQueries.SelectedCells[0].Column.DisplayIndex == 0) {
                    int startRow = this.Datagrid_FragmentQueries.Items.IndexOf(this.Datagrid_FragmentQueries.SelectedCells[0].Item);

                    for (int i = 0; i < clipTextList.Count; i++) {
                        if (startRow + i > this.Datagrid_FragmentQueries.Items.Count - 1) break;

                        double doubleValue;
                        if (double.TryParse(clipTextList[i][0], out doubleValue))
                            ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).Mass = doubleValue;

                        if (clipTextList[i].Length > 1) {
                            if (double.TryParse(clipTextList[i][1], out doubleValue)) {
                                ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).MassTolerance = doubleValue;
                            }
                        }

                        if (clipTextList[i].Length > 2) {
                            if (double.TryParse(clipTextList[i][2], out doubleValue)) {
                                ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).RelativeIntensity = doubleValue;
                            }
                        }

                        if (clipTextList[i].Length > 3) {

                            var searchType = clipTextList[i][3];
                            if (searchType.Contains("Product")) {
                                ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).SearchType = SearchType.ProductIon;
                            }
                            else {
                                ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).SearchType = SearchType.NeutralLoss;
                            }

                        }
                    }
                    this.Datagrid_FragmentQueries.UpdateLayout();
                }
                else if (clipTextList.Count > 1 && this.Datagrid_FragmentQueries.SelectedCells[0].Column.DisplayIndex == 1) {
                    int startRow = this.Datagrid_FragmentQueries.Items.IndexOf(this.Datagrid_FragmentQueries.SelectedCells[0].Item);
                    double doubleValue;
                    for (int i = 0; i < clipTextList.Count; i++) {
                        if (startRow + i > this.Datagrid_FragmentQueries.Items.Count - 1) break;
                        if (double.TryParse(clipTextList[i][0], out doubleValue)) {
                            ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).MassTolerance = doubleValue;
                        }

                        if (clipTextList[i].Length > 1) {
                            if (double.TryParse(clipTextList[i][1], out doubleValue)) {
                                ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).RelativeIntensity = doubleValue;
                            }
                        }

                        if (clipTextList[i].Length > 2) {

                            var searchType = clipTextList[i][2];
                            if (searchType.Contains("Product")) {
                                ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).SearchType = SearchType.ProductIon;
                            }
                            else {
                                ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).SearchType = SearchType.NeutralLoss;
                            }
                        }
                    }
                    this.Datagrid_FragmentQueries.UpdateLayout();
                }
                else if (clipTextList.Count > 1 && this.Datagrid_FragmentQueries.SelectedCells[0].Column.DisplayIndex == 2) {
                    int startRow = this.Datagrid_FragmentQueries.Items.IndexOf(this.Datagrid_FragmentQueries.SelectedCells[0].Item);
                    double doubleValue;
                    for (int i = 0; i < clipTextList.Count; i++) {
                        if (startRow + i > this.Datagrid_FragmentQueries.Items.Count - 1) break;

                        if (double.TryParse(clipTextList[i][0], out doubleValue)) {
                            ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).MassTolerance = doubleValue;
                        }

                        if (clipTextList[i].Length > 1) {

                            var searchType = clipTextList[i][1];
                            if (searchType.Contains("Product")) {
                                ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).SearchType = SearchType.ProductIon;
                            }
                            else {
                                ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).SearchType = SearchType.NeutralLoss;
                            }
                        }
                    }
                    this.Datagrid_FragmentQueries.UpdateLayout();
                }
                else if (clipTextList.Count > 1 && this.Datagrid_FragmentQueries.SelectedCells[0].Column.DisplayIndex == 3) {
                    int startRow = this.Datagrid_FragmentQueries.Items.IndexOf(this.Datagrid_FragmentQueries.SelectedCells[0].Item);
                    for (int i = 0; i < clipTextList.Count; i++) {
                        if (startRow + i > this.Datagrid_FragmentQueries.Items.Count - 1) break;

                        var searchType = clipTextList[i][0];
                        if (searchType.Contains("Product")) {
                            ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).SearchType = SearchType.ProductIon;
                        }
                        else {
                            ((FragmentSearcherQuery)this.Datagrid_FragmentQueries.Items[startRow + i]).SearchType = SearchType.NeutralLoss;
                        }
                    }
                    this.Datagrid_FragmentQueries.UpdateLayout();
                }
            }
        }
    }
}
