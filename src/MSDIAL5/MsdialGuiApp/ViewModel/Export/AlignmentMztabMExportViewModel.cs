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
        get => _isExapnded;
        set => SetProperty(ref _isExapnded, value);
    }
    private bool _isExapnded = false;

    public ReadOnlyCollection<ExportType> Types => _model.Types;

    public IReadOnlyReactiveProperty<bool> CanExport { get; }
}
