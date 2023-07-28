using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentResultExportModel : BindableBase
    {
        private readonly DataExportBaseParameter _dataExportParameter;

        public AlignmentResultExportModel(IEnumerable<IAlignmentResultExportModel> exportGroups, AlignmentFilesForExport alignmentFilesForExport, AlignmentPeakSpotSupplyer peakSpotSupplyer, DataExportBaseParameter dataExportParameter) {
            AlignmentFilesForExport = alignmentFilesForExport;
            PeakSpotSupplyer = peakSpotSupplyer ?? throw new ArgumentNullException(nameof(peakSpotSupplyer));
            _dataExportParameter = dataExportParameter;
            var groups = new ObservableCollection<IAlignmentResultExportModel>(exportGroups);
            Groups = new ReadOnlyObservableCollection<IAlignmentResultExportModel>(groups);
            ExportDirectory = dataExportParameter.ExportFolderPath;
        }

        public string ExportDirectory {
            get => _exportDirectory;
            set => SetProperty(ref _exportDirectory, value);
        }
        private string _exportDirectory;

        public AlignmentFilesForExport AlignmentFilesForExport { get; }

        public AlignmentPeakSpotSupplyer PeakSpotSupplyer { get; }
        public ReadOnlyObservableCollection<IAlignmentResultExportModel> Groups { get; }

        public Task ExportAlignmentResultAsync(IMessageBroker broker) {
            return Task.Run(() => {
                var task = TaskNotification.Start($"Exporting {AlignmentFilesForExport.SelectedFile.FileName}");
                broker.Publish(task);

                var numExportFile = (double)Groups.Sum(group => group.CountExportFiles());
                var count = 0;
                void notify(string file) {
                    broker.Publish(task.Progress(Interlocked.Increment(ref count) / numExportFile, file));
                }
                foreach (var group in Groups) {
                    group.Export(AlignmentFilesForExport.SelectedFile, ExportDirectory, notify);
                }
                _dataExportParameter.ExportFolderPath = ExportDirectory;

                broker.Publish(task.End());
            });
        }
    }
}
