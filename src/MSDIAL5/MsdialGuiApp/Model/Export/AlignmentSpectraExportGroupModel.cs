using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentSpectraExportGroupModel : BindableBase, IAlignmentResultExportModel
    {
        public AlignmentSpectraExportGroupModel(IEnumerable<ExportspectraType> spectraTypes, AlignmentPeakSpotSupplyer peakSpotSupplyer, params AlignmentSpectraExportFormat[] formats) {
            SpectraTypes = new ObservableCollection<ExportspectraType>(spectraTypes);
            Formats = new ObservableCollection<AlignmentSpectraExportFormat>(formats);
            _peakSpotSupplyer = peakSpotSupplyer ?? throw new ArgumentNullException(nameof(peakSpotSupplyer));
        }

        public ExportspectraType SpectraType {
            get => _spectraType;
            set => SetProperty(ref _spectraType, value);
        }
        private ExportspectraType _spectraType = ExportspectraType.deconvoluted;
        private readonly AlignmentPeakSpotSupplyer _peakSpotSupplyer;

        public ObservableCollection<ExportspectraType> SpectraTypes { get; }

        public ObservableCollection<AlignmentSpectraExportFormat> Formats { get; }

        public bool ExportIndividually {
            get => _exportIndividually;
            set => SetProperty(ref _exportIndividually, value);
        }
        private bool _exportIndividually = false;

        public int CountExportFiles(AlignmentFileBeanModel alignmentFile) {
            return Formats.Count(f => f.IsSelected);
        }

        public void Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
            var dt = DateTime.Now;
            var cts = new CancellationTokenSource();
            var lazySpots = new Lazy<IReadOnlyList<AlignmentSpotProperty>>(() => _peakSpotSupplyer.Supply(alignmentFile, cts.Token));
            var msdecResults = alignmentFile.LoadMSDecResults();
            if (ExportIndividually) {
                foreach (var format in Formats) {
                    notification?.Invoke($"Exporting {((IFileBean)alignmentFile).FileName}");
                    foreach (var spot in lazySpots.Value) {
                        var outNameTemplate = $"{{0}}_AlignmentID {spot.MasterAlignmentID}_{spot.TimesCenter.Value:f2}_{spot.MassCenter:f4}_{dt:yyyy_MM_dd_HH_mm_ss}_{((IFileBean)alignmentFile).FileName}.{{1}}";
                        if (format.IsSelected) {
                            format.Export(new[] { spot }, msdecResults, outNameTemplate, exportDirectory, notification: null);
                        }
                    }
                }
            }
            else {
                var outNameTemplate = $"{{0}}_{dt:yyyy_MM_dd_HH_mm_ss}_{((IFileBean)alignmentFile).FileName}.{{1}}";
                foreach (var format in Formats) {
                    if (format.IsSelected) {
                        format.Export(lazySpots.Value, msdecResults, outNameTemplate, exportDirectory, notification);
                    }
                }
            }
            cts.Cancel();
        }
    }
}
