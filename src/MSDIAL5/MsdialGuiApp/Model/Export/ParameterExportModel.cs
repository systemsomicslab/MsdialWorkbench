using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class ParameterExportModel : BindableBase
    {
        private readonly DataBaseStorage _dbs;
        private readonly ParameterBase _baseParmaeter;
        private readonly IMessageBroker _broker;

        public ParameterExportModel(DataBaseStorage dbs, ParameterBase baseParmaeter, IMessageBroker broker) {
            _dbs = dbs;
            _baseParmaeter = baseParmaeter;
            _broker = broker;
        }

        public async Task ExportAsync() {
            var filename = string.Empty;
            var request = new SaveFileNameRequest(f => filename = f)
            {
                Title = "Save parameter file as",
                Filter = "text file|*.txt",
            };
            _broker.Publish(request);

            if (request.Result != true) {
                return;
            }

            var publisher = new TaskProgressPublisher(_broker, $"Exporting {filename}");
            using (publisher.Start())
            using (var writer = new StreamWriter(filename)) {
                await writer.WriteAsync(string.Join(Environment.NewLine, _baseParmaeter.ParametersAsText())).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
                await writer.WriteAsync(_dbs.ParameterAsSimpleText()).ConfigureAwait(false);
            }
        }
    }
}
