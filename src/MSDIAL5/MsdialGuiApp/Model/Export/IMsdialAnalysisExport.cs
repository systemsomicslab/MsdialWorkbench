using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using System;
using System.IO;

namespace CompMs.App.Msdial.Model.Export
{
    internal interface IMsdialAnalysisExport {
        void Export(string destinationFolder, AnalysisFileBeanModel fileBeanModel);
    }

    internal sealed class MsdialAnalysisExportModel : BindableBase, IMsdialAnalysisExport
    {
        private readonly IAnalysisExporterZZZ _exporter;

        public MsdialAnalysisExportModel(IAnalysisExporterZZZ exporter) {
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        }

        public string Label { get; set; }
        public string FilePrefix { get; set; } = string.Empty;
        public string FileSuffix { get; set; } = "txt";

        public bool ShouldExport {
            get => _shouldExport;
            set => SetProperty(ref _shouldExport, value);
        }
        private bool _shouldExport = false;

        void IMsdialAnalysisExport.Export(string destinationFolder, AnalysisFileBeanModel fileBeanModel) {
            var filename = Path.Combine(destinationFolder, $"{FilePrefix}_{fileBeanModel.AnalysisFileName}.{FileSuffix}");
            using (var stream = File.Open(filename, FileMode.Create, FileAccess.Write)) {
                var features = ChromatogramPeakFeatureCollection.LoadAsync(fileBeanModel.PeakAreaBeanInformationFilePath).Result;
                _exporter.Export(stream, fileBeanModel.File, features);
            }
        }
    }
}
