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
            IObservable<AxisRange> rangeSource) {
            AxisManagerImpl = axisManager;

            var observer = new RangeObserver(AxisManagerImpl);
            observer.Subscribe(rangeSource);
            Disposables.Add(observer);
            axisManager.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(BaseAxisManager<T>.UnitLabel)) OnPropertyChanged(nameof(UnitLabel)); };
        }

        public ReactiveAxisManager(
            BaseAxisManager<T> axisManager,
            IObservable<(T, T)> rangeSource) {
            AxisManagerImpl = axisManager;

            var observer = new RangeObserver(AxisManagerImpl);
            observer.Subscribe(rangeSource);
            Disposables.Add(observer);
            axisManager.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(BaseAxisManager<T>.UnitLabel)) OnPropertyChanged(nameof(UnitLabel)); };
        }

        public AxisRange Range => AxisManagerImpl.Range;

        public event EventHandler RangeChanged {
            add => AxisManagerImpl.RangeChanged += value;
            remove => AxisManagerImpl.RangeChanged -= value;
        }

        public event EventHandler InitialRangeChanged {
            add => AxisManagerImpl.InitialRangeChanged += value;
            remove => AxisManagerImpl.InitialRangeChanged -= value;
        }

        public event EventHandler AxisValueMappingChanged {
            add => AxisManagerImpl.AxisValueMappingChanged += value;
            remove => AxisManagerImpl.AxisValueMappingChanged -= value;
        }

        public string UnitLabel => AxisManagerImpl.UnitLabel;

        public bool Contains(AxisValue value) => AxisManagerImpl.Contains(value);

        public bool ContainsCurrent(AxisValue value) => AxisManagerImpl.ContainsCurrent(value);

        public void Focus(AxisRange range) => AxisManagerImpl.Focus(range);

        public List<LabelTickData> GetLabelTicks() => AxisManagerImpl.GetLabelTicks();

        public void Recalculate(double drawableLength) => AxisManagerImpl.Recalculate(drawableLength);

        public void Reset() => AxisManagerImpl.Reset();

        public AxisValue TranslateFromRenderPoint(double value, bool isFlipped, double drawableLength) => AxisManagerImpl.TranslateFromRenderPoint(value, isFlipped, drawableLength);

        public AxisValue TranslateToAxisValue(T value) => AxisManagerImpl.TranslateToAxisValue(value);

        public AxisValue TranslateToAxisValue(object value) => AxisManagerImpl.TranslateToAxisValue(value);

        public double TranslateToRenderPoint(AxisValue value, bool isFlipped, double drawableLength) => AxisManagerImpl.TranslateToRenderPoint(value, isFlipped, drawableLength);

        public List<double> TranslateToRenderPoints(IEnumerable<T> values, bool isFlipped, double drawableLength) => AxisManagerImpl.TranslateToRenderPoints(values, isFlipped, drawableLength);

        public List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped, double drawableLength) => AxisManagerImpl.TranslateToRenderPoints(values, isFlipped, drawableLength);

        public List<double> TranslateToRenderPoints(IEnumerable<AxisValue> values, bool isFlipped, double drawableLength) => AxisManagerImpl.TranslateToRenderPoints(values, isFlipped, drawableLength);

        class RangeObserver : IObserver<AxisRange>, IObserver<(T, T)>, IDisposable
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

            public void Subscribe(IObservable<AxisRange> provider) {
                if (!disposedValue && provider != null) {
                    unsubscriber = provider.Subscribe(this);
                }
            }

            public void OnCompleted() {

            }

            public void OnError(Exception error) {

            }

            public void OnNext(AxisRange value) {
                _ = UpdateInitialRange(value);
            }

            public void OnNext((T, T) value) {
                _ = UpdateInitialRange(new AxisRange(
                        axis?.TranslateToAxisValue(value.Item1) ?? 0d,
                        axis?.TranslateToAxisValue(value.Item2) ?? 0d));
            }

            private Task UpdateInitialRange(AxisRange range) {
                return System.Windows.Application.Current.Dispatcher.InvokeAsync(() => axis?.UpdateInitialRange(range)).Task;
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
            return new ReactiveAxisManager<T>(new LogScaleAxisManager<T>(new AxisRange(0, 1)), self);
        }

        public static ReactiveAxisManager<T> ToReactiveLogScaleAxisManager<T> (this IObservable<(T, T)> self, T lowBound, T highBound)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new LogScaleAxisManager<T>(new AxisRange(0, 1), lowBound, highBound), self);
        }

        public static ReactiveAxisManager<T> ToReactiveLogScaleAxisManager<T> (this IObservable<(T, T)> self, IChartMargin margin, T lowBound, T highBound, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new LogScaleAxisManager<T>(new AxisRange(0, 1), margin, lowBound, highBound) { LabelType = labelType, }, self);
        }

        public static ReactiveAxisManager<double> ToReactiveSqrtAxisManager(this IObservable<(double, double)> self, IChartMargin margin, double lowBound, double highBound, LabelType labelType = LabelType.Standard) {
            return new ReactiveAxisManager<double>(new SqrtAxisManager(new AxisRange(0, 1), margin, lowBound, highBound) { LabelType = labelType, }, self);
        }

        public static ReactiveAxisManager<T> ToReactiveContinuousAxisManager<T> (this IObservable<(T, T)> self, T lowBound, T highBound, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new ContinuousAxisManager<T>(new AxisRange(0, 1), lowBound, highBound) { LabelType = labelType, }, self);
        }

        public static ReactiveAxisManager<T> ToReactiveContinuousAxisManager<T>(this IObservable<(T, T)> self, IChartMargin margin, T lowBound, T highBound, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new ContinuousAxisManager<T>(new AxisRange(0, 1), margin, lowBound, highBound) { LabelType = labelType, }, self);
        }

        public static ReactiveAxisManager<T> ToReactiveContinuousAxisManager<T> (this IObservable<AxisRange> self, AxisRange bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new ContinuousAxisManager<T>(new AxisRange(0, 1), bounds) { LabelType = labelType, }, self);
        }

        public static ReactiveAxisManager<T> ToReactiveContinuousAxisManager<T> (this IObservable<AxisRange> self, IChartMargin margin, AxisRange bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new ContinuousAxisManager<T>(new AxisRange(0, 1), margin, bounds) { LabelType = labelType, }, self);
        }

        public static ReactiveAxisManager<T> ToReactiveContinuousAxisManager<T> (this IObservable<(T, T)> self, AxisRange bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new ContinuousAxisManager<T>(new AxisRange(0, 1), bounds) { LabelType = labelType, }, self);
        }

        public static ReactiveAxisManager<T> ToReactiveContinuousAxisManager<T> (this IObservable<(T, T)> self, IChartMargin margin, AxisRange bounds = null, LabelType labelType = LabelType.Standard)
            where T : IConvertible {
            return new ReactiveAxisManager<T>(new ContinuousAxisManager<T>(new AxisRange(0, 1), margin, bounds) { LabelType = labelType, }, self);
        }
    }
}
