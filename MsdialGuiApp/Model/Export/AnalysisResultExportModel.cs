using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    class AnalysisResultExportModel : ValidatableBase
    {
        public AnalysisResultExportModel(
            IEnumerable<AnalysisFileBean> files,
            IEnumerable<SpectraType> spectraTypes,
            IEnumerable<SpectraFormat> spectraFormats,
            IDataProviderFactory<AnalysisFileBean> providerFactory) {
            if (files is null) {
                throw new ArgumentNullException(nameof(files));
            }

            if (spectraTypes is null) {
                throw new ArgumentNullException(nameof(spectraTypes));
            }

            if (spectraFormats is null) {
                throw new ArgumentNullException(nameof(spectraFormats));
            }

            if (providerFactory is null) {
                throw new ArgumentNullException(nameof(providerFactory));
            }

            this.providerFactory = providerFactory;

            UnSelectedFiles = new ObservableCollection<AnalysisFileBean>(files);
            SelectedFiles = new ObservableCollection<AnalysisFileBean>();

            ExportSpectraTypes = new ObservableCollection<SpectraType>(spectraTypes);
            SelectedSpectraType = ExportSpectraTypes.FirstOrDefault();

            ExportSpectraFileFormats = new ObservableCollection<SpectraFormat>(spectraFormats);
            SelectedFileFormat = ExportSpectraFileFormats.FirstOrDefault();
        }

        private readonly IDataProviderFactory<AnalysisFileBean> providerFactory;

        public void Export() {
            Export(SelectedFiles);
        }

        public void Export(IEnumerable<AnalysisFileBean> files) {

            foreach (var file in files) {
                var provider = providerFactory.Create(file);
                var filename = Path.Combine(DestinationFolder, file.AnalysisFileName + "." + SelectedFileFormat.Format);
                using (var stream = File.Open(filename, FileMode.Create, FileAccess.Write)) {
                    Export(file, provider, SelectedFileFormat.Exporter, SelectedSpectraType.Accessor, stream);
                }
            }
        }

        public ObservableCollection<AnalysisFileBean> SelectedFiles {
            get => selectedFiles;
            set => SetProperty(ref selectedFiles, value);
        }

        private ObservableCollection<AnalysisFileBean> selectedFiles;

        public ObservableCollection<AnalysisFileBean> UnSelectedFiles {
            get => unSelectedFiles;
            set => SetProperty(ref unSelectedFiles, value);
        }

        private ObservableCollection<AnalysisFileBean> unSelectedFiles;

        public void Selects(IEnumerable<AnalysisFileBean> files) {
            foreach (var file in files) {
                UnSelectedFiles.Remove(file);
                SelectedFiles.Add(file);
            }
        }
 
        public void UnSelects(IEnumerable<AnalysisFileBean> files) {
            foreach (var file in files) {
                SelectedFiles.Remove(file);
                UnSelectedFiles.Add(file);
            }
        }

        public string DestinationFolder {
            get => destinationFolder;
            set => SetProperty(ref destinationFolder, value);
        }

        private string destinationFolder;

        public ObservableCollection<SpectraType> ExportSpectraTypes { get; }

        public ObservableCollection<SpectraFormat> ExportSpectraFileFormats { get; }

        public SpectraType SelectedSpectraType {
            get => selectedSpectraType;
            set => SetProperty(ref selectedSpectraType, value);
        }

        private SpectraType selectedSpectraType;

        public SpectraFormat SelectedFileFormat {
            get => selectedFileFormat;
            set => SetProperty(ref selectedFileFormat, value);
        }

        private SpectraFormat selectedFileFormat;

        public int IsotopeExportMax {
            get => isotopeExportMax;
            set => SetProperty(ref isotopeExportMax, value);
        }

        private int isotopeExportMax = 2;

        public static void Export(
            AnalysisFileBean file,
            IDataProvider provider,
            IAnalysisExporter exporter,
            IAnalysisMetadataAccessor metaAccessor,
            Stream dest) {

            var features = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);
            var msdecs = MsdecResultsReader.ReadMSDecResults(file.DeconvolutionFilePath, out _, out _);

            exporter.Export(dest, features, msdecs, provider, metaAccessor);
        }
    }

    class SpectraFormat
    {
        public SpectraFormat(ExportSpectraFileFormat format, IAnalysisExporter exporter) {
            Format = format;
            Exporter = exporter;
        }

        public ExportSpectraFileFormat Format { get; }
        public IAnalysisExporter Exporter { get; }
    }

    class SpectraType
    {
        public SpectraType(ExportspectraType type, IAnalysisMetadataAccessor accessor) {
            Type = type;
            Accessor = accessor;
        }

        public ExportspectraType Type { get; }
        public IAnalysisMetadataAccessor Accessor { get; }
    }
}
