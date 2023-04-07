using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AnalysisResultExportModel : BindableBase
    {
        private readonly object _syncObject = new object();

        public AnalysisResultExportModel(
            AnalysisFileBeanModelCollection files,
            params IMsdialAnalysisExport[] msdialAnalysisExports) {
            if (files is null) {
                throw new ArgumentNullException(nameof(files));
            }

            if (msdialAnalysisExports is null) {
                throw new ArgumentNullException(nameof(msdialAnalysisExports));
            }

            AnalysisExports = msdialAnalysisExports;
            UnSelectedFiles = new ObservableCollection<AnalysisFileBeanModel>(files.AnalysisFiles);
            SelectedFiles = new ObservableCollection<AnalysisFileBeanModel>();
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
        private string _destinationFolder;

        public void Export() {
            foreach (var exporter in AnalysisExports) {
                foreach (var file in SelectedFiles) {
                    exporter.Export(DestinationFolder, file);
                }
            }
        }
    }
}
