using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// ExtractedIonChromatogramDisplaySettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ExtractedIonChromatogramDisplaySetWin : Window
    {
        private MainWindow mainWindow;

        public ExtractedIonChromatogramDisplaySetWin(MainWindow mainWindow)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new ExtractedIonChromatogramDisplaySetVM(this.mainWindow, this);
        }
        
        private void Datagrid_EicProperty_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V & Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                string[] clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
                List<string[]> clipTextList = new List<string[]>();
                for (int i = 0; i < clipText.Length; i++) { clipTextList.Add(clipText[i].Split('\t')); }
                if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

                if (clipTextList.Count > 1 && this.Datagrid_EicProperty.SelectedCells[0].Column.DisplayIndex == 0)
                {
                    int startRow = this.Datagrid_EicProperty.Items.IndexOf(this.Datagrid_EicProperty.SelectedCells[0].Item);
                    double exactMass, massTolerance;
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_EicProperty.Items.Count - 1) break;
                        ((EicDisplayQueryVM)this.Datagrid_EicProperty.Items[startRow + i]).EicName = clipTextList[i][0];
                        if (clipTextList[i].Length > 1)
                        {
                            if (double.TryParse(clipTextList[i][1], out exactMass))
                            {
                                ((EicDisplayQueryVM)this.Datagrid_EicProperty.Items[startRow + i]).ExactMass = exactMass;
                            }
                        }
                        if (clipTextList[i].Length > 2)
                        {
                            if (double.TryParse(clipTextList[i][2], out massTolerance))
                            {
                                ((EicDisplayQueryVM)this.Datagrid_EicProperty.Items[startRow + i]).MassTolerance = massTolerance;
                            }
                        }
                    }
                    this.Datagrid_EicProperty.UpdateLayout();
                }
                else if (clipTextList.Count > 1 && this.Datagrid_EicProperty.SelectedCells[0].Column.DisplayIndex == 1)
                {
                    int startRow = this.Datagrid_EicProperty.Items.IndexOf(this.Datagrid_EicProperty.SelectedCells[0].Item);
                    double exactMass, massTolerance;
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_EicProperty.Items.Count - 1) break;
                        if (double.TryParse(clipTextList[i][0], out exactMass))
                        {
                            ((EicDisplayQueryVM)this.Datagrid_EicProperty.Items[startRow + i]).ExactMass = exactMass;
                        }
                        if (clipTextList[i].Length > 1)
                        {
                            if (double.TryParse(clipTextList[i][1], out massTolerance))
                            {
                                ((EicDisplayQueryVM)this.Datagrid_EicProperty.Items[startRow + i]).MassTolerance = massTolerance;
                            }
                        }
                    }
                    this.Datagrid_EicProperty.UpdateLayout();
                }
                else if (clipTextList.Count > 1 && this.Datagrid_EicProperty.SelectedCells[0].Column.DisplayIndex == 2)
                {
                    int startRow = this.Datagrid_EicProperty.Items.IndexOf(this.Datagrid_EicProperty.SelectedCells[0].Item);
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_EicProperty.Items.Count - 1) break;
                        double d;
                        if (double.TryParse(clipTextList[i][0], out d))
                            ((EicDisplayQueryVM)this.Datagrid_EicProperty.Items[startRow + i]).MassTolerance = d;
                    }
                    this.Datagrid_EicProperty.UpdateLayout();
                }
                else if (clipTextList.Count == 1 && this.Datagrid_EicProperty.SelectedCells[0].Column.DisplayIndex == 2)
                {
                    int startRow = this.Datagrid_EicProperty.Items.IndexOf(this.Datagrid_EicProperty.SelectedCells[0].Item);
                    for (int i = 0; i < this.Datagrid_EicProperty.SelectedCells.Count; i++)
                    {
                        if (this.Datagrid_EicProperty.SelectedCells[i].Column.DisplayIndex != 2) continue;
                        if (startRow + i > this.Datagrid_EicProperty.Items.Count - 1) break;
                        double d;
                        if (double.TryParse(clipTextList[0][0], out d))
                            ((EicDisplayQueryVM)this.Datagrid_EicProperty.Items[startRow + i]).MassTolerance = d;
                    }
                    this.Datagrid_EicProperty.UpdateLayout();
                }
            }
        }

        private void Button_Delete_Click(object sender, RoutedEventArgs e)
        {
            var vm = (ExtractedIonChromatogramDisplaySetVM)DataContext;
            for (int i = 0; i < vm.EicDisplaySettingBeanCollection.Count; i++)
            {
                var query = vm.EicDisplaySettingBeanCollection[i];
                query.EicName = null;
                query.ExactMass = null;
                query.MassTolerance = null;
            }
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Datagrid_EicProperty_CurrentCellChanged(object sender, EventArgs e)
        {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.Datagrid_EicProperty.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.Datagrid_EicProperty.BeginEdit();
        }
    }
}
