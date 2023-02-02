using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class ProteinGroupExportViewModel : ViewModelBase, IAlignmentResultExportViewModel
    {
        public ProteinGroupExportViewModel(ProteinGroupExportModel model) {
            IsSelected = model.ToReactivePropertySlimAsSynchronized(m => m.IsSelected).AddTo(Disposables);
        }

        public ReactivePropertySlim<bool> IsSelected { get; }

        public bool IsExpanded {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
        private bool _isExpanded = false;
    }
}
