using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
            if (ExportMethod.IsLongFormat) {
                return 2;
            }
            return Types.Count(type => type.IsSelected);
        }

        public void Export(AlignmentFileBean alignmentFile, string exportDirectory, Action<string> notification) {
            var outNameTemplate = $"{{0}}_{alignmentFile.FileID}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
            var resultContainer = AlignmentResultContainer.Load(alignmentFile);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _);
            ExportMethod.Export(outNameTemplate, exportDirectory, resultContainer, msdecResults, notification, Types.Where(type => type.IsSelected));
        }
    }
}
