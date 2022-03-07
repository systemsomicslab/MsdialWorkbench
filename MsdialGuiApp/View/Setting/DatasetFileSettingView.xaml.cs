using CompMs.App.Msdial.ViewModel.Setting;
using Microsoft.Win32;
using Reactive.Bindings.Notifiers;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Setting
{
    /// <summary>
    /// Interaction logic for DatasetFileSettingView.xaml
    /// </summary>
    public partial class DatasetFileSettingView : UserControl
    {
        public DatasetFileSettingView() {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(OpenDialogCommand, ExecuteOpenDialog));
        }

        public static RoutedCommand OpenDialogCommand = new RoutedCommand();

        private void ExecuteOpenDialog(object sender, ExecutedRoutedEventArgs e) {
            var ofd = new OpenFileDialog()
            {
                Title = "Import analysis files",
                RestoreDirectory = true,
                Multiselect = true,
            };
            ofd.Filter = string.Join("|",
                new[]
                {
                    "ABF file(*.abf)|*.abf",
                    "mzML file(*.mzml)|*.mzml",
                    "netCDF file(*.cdf)|*.cdf",
                    "IBF file(*.ibf)|*.ibf",
                    "WIFF file(*.wiff)|*.wiff",
                    "WIFF2 file(*.wiff2)|*.wiff2",
                    "Raw file(*.raw)|*.raw",
                    "LCD file(*.lcd)|*.lcd",
                    "QGD file(*.qgd)|*.qgd",
                    "All file(*.*)|*.*"
                });

            if (ofd.ShowDialog() == true) {
                SendQuery(ofd.FileNames);
            }
        }

        private void ExecuteDrop(object sender, DragEventArgs e) {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            SendQuery(files);
        }

        private void SendQuery(string[] files) {
            if (!(files is null)) {
                if (files.Select(Path.GetDirectoryName).Distinct().Count() != 1) {
                    MessageBox.Show("All analysis files should be placed in the same directory.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var unacceptableFiles = files.Where(f => !IsAccepted(f)).ToArray();
                if (unacceptableFiles.Length > 0) {
                    MessageBox.Show("The following file(s) cannot be converted because they are not acceptable raw files\n" +
                        string.Join("\n", unacceptableFiles),
                        "Unacceptable Files",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                MessageBroker.Default.Publish(new SelectedAnalysisFileQuery(files.Where(IsAccepted).ToArray()));
            }
        }

        private bool IsAccepted(string file) {
            var extension = Path.GetExtension(file).ToLower();
            switch (extension) {
                case ".abf":
                case ".mzml":
                case ".cdf":
                case ".raw":
                case ".d":
                case ".iabf":
                case ".ibf":
                case ".wiff":
                case ".wiff2":
                case ".qgd":
                case ".lcd":
                    return true;
                default:
                    return false;
            }
        }
    }
}
