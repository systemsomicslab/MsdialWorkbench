using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System;
using System.IO;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class ProteinGroupExportModel : BindableBase, IAlignmentResultExportModel
    {
        private readonly ProteinGroupExporter _exporter;

        public ProteinGroupExportModel(ProteinGroupExporter exporter) {
            _exporter = exporter;
        }

        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        void IAlignmentResultExportModel.Export(IMsdialDataStorage<ParameterBase> storage, AlignmentFileBean alignmentFile, string exportDirectory, Action<double, string> notification) {
            var outfile = Path.Combine(exportDirectory, $"Protein_{alignmentFile.FileID}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.txt");
            var resultContainer = MsdialProteomicsSerializer.LoadProteinResultContainer(alignmentFile.ProteinAssembledResultFilePath);
            using (var outstream = File.Open(outfile, FileMode.Create, FileAccess.Write)) {
                _exporter.Export(outstream, resultContainer, storage.AnalysisFiles);
            }
        }
    }
}
