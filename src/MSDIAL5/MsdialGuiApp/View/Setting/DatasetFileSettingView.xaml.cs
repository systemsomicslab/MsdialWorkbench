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
    public partial class DatasetFileSettingView : UserControl {
        private static readonly IFileSelectionItem[] FILE_SELECTION_ITEMS = new[] {
            new FileSelectionItem("ABF file", ".abf"),
            new FileSelectionItem("Hive HMD file", ".hmd"),
            new FileSelectionItem("Hive mzB file", ".mzb"),
            new FileSelectionItem("mzML file", ".mzml"),
            new FileSelectionItem("netCDF file", ".cdf"),
            new FileSelectionItem("IBF file", ".ibf"),
            new FileSelectionItem("WIFF file", ".wiff"),
            new FileSelectionItem("WIFF2 file", ".wiff2"),
            new FileSelectionItem("Raw file", ".raw"),
            new FileSelectionItem("LCD file", ".lcd"),
            new FileSelectionItem("QGD file", ".qgd"),
            new FileSelectionItem("LRP file", ".lrp"),
            // new FileSelectionItem("MIS file", ".mis"),
        };
        private static readonly IFileSelectionItem[] ACCEPTABLE_ITEMS = FILE_SELECTION_ITEMS
            .Concat(new[]
            {
                new FileSelectionItem("D file", ".d"),
                new FileSelectionItem("IABF file", ".iabf"),
            }).ToArray();

        public DatasetFileSettingView() {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(OpenDialogCommand, ExecuteOpenDialog));
        }

        public static RoutedCommand OpenDialogCommand = new RoutedCommand();

        private void ExecuteOpenDialog(object sender, ExecutedRoutedEventArgs e) {
            var filterItems = FILE_SELECTION_ITEMS.Append(WildCardSelectionItem.Instance).ToList();
            var ofd = new OpenFileDialog()
            {
                Title = "Import analysis files",
                RestoreDirectory = true,
                Multiselect = true,
                Filter = string.Join("|", filterItems),
            };

            if (ofd.ShowDialog() == true) {
                SendQuery(ofd.FileNames);
            }
        }

        private void ExecuteDrop(object sender, DragEventArgs e) {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            SendQuery(files);
        }

        private void SendQuery(string[]? files) {
            if (files is not null) {
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
            return ACCEPTABLE_ITEMS.Any(item => item.IsMatched(extension));
        }

        private interface IFileSelectionItem {
            bool IsMatched(string extension);
        }

        private class FileSelectionItem : IFileSelectionItem {
            public FileSelectionItem(string label, string extension) {
                Label = label;
                Extension = extension;
            }

            public string Label { get; }
            public string Extension { get; }

            public override string ToString() {
                return $"{Label}(*{Extension})|*{Extension}";
            }

            public bool IsMatched(string extension) {
                return Extension == extension;
            }
        }

        private class WildCardSelectionItem : IFileSelectionItem {
            public static readonly WildCardSelectionItem Instance = new WildCardSelectionItem();

            private WildCardSelectionItem() {

            }

            public override string ToString() {
                return "All file(*.*)|*.*";
            }

            public bool IsMatched(string extension) {
                return true;
            }
        }
    }
}
