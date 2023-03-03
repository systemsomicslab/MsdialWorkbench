using Reactive.Bindings;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal interface IAlignmentResultExportViewModel
    {
        bool IsExpanded { get; set; }
        bool HasValidationErrors { get; }
        IReadOnlyReactiveProperty<bool> CanExport { get; }
    }
}
