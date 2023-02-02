using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv.ForAIF
{
    class MsFinderMultipleExporter
    {
        public MsFinderMultipleExporter() { }
        private PeakSpotTableViewer viewer;
        private bool isPeak = false;
        private AlignmentSpotTableViewer viewer2;
        private AifViewerControl aifViewer;

        #region alignment spot table
        public MsFinderMultipleExporter(AlignmentSpotTableViewer viewer, AifViewerControl aifViewer) {
            this.viewer2 = viewer;
            this.isPeak = false;
            this.aifViewer = aifViewer;
            viewer.Title = "MS-FINDER exporter: Alignment Spot Table";

            var dg = viewer.DataGrid_RawData;
            var b1 = new Binding("Checked") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            FrameworkElementFactory fact = new FrameworkElementFactory(typeof(CheckBox));

            fact.SetValue(CheckBox.IsCheckedProperty, b1);
            fact.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            fact.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            var cellTemp = new DataTemplate();
            cellTemp.VisualTree = fact;
            var TargetColumn = new DataGridTemplateColumn() {
                CellTemplate = cellTemp,
                Header = "Target",
                CanUserSort = true,
                HeaderStyle = dg.ColumnHeaderStyle,
                Width = new DataGridLength(0.5, DataGridLengthUnitType.Star)
            };
            //dg.Columns.Insert(0, TargetColumn);
            dg.Columns.Add(TargetColumn);

            // added fotter
            var dg2 = new Grid() { Name = "footer", VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };

            // Export
            var butExport = new System.Windows.Controls.Button() {
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
            var butClose = new System.Windows.Controls.Button() {
                Name = "Button_Close",
                Content = "Close",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 25,
                Width = 70,
                Margin = new Thickness(5, 5, 5, 5)
            };
            butClose.Click += button_close_click;

            // settings
            var butSetting = new System.Windows.Controls.Button() {
                Name = "Button_Setting",
                Content = "Setting",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 25,
                Width = 70,
                Margin = new Thickness(5, 5, 230, 5)
            };
            butSetting.Click += button_setting_click;

            var butSaveAsMSP = new System.Windows.Controls.Button() {
                Name = "Button_SaveAsMsp",
                Content = "Save as MSP",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 25,
                Width = 70,
                Margin = new Thickness(5, 5, 155, 5)
            };
            butSaveAsMSP.Click += button_saveAsMsp_click;

            var butCheckAll = new System.Windows.Controls.Button() {
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
            Grid.SetRow(dg, 0);
            Grid.SetRow(dg2, 1);

            viewer.Height = 400;
            viewer.UpdateLayout();
        }
        #endregion

        #region peak spot table
        public MsFinderMultipleExporter(PeakSpotTableViewer viewer, AifViewerControl aifViewer) {
            this.viewer = viewer;
            this.aifViewer = aifViewer;
            this.isPeak = true;
            viewer.Title = "MS-FINDER exporter: Peak Spot Table";

            var dg = viewer.DataGrid_RawData;
            var b1 = new Binding("Checked") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            FrameworkElementFactory fact = new FrameworkElementFactory(typeof(CheckBox));

            fact.SetValue(CheckBox.IsCheckedProperty, b1);
            fact.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            fact.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            var cellTemp = new DataTemplate();
            cellTemp.VisualTree = fact;
            var TargetColumn = new DataGridTemplateColumn() {
                CellTemplate = cellTemp,
                Header = "Target",
                CanUserSort = true,
                HeaderStyle = dg.ColumnHeaderStyle,
                Width = new DataGridLength(0.5, DataGridLengthUnitType.Star)
            };
            //dg.Columns.Insert(0, TargetColumn);
            dg.Columns.Add(TargetColumn);

            // added fotter
            var dg2 = new Grid() { Name = "footer", VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };

            // Export
            var butExport = new System.Windows.Controls.Button() {
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
            var butClose = new System.Windows.Controls.Button() {
                Name = "Button_Close",
                Content = "Close",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 25,
                Width = 70,
                Margin = new Thickness(5, 5, 5, 5)
            };
            butClose.Click += button_close_click;

            // settings
            var butSetting = new System.Windows.Controls.Button() {
                Name = "Button_Setting",
                Content = "Setting",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 25,
                Width = 70,
                Margin = new Thickness(5, 5, 230, 5)
            };
            butSetting.Click += button_setting_click;

            // settings
            var butSaveAsMSP = new System.Windows.Controls.Button() {
                Name = "Button_SaveAsMsp",
                Content = "Save as MSP",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 25,
                Width = 70,
                Margin = new Thickness(5, 5, 155, 5)
            };
            butSaveAsMSP.Click += button_saveAsMsp_click;


            var butCheckAll = new System.Windows.Controls.Button() {
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
            Grid.SetRow(dg, 0);
            Grid.SetRow(dg2, 1);

            viewer.Height = 400;
            viewer.UpdateLayout();
        }
        #endregion

        private void button_export_click(object sender, RoutedEventArgs e) {

            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.Description = "Please select export folder.";

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                exportToMsFinder(fbd.SelectedPath);
        }

        private void button_setting_click(object sender, RoutedEventArgs e) {
            if (isPeak) {
                var source = this.viewer.PeakSpotTableViewerVM.Source;
                var w = new MsFinderExporterSettingWin(source);
                w.Owner = this.viewer;

                w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (w.ShowDialog() == true)
                    this.viewer.UpdateLayout();
            }
            else {
                var source = this.viewer2.AlignmentSpotTableViewerVM.Source;
                var w = new MsFinderExporterSettingWin(source);
                w.Owner = this.viewer2;

                w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (w.ShowDialog() == true)
                    this.viewer2.UpdateLayout();
            }
        }

        private void button_checkAll_click(object sender, RoutedEventArgs e) {
            var isAllChecked = true;
            if (((Button)sender).Content.ToString() == "Check all") {
                 isAllChecked = false;
            }
            if (isPeak) {
                var source = this.viewer.PeakSpotTableViewerVM.Source;
                if (isAllChecked) {
                    foreach (var spot in source) {
                        spot.Checked = false;
                    }
                }
                else {
                    foreach (var spot in source) {
                        spot.Checked = true;
                    }
                }
            }
            else {
                var source = this.viewer2.AlignmentSpotTableViewerVM.Source;
                if (isAllChecked) {
                    foreach (var spot in source) {
                        spot.Checked = false;
                    }
                }
                else {
                    foreach (var spot in source) {
                        spot.Checked = true;
                    }
                }
            }
            var content = isAllChecked ? "Check all" : "Uncheck all";
            ((Button)sender).Content = content;
        }
        

        private void button_saveAsMsp_click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.Description = "Please select export folder.";

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                saveAsMspFormat(fbd.SelectedPath);
        }


        private void saveAsMspFormat(string filePath) {
            if (isPeak) {
                var source = this.viewer.PeakSpotTableViewerVM.Source;
                aifViewer.AifViewControlForPeakVM.MultipleSaveAsMsp(source, filePath, viewer);
            }
            else {
                var source = this.viewer2.AlignmentSpotTableViewerVM.Source;
                aifViewer.AifViewControlForPeakVM.MultipleSaveAsMsp(source, filePath, viewer2);
            }
        }

        private void exportToMsFinder(string filePath) {
            if (isPeak) {
                var source = this.viewer.PeakSpotTableViewerVM.Source;
                aifViewer.AifViewControlForPeakVM.BulkExportToMsFinder(source, filePath, viewer);
            }
            else {
                var source = this.viewer2.AlignmentSpotTableViewerVM.Source;
                aifViewer.AifViewControlForPeakVM.BulkExportToMsFinder(source, filePath, viewer2);
            }
        }

        private void button_close_click(object sender, RoutedEventArgs e) {
            if (isPeak)
                this.viewer.Close();
            else
                this.viewer2.Close();
        }
    }
}
