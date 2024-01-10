using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using System.Collections.Generic;
using System.IO;
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
        private readonly IReadOnlyList<CompoundSearcher> _compoundSearchers;
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

        public Task BatchExportAsync(Stream stream, CancellationToken token) {
            throw new System.NotImplementedException();
        }

        public Task ExportAsync(Stream stream, CancellationToken token) {
            var loader = _alignmentFile.CreateTemporaryMSDecLoader();
            if (ExportParameter.MpIsFocusedSpotOutput) {
                var msdec = loader.LoadMSDecResult(_target.Value.innerModel.MSDecResultIdUsed);
                _exporter.ExportExperimentalMsms(string.Empty, _target.Value.innerModel, msdec, ExportParameter);
            }
            else if (ExportParameter.MpIsExportOtherCandidates) {
                var searchParameter = _queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff;
                _exporter.ExportReferenceMsms(string.Empty, _target.Value.innerModel, ExportParameter, _queryFactory, loader, searchParameter);
            }
            else {
                var searchParameter = _queryFactory.PrepareParameter();
                searchParameter.TotalScoreCutoff = ExportParameter.MpIdentificationScoreCutOff;
                _exporter.ExportReferenceMsms(string.Empty, _target.Value.innerModel, ExportParameter, _queryFactory, loader, searchParameter);

            }
            return Task.CompletedTask;
        }
    }
}
