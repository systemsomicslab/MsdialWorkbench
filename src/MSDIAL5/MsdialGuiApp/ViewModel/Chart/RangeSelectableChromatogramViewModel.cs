using CompMs.App.Msdial.Model.Chart;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class RangeSelectableChromatogramViewModel : ViewModelBase
    {
        public RangeSelectableChromatogramViewModel(RangeSelectableChromatogramModel model) {
            Model = model;
            ChromatogramsViewModel = new ChromatogramsViewModel(model.ChromatogramModel);
            SelectedRange = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedRange).AddTo(Disposables);

            var commandChange = model.ObserveProperty(m => m.SelectedRange);

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

        public ReadOnlyObservableCollection<RangeSelection> SelectedRanges => Model.SelectedRanges;

        public RangeSelectableChromatogramModel Model { get; }
    }
}
