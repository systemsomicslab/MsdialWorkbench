using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class ProteinGroupExportModel : BindableBase, IAlignmentResultExportModel
    {
        private readonly ProteinGroupExporter _exporter;
        private readonly IReadOnlyList<AnalysisFileBean> _analysisFiles;

        public ProteinGroupExportModel(ProteinGroupExporter exporter, IReadOnlyList<AnalysisFileBean> analysisFiles) {
            _exporter = exporter;
            _analysisFiles = analysisFiles ?? throw new ArgumentNullException(nameof(analysisFiles));
        }

        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        public int CountExportFiles(AlignmentFileBeanModel alignmentFile) {
            return 1;
        }

        public void Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
            string outFile = $"Protein_{((IFileBean)alignmentFile).FileID}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.txt";
            var outPath = Path.Combine(exportDirectory, outFile);
            var resultContainer = alignmentFile.LoadProteinResult();
            notification?.Invoke(outFile);
            using (var outstream = File.Open(outPath, FileMode.Create, FileAccess.Write)) {
                _exporter.Export(outstream, resultContainer, _analysisFiles);
            }
        }
    }
}
