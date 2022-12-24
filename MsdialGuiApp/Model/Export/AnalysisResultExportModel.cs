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
    internal sealed class AnalysisResultExportModel : BindableBase
    {
        private readonly IDataProviderFactory<AnalysisFileBeanModel> _providerFactory;
        private readonly object _syncObject = new object();

        public AnalysisResultExportModel(
            AnalysisFileBeanModelCollection files,
            IEnumerable<SpectraType> spectraTypes,
            IEnumerable<SpectraFormat> spectraFormats,
            IDataProviderFactory<AnalysisFileBeanModel> providerFactory) {
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

            _providerFactory = providerFactory;

            UnSelectedFiles = new ObservableCollection<AnalysisFileBeanModel>(files.AnalysisFiles);
            SelectedFiles = new ObservableCollection<AnalysisFileBeanModel>();

            ExportSpectraTypes = new ObservableCollection<SpectraType>(spectraTypes);
            SelectedSpectraType = ExportSpectraTypes.FirstOrDefault();

            ExportSpectraFileFormats = new ObservableCollection<SpectraFormat>(spectraFormats);
            SelectedFileFormat = ExportSpectraFileFormats.FirstOrDefault();
        }

        public void Export() {
            foreach (var file in SelectedFiles) {
                var provider = _providerFactory.Create(file);
                var filename = Path.Combine(DestinationFolder, file.AnalysisFileName + "." + SelectedFileFormat.Format);
                using (var stream = File.Open(filename, FileMode.Create, FileAccess.Write)) {
                    var features = ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath).Result;
                    SelectedFileFormat.Export(stream, features.Items, provider, SelectedSpectraType, file);
                }
            }
        }

        public ObservableCollection<AnalysisFileBeanModel> SelectedFiles { get; }
        public ObservableCollection<AnalysisFileBeanModel> UnSelectedFiles { get; }

        public void Selects(IEnumerable<AnalysisFileBeanModel> files) {
            lock (_syncObject) {
                foreach (var file in files) {
                    if (UnSelectedFiles.Contains(file)) {
                        UnSelectedFiles.Remove(file);
                        SelectedFiles.Add(file);
                    }
                }
            }
        }
 
        public void UnSelects(IEnumerable<AnalysisFileBeanModel> files) {
            lock (_syncObject) {
                foreach (var file in files) {
                    if (SelectedFiles.Contains(file)) {
                        SelectedFiles.Remove(file);
                        UnSelectedFiles.Add(file);
                    }
                }
            }
        }

        public string DestinationFolder {
            get => _destinationFolder;
            set => SetProperty(ref _destinationFolder, value);
        }
        private string _destinationFolder;

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
            Exporter.Export(stream, features, msdecs, provider, spectraType.Accessor);
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
