using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class ProteinGroupExportViewModel : ViewModelBase, IAlignmentResultExportViewModel
    {
        public ProteinGroupExportViewModel(ProteinGroupExportModel model) {
            IsSelected = model.ToReactivePropertySlimAsSynchronized(m => m.IsSelected).AddTo(Disposables);
            CanExport = Observable.Return(true).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReactivePropertySlim<bool> IsSelected { get; }

        public bool IsExpanded {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
        private bool _isExpanded = false;

        public IReadOnlyReactiveProperty<bool> CanExport { get; }
    }
}
