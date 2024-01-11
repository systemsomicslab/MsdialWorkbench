using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
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
        private readonly IExportMrmprobsUsecase _exportUsecase;

        public ExportMrmprobsModel(IExportMrmprobsUsecase exportUsecase) {
            _exportUsecase = exportUsecase;
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
        private string _exportFilePath;

        public MrmprobsExportParameterModel ExportParameter { get; }

        public async Task ExportAsync(CancellationToken token) {
            ExportParameter.Commit();

            if (Copy) {
                var stream = new MemoryStream();
                if (ExportParameter.MpIsFocusedSpotOutput) {
                    await _exportUsecase.ExportAsync(stream, token).ConfigureAwait(false);
                }
                else {
                    await _exportUsecase.BatchExportAsync(stream, token).ConfigureAwait(false);
                }
                Clipboard.SetText(Encoding.ASCII.GetString(stream.ToArray()), TextDataFormat.Text);
            }
            else {
                using var stream = File.Open(ExportFilePath, FileMode.Create, FileAccess.Write);
                if (ExportParameter.MpIsFocusedSpotOutput) {
                    await _exportUsecase.ExportAsync(stream, token).ConfigureAwait(false);
                }
                else {
                    await _exportUsecase.BatchExportAsync(stream, token).ConfigureAwait(false);
                }
            }
        }
    }

    interface IExportMrmprobsUsecase {
        Task ExportAsync(Stream stream, CancellationToken token);
        Task BatchExportAsync(Stream stream, CancellationToken token);

        MrmprobsExportBaseParameter ExportParameter { get; }
    }

    internal sealed class AlignmentSpotExportMrmprobsUsecase : IExportMrmprobsUsecase
    {
        private readonly AlignmentSpotSource _spots;
        private readonly AlignmentFileBeanModel _alignmentFile;
        private readonly IAnnotationQueryFactory<MsScanMatchResult> _queryFactory;
        private readonly EsiMrmprobsExporter _exporter;
        private readonly IReadOnlyReactiveProperty<AlignmentSpotPropertyModel> _target;

        public AlignmentSpotExportMrmprobsUsecase(
            MrmprobsExportBaseParameter parameter,
            AlignmentSpotSource spots,
            AlignmentFileBeanModel alignmentFile,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            EsiMrmprobsExporter exporter,
            IReadOnlyReactiveProperty<AlignmentSpotPropertyModel> target)
        {
            ExportParameter = parameter;
            _spots = spots;
            _alignmentFile = alignmentFile;
            _queryFactory = queryFactory;
            _exporter = exporter;
            _target = target;
        }

        public MrmprobsExportBaseParameter ExportParameter { get; }

        public async Task BatchExportAsync(Stream stream, CancellationToken token) {
            var loader = _alignmentFile.CreateTemporaryMSDecLoader();
            var spots = _spots.Spots.Items.Select(s => s.innerModel).ToArray();
            if (ExportParameter.MpIsFocusedSpotOutput) {
                await Task.Run(() => _exporter.ExportExperimentalMsms(stream, spots, loader, ExportParameter), token).ConfigureAwait(false);
            }
            else if (ExportParameter.MpIsExportOtherCandidates) {
                var searchParameter = _queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff;
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, spots, ExportParameter, _queryFactory, loader, searchParameter), token).ConfigureAwait(false);
            }
            else {
                var searchParameter = _queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff;
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, spots, ExportParameter), token).ConfigureAwait(false);
            }
        }

        public async Task ExportAsync(Stream stream, CancellationToken token) {
            var loader = _alignmentFile.CreateTemporaryMSDecLoader();
            if (ExportParameter.MpIsFocusedSpotOutput) {
                var msdec = loader.LoadMSDecResult(_target.Value.innerModel.MSDecResultIdUsed);
                await Task.Run(() => _exporter.ExportExperimentalMsms(stream, _target.Value.innerModel, msdec, ExportParameter), token).ConfigureAwait(false);
            }
            else if (ExportParameter.MpIsExportOtherCandidates) {
                var searchParameter = _queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff;
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, _target.Value.innerModel, ExportParameter, _queryFactory, loader, searchParameter), token).ConfigureAwait(false);
            }
            else {
                var searchParameter = _queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff;
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, _target.Value.innerModel, ExportParameter), token).ConfigureAwait(false);
            }
        }
    }

    internal sealed class ChromatogramPeakExportMrmprobsUsecase : IExportMrmprobsUsecase
    {
        private readonly IReadOnlyList<ChromatogramPeakFeatureModel> _peaks;
        private readonly AnalysisFileBeanModel _analysisFile;
        private readonly IAnnotationQueryFactory<MsScanMatchResult> _queryFactory;
        private readonly EsiMrmprobsExporter _exporter;
        private readonly IReadOnlyReactiveProperty<ChromatogramPeakFeatureModel> _target;

        public ChromatogramPeakExportMrmprobsUsecase(
            MrmprobsExportBaseParameter parameter,
            IReadOnlyList<ChromatogramPeakFeatureModel> peaks,
            AnalysisFileBeanModel analysisFile,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            EsiMrmprobsExporter exporter,
            IReadOnlyReactiveProperty<ChromatogramPeakFeatureModel> target)
        {
            ExportParameter = parameter;
            _peaks = peaks;
            _analysisFile = analysisFile;
            _queryFactory = queryFactory;
            _exporter = exporter;
            _target = target;
        }

        public MrmprobsExportBaseParameter ExportParameter { get; }

        public async Task BatchExportAsync(Stream stream, CancellationToken token) {
            var spots = _peaks.Select(s => s.InnerModel).ToArray();
            if (ExportParameter.MpIsFocusedSpotOutput) {
                await Task.Run(() => _exporter.ExportExperimentalMsms(stream, spots, _analysisFile.MSDecLoader, ExportParameter), token).ConfigureAwait(false);
            }
            else if (ExportParameter.MpIsExportOtherCandidates) {
                var searchParameter = _queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff;
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, spots, ExportParameter, _queryFactory, _analysisFile.MSDecLoader, searchParameter), token).ConfigureAwait(false);
            }
            else {
                var searchParameter = _queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff;
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, spots, ExportParameter), token).ConfigureAwait(false);
            }
        }

        public async Task ExportAsync(Stream stream, CancellationToken token) {
            if (ExportParameter.MpIsFocusedSpotOutput) {
                var msdec = _analysisFile.MSDecLoader.LoadMSDecResult(_target.Value.InnerModel.MSDecResultIdUsed);
                await Task.Run(() => _exporter.ExportExperimentalMsms(stream, _target.Value.InnerModel, msdec, ExportParameter), token).ConfigureAwait(false);
            }
            else if (ExportParameter.MpIsExportOtherCandidates) {
                var searchParameter = _queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff;
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, _target.Value.InnerModel, ExportParameter, _queryFactory, _analysisFile.MSDecLoader, searchParameter), token).ConfigureAwait(false);
            }
            else {
                var searchParameter = _queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff;
                await Task.Run(() => _exporter.ExportReferenceMsms(stream, _target.Value.InnerModel, ExportParameter), token).ConfigureAwait(false);
            }
        }
    }
}
