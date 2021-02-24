using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Export
{
    public class AlignmentResultExportVM : ViewModelBase {

        public string ExportDirectory {
            get => exportDirectory;
            set {
                if (SetProperty(ref exportDirectory, value)) {
                    ExportCommand?.RaiseCanExecuteChanged();
                }
            }
        }
        private string exportDirectory = string.Empty;

        public ExportSpectraFileFormat Format {
            get => format;
            set => SetProperty(ref format, value);
        }
        private ExportSpectraFileFormat format = ExportSpectraFileFormat.mgf;
        public ReadOnlyCollection<ExportSpectraFileFormat> Formats { get; } = new List<ExportSpectraFileFormat> { ExportSpectraFileFormat.txt }.AsReadOnly();

        public ExportspectraType SpectraType {
            get => spectraType;
            set => SetProperty(ref spectraType, value);
        }
        private ExportspectraType spectraType = ExportspectraType.centroid;
        public ReadOnlyCollection<ExportspectraType> SpectraTypes { get; } = new List<ExportspectraType> { ExportspectraType.deconvoluted }.AsReadOnly();

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

        public ReadOnlyCollection<ExportType> ExportTypes { get; } = new List<ExportType> {
            new ExportType("Raw data (Height)", "Height", "Height", true),
            new ExportType("Raw data (Area)", "Area", "Area"),
            new ExportType("Normalized data (Height)", "Normalized height", "NormalizedHeight"),
            new ExportType("Normalized data (Area)", "Normalized area", "NormalizedArea"),
            new ExportType("Alignment ID", "ID", "PeakID"),
            new ExportType("m/z", "MZ", "Mz"),
            new ExportType("Retention time", "RT", "RT"),
            new ExportType("Retention index", "RI", "RI"),
            new ExportType("Drift time", "Mobility", "DT"),
            new ExportType("Collision cross section", "CCS", "CCS"),
            new ExportType("S/N", "SN", "SN"),
            new ExportType("MS/MS included", "MSMS", "MsmsIncluded"),
        }.AsReadOnly();

        private readonly MsdialDataStorage container;

        public AlignmentResultExportVM(AlignmentFileBean alignmentFile, ICollection<AlignmentFileBean> alignmentFiles, MsdialDataStorage container) {
            this.alignmentFiles = CollectionViewSource.GetDefaultView(alignmentFiles);
            if (alignmentFile != null)
                this.alignmentFiles.MoveCurrentTo(alignmentFile);
            this.container = container;
        }

        public AlignmentResultExportVM(ICollection<AlignmentFileBean> alignmentFiles, MsdialDataStorage container) : this(null, alignmentFiles, container) { }

        public DelegateCommand BrowseDirectoryCommand => browseDirectoryCommand ?? (browseDirectoryCommand = new DelegateCommand(BrowseDirectory));
        private DelegateCommand browseDirectoryCommand;

        private void BrowseDirectory() {
            var fbd = new Graphics.Window.SelectFolderDialog
            {
                Title = "Chose a export folder.",
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            };

            if (fbd.ShowDialog() == Graphics.Window.DialogResult.OK) {
                ExportDirectory = fbd.SelectedPath;
            }
        }

        public DelegateCommand ExportCommand => exportCommand ?? (exportCommand = new DelegateCommand(ExportAlignmentResult, CanExportAlignmentResult));
        private DelegateCommand exportCommand;

        private void ExportAlignmentResult() {
            var param = container.ParameterBase;
            var mspDB = container.MspDB;
            var textDB = container.TextDB;
            var category = param.MachineCategory;
            var files = container.AnalysisFiles;
            var alignmentFile = AlignmentFile;
            var dt = DateTime.Now;

            var resultContainer = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(alignmentFile.FilePath);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _);

            foreach (var exportType in ExportTypes.Where(type => type.IsSelected)) {
                var outfile = System.IO.Path.Combine(ExportDirectory, $"{exportType.FilePrefix}_{alignmentFile.FileID}_{dt:yyyy_MM_dd_HH_mm_ss}.txt");
                ExportAlignmentResult(
                    outfile, exportType.TypeName,
                    resultContainer, msdecResults,
                    files, mspDB, textDB,
                    param, category);
            }
        }

        private static void ExportAlignmentResult(
            string outfile, string exportType,
            AlignmentResultContainer resultContainer, List<MSDecResult> msdecResults,
            List<AnalysisFileBean> files,
            List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB,
            ParameterBase param, MachineCategory category
            ) {
            using (var sw = new System.IO.StreamWriter(outfile, false, Encoding.ASCII)) {
                // Header
                MsdialCore.Export.ResultExport.WriteAlignmentResultHeader(sw, category, files);

                // From the second
                foreach (var spot in resultContainer.AlignmentSpotProperties) {
                    var msdecID = spot.MasterAlignmentID;
                    var msdec = msdecResults[msdecID];
                    MsdialCore.Export.ResultExport.WriteAlignmentSpotFeature(sw, spot, msdec, param, mspDB, textDB, exportType);

                    foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>()) {
                        msdecID = driftSpot.MasterAlignmentID;
                        msdec = msdecResults[msdecID];
                        MsdialCore.Export.ResultExport.WriteAlignmentSpotFeature(sw, driftSpot, msdec, param, mspDB, textDB, exportType);
                    }
                }
            }
        }

        private bool CanExportAlignmentResult() {
            if (AlignmentFile == null)
                return false;

            return System.IO.Directory.Exists(ExportDirectory);
        }

        public DelegateCommand<Window> CancelCommand => cancelCommand ?? (cancelCommand = new DelegateCommand<Window>(Cancel));
        private DelegateCommand<Window> cancelCommand;

        private void Cancel(Window window) {
            window.DialogResult = false;
            window.Close();
        }
    }

    public class ExportType : ViewModelBase
    {
        public ExportType(string label, string typeName, string filePrefix, bool isSelected = false) {
            Label = label;
            TypeName = typeName;
            FilePrefix = filePrefix;
            IsSelected = isSelected;
        }

        public string Label { get; }
        public string TypeName { get; }
        public bool IsSelected {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
        private bool isSelected = false;
        public string FilePrefix { get; }
    }
}
