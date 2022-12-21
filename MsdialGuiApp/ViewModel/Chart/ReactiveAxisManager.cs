using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public class ReactiveAxisManager<T> : ContinuousAxisManager<T> where T : IConvertible
    {
        public ReactiveAxisManager(
            IObservable<Range> rangeSource,
            Range bounds = null) : base(new Range(0, 1), bounds) {
            Disposables.Add(rangeSource.ObserveOnUIDispatcher().Subscribe(UpdateRange));
        }

        public ReactiveAxisManager(
            IObservable<Range> rangeSource,
            IChartMargin margin,
            Range bounds = null) : base(new Range(0, 1), margin, bounds) {
            Disposables.Add(rangeSource.ObserveOnUIDispatcher().Subscribe(UpdateRange));
        }

        private void UpdateRange(Range initial) {
            UpdateInitialRange(initial);
        }
    }

    public static class ReactiveAxisManager
    {
        [Obsolete("Use CompMs.Graphics.AxisManager.Generic.ReactiveAxisManager.ToReactiveContinuousAxisManager method.")]
        public static ReactiveAxisManager<T> ToReactiveAxisManager<T>(this IObservable<Range> self, Range bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(self, bounds) { LabelType = labelType };
        }

        [Obsolete("Use CompMs.Graphics.AxisManager.Generic.ReactiveAxisManager.ToReactiveContinuousAxisManager method.")]
        public static ReactiveAxisManager<T> ToReactiveAxisManager<T>(this IObservable<Range> self, IChartMargin margin, Range bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(self, margin, bounds) { LabelType = labelType };
        }
    }
}
