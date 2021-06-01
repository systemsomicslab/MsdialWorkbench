using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public class ReactiveAxisManager<T> : ContinuousAxisManager<T>, IDisposable where T : IConvertible
    {
        public ReactiveAxisManager(
            IObservable<Range> rangeSource,
            Range bounds = null) : base(new Range(0, 1), bounds) {
            rangeUnSubscriber = rangeSource.Subscribe(UpdateRange);
        }

        public ReactiveAxisManager(
            IObservable<Range> rangeSource,
            ChartMargin margin,
            Range bounds = null) : base(new Range(0, 1), margin, bounds) {
            rangeUnSubscriber = rangeSource.Subscribe(UpdateRange);
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
        public static ReactiveAxisManager<T> ToReactiveAxisManager<T>(this IObservable<Range> self, Range bounds = null)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(self, bounds);
        }

        public static ReactiveAxisManager<T> ToReactiveAxisManager<T>(this IObservable<Range> self, ChartMargin margin, Range bounds = null)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(self, margin, bounds);
        }
    }
}
