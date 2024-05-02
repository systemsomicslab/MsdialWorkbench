using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class ChromatogramsViewModel : ViewModelBase {
        private readonly ChromatogramsModel _model;
        private readonly IMessageBroker _broker;

        public ChromatogramsViewModel(ChromatogramsModel model, IMessageBroker? broker = null) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            _broker = broker ?? MessageBroker.Default;
            CopyAsTableCommand = new ReactiveCommand().WithSubscribe(CopyAsTable).AddTo(Disposables);
            SaveAsTableCommand = new AsyncReactiveCommand().WithSubscribe(SaveAsTableAsync).AddTo(Disposables);
        }

        public ReadOnlyObservableCollection<DisplayChromatogram> DisplayChromatograms => _model.DisplayChromatograms;

        public AxisItemSelector<double> HorizontalSelector => _model.ChromAxisItemSelector;
        public AxisItemSelector<double> VerticalSelector => _model.AbundanceAxisItemSelector;

        public IAxisManager<double> HorizontalAxis => _model.ChromAxisItemSelector.SelectedAxisItem.AxisManager;
        public IAxisManager<double> VerticalAxis => _model.AbundanceAxisItemSelector.SelectedAxisItem.AxisManager;

        public string GraphTitle => _model.GraphTitle;

        public string HorizontalTitle => _model.ChromAxisItemSelector.SelectedAxisItem.GraphLabel;
        public string VerticalTitle => _model.AbundanceAxisItemSelector.SelectedAxisItem.GraphLabel;

        public string HorizontalProperty => _model.HorizontalProperty;
        public string VerticalProperty => _model.VerticalProperty;

        public ReactiveCommand CopyAsTableCommand { get; }

        private void CopyAsTable() {
            using (var stream = new MemoryStream()) {
                _model.ExportAsync(stream, "\t").Wait();
                Clipboard.SetDataObject(Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        public AsyncReactiveCommand SaveAsTableCommand { get; }

        private async Task SaveAsTableAsync() {
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
                    await _model.ExportAsync(stream, "\t").ConfigureAwait(false);
                }
            }
        }
    }
}
