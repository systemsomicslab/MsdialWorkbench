using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Export
{
    public class AlignmentResultExport2VM : ViewModelBase
    {
        public AlignmentResultExport2VM(
            AlignmentFileBean alignmentFile,
            ICollection<AlignmentFileBean> alignmentFiles,
            IMsdialDataStorage<ParameterBase> container) {

            this.alignmentFiles = CollectionViewSource.GetDefaultView(alignmentFiles);
            if (alignmentFile != null)
                this.alignmentFiles.MoveCurrentTo(alignmentFile);
            this.container = container;
            ExportTypes = new List<ExportType2> {
            };

        }

        public string ExportDirectory {
            get => exportDirectory;
            set {
                if (SetProperty(ref exportDirectory, value)) {
                    ExportCommand?.RaiseCanExecuteChanged();
                }
            }
        }
        private string exportDirectory = string.Empty;

        public ExportFormat2 Format {
            get => format;
            set => SetProperty(ref format, value);
        }
        private ExportFormat2 format;
        public List<ExportFormat2> Formats { get; } = new List<ExportFormat2> {
            new ExportFormat2("txt", new AlignmentCSVExporter()),
            new ExportFormat2("csv", new AlignmentCSVExporter(separator: ",")),
            // mztabm
        };

        public ExportspectraType SpectraType {
            get => spectraType;
            set => SetProperty(ref spectraType, value);
        }
        private ExportspectraType spectraType = ExportspectraType.deconvoluted;
        public List<ExportspectraType> SpectraTypes { get; } = new List<ExportspectraType> {
            ExportspectraType.deconvoluted
        };

        public AlignmentFileBean AlignmentFile {
            get => alignmentFile;
            set {
                if (SetProperty(ref alignmentFile, value)) {
                    ExportCommand?.RaiseCanExecuteChanged();
                }
            }
        }
        private AlignmentFileBean alignmentFile;

        public ICollectionView AlignmentFiles {
            get => alignmentFiles;
            set => SetProperty(ref alignmentFiles, value);
        }
        private ICollectionView alignmentFiles;

        public List<ExportType2> ExportTypes { get; } = new List<ExportType2>();

        private readonly IMsdialDataStorage<ParameterBase> container;

        public DelegateCommand BrowseDirectoryCommand => browseDirectoryCommand ?? (browseDirectoryCommand = new DelegateCommand(BrowseDirectory));
        private DelegateCommand browseDirectoryCommand;

        private void BrowseDirectory() {
            var fbd = new Graphics.Window.SelectFolderDialog
            {
                Title = "Chose a export folder.",
            };

            if (fbd.ShowDialog() == Graphics.Window.DialogResult.OK) {
                ExportDirectory = fbd.SelectedPath;
            }
        }

        public DelegateCommand ExportCommand => exportCommand ?? (exportCommand = new DelegateCommand(ExportAlignmentResult, CanExportAlignmentResult));
        private DelegateCommand exportCommand;

        private void ExportAlignmentResult() {
            var files = container.AnalysisFiles;
            var alignmentFile = AlignmentFile;
            var dt = DateTime.Now;

            var resultContainer = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(alignmentFile.FilePath);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _);

            var exporter = Format.Exporter;
            
            foreach (var exportType in ExportTypes.Where(type => type.IsSelected)) {
                var outfile = Path.Combine(ExportDirectory, $"{exportType.FilePrefix}_{alignmentFile.FileID}_{dt:yyyy_MM_dd_HH_mm_ss}.txt");
                using (var outstream = File.Open(outfile, FileMode.Create, FileAccess.Write)) {
                    if (exportType.FilePrefix == "Protein") {
                        var container = MsdialProteomicsSerializer.LoadProteinResultContainer(alignmentFile.ProteinAssembledResultFilePath);
                        var header = new List<string>() {
                            "Protein group ID", "Protein ID", "Protein name", "Protein description", "Coverage", "Score", "Peptide count", "Unique peptide count"
                        };
                        foreach (var file in files) header.Add(file.AnalysisFileName);
                        using (var sw = new StreamWriter(outstream, Encoding.ASCII)) {
                            sw.WriteLine(String.Join("\t", header));
                            foreach (var group in container.ProteinGroups) {
                                foreach (var protein in group.ProteinMsResults) {

                                    var values = new List<string>() {
                                        group.GroupID.ToString(), protein.FastaProperty.UniqueIdentifier, protein.FastaProperty.ProteinName, protein.FastaProperty.Description,
                                        protein.PeptideCoverage.ToString(), protein.Score.ToString(), protein.MatchedPeptideResults.Count().ToString(), protein.UniquePeptides.Count().ToString()
                                    };
                                    foreach (var height in protein.PeakHeights) values.Add(height.ToString());
                                    sw.WriteLine(String.Join("\t", values));
                                }
                            }
                        }
                    }
                    else {
                        exporter.Export(
                        outstream,
                        resultContainer.AlignmentSpotProperties,
                        msdecResults,
                        files,
                        exportType.MetadataAccessor,
                        exportType.QuantValueAccessor,
                        exportType.Stats);
                    }
                }
            }
        }

        private bool CanExportAlignmentResult() {
            if (AlignmentFile == null)
                return false;

            return Directory.Exists(ExportDirectory);
        }

        public DelegateCommand<Window> CancelCommand => cancelCommand ?? (cancelCommand = new DelegateCommand<Window>(Cancel));
        private DelegateCommand<Window> cancelCommand;

        private void Cancel(Window window) {
            window.DialogResult = false;
            window.Close();
        }
    }

    public class ExportType2 : ViewModelBase {
        public ExportType2(string label, IMetadataAccessor metadataAccessor, IQuantValueAccessor quantValueAccessor, string filePrefix, bool isSelected = false) {
            Label = label;
            MetadataAccessor = metadataAccessor;
            QuantValueAccessor = quantValueAccessor;
            FilePrefix = filePrefix;
            IsSelected = isSelected;
        }

        public ExportType2(string label, IMetadataAccessor metadataAccessor, IQuantValueAccessor quantValueAccessor, string filePrefix, List<StatsValue> stats, bool isSelected = false) {
            Label = label;
            MetadataAccessor = metadataAccessor;
            QuantValueAccessor = quantValueAccessor;
            FilePrefix = filePrefix;
            IsSelected = isSelected;
            Stats = stats;
        }

        public string Label { get; }

        public IMetadataAccessor MetadataAccessor { get; }

        public IQuantValueAccessor QuantValueAccessor { get; }
        public bool IsSelected {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
        private bool isSelected = false;
        public string FilePrefix { get; }

        public List<StatsValue> Stats { get; } = new List<StatsValue>();
    }

    public class ExportFormat2 : ViewModelBase
    {
        public ExportFormat2(string label, IAlignmentExporter exporter) {
            Label = label;
            Exporter = exporter;
        }

        public string Label { get; }
        public IAlignmentExporter Exporter { get; }
    }
}
