using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class SelectableChromatogram : DisposableModelBase {
        public SelectableChromatogram(IObservable<ChromatogramsModel> chromatogram, ReactivePropertySlim<bool> isSelected, ReadOnlyReactivePropertySlim<bool> isEnabled) {
            Chromatogram = chromatogram;
            IsSelected = isSelected.AddTo(Disposables);
            IsEnabled = isEnabled.AddTo(Disposables);
        }

        public IObservable<ChromatogramsModel> Chromatogram { get; }
        public ReactivePropertySlim<bool> IsSelected { get; }
        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }

        public SelectableChromatogram Merge(SelectableChromatogram other) {
            var both = Chromatogram.CombineLatest(other.Chromatogram, (s, o) => s.Merge(o));
            var isSelected = new ReactivePropertySlim<bool>(false);
            var isEnabled = IsEnabled.CombineLatest(other.IsEnabled, (s, o) => s && o).StartWith(IsEnabled.Value && other.IsEnabled.Value).ToReadOnlyReactivePropertySlim();
            return new SelectableChromatogram(both, isSelected, isEnabled);
        }

        public IObservable<SelectableChromatogram> ObserveWhenSelected() {
            return IsSelected.Where(x => x).ToConstant(this);
        }
    }
}
