using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class MsdialAnalysisTableExportModel : BindableBase, IMsdialAnalysisExport
    {
        private readonly IMessageBroker _broker;

        public MsdialAnalysisTableExportModel(IEnumerable<ISpectraType> spectraTypes, IEnumerable<SpectraFormat> spectraFormats, IMessageBroker? broker = null) {
            if (spectraTypes is null) {
                throw new ArgumentNullException(nameof(spectraTypes));
            }

            if (spectraFormats is null) {
                throw new ArgumentNullException(nameof(spectraFormats));
            }

            ExportSpectraTypes = new ObservableCollection<ISpectraType>(spectraTypes);
            SelectedSpectraType = ExportSpectraTypes.FirstOrDefault();

            ExportSpectraFileFormats = new ObservableCollection<SpectraFormat>(spectraFormats);
            SelectedFileFormat = ExportSpectraFileFormats.FirstOrDefault();
            _broker = broker ?? MessageBroker.Default;
        }

        public bool ShouldExport {
            get => _shoudlExport;
            set => SetProperty(ref _shoudlExport, value);
        }
        private bool _shoudlExport = true;

        public ObservableCollection<ISpectraType> ExportSpectraTypes { get; }
        public ISpectraType? SelectedSpectraType {
            get => _selectedSpectraType;
            set => SetProperty(ref _selectedSpectraType, value);
        }
        private ISpectraType? _selectedSpectraType;
        public ObservableCollection<SpectraFormat> ExportSpectraFileFormats { get; }
        public SpectraFormat? SelectedFileFormat {
            get => _selectedFileFormat;
            set => SetProperty(ref _selectedFileFormat, value);
        }
        private SpectraFormat? _selectedFileFormat;

        public int IsotopeExportMax {
            get => _isotopeExportMax;
            set => SetProperty(ref _isotopeExportMax, value);
        }
        private int _isotopeExportMax = 2;

        public bool TrimToExcelLimit {
            get => _trimToExcelLimit;
            set => SetProperty(ref _trimToExcelLimit, value);
        }
        private bool _trimToExcelLimit = true;

        /// <summary>
        /// Exports the analysis file result to the specified destination folder.
        /// </summary>
        /// <param name="destinationFolder">The destination folder to export the file to.</param>
        /// <param name="fileBeanModel">The analysis file model.</param>
        public void Export(string destinationFolder, AnalysisFileBeanModel fileBeanModel) {
            if (!ShouldExport || SelectedFileFormat is null || SelectedSpectraType is null) {
                return;
            }
            var filename = Path.Combine(destinationFolder, fileBeanModel.AnalysisFileName + "." + SelectedFileFormat.Format);
            try {
                using var stream = File.Open(filename, FileMode.Create, FileAccess.Write);
                SelectedSpectraType.Export(stream, fileBeanModel.File, SelectedFileFormat.ExporterFactory, new ExportStyle { TrimToExcelLimit = TrimToExcelLimit });
            }
            catch (IOException ex) {
                var request = new ShortMessageRequest("Failed to export file: " + ex.Message);
                _broker.Publish(request);
            }
        }
    }

    internal sealed class SpectraFormat
    {
        public SpectraFormat(ExportSpectraFileFormat format, AnalysisCSVExporterFactory exporterFactory) {
            Format = format;
            ExporterFactory = exporterFactory;
        }

        public ExportSpectraFileFormat Format { get; }

        public AnalysisCSVExporterFactory ExporterFactory { get; }
    }

    interface ISpectraType
    {
        void Export(Stream stream, AnalysisFileBean file, AnalysisCSVExporterFactory exporterFactory, ExportStyle exportStyle);
    }

    internal sealed class SpectraType : ISpectraType
    {
        private readonly IDataProviderFactory<AnalysisFileBean> _providerFactory;

        public SpectraType(ExportspectraType type, IAnalysisMetadataAccessor accessor, IDataProviderFactory<AnalysisFileBean> providerFactory) {
            Type = type;
            Accessor = accessor;
            _providerFactory = providerFactory;
        }

        public ExportspectraType Type { get; } // TODO: Account this property for spectra source
        public IAnalysisMetadataAccessor Accessor { get; }

        public void Export(Stream stream, AnalysisFileBean file, AnalysisCSVExporterFactory exporterFactory, ExportStyle exportStyle) {
            var peaks = ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath).Result;
            peaks = peaks.Flatten();
            exporterFactory.CreateExporter(_providerFactory, Accessor).Export(stream, file, peaks, exportStyle);
        }

        //public IReadOnlyList<MSDecResult> GetSpectra(AnalysisFileBeanModel file) {
        //    switch (Type) {
        //        //case ExportspectraType.centroid:
        //        //case ExportspectraType.profile:
        //        case ExportspectraType.deconvoluted:
        //        default:
        //            return file.MSDecLoader.LoadMSDecResults();
        //    }
        //}
    }

    internal sealed class SpectraType<T> : ISpectraType
    {
        private readonly Func<AnalysisFileBean, IReadOnlyList<T>> _dataLoader;

        public SpectraType(ExportspectraType type, IAnalysisMetadataAccessor<T> accessor, Func<AnalysisFileBean, IReadOnlyList<T>> dataLoader) {
            Type = type;
            Accessor = accessor;
            _dataLoader = dataLoader;
        }

        public ExportspectraType Type { get; } // TODO: Account this property for spectra source
        public IAnalysisMetadataAccessor<T> Accessor { get; }

        public void Export(Stream stream, AnalysisFileBean file, AnalysisCSVExporterFactory exporterFactory, ExportStyle exportStyle) {
            var data = _dataLoader.Invoke(file);
            exporterFactory.CreateExporter(Accessor).Export(stream, file, data, exportStyle);
        }
    }
}
