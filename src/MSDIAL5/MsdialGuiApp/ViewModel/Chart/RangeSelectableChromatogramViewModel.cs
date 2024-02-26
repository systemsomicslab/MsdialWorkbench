using CompMs.App.Msdial.Model.Chart;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal class RangeSelectableChromatogramViewModel : ViewModelBase
    {
        public RangeSelectableChromatogramViewModel(RangeSelectableChromatogramModel model) {
            Model = model;
            SelectedRanges = model.SelectedRanges.ToReadOnlyReactiveCollection().AddTo(Disposables);
            ChromatogramsViewModel = new ChromatogramsViewModel(model.ChromatogramModel);
            SelectedRange = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedRange).AddTo(Disposables);

            var commandChange = model.SelectedRanges
                .CollectionChangedAsObservable()
                .ToUnit()
                .StartWith(Unit.Default);

            SetMainRangeCommand = commandChange.Select(_ => model.CanSetMainRange())
                .ToReactiveCommand()
                .WithSubscribe(model.SetMainRange)
                .AddTo(Disposables);

            SetSubtractRangeCommand = commandChange.Select(_ => model.CanSetSubstractRange())
                .ToReactiveCommand()
                .WithSubscribe(model.SetSubtractRange)
                .AddTo(Disposables);

            RemoveRangesCommand = new ReactiveCommand()
                .WithSubscribe(model.RemoveRanges)
                .AddTo(Disposables);
        }

        public ChromatogramsViewModel ChromatogramsViewModel { get; }

        public ReactiveCommand SetMainRangeCommand { get; }

        public ReactiveCommand SetSubtractRangeCommand { get; }

        public ReactiveCommand RemoveRangesCommand { get; }

        public ReactivePropertySlim<AxisRange?> SelectedRange { get; }

        public ReadOnlyReactiveCollection<RangeSelection> SelectedRanges { get; }

        public RangeSelectableChromatogramModel Model { get; }
    }
}
