using CompMs.App.Msdial.Model.Chart;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public class RangeSelectableChromatogramViewModel : ViewModelBase
    {
        public RangeSelectableChromatogramViewModel(RangeSelectableChromatogramModel model) {
            Model = model;
            SelectedRanges = Model.SelectedRanges.ToReadOnlyReactiveCollection().AddTo(Disposables);
            ChromatogramsViewModel = new ChromatogramsViewModel(model.ChromatogramModel);
            SelectedRange = Model.ToReactivePropertySlimAsSynchronized(m => m.SelectedRange).AddTo(Disposables);

            var collectionChanged = Model.SelectedRanges.CollectionChangedAsObservable();

            SetMainRangeCommand = collectionChanged.Select(_ => Model.CanSetMainRange())
                .ToReactiveCommand()
                .WithSubscribe(Model.SetMainRange)
                .AddTo(Disposables);

            SetSubtractRangeCommand = collectionChanged.Select(_ => Model.CanSetSubstractRange())
                .ToReactiveCommand()
                .WithSubscribe(Model.SetSubtractRange)
                .AddTo(Disposables);
        }

        public ChromatogramsViewModel ChromatogramsViewModel { get; }

        public ReactiveCommand SetMainRangeCommand { get; }

        public ReactiveCommand SetSubtractRangeCommand { get; }

        public ReactivePropertySlim<Range> SelectedRange { get; }

        public ReadOnlyReactiveCollection<RangeSelection> SelectedRanges { get; }
        public RangeSelectableChromatogramModel Model { get; }
    }
}
