using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentResultMassBankRecordExportModel : BindableBase, IAlignmentResultExportModel
    {
        private readonly AlignmentPeakSpotSupplyer _supplyer;
        private readonly ProjectBaseParameter _parameter;
        private readonly StudyContextModel _studyContextModel;

        public AlignmentResultMassBankRecordExportModel(AlignmentPeakSpotSupplyer supplyer, ProjectBaseParameter parameter, StudyContextModel studyContextModel) {
            _supplyer = supplyer;
            _parameter = parameter;
            _studyContextModel = studyContextModel;
        }

        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        public string ContributorID {
            get => _contributorID;
            set => SetProperty(ref _contributorID, value);
        }
        private string _contributorID = "Xxx";

        int IAlignmentResultExportModel.CountExportFiles(AlignmentFileBeanModel alignmentFile) {
            if (!IsSelected) {
                return 0;
            }
            var peaks = _supplyer.Supply(alignmentFile, default);
            var tasks = peaks.Select(peak => alignmentFile.LoadMSDecResultByIndexAsync(peak.MasterAlignmentID).ContinueWith(t => t.Result is not null && t.Result.Spectrum.Count >= 2)).ToArray();
            Task.WaitAll(tasks);
            return tasks.Count(t => t.Result);
        }

        void IAlignmentResultExportModel.Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
            if (!IsSelected) {
                return;
            }
            var splash = new NSSplash.Splash();
            var handler = new MassBankRecordHandler(_parameter.IonMode, _studyContextModel.InstrumentType, scan => scan.Spectrum.IsEmptyOrNull() ? "NA" : splash.splashIt(new NSSplash.impl.MSSpectrum(string.Join(" ", scan.Spectrum.Select(p => $"{p.Mass}:{p.Intensity}")))))
            {
                Software = "MSDIAL",
                Authors = _studyContextModel.Authors,
                License = _studyContextModel.License,
                Instrument = _studyContextModel.Instrument,
                ContributorIdentifier = ContributorID,
            };
            var peaks = _supplyer.Supply(alignmentFile, default);
            foreach (var peak in peaks) {
                string accession = handler.GetAccession(peak);
                MsdialCore.MSDec.MSDecResult? scan = alignmentFile.LoadMSDecResultByIndexAsync(peak.MasterAlignmentID).Result;
                if (scan is null) {
                    continue;
                }
                notification.Invoke($"Exporting {accession}");
                using var stream = File.Open(Path.Combine(exportDirectory, accession + ".txt"), FileMode.Create);
                handler.WriteRecord(stream, peak, peak, scan, peak);
            }
        }
    }
}
