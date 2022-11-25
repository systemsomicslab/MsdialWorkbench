using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace CompMs.App.Msdial.Model.Export
{
    internal interface IAlignmentResultExportModel {
        int CountExportFiles();
        void Export(AlignmentFileBean alignmentFile, string exportDirectory, Action<string> notification);
    }

    internal sealed class AlignmentExportGroupModel : BindableBase, IAlignmentResultExportModel {
        public AlignmentExportGroupModel(string label, ExportMethod method, IEnumerable<ExportType> types, IEnumerable<ExportspectraType> spectraTypes) {
            Label = label;
            _types = new ObservableCollection<ExportType>(types);
            Types = new ReadOnlyObservableCollection<ExportType>(_types);
            ExportMethod = method;
            _spectraTypes = new ObservableCollection<ExportspectraType>(spectraTypes);
            SpectraTypes = new ReadOnlyObservableCollection<ExportspectraType>(_spectraTypes);
        }

        public string Label { get; }

        public ExportMethod ExportMethod { get; }

        public ReadOnlyObservableCollection<ExportType> Types { get; }
        private readonly ObservableCollection<ExportType> _types;

        public ExportspectraType SpectraType {
            get => _spectraType;
            set => SetProperty(ref _spectraType, value);
        }
        private ExportspectraType _spectraType = ExportspectraType.deconvoluted;

        public ReadOnlyObservableCollection<ExportspectraType> SpectraTypes { get; }

        private readonly ObservableCollection<ExportspectraType> _spectraTypes;

        public int CountExportFiles() {
            return Types.Count(type => type.IsSelected);
        }

        public void Export(AlignmentFileBean alignmentFile, string exportDirectory, Action<string> notification) {
            var dt = DateTime.Now;
            var resultContainer = AlignmentResultContainer.Load(alignmentFile);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _);

            foreach (var exportType in Types.Where(type => type.IsSelected)) {
                var outName = $"{exportType.FilePrefix}_{alignmentFile.FileID}_{dt:yyyy_MM_dd_HH_mm_ss}";
                ExportMethod.Export(outName, exportType, exportDirectory, resultContainer, msdecResults, notification);
            }
        }
    }
}
