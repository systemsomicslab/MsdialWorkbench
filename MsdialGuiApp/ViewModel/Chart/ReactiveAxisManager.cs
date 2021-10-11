using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Reactive.Linq;
using System.Threading;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public class ReactiveAxisManager<T> : ContinuousAxisManager<T>, IDisposable where T : IConvertible
    {
        public ReactiveAxisManager(
            IObservable<Range> rangeSource,
            Range bounds = null) : base(new Range(0, 1), bounds) {
            rangeUnSubscriber = rangeSource.ObserveOnDispatcher().Subscribe(UpdateRange);
        }

        public ReactiveAxisManager(
            IObservable<Range> rangeSource,
            IChartMargin margin,
            Range bounds = null) : base(new Range(0, 1), margin, bounds) {
            rangeUnSubscriber = rangeSource.ObserveOnDispatcher().Subscribe(UpdateRange);
        }

        private void UpdateRange(Range initial) {
            UpdateInitialRange(initial);
        }

        private bool disposedValue;
        private IDisposable rangeUnSubscriber;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                    rangeUnSubscriber.Dispose();
                    rangeUnSubscriber = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public static class ReactiveAxisManager
    {
        public static ReactiveAxisManager<T> ToReactiveAxisManager<T>(this IObservable<Range> self, Range bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(self, bounds) { LabelType = labelType };
        }

        public static ReactiveAxisManager<T> ToReactiveAxisManager<T>(this IObservable<Range> self, IChartMargin margin, Range bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(self, margin, bounds) { LabelType = labelType };
        }
    }
}
