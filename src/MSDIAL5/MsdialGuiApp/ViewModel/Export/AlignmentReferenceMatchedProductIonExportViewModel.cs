using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Export;

internal sealed class AlignmentReferenceMatchedProductIonExportViewModel : ViewModelBase, IAlignmentResultExportViewModel
{
    private readonly AlignmentReferenceMatchedProductIonExportModel _model;

    public AlignmentReferenceMatchedProductIonExportViewModel(AlignmentReferenceMatchedProductIonExportModel model) {
        _model = model;
        IsSelected = model.ToReactivePropertySlimAsSynchronized(m => m.IsSelected).AddTo(Disposables);
        CanExport = this.ErrorsChangedAsObservable().Select(_ => !HasValidationErrors).ToReadOnlyReactivePropertySlim(initialValue: !HasValidationErrors).AddTo(Disposables);
    }

    public bool IsExpanded {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }
    private bool _isExpanded = false;

    public ReactivePropertySlim<bool> IsSelected { get; }

    public IReadOnlyReactiveProperty<bool> CanExport { get; }
}
