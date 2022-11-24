using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentResultExportModel : DisposableModelBase
    {
        private readonly IMsdialDataStorage<ParameterBase> _container;

        public AlignmentResultExportModel(AlignmentFileBean alignmentFile, IReadOnlyList<AlignmentFileBean> alignmentFiles, IMsdialDataStorage<ParameterBase> container, IEnumerable<AlignmentExportGroupModel> exportGroups) {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            AlignmentFiles = alignmentFiles;
            AlignmentFile = alignmentFile;
            var groups = new ObservableCollection<AlignmentExportGroupModel>(exportGroups);
            Groups = new ReadOnlyObservableCollection<AlignmentExportGroupModel>(groups);
        }

        public string ExportDirectory {
            get => _exportDirectory;
            set => SetProperty(ref _exportDirectory, value);
        }
        private string _exportDirectory = string.Empty;

        public AlignmentFileBean AlignmentFile {
            get => _alignmentFile;
            set => SetProperty(ref _alignmentFile, value);
        }
        private AlignmentFileBean _alignmentFile;

        public IReadOnlyList<AlignmentFileBean> AlignmentFiles { get; }

        public ReadOnlyObservableCollection<AlignmentExportGroupModel> Groups { get; }

        public void ExportAlignmentResult(Action<double, string> notification = null) {
            foreach (var group in Groups) {
                group.ExportAlignmentResult(_container, AlignmentFile, ExportDirectory, notification);
            }
        }

        public bool CanExportAlignmentResult() {
            return !(AlignmentFile is null) && Directory.Exists(ExportDirectory);
        }
    }
}
