using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class SingleAlignmentSpotExporterViewModel : ViewModelBase
    {
        public SingleAlignmentSpotExporterViewModel(SingleAlignmentSpotExporter exporter)
        {
            Commands = exporter.Exporters.ToReadOnlyReactiveCollection(
                exporter => new LabeledCommand(
                    new AsyncReactiveCommand<AlignmentSpotPropertyModel>().WithSubscribe(exporter.ExportAsync),
                    exporter.Label)
                ).AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<LabeledCommand> Commands { get; }
    }
}
