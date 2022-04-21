using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompMs.Graphics.AxisManager.Generic
{
    public class ReactiveAxisManager<T> : ViewModelBase, IAxisManager<T> where T : IConvertible
    {
        private readonly BaseAxisManager<T> AxisManagerImpl;

        public ReactiveAxisManager(
            BaseAxisManager<T> axisManager,
            IObservable<Range> rangeSource) {
            AxisManagerImpl = axisManager;

            var observer = new RangeObserver(AxisManagerImpl);
            observer.Subscribe(rangeSource);
            Disposables.Add(observer);
        }

        public ReactiveAxisManager(
            BaseAxisManager<T> axisManager,
            IObservable<(T, T)> rangeSource) {
            AxisManagerImpl = axisManager;

            var observer = new RangeObserver(AxisManagerImpl);
            observer.Subscribe(rangeSource);
            Disposables.Add(observer);
        }

        public Range Range => AxisManagerImpl.Range;

        public event EventHandler RangeChanged {
            add => AxisManagerImpl.RangeChanged += value;
            remove => AxisManagerImpl.RangeChanged -= value;
        }

        public event EventHandler InitialRangeChanged {
            add => AxisManagerImpl.InitialRangeChanged += value;
            remove => AxisManagerImpl.InitialRangeChanged -= value;
        }

        public bool Contains(AxisValue value) => AxisManagerImpl.Contains(value);

        public bool ContainsCurrent(AxisValue value) => AxisManagerImpl.ContainsCurrent(value);

        public void Focus(Range range) => AxisManagerImpl.Focus(range);

        public List<LabelTickData> GetLabelTicks() => AxisManagerImpl.GetLabelTicks();

        public void Recalculate(double drawableLength) => AxisManagerImpl.Recalculate(drawableLength);

        public void Reset() => AxisManagerImpl.Reset();

        public AxisValue TranslateFromRenderPoint(double value, bool isFlipped, double drawableLength) => AxisManagerImpl.TranslateFromRenderPoint(value, isFlipped, drawableLength);

        public AxisValue TranslateToAxisValue(T value) => AxisManagerImpl.TranslateToAxisValue(value);

        public AxisValue TranslateToAxisValue(object value) => AxisManagerImpl.TranslateToAxisValue(value);

        public double TranslateToRenderPoint(AxisValue value, bool isFlipped, double drawableLength) => AxisManagerImpl.TranslateToRenderPoint(value, isFlipped, drawableLength);

        public List<double> TranslateToRenderPoints(IEnumerable<T> values, bool isFlipped, double drawableLength) => AxisManagerImpl.TranslateToRenderPoints(values, isFlipped, drawableLength);

        public List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped, double drawableLength) => AxisManagerImpl.TranslateToRenderPoints(values, isFlipped, drawableLength);

        class RangeObserver : IObserver<Range>, IObserver<(T, T)>, IDisposable
        {
            private BaseAxisManager<T> axis;
            private IDisposable unsubscriber;
            private bool disposedValue;

            public RangeObserver(BaseAxisManager<T> axis) {
                this.axis = axis;
            }

            public void Subscribe(IObservable<(T, T)> provider) {
                if (!disposedValue && provider != null) {
                    unsubscriber = provider.Subscribe(this);
                }
            }

            public void Subscribe(IObservable<Range> provider) {
                if (!disposedValue && provider != null) {
                    unsubscriber = provider.Subscribe(this);
                }
            }

            public void OnCompleted() {

            }

            public void OnError(Exception error) {

            }

            public void OnNext(Range value) {
                _ = UpdateInitialRange(value);
            }

            public void OnNext((T, T) value) {
                _ = UpdateInitialRange(new Range(
                        axis.TranslateToAxisValue(value.Item1),
                        axis.TranslateToAxisValue(value.Item2)));
            }

            private async Task UpdateInitialRange(Range range) {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => axis?.UpdateInitialRange(range));
            }

            protected virtual void Dispose(bool disposing) {
                if (!disposedValue) {
                    if (disposing) {
                        unsubscriber?.Dispose();
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

    public static class ReactiveAxisManager
    {
        public static ReactiveAxisManager<T> ToReactiveLogScaleAxisManager<T> (this IObservable<(T, T)> self)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new LogScaleAxisManager<T>(new Range(0, 1)), self);
        }

        public static ReactiveAxisManager<T> ToReactiveLogScaleAxisManager<T> (this IObservable<(T, T)> self, T lowBound, T highBound)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new LogScaleAxisManager<T>(new Range(0, 1), lowBound, highBound), self);
        }

        public static ReactiveAxisManager<T> ToReactiveLogScaleAxisManager<T> (this IObservable<(T, T)> self, IChartMargin margin, T lowBound, T highBound)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new LogScaleAxisManager<T>(new Range(0, 1), margin, lowBound, highBound), self);
        }

        public static ReactiveAxisManager<T> ToReactiveContinuousAxisManager<T> (this IObservable<Range> self, Range bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new ContinuousAxisManager<T>(new Range(0, 1), bounds) { LabelType = labelType, }, self);
        }

        public static ReactiveAxisManager<T> ToReactiveContinuousAxisManager<T> (this IObservable<Range> self, IChartMargin margin, Range bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new ContinuousAxisManager<T>(new Range(0, 1), margin, bounds) { LabelType = labelType, }, self);
        }

        public static ReactiveAxisManager<T> ToReactiveContinuousAxisManager<T> (this IObservable<(T, T)> self, Range bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new ContinuousAxisManager<T>(new Range(0, 1), bounds) { LabelType = labelType, }, self);
        }

        public static ReactiveAxisManager<T> ToReactiveContinuousAxisManager<T> (this IObservable<(T, T)> self, IChartMargin margin, Range bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new ContinuousAxisManager<T>(new Range(0, 1), margin, bounds) { LabelType = labelType, }, self);
        }
    }
}
