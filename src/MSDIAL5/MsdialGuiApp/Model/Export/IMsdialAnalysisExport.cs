using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    internal interface IMsdialAnalysisExport {
        void Export(string destinationFolder, AnalysisFileBeanModel fileBeanModel);
    }

    internal sealed class MsdialAnalysisExport : BindableBase, IMsdialAnalysisExport
    {
        private readonly IDataProviderFactory<AnalysisFileBeanModel> _providerFactory;

        public MsdialAnalysisExport(
            IEnumerable<SpectraType> spectraTypes,
            IEnumerable<SpectraFormat> spectraFormats,
            IDataProviderFactory<AnalysisFileBeanModel> providerFactory) {

            if (spectraTypes is null) {
                throw new ArgumentNullException(nameof(spectraTypes));
            }

            if (spectraFormats is null) {
                throw new ArgumentNullException(nameof(spectraFormats));
            }

            if (providerFactory is null) {
                throw new ArgumentNullException(nameof(providerFactory));
            }

            _providerFactory = providerFactory;

            ExportSpectraTypes = new ObservableCollection<SpectraType>(spectraTypes);
            SelectedSpectraType = ExportSpectraTypes.FirstOrDefault();

            ExportSpectraFileFormats = new ObservableCollection<SpectraFormat>(spectraFormats);
            SelectedFileFormat = ExportSpectraFileFormats.FirstOrDefault();
        }

        public ObservableCollection<SpectraType> ExportSpectraTypes { get; }
        public SpectraType SelectedSpectraType {
            get => _selectedSpectraType;
            set => SetProperty(ref _selectedSpectraType, value);
        }
        private SpectraType _selectedSpectraType;
        public ObservableCollection<SpectraFormat> ExportSpectraFileFormats { get; }
        public SpectraFormat SelectedFileFormat {
            get => _selectedFileFormat;
            set => SetProperty(ref _selectedFileFormat, value);
        }
        private SpectraFormat _selectedFileFormat;

        public int IsotopeExportMax {
            get => _isotopeExportMax;
            set => SetProperty(ref _isotopeExportMax, value);
        }
        private int _isotopeExportMax = 2;

        public void Export(string destinationFolder, AnalysisFileBeanModel fileBeanModel) {
            var filename = Path.Combine(destinationFolder, fileBeanModel.AnalysisFileName + "." + SelectedFileFormat.Format);
            using (var stream = File.Open(filename, FileMode.Create, FileAccess.Write)) {
                var provider = _providerFactory.Create(fileBeanModel);
                var features = ChromatogramPeakFeatureCollection.LoadAsync(fileBeanModel.PeakAreaBeanInformationFilePath).Result;
                SelectedFileFormat.Export(stream, features.Items, provider, SelectedSpectraType, fileBeanModel);
            }
        }
    }

    internal sealed class SpectraFormat
    {
        public SpectraFormat(ExportSpectraFileFormat format, IAnalysisExporter exporter) {
            Format = format;
            Exporter = exporter;
        }

        public ExportSpectraFileFormat Format { get; }
        public IAnalysisExporter Exporter { get; }

        public void Export(Stream stream, IReadOnlyList<ChromatogramPeakFeature> features, IDataProvider provider, SpectraType spectraType, AnalysisFileBeanModel fileBeanModel) {
            var msdecs = spectraType.GetSpectra(fileBeanModel);
            Exporter.Export(stream, features, msdecs, provider, spectraType.Accessor, fileBeanModel.File);
        }
    }

    internal sealed class SpectraType
    {
        public SpectraType(ExportspectraType type, IAnalysisMetadataAccessor accessor) {
            Type = type;
            Accessor = accessor;
        }

        public ExportspectraType Type { get; }
        public IAnalysisMetadataAccessor Accessor { get; }

        public IReadOnlyList<MSDecResult> GetSpectra(AnalysisFileBeanModel file) {
            return MsdecResultsReader.ReadMSDecResults(file.DeconvolutionFilePath, out _, out _);
        }
    }
}
