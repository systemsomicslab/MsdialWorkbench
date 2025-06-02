using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.ViewModel.Export;

internal sealed class AnnotationResultExportViewModel : ViewModelBase, IAlignmentResultExportViewModel
{
    private readonly AnnotationResultExportModel _model;
    private readonly ReactivePropertySlim<bool> _canExport;

    public AnnotationResultExportViewModel(AnnotationResultExportModel model) {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        IsSelected = model.ToReactivePropertySlimAsSynchronized(m => m.IsSelected).AddTo(Disposables);
        _canExport = new ReactivePropertySlim<bool>(true).AddTo(Disposables);
    }

    public bool IsExpanded {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }
    private bool _isExpanded;

    public ReactivePropertySlim<bool> IsSelected { get; }

    IReadOnlyReactiveProperty<bool> IAlignmentResultExportViewModel.CanExport => _canExport; 
}
