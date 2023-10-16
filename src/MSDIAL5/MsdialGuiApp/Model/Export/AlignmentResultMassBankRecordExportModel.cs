using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentResultMassBankRecordExportModel : BindableBase, IAlignmentResultExportModel
    {
        private readonly AlignmentPeakSpotSupplyer _supplyer;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;
        private readonly ProjectBaseParameter _parameter;

        public AlignmentResultMassBankRecordExportModel(AlignmentPeakSpotSupplyer supplyer, IMatchResultEvaluator<MsScanMatchResult> evaluator, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ProjectBaseParameter parameter) {
            _supplyer = supplyer;
            _evaluator = evaluator;
            _refer = refer;
            _parameter = parameter;
        }

        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        int IAlignmentResultExportModel.CountExportFiles(AlignmentFileBeanModel alignmentFile) {
            if (!IsSelected) {
                return 0;
            }
            var peaks = _supplyer.Supply(alignmentFile, default);
            var tasks = new List<Task<bool>>();
            foreach (var peak in peaks) {
                if (!peak.IsReferenceMatched(_evaluator)) {
                    continue;
                }
                tasks.Add(alignmentFile.LoadMSDecResultByIndexAsync(peak.MasterAlignmentID).ContinueWith(t => t.Result.Spectrum.Count >= 2));
            }
            Task.WaitAll(tasks.ToArray());
            return tasks.Count(t => t.Result);
        }

        void IAlignmentResultExportModel.Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
            if (!IsSelected) {
                return;
            }
            var splash = new NSSplash.Splash();
            var handler = new MassBankRecordHandler(_parameter.IonMode, _parameter.InstrumentType, scan => splash.splashIt(new NSSplash.impl.MSSpectrum(string.Join(" ", scan.Spectrum.Select(p => $"{p.Mass}:{p.Intensity}")))));
            var peaks = _supplyer.Supply(alignmentFile, default);
            foreach (var peak in peaks) {
                if (!peak.IsReferenceMatched(_evaluator) || !(_refer.Refer(peak.MatchResults.Representative) is MoleculeMsReference reference)) {
                    continue;
                }

                string accession = handler.GetAccession(peak);
                using (var stream = File.Open(Path.Combine(exportDirectory, accession + ".txt"), FileMode.Create)) {
                    notification.Invoke($"Exporting {accession}");
                    handler.WriteRecord(stream, peak, reference, alignmentFile.LoadMSDecResultByIndexAsync(peak.MasterAlignmentID).Result, peak);
                }
            }
        }
    }
}
