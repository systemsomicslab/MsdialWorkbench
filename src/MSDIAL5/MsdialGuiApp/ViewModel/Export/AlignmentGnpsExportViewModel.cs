using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Export;

internal sealed class AlignmentGnpsExportViewModel : ViewModelBase, IAlignmentResultExportViewModel
{
    private readonly AlignmentGnpsExportModel _model;

    public AlignmentGnpsExportViewModel(AlignmentGnpsExportModel model) {
        _model = model;
        CanExport = this.ErrorsChangedAsObservable().Select(_ => !HasValidationErrors).ToReadOnlyReactivePropertySlim(!HasValidationErrors).AddTo(Disposables);
        IsSelected = model.ToReactivePropertySlimAsSynchronized(m => m.IsSelected).AddTo(Disposables);
    }

    public string Label => _model.Label;

    public bool IsExpanded {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }
    private bool _isExpanded = false;

    public ReactivePropertySlim<bool> IsSelected { get; }

    public ReadOnlyObservableCollection<ExportType> Types => _model.Types;

    public IReadOnlyReactiveProperty<bool> CanExport { get; }
}
