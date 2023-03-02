using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentResultExportModel : DisposableModelBase
    {
        private readonly DataExportBaseParameter _dataExportParameter;

        public AlignmentResultExportModel(IEnumerable<IAlignmentResultExportModel> exportGroups, AlignmentFileBean alignmentFile, IReadOnlyList<AlignmentFileBean> alignmentFiles, AlignmentPeakSpotSupplyer peakSpotSupplyer, DataExportBaseParameter dataExportParameter) {
            AlignmentFiles = alignmentFiles;
            PeakSpotSupplyer = peakSpotSupplyer ?? throw new ArgumentNullException(nameof(peakSpotSupplyer));
            _dataExportParameter = dataExportParameter;
            AlignmentFile = alignmentFile;
            var groups = new ObservableCollection<IAlignmentResultExportModel>(exportGroups);
            Groups = new ReadOnlyObservableCollection<IAlignmentResultExportModel>(groups);
            ExportDirectory = dataExportParameter.ExportFolderPath;
        }

        public string ExportDirectory {
            get => _exportDirectory;
            set => SetProperty(ref _exportDirectory, value);
        }
        private string _exportDirectory;

        public AlignmentFileBean AlignmentFile {
            get => _alignmentFile;
            set => SetProperty(ref _alignmentFile, value);
        }
        private AlignmentFileBean _alignmentFile;

        public IReadOnlyList<AlignmentFileBean> AlignmentFiles { get; }
        public AlignmentPeakSpotSupplyer PeakSpotSupplyer { get; }
        public ReadOnlyObservableCollection<IAlignmentResultExportModel> Groups { get; }

        public Task ExportAlignmentResultAsync(IMessageBroker broker) {
            return Task.Run(() => {
                var task = TaskNotification.Start($"Exporting {AlignmentFile.FileName}");
                broker.Publish(task);

                var numExportFile = (double)Groups.Sum(group => group.CountExportFiles());
                var count = 0;
                void notify(string file) {
                    broker.Publish(task.Progress(Interlocked.Increment(ref count) / numExportFile, file));
                }
                foreach (var group in Groups) {
                    group.Export(AlignmentFile, ExportDirectory, notify);
                }
                _dataExportParameter.ExportFolderPath = ExportDirectory;

                broker.Publish(task.End());
            });
        }
    }
}
