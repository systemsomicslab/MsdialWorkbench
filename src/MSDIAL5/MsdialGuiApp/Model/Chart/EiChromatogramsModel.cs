using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class EiChromatogramsModel : DisposableModelBase
    {
        private readonly IMessageBroker _broker;
        private readonly SelectableChromatogram _rawChromatogram, _deconvolutedChromatogram, _combinedChromatogram;

        public EiChromatogramsModel(SelectableChromatogram rawChromatogram, SelectableChromatogram deconvolutedChromatogram, IMessageBroker broker) {
            _rawChromatogram = rawChromatogram;
            _deconvolutedChromatogram = deconvolutedChromatogram;
            _broker = broker;

            var combinedChromatogram = rawChromatogram.Merge(deconvolutedChromatogram).AddTo(Disposables);
            _combinedChromatogram = combinedChromatogram;

            ChromatogramsModel = new[]
            {
                rawChromatogram.ObserveWhenSelected(),
                deconvolutedChromatogram.ObserveWhenSelected(),
                combinedChromatogram.ObserveWhenSelected(),
            }.Merge()
            .Select(c => c.Chromatogram)
            .Switch()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<ChromatogramsModel> ChromatogramsModel { get; }

        public ReactivePropertySlim<bool> IsRawSelected => _rawChromatogram.IsSelected;
        public ReactivePropertySlim<bool> IsDeconvolutedSelected => _deconvolutedChromatogram.IsSelected;
        public ReactivePropertySlim<bool> IsBothSelected => _combinedChromatogram.IsSelected;

        public ReadOnlyReactivePropertySlim<bool> IsRawEnabled => _rawChromatogram.IsEnabled;
        public ReadOnlyReactivePropertySlim<bool> IsDeconvolutedEnabled => _deconvolutedChromatogram.IsEnabled;
        public ReadOnlyReactivePropertySlim<bool> IsBothEnabled => _combinedChromatogram.IsEnabled;

        public void CopyAsTable() {
            if (!(ChromatogramsModel.Value is ChromatogramsModel chromatograms)) {
                return;
            }
            using (var stream = new MemoryStream()) {
                chromatograms.ExportAsync(stream, "\t").Wait();
                Clipboard.SetDataObject(Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        public async Task SaveAsTableAsync() {
            if (!(ChromatogramsModel.Value is ChromatogramsModel chromatograms)) {
                return;
            }
            var fileName = string.Empty;
            var request = new SaveFileNameRequest(name => fileName = name)
            {
                AddExtension = true,
                Filter = "tab separated values|*.txt",
                RestoreDirectory = true,
            };
            _broker.Publish(request);
            if (request.Result == true) {
                using (var stream = File.Open(fileName, FileMode.Create)) {
                    await chromatograms.ExportAsync(stream, "\t").ConfigureAwait(false);
                }
            }
        }
    }
}
