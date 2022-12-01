using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentResultExportModel : DisposableModelBase
    {
        public AlignmentResultExportModel(IEnumerable<IAlignmentResultExportModel> exportGroups, AlignmentFileBean alignmentFile, IReadOnlyList<AlignmentFileBean> alignmentFiles, AlignmentPeakSpotSupplyer peakSpotSupplyer) {
            AlignmentFiles = alignmentFiles;
            PeakSpotSupplyer = peakSpotSupplyer ?? throw new ArgumentNullException(nameof(peakSpotSupplyer));
            AlignmentFile = alignmentFile;
            var groups = new ObservableCollection<IAlignmentResultExportModel>(exportGroups);
            Groups = new ReadOnlyObservableCollection<IAlignmentResultExportModel>(groups);
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
        public AlignmentPeakSpotSupplyer PeakSpotSupplyer { get; }
        public ReadOnlyObservableCollection<IAlignmentResultExportModel> Groups { get; }

        public void ExportAlignmentResult(Action<double, string> notification = null) {
            var numExportFile = (double)Groups.Sum(group => group.CountExportFiles());
            var count = 0;
            void notify(string file) {
                notification?.Invoke(Interlocked.Increment(ref count) / numExportFile, file);
            }
            foreach (var group in Groups) {
                group.Export(AlignmentFile, ExportDirectory, notify);
            }
        }

        public bool CanExportAlignmentResult() {
            return !(AlignmentFile is null) && Directory.Exists(ExportDirectory);
        }
    }
}
