using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Search
{
    public class PeakSpotNavigatorViewModel : ViewModelBase
    {
        public PeakSpotNavigatorViewModel(PeakSpotNavigatorModel model) {
            SelectedAnnotationLabel = model
                .ToReactivePropertySlimAsSynchronized(m => m.SelectedAnnotationLabel)
                .AddTo(Disposables);
            PeakSpotsView = CollectionViewSource.GetDefaultView(model.PeakSpots);
            AmplitudeLowerValue = model
                .ToReactivePropertySlimAsSynchronized(m => m.AmplitudeLowerValue)
                .AddTo(Disposables);
            AmplitudeUpperValue = model
                .ToReactivePropertySlimAsSynchronized(m => m.AmplitudeUpperValue)
                .AddTo(Disposables);
            PeakFilterViewModel = new PeakFilterViewModel(model.PeakFilterModel).AddTo(Disposables);
        }

        public ReactivePropertySlim<string> SelectedAnnotationLabel { get; }

        public ICollectionView PeakSpotsView { get; }

        public ReactivePropertySlim<double> AmplitudeLowerValue { get; }
        public ReactivePropertySlim<double> AmplitudeUpperValue { get; }

        public PeakFilterViewModel PeakFilterViewModel { get; }
    }
}
