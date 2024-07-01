using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class ExportMrmprobsModel : BindableBase {
        public ExportMrmprobsModel(IExportMrmprobsUsecase exportUsecase) {
            ExportUsecase = exportUsecase;
            ExportParameter = new MrmprobsExportParameterModel(exportUsecase.ExportParameter);
        }

        public bool Copy {
            get => _copy;
            set => SetProperty(ref _copy, value);
        }
        private bool _copy;

        public string ExportFilePath {
            get => _exportFilePath;
            set => SetProperty(ref _exportFilePath, value);
        }
        private string _exportFilePath = string.Empty;

        public IExportMrmprobsUsecase ExportUsecase { get; }

        public MrmprobsExportParameterModel ExportParameter { get; }

        public async Task ExportAsync(CancellationToken token) {
            ExportParameter.Commit();

            if (Copy) {
                var stream = new MemoryStream();
                if (ExportParameter.MpIsFocusedSpotOutput) {
                    await ExportUsecase.ExportAsync(stream, token);
                }
                else {
                    await ExportUsecase.BatchExportAsync(stream, token);
                }
                Clipboard.SetText(Encoding.ASCII.GetString(stream.ToArray()), TextDataFormat.Text);
            }
            else {
                using var stream = File.Open(ExportFilePath, FileMode.Create, FileAccess.Write);
                if (ExportParameter.MpIsFocusedSpotOutput) {
                    await ExportUsecase.ExportAsync(stream, token).ConfigureAwait(false);
                }
                else {
                    await ExportUsecase.BatchExportAsync(stream, token).ConfigureAwait(false);
                }
            }
        }
    }

    interface IExportMrmprobsUsecase {
        Task ExportAsync(Stream stream, CancellationToken token);
        Task BatchExportAsync(Stream stream, CancellationToken token);

        MrmprobsExportBaseParameter ExportParameter { get; }

        CompoundSearcherCollection CompoundSearchers { get; }
        CompoundSearcher? SelectedCompoundSearcher { get; set; }
    }

    internal sealed class AlignmentSpotExportMrmprobsUsecase : BindableBase, IExportMrmprobsUsecase
    {
        private readonly AlignmentSpotSource _spots;
        private readonly AlignmentFileBeanModel _alignmentFile;
        private readonly EsiMrmprobsExporter _exporter;
        private readonly IReadOnlyReactiveProperty<AlignmentSpotPropertyModel?> _target;
        private readonly IMessageBroker _broker;

        public AlignmentSpotExportMrmprobsUsecase(
            MrmprobsExportBaseParameter parameter,
            AlignmentSpotSource spots,
            AlignmentFileBeanModel alignmentFile,
            CompoundSearcherCollection compoundSearchers,
            EsiMrmprobsExporter exporter,
            IReadOnlyReactiveProperty<AlignmentSpotPropertyModel?> target,
            IMessageBroker? broker = null)
        {
            ExportParameter = parameter;
            _spots = spots;
            _alignmentFile = alignmentFile;
            CompoundSearchers = compoundSearchers;
            _selectedCompoundSearcher = compoundSearchers.Items.FirstOrDefault();
            _exporter = exporter;
            _target = target;
            _broker = broker ?? MessageBroker.Default;
        }

        public MrmprobsExportBaseParameter ExportParameter { get; }

        public CompoundSearcherCollection CompoundSearchers { get; }

        public CompoundSearcher? SelectedCompoundSearcher {
            get => _selectedCompoundSearcher;
            set => SetProperty(ref _selectedCompoundSearcher, value);
        }
        private CompoundSearcher? _selectedCompoundSearcher;

        public async Task BatchExportAsync(Stream stream, CancellationToken token) {
            var loader = _alignmentFile.CreateTemporaryMSDecLoader();
            if (_spots.Spots is null) {
                _broker.Publish(new ShortMessageRequest("Failed to get peak spots."));
                return;
            }
            var spots = _spots.Spots.Items.Select(s => s.innerModel).ToArray();
            if (!ExportParameter.MpIsReferenceBaseOutput) {
                await Task.Run(() => _exporter.ExportExperimentalMsms(stream, spots, loader, ExportParameter), token).ConfigureAwait(false);
            }
            else if (ExportParameter.MpIsExportOtherCandidates && SelectedCompoundSearcher != null) {
                var queryFactory = SelectedCompoundSearcher.QueryFactory;
                var searchParameter = queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff / 100;
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, spots, ExportParameter, queryFactory, loader, searchParameter), token).ConfigureAwait(false);
            }
            else {
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, spots, ExportParameter), token).ConfigureAwait(false);
            }
        }

        public async Task ExportAsync(Stream stream, CancellationToken token) {
            if (_target.Value is not AlignmentSpotPropertyModel spot) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return;
            }
            var loader = _alignmentFile.CreateTemporaryMSDecLoader();
            if (!ExportParameter.MpIsReferenceBaseOutput) {
                var msdec = loader.LoadMSDecResult(spot.innerModel.MSDecResultIdUsed);
                await Task.Run(() => _exporter.ExportExperimentalMsms(stream, spot.innerModel, msdec, ExportParameter), token).ConfigureAwait(false);
            }
            else if (ExportParameter.MpIsExportOtherCandidates && SelectedCompoundSearcher != null) {
                var queryFactory = SelectedCompoundSearcher.QueryFactory;
                var searchParameter = queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff / 100;
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, spot.innerModel, ExportParameter, queryFactory, loader, searchParameter), token).ConfigureAwait(false);
            }
            else {
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, spot.innerModel, ExportParameter), token).ConfigureAwait(false);
            }
        }
    }

    internal sealed class ChromatogramPeakExportMrmprobsUsecase : BindableBase, IExportMrmprobsUsecase
    {
        private readonly IReadOnlyList<ChromatogramPeakFeatureModel> _peaks;
        private readonly AnalysisFileBeanModel _analysisFile;
        private readonly EsiMrmprobsExporter _exporter;
        private readonly IReadOnlyReactiveProperty<ChromatogramPeakFeatureModel?> _target;
        private readonly IMessageBroker _broker;

        public ChromatogramPeakExportMrmprobsUsecase(
            MrmprobsExportBaseParameter parameter,
            IReadOnlyList<ChromatogramPeakFeatureModel> peaks,
            AnalysisFileBeanModel analysisFile,
            CompoundSearcherCollection compoundSearchers,
            EsiMrmprobsExporter exporter,
            IReadOnlyReactiveProperty<ChromatogramPeakFeatureModel?> target,
            IMessageBroker? broker = null)
        {
            ExportParameter = parameter;
            _peaks = peaks;
            _analysisFile = analysisFile;
            CompoundSearchers = compoundSearchers;
            SelectedCompoundSearcher = compoundSearchers.Items.FirstOrDefault();
            _exporter = exporter;
            _target = target;
            _broker = broker ?? MessageBroker.Default;
        }

        public MrmprobsExportBaseParameter ExportParameter { get; }

        public CompoundSearcherCollection CompoundSearchers { get; }

        public CompoundSearcher? SelectedCompoundSearcher {
            get => _selectedCompoundSearcher;
            set => SetProperty(ref _selectedCompoundSearcher, value);
        }
        private CompoundSearcher? _selectedCompoundSearcher;

        public async Task BatchExportAsync(Stream stream, CancellationToken token) {
            var spots = _peaks.Select(s => s.InnerModel).ToArray();
            if (!ExportParameter.MpIsReferenceBaseOutput) {
                await Task.Run(() => _exporter.ExportExperimentalMsms(stream, spots, _analysisFile.MSDecLoader, ExportParameter), token).ConfigureAwait(false);
            }
            else if (ExportParameter.MpIsExportOtherCandidates && SelectedCompoundSearcher != null) {
                var queryFactory = SelectedCompoundSearcher.QueryFactory;
                var searchParameter = queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff;
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, spots, ExportParameter, queryFactory, _analysisFile.MSDecLoader, searchParameter), token).ConfigureAwait(false);
            }
            else {
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, spots, ExportParameter), token).ConfigureAwait(false);
            }
        }

        public async Task ExportAsync(Stream stream, CancellationToken token) {
            if (_target.Value is not ChromatogramPeakFeatureModel peak) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return;
            }
            if (!ExportParameter.MpIsReferenceBaseOutput) {
                var msdec = _analysisFile.MSDecLoader.LoadMSDecResult(_target.Value.InnerModel.MSDecResultIdUsed);
                await Task.Run(() => _exporter.ExportExperimentalMsms(stream, _target.Value.InnerModel, msdec, ExportParameter), token).ConfigureAwait(false);
            }
            else if (ExportParameter.MpIsExportOtherCandidates && SelectedCompoundSearcher != null) {
                var queryFactory = SelectedCompoundSearcher.QueryFactory;
                var searchParameter = queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff;
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, _target.Value.InnerModel, ExportParameter, queryFactory, _analysisFile.MSDecLoader, searchParameter), token).ConfigureAwait(false);
            }
            else {
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, _target.Value.InnerModel, ExportParameter), token).ConfigureAwait(false);
            }
        }
    }
}
