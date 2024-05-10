using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.MsdialCore.Export;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class SingleAlignmentSpotExporter
    {
        private readonly AlignmentFileBeanModel _file;
        private readonly IMessageBroker _broker;

        public SingleAlignmentSpotExporter(AlignmentFileBeanModel file, IMessageBroker broker)
        {
            Exporters = new ObservableCollection<IExport>();
            _file = file;
            _broker = broker;
        }

        public ObservableCollection<IExport> Exporters { get; }

        public void AddExporter(IAlignmentSpectraExporter exporter, string label, string extension) {
            Exporters.Add(new ExportMethod(exporter, _file, label, DefaultFileName(extension), _broker));
        }

        public void AddExporter(IAlignmentSpectraExporter exporter, string label, Func<AlignmentSpotPropertyModel, string> getFileName) {
            Exporters.Add(new ExportMethod(exporter, _file, label, getFileName, _broker));
        }

        private static Func<AlignmentSpotPropertyModel, string> DefaultFileName(string extension) {
            string f(AlignmentSpotPropertyModel spot) {
                return $"AlignmentID{spot.MasterAlignmentID}{extension}";
            }
            return f;
        }

        public interface IExport {
            string Label { get; }
            Task ExportAsync(AlignmentSpotPropertyModel spot);
        }

        class ExportMethod : IExport {
            private readonly IAlignmentSpectraExporter _exporter;
            private readonly AlignmentFileBeanModel _file;
            private readonly Func<AlignmentSpotPropertyModel, string> _fileName;
            private readonly IMessageBroker _broker;

            public ExportMethod(IAlignmentSpectraExporter exporter, AlignmentFileBeanModel file, string label, Func<AlignmentSpotPropertyModel, string> fileName, IMessageBroker broker) {
                _exporter = exporter;
                _file = file;
                Label = label;
                _fileName = fileName;
                _broker = broker;
            }

            public string Label { get; }

            async Task IExport.ExportAsync(AlignmentSpotPropertyModel spot) {
                var decResult = await _file.LoadMSDecResultByIndexAsync(spot.MasterAlignmentID).ConfigureAwait(false);
                if (decResult is null) {
                    return;
                }
                var request = new SelectFolderRequest
                {
                    Title = "Choose the folder",
                };
                _broker.Publish(request);
                if (string.IsNullOrEmpty(request.SelectedPath)) {
                    return;
                }
                string destDirectory = request.SelectedPath!;
                var fileName = _fileName.Invoke(spot);
                using var stream = File.Open(Path.Combine(destDirectory, fileName), FileMode.Create);
                _exporter.Export(stream, spot.innerModel, decResult);
            }
        }
    }
}
