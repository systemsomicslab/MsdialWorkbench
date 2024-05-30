using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Extensions
{
    internal static class RangeExtensions
    {
        public static ReactiveAxisManager<double> ToContinuousAxis(this IObservable<AxisRange> observableRange) {
            return observableRange.ToReactiveContinuousAxisManager<double>(new ConstantMargin(0, 30), new AxisRange(0d, 0d), LabelType.Percent);
        }

        public static ReactiveAxisManager<double> ToLogAxis(this IObservable<AxisRange> observableRange) {
            return observableRange
                .Select(range => (range.Minimum.Value, range.Maximum.Value))
                .ToReactiveLogScaleAxisManager(new ConstantMargin(0, 30), 1d, 1d, labelType: LabelType.Percent);
        }

        public static ReactiveAxisManager<double> ToSqrtAxis(this IObservable<AxisRange> observableRange) {
            return observableRange
                .Select(range => (range.Minimum.Value, range.Maximum.Value))
                .ToReactiveSqrtAxisManager(new ConstantMargin(0, 30), 0, 0, labelType: LabelType.Percent);
        }
    }
}
