using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Export;

internal sealed class AlignmentMztabMExportViewModel : ViewModelBase, IAlignmentResultExportViewModel
{
    private readonly AlignmentMztabMExportModel _model;

    public AlignmentMztabMExportViewModel(AlignmentMztabMExportModel model) {
        _model = model ?? throw new System.ArgumentNullException(nameof(model));
        IsSelected = model.ToReactivePropertySlimAsSynchronized(m => m.IsSelected).AddTo(Disposables);
        CanExport = this.ErrorsChangedAsObservable().Select(_ => !HasValidationErrors).ToReadOnlyReactivePropertySlim(!HasValidationErrors).AddTo(Disposables);
    }

    public ReactivePropertySlim<bool> IsSelected { get; }

    public bool IsExpanded {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }
    private bool _isExpanded = false;

    public ReadOnlyCollection<ExportType> Types => _model.Types;

    public IReadOnlyReactiveProperty<bool> CanExport { get; }
}
