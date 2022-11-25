using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    internal interface IAlignmentResultExportModel {
        void Export(IMsdialDataStorage<ParameterBase> storage, AlignmentFileBean alignmentFile, string exportDirectory, Action<double, string> notification);
    }

    internal sealed class AlignmentExportGroupModel : BindableBase, IAlignmentResultExportModel {
        public AlignmentExportGroupModel(string label, IEnumerable<ExportFormat> formats, IEnumerable<ExportType> types, IEnumerable<ExportspectraType> spectraTypes) {
            Label = label;
            _types = new ObservableCollection<ExportType>(types);
            Types = new ReadOnlyObservableCollection<ExportType>(_types);
            _formats = new ObservableCollection<ExportFormat>(formats);
            Formats = new ReadOnlyObservableCollection<ExportFormat>(_formats);
            _spectraTypes = new ObservableCollection<ExportspectraType>(spectraTypes);
            SpectraTypes = new ReadOnlyObservableCollection<ExportspectraType>(_spectraTypes);
        }

        public string Label { get; }

        public ExportFormat Format {
            get => _format;
            set => SetProperty(ref _format, value);
        }
        private ExportFormat _format;

        public ReadOnlyObservableCollection<ExportFormat> Formats { get; }
        private readonly ObservableCollection<ExportFormat> _formats;

        public ReadOnlyObservableCollection<ExportType> Types { get; }
        private readonly ObservableCollection<ExportType> _types;

        public void AddExportTypes(params ExportType[] exportTypes) {
            foreach (var type in exportTypes) {
                _types.Add(type);
            }
        }

        public ExportspectraType SpectraType {
            get => _spectraType;
            set => SetProperty(ref _spectraType, value);
        }
        private ExportspectraType _spectraType = ExportspectraType.deconvoluted;

        public ReadOnlyObservableCollection<ExportspectraType> SpectraTypes { get; }

        private readonly ObservableCollection<ExportspectraType> _spectraTypes;

        public void ExportAlignmentResult(IMsdialDataStorage<ParameterBase> storage, AlignmentFileBean alignmentFile, string exportDirectory, Action<double, string> notification = null) {
            var files = storage.AnalysisFiles;
            var dt = DateTime.Now;
            var resultContainer = AlignmentResultContainer.Load(alignmentFile);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _);
            var exportTypes = Types.Where(type => type.IsSelected).ToArray();
            
            foreach (var (exportType, index) in exportTypes.WithIndex()) {
                var outName = $"{exportType.FilePrefix}_{alignmentFile.FileID}_{dt:yyyy_MM_dd_HH_mm_ss}.{Format.FileExtension}";
                var outfile = Path.Combine(exportDirectory, outName);
                notification?.Invoke(((double)index) / exportTypes.Length, $"Exporting {outName}");

                using (var outstream = File.Open(outfile, FileMode.Create, FileAccess.Write)) {
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

        void IAlignmentResultExportModel.Export(IMsdialDataStorage<ParameterBase> storage, AlignmentFileBean alignmentFile, string exportDirectory, Action<double, string> notification)
            => ExportAlignmentResult(storage, alignmentFile, exportDirectory, notification);
    }
}
