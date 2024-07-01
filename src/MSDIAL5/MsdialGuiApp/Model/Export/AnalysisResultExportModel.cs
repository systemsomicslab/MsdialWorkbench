using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AnalysisResultExportModel : BindableBase
    {
        private readonly object _syncObject = new object();
        private readonly IMessageBroker _broker;

        public AnalysisResultExportModel(AnalysisFileBeanModelCollection files, string initialFolder, IMessageBroker broker, params IMsdialAnalysisExport[] msdialAnalysisExports) {
            if (files is null) {
                throw new ArgumentNullException(nameof(files));
            }

            if (msdialAnalysisExports is null) {
                throw new ArgumentNullException(nameof(msdialAnalysisExports));
            }
            _broker = broker;
            AnalysisExports = msdialAnalysisExports;
            UnSelectedFiles = new ObservableCollection<AnalysisFileBeanModel>(files.AnalysisFiles);
            SelectedFiles = new ObservableCollection<AnalysisFileBeanModel>();

            if (string.IsNullOrEmpty(DestinationFolder)) {
                DestinationFolder = initialFolder;
            }
        }

        public IMsdialAnalysisExport[] AnalysisExports { get; }

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
        private string _destinationFolder = string.Empty;

        public void Export() {
            var publisher = new TaskProgressPublisher(_broker, "Exporting analysis result");
            using (publisher.Start()) {
                double all = SelectedFiles.Count;
                var counter = 0;
                foreach (var file in SelectedFiles) {
                    publisher.Progress(counter++ / all, $"Exporting {file.AnalysisFileName}");
                    foreach (var exporter in AnalysisExports) {
                        exporter.Export(DestinationFolder, file);
                    }
                }
            }
        }
    }
}
