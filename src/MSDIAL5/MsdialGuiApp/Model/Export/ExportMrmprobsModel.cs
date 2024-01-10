using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
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
}
