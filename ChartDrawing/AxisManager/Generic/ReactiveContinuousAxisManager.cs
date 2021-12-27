using CompMs.Graphics.Core.Base;
using System;

namespace CompMs.Graphics.AxisManager.Generic
{
    public class ReactiveContinuousAxisManager<T> : ContinuousAxisManager<T> where T : IConvertible
    {
        public ReactiveContinuousAxisManager(
            IObservable<Range> rangeSource,
            Range bounds = null) : base(new Range(0, 1), bounds) {
            var observer = new RangeObserver(this);
            observer.Subscribe(rangeSource);

            Disposables.Add(observer);
        }

        public ReactiveContinuousAxisManager(
            IObservable<Range> rangeSource,
            IChartMargin margin,
            Range bounds = null) : base(new Range(0, 1), margin, bounds) {
            var observer = new RangeObserver(this);
            observer.Subscribe(rangeSource);

            Disposables.Add(observer);
        }

        class RangeObserver : IObserver<Range>, IDisposable
        {
            private ReactiveContinuousAxisManager<T> axis;
            private IDisposable unsubscriber;
            private bool disposedValue;

            public RangeObserver(ReactiveContinuousAxisManager<T> axis) {
                this.axis = axis;
            }

            public void Subscribe(IObservable<Range> provider) {
                if (!disposedValue && provider != null) {
                    unsubscriber = provider.Subscribe(this);
                }
            }

            public void OnCompleted() {
                Dispose();
            }

            public void OnError(Exception error) {

            }

            public void OnNext(Range value) {
                axis.UpdateInitialRange(value);
            }

            protected virtual void Dispose(bool disposing) {
                if (!disposedValue) {
                    if (disposing) {
                        unsubscriber.Dispose();
                        unsubscriber = null;
                        axis = null;
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
    }

    public static class ReactiveContinuousAxisManager
    {
        public static ReactiveContinuousAxisManager<T> ToReactiveContinuousAxisManager<T> (this IObservable<Range> self, Range bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveContinuousAxisManager<T>(self, bounds) { LabelType = labelType };
        }

        public static ReactiveContinuousAxisManager<T> ToReactiveContinuousAxisManager<T> (this IObservable<Range> self, IChartMargin margin, Range bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveContinuousAxisManager<T>(self, margin, bounds) { LabelType = labelType };
        }
    }
}
