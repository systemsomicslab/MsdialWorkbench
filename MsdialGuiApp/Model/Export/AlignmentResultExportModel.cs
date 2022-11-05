using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentResultExportModel : DisposableModelBase
    {
        private readonly IMsdialDataStorage<ParameterBase> _container;

        public AlignmentResultExportModel(AlignmentFileBean alignmentFile, IReadOnlyList<AlignmentFileBean> alignmentFiles, IMsdialDataStorage<ParameterBase> container) {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            AlignmentFiles = alignmentFiles;
            AlignmentFile = alignmentFile;
            _exportTypes = new ObservableCollection<ExportType>();
            ExportTypes = new ReadOnlyObservableCollection<ExportType>(_exportTypes);
            _formats = new ObservableCollection<ExportFormat>
            {
                new ExportFormat("txt", new AlignmentCSVExporter()),
                new ExportFormat("csv", new AlignmentCSVExporter(separator: ",")),
                // mztabm
            };
            Formats = new ReadOnlyObservableCollection<ExportFormat>(_formats);
            _spectraTypes = new ObservableCollection<ExportspectraType> {
                ExportspectraType.deconvoluted,
            };
            SpectraTypes = new ReadOnlyObservableCollection<ExportspectraType>(_spectraTypes);
        }

        public string ExportDirectory {
            get => _exportDirectory;
            set => SetProperty(ref _exportDirectory, value);
        }
        private string _exportDirectory = string.Empty;

        public ExportFormat Format {
            get => _format;
            set => SetProperty(ref _format, value);
        }
        private ExportFormat _format;

        public ReadOnlyObservableCollection<ExportFormat> Formats { get; }
        private readonly ObservableCollection<ExportFormat> _formats;

        public ExportspectraType SpectraType {
            get => _spectraType;
            set => SetProperty(ref _spectraType, value);
        }
        private ExportspectraType _spectraType = ExportspectraType.deconvoluted;

        public ReadOnlyObservableCollection<ExportspectraType> SpectraTypes { get; }
        private readonly ObservableCollection<ExportspectraType> _spectraTypes;

        public AlignmentFileBean AlignmentFile {
            get => _alignmentFile;
            set => SetProperty(ref _alignmentFile, value);
        }
        private AlignmentFileBean _alignmentFile;

        public IReadOnlyList<AlignmentFileBean> AlignmentFiles { get; }

        public ReadOnlyObservableCollection<ExportType> ExportTypes { get; }
        private readonly ObservableCollection<ExportType> _exportTypes;

        public void AddExportTypes(params ExportType[] exportTypes) {
            foreach (var type in exportTypes) {
                _exportTypes.Add(type);
            }
        }

        public void ExportAlignmentResult(Action<double, string> notification = null) {
            var files = _container.AnalysisFiles;
            var alignmentFile = AlignmentFile;
            var dt = DateTime.Now;

            var resultContainer = AlignmentResultContainer.Load(alignmentFile);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _);
            var exportTypes = ExportTypes.Where(type => type.IsSelected).ToArray();
            
            foreach (var (exportType, index) in exportTypes.WithIndex()) {
                var outName = $"{exportType.FilePrefix}_{alignmentFile.FileID}_{dt:yyyy_MM_dd_HH_mm_ss}.txt";
                var outfile = Path.Combine(ExportDirectory, outName);
                notification?.Invoke(((double)index) / exportTypes.Length, $"Exporting {outName}");

                using (var outstream = File.Open(outfile, FileMode.Create, FileAccess.Write)) {
                    if (exportType.FilePrefix == "Protein") {
                        var container = MsdialProteomicsSerializer.LoadProteinResultContainer(alignmentFile.ProteinAssembledResultFilePath);
                        var header = new List<string>() {
                            "Protein group ID", "Protein ID", "Protein name", "Protein description", "Coverage", "Score", "Peptide count", "Unique peptide count"
                        };
                        foreach (var file in files) header.Add(file.AnalysisFileName);
                        using (var sw = new StreamWriter(outstream, Encoding.ASCII)) {
                            sw.WriteLine(string.Join("\t", header));
                            foreach (var group in container.ProteinGroups) {
                                foreach (var protein in group.ProteinMsResults) {

                                    var values = new List<string>() {
                                        group.GroupID.ToString(), protein.FastaProperty.UniqueIdentifier, protein.FastaProperty.ProteinName, protein.FastaProperty.Description,
                                        protein.PeptideCoverage.ToString(), protein.Score.ToString(), protein.MatchedPeptideResults.Count().ToString(), protein.UniquePeptides.Count().ToString()
                                    };

                                    foreach (var height in protein.PeakHeights) values.Add(height.ToString());
                                    sw.WriteLine(string.Join("\t", values));
                                }
                            }
                        }
                    }
                    else {
                        var exporter = Format.Exporter;
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

        public bool CanExportAlignmentResult() {
            return !(AlignmentFile is null) && Directory.Exists(ExportDirectory);
        }
    }

    public sealed class ExportType : BindableBase {
        public ExportType(string label, IMetadataAccessor metadataAccessor, IQuantValueAccessor quantValueAccessor, string filePrefix, bool isSelected = false) {
            Label = label;
            MetadataAccessor = metadataAccessor;
            QuantValueAccessor = quantValueAccessor;
            FilePrefix = filePrefix;
            IsSelected = isSelected;
        }

        public ExportType(string label, IMetadataAccessor metadataAccessor, IQuantValueAccessor quantValueAccessor, string filePrefix, List<StatsValue> stats, bool isSelected = false) {
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
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        public string FilePrefix { get; }

        public List<StatsValue> Stats { get; } = new List<StatsValue>();
    }

    public sealed class ExportFormat : BindableBase
    {
        public ExportFormat(string label, IAlignmentExporter exporter) {
            Label = label;
            Exporter = exporter;
        }

        public string Label { get; }
        public IAlignmentExporter Exporter { get; }
    }
}
