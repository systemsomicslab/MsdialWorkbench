using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentSpectraExportGroupModel : BindableBase, IAlignmentResultExportModel
    {
        private readonly AlignmentMspExporter _exporter;

        public AlignmentSpectraExportGroupModel(AlignmentMspExporter exporter, IEnumerable<ExportspectraType> spectraTypes) {
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
            SpectraTypes = new ObservableCollection<ExportspectraType>(spectraTypes);
        }

        public ExportspectraType SpectraType {
            get => _spectraType;
            set => SetProperty(ref _spectraType, value);
        }
        private ExportspectraType _spectraType = ExportspectraType.deconvoluted;

        public ObservableCollection<ExportspectraType> SpectraTypes { get; }

        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        public int CountExportFiles() {
            return IsSelected ? 1 : 0;
        }

        public void Export(AlignmentFileBean alignmentFile, string exportDirectory, Action<string> notification) {
            if (!IsSelected) {
                return;
            }
            var dt = DateTime.Now;
            var cts = new CancellationTokenSource();
            var resultContainer = AlignmentResultContainer.LoadLazy(alignmentFile, cts.Token);
            var msdecResults = MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _);
            var outName = $"Msp_{alignmentFile.FileID}_{dt:yyyy_MM_dd_HH_mm_ss}.msp";
            var outPath = Path.Combine(exportDirectory, outName);
            notification?.Invoke(outName);
            using (var stream = File.Open(outPath, FileMode.Create, FileAccess.Write)) {
                _exporter.Export(stream, resultContainer.AlignmentSpotProperties, msdecResults);
            }
            cts.Cancel();
        }
    }
}
