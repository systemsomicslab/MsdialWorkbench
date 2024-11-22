using CompMs.App.Msdial.Model.Imaging;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class ImagingRoiViewModel : ViewModelBase
    {
        public ImagingRoiViewModel(ImagingRoiModel model) {
            Id = model.Id;
            RoiPeakSummaries = model.RoiPeakSummaries.ToReadOnlyReactiveCollection(summary => new RoiPeakSummaryViewModel(summary)).AddTo(Disposables);
            SelectedRoiPeakSummary = model.ToReactivePropertyAsSynchronized(
                m => m.SelectedRoiPeakSummary,
                mox => mox.Select(m => RoiPeakSummaries.FirstOrDefault(vm => vm.Model == m)),
                vmox => vmox.Select(vm => vm?.Model))
                .AddTo(Disposables);
            Roi = new RoiViewModel(model.Roi).AddTo(Disposables);
            IsSelected = model.ToReactivePropertySlimAsSynchronized(m => m.IsSelected).AddTo(Disposables);
        }

        public string Id { get; }
        public ReadOnlyReactiveCollection<RoiPeakSummaryViewModel> RoiPeakSummaries { get; }
        public ReactiveProperty<RoiPeakSummaryViewModel> SelectedRoiPeakSummary { get; }
        public RoiViewModel Roi { get; }
        public ReactivePropertySlim<bool> IsSelected { get; }
    }
}
